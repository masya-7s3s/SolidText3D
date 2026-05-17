using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MasaChuang.SolidText3D.Tests.Runtime
{
    public class DeferredRegenerationRuntimeTests
    {
        private const float HeavyLightLatencyLimitSeconds = 0.1f;
        private const float FiveObjectLatencyLimitSeconds = 0.1f;

        private static readonly FieldInfo DeferredStateField = typeof(SolidText3DComponent)
            .GetField("_deferredRegenerationState", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo FontAssetField = typeof(SolidText3DComponent)
            .GetField("_fontAsset", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo FontBytesCacheField = typeof(SolidText3DComponent)
            .GetField("_fontBytesCache", BindingFlags.NonPublic | BindingFlags.Instance);

        private static DeferredRegenerationState GetDeferredState(SolidText3DComponent component)
        {
            Assert.IsNotNull(DeferredStateField, "deferred regeneration state field が必要です。");
            return (DeferredRegenerationState)DeferredStateField.GetValue(component);
        }

        private static void ForceFontUnavailable(SolidText3DComponent component)
        {
            Assert.IsNotNull(FontAssetField);
            Assert.IsNotNull(FontBytesCacheField);
            FontAssetField.SetValue(component, null);
            FontBytesCacheField.SetValue(component, null);
        }

        private static IEnumerator WaitForDeferredToSettle(SolidText3DComponent component, int maxFrames = 240)
        {
            var state = GetDeferredState(component);
            float deadline = Time.realtimeSinceStartup + 10f;
            int frame = 0;

            while (component.HasPendingRegeneration && Time.realtimeSinceStartup < deadline)
            {
                frame++;
                yield return null;
            }

            Assert.IsFalse(component.HasPendingRegeneration,
                $"deferred regeneration が制限時間内に収束すること (frames={frame}, lastRequested={state.LastRequestedVersion}, lastApplied={state.LastAppliedVersion}, inFlight={state.InFlightRequest.HasValue}, pending={state.PendingLatestRequest.HasValue}, ready={state.ReadyResult != null})");
        }

        [UnityTest]
        public IEnumerator TimerDisplay_10HzFor60SecondsEquivalent_NeverRollsBackFromLatestRequest()
        {
            const int totalUpdates = 600;
            const int framesPerUpdate = 6;

            var gameObject = new GameObject("DeferredTimerRuntimeTest");
            var component = gameObject.AddComponent<SolidText3DComponent>();
            var state = GetDeferredState(component);
            int highestAppliedValue = -1;

            for (int updateIndex = 0; updateIndex < totalUpdates; updateIndex++)
            {
                string displayText = updateIndex.ToString("D4");
                component.Text = displayText;
                component.RequestRegenerateMesh();

                for (int frame = 0; frame < framesPerUpdate; frame++)
                {
                    yield return null;

                    if (string.IsNullOrEmpty(state.LastAppliedSignature.Text))
                        continue;

                    int appliedValue = int.Parse(state.LastAppliedSignature.Text);
                    Assert.GreaterOrEqual(appliedValue, highestAppliedValue,
                        "deferred regeneration 中に表示が過去の request へ巻き戻らないこと");
                    highestAppliedValue = appliedValue;
                }
            }

            yield return WaitForDeferredToSettle(component);
            Assert.AreEqual("0599", state.LastAppliedSignature.Text,
                "観測完了時点で最後の request が表示へ反映されていること");

#if UNITY_EDITOR
            Object.DestroyImmediate(gameObject);
#else
            Object.Destroy(gameObject);
#endif
        }

        [UnityTest]
        public IEnumerator HeavyAndLightObjects_UpdatesReach90PercentWithin100Milliseconds()
        {
            var heavyGo = new GameObject("HeavyDeferredObject");
            var lightGo = new GameObject("LightDeferredObject");
            var heavy = heavyGo.AddComponent<SolidText3DComponent>();
            var light = lightGo.AddComponent<SolidText3DComponent>();

            heavy.Text = new string('W', 48);
            light.Text = "0000";

            var lightLatencies = new List<float>(50);
            for (int index = 0; index < 50; index++)
            {
                heavy.Text = new string((char)('A' + (index % 26)), 48);
                light.Text = index.ToString("D4");

                heavy.RequestRegenerateMesh();
                light.RequestRegenerateMesh();

                float startedAt = Time.realtimeSinceStartup;
                while (GetDeferredState(light).LastAppliedSignature.Text != light.Text && Time.realtimeSinceStartup - startedAt < 2f)
                    yield return null;

                lightLatencies.Add(Time.realtimeSinceStartup - startedAt);
            }

            lightLatencies.Sort();
            float p90 = lightLatencies[Mathf.Clamp(Mathf.CeilToInt(lightLatencies.Count * 0.9f) - 1, 0, lightLatencies.Count - 1)];

#if UNITY_EDITOR
            Object.DestroyImmediate(heavyGo);
            Object.DestroyImmediate(lightGo);
#else
            Object.Destroy(heavyGo);
            Object.Destroy(lightGo);
#endif

            Assert.LessOrEqual(p90, HeavyLightLatencyLimitSeconds,
                $"heavy/light 同時更新の 90 percentile が {p90 * 1000f:F1}ms でした（上限: {HeavyLightLatencyLimitSeconds * 1000f:F1}ms）");
        }

        [UnityTest]
        public IEnumerator FiveObjects_UpdatesReach90PercentWithin100Milliseconds()
        {
            const int objectCount = 5;
            const int measuredIterations = 20;
            var objects = new GameObject[objectCount];
            var components = new SolidText3DComponent[objectCount];
            var latencies = new List<float>(objectCount * measuredIterations);

            for (int index = 0; index < objectCount; index++)
            {
                objects[index] = new GameObject($"DeferredObject_{index}");
                components[index] = objects[index].AddComponent<SolidText3DComponent>();
                components[index].Text = $"{index:00}:00";
                components[index].RegenerateMesh();
            }

            yield return null;

            for (int iteration = 1; iteration <= measuredIterations; iteration++)
            {
                float startedAt = Time.realtimeSinceStartup;
                var applied = new bool[objectCount];
                for (int index = 0; index < objectCount; index++)
                {
                    components[index].Text = $"{index:00}:{iteration:00}";
                    components[index].RequestRegenerateMesh();
                }

                int appliedCount = 0;
                while (appliedCount < objectCount && Time.realtimeSinceStartup - startedAt < 2f)
                {
                    for (int index = 0; index < objectCount; index++)
                    {
                        if (applied[index])
                            continue;

                        if (GetDeferredState(components[index]).LastAppliedSignature.Text != components[index].Text)
                            continue;

                        applied[index] = true;
                        appliedCount++;
                        latencies.Add(Time.realtimeSinceStartup - startedAt);
                    }

                    if (appliedCount < objectCount)
                        yield return null;
                }

                for (int index = 0; index < objectCount; index++)
                {
                    if (!applied[index])
                        latencies.Add(2f);
                }
            }

            latencies.Sort();
            float p90 = latencies[Mathf.Clamp(Mathf.CeilToInt(latencies.Count * 0.9f) - 1, 0, latencies.Count - 1)];

            for (int index = 0; index < objectCount; index++)
            {
#if UNITY_EDITOR
                Object.DestroyImmediate(objects[index]);
#else
                Object.Destroy(objects[index]);
#endif
            }

            Assert.LessOrEqual(p90, FiveObjectLatencyLimitSeconds,
                $"5 object 同時更新の 90 percentile が {p90 * 1000f:F1}ms でした（上限: {FiveObjectLatencyLimitSeconds * 1000f:F1}ms）");
        }

        [UnityTest]
        public IEnumerator StaleReadyResult_IsDiscardedWithoutRollback()
        {
            var gameObject = new GameObject("RuntimeDeferredStaleDiscard");
            var component = gameObject.AddComponent<SolidText3DComponent>();
            component.Text = "LIVE";
            component.RegenerateMesh();

            var state = GetDeferredState(component);
            var lastGoodSignature = state.LastAppliedSignature;
            state.LastRequestedVersion = state.LastAppliedVersion + 1;
            state.ReadyResult = new PreparedDisplayResult
            {
                Version = state.LastAppliedVersion,
                Signature = new DisplayResultSignature("STALE", 10, 10, 1, ObjectMode.SingleObject)
            };

            yield return null;

            Assert.AreEqual(lastGoodSignature, state.LastAppliedSignature,
                "stale ready result を捨てても visible display は巻き戻らないこと");
            Assert.IsNull(state.ReadyResult, "stale ready result は次 frame で破棄されること");

#if UNITY_EDITOR
            Object.DestroyImmediate(gameObject);
#else
            Object.Destroy(gameObject);
#endif
        }

        [UnityTest]
        public IEnumerator RequestRegenerateMesh_FontFailure_KeepsLastGoodDisplay()
        {
            var gameObject = new GameObject("RuntimeDeferredFailureKeepLastGood");
            var component = gameObject.AddComponent<SolidText3DComponent>();
            component.Text = "GOOD";
            component.RegenerateMesh();

            var state = GetDeferredState(component);
            var lastGoodSignature = state.LastAppliedSignature;
            int failureCount = 0;
            component.DeferredRegenerationFailed += _ => failureCount++;

            ForceFontUnavailable(component);
            component.Text = "BROKEN";
            component.RequestRegenerateMesh();
            yield return null;

            Assert.AreEqual(1, failureCount, "runtime の capture failure で failure event を 1 回通知すること");
            Assert.AreEqual(lastGoodSignature, state.LastAppliedSignature,
                "runtime の deferred failure でも visible display は keep-last-good を維持すること");

#if UNITY_EDITOR
            Object.DestroyImmediate(gameObject);
#else
            Object.Destroy(gameObject);
#endif
        }
    }
}