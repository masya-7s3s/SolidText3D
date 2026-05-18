// テスト実行環境: Intel Core i5 第10世代相当または Apple M1 以上
// それ以下のスペックでは [Ignore] 属性でスキップし警告ログを出力する
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Runtime
{
    /// <summary>
    /// SC-004 ランタイムパフォーマンスベンチマークテスト（T039）。
    /// 20 個の SolidText3DComponent（各 50 文字）を同一シーンに配置し、
    /// 30 フレームの平均フレーム時間が 16.7ms 未満（60fps 相当）であることを検証する。
    /// </summary>
    public class PerformanceRuntimeTests
    {
        private const int DeferredSubmitSamples = 600;
        private const float SubmitP95LimitMs = 50f;
        private const float CacheDriftToleranceRatio = 1.10f;

        private static byte[] LoadDefaultFont() =>
            File.ReadAllBytes(
                Path.GetFullPath("Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes"));

        private static float GetPercentileMilliseconds(List<float> samples, float percentile)
        {
            Assert.IsNotEmpty(samples, "percentile 計算対象の sample が必要です。");
            samples.Sort();
            int index = Mathf.Clamp(Mathf.CeilToInt(samples.Count * percentile) - 1, 0, samples.Count - 1);
            return samples[index];
        }

        private static float GetMedianMilliseconds(List<float> samples)
        {
            return GetPercentileMilliseconds(samples, 0.5f);
        }

        private const int ComponentCount = 20;
        private const int MeasureFrames = 30;
        private const float MaxAverageDeltaMs = 16.7f; // 60fps

        [UnityTest]
        public IEnumerator TwentyComponents_60FPS_Average()
        {
            // 最低動作スペック以下の環境ではスキップ
            if (SystemInfo.processorFrequency < 2000)
            {
                Assert.Ignore("最低動作保証スペック（2GHz 以上の CPU）以下の環境のためテストをスキップします。");
                yield break;
            }

            var objects = new GameObject[ComponentCount];
            const string text50 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwx";
            byte[] fontData = LoadDefaultFont();

            // 20 個の SolidText3DComponent を生成
            for (int i = 0; i < ComponentCount; i++)
            {
                objects[i] = new GameObject($"PerfTest_{i}");
                var comp = objects[i].AddComponent<SolidText3DComponent>();
                comp.Text = text50;
            }

            // 最初の数フレームはウォームアップ
            for (int i = 0; i < 5; i++)
                yield return null;

            // 30 フレーム計測
            float totalDelta = 0f;
            for (int frame = 0; frame < MeasureFrames; frame++)
            {
                yield return null;
                totalDelta += Time.deltaTime;
            }

            float averageDeltaMs = (totalDelta / MeasureFrames) * 1000f;
            Debug.Log($"[PerformanceRuntimeTests] 平均フレーム時間: {averageDeltaMs:F2}ms ({1000f / averageDeltaMs:F1}fps) / 20コンポーネント / {MeasureFrames}フレーム平均");

            // クリーンアップ
            for (int i = 0; i < ComponentCount; i++)
            {
                if (objects[i] != null)
                {
#if UNITY_EDITOR
                    Object.DestroyImmediate(objects[i]);
#else
                    Object.Destroy(objects[i]);
#endif
                }
            }

            Assert.Less(averageDeltaMs, MaxAverageDeltaMs,
                $"平均フレーム時間が {averageDeltaMs:F2}ms でした（上限: {MaxAverageDeltaMs}ms）");
        }

        [UnityTest]
        public IEnumerator RequestRegenerateMesh_10HzObservation_P95Under50Milliseconds()
        {
            var go = new GameObject("RuntimeDeferredSubmitPerf");
            var component = go.AddComponent<SolidText3DComponent>();
            var samples = new List<float>(DeferredSubmitSamples);

            for (int index = 0; index < DeferredSubmitSamples; index++)
            {
                component.Text = index.ToString("D4");
                float before = Time.realtimeSinceStartup;
                component.RequestRegenerateMesh();
                float elapsedMs = (Time.realtimeSinceStartup - before) * 1000f;
                samples.Add(elapsedMs);

                if ((index + 1) % 10 == 0)
                    yield return null;
            }

            float p95 = GetPercentileMilliseconds(samples, 0.95f);
            Debug.Log($"[PerformanceRuntimeTests] RequestRegenerateMesh submit p95: {p95:F3}ms / samples={DeferredSubmitSamples}");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif

            Assert.LessOrEqual(p95, SubmitP95LimitMs,
                $"RequestRegenerateMesh submit の 95 percentile が {p95:F3}ms でした（上限: {SubmitP95LimitMs}ms）");
        }

        [UnityTest]
        public IEnumerator RegenerateMesh_CacheHitMedian_DoesNotDriftMoreThan10PercentAfterLongRun()
        {
            var go = new GameObject("RuntimePreparedResultCacheDrift");
            var component = go.AddComponent<SolidText3DComponent>();
            var samples = new List<float>(240);

            component.Text = "CACHE-A";
            component.RegenerateMesh();
            component.Text = "CACHE-B";
            component.RegenerateMesh();

            for (int iteration = 0; iteration < 240; iteration++)
            {
                component.Text = iteration % 2 == 0 ? "CACHE-A" : "CACHE-B";
                float before = Time.realtimeSinceStartup;
                component.RegenerateMesh();
                samples.Add((Time.realtimeSinceStartup - before) * 1000f);

                if ((iteration + 1) % 30 == 0)
                    yield return null;
            }

            var earlySamples = samples.GetRange(0, 40);
            var lateSamples = samples.GetRange(samples.Count - 40, 40);
            float earlyMedian = GetMedianMilliseconds(earlySamples);
            float lateMedian = GetMedianMilliseconds(lateSamples);

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif

            Assert.LessOrEqual(lateMedian, earlyMedian * CacheDriftToleranceRatio,
                $"長時間運用後の runtime cache-hit median {lateMedian:F3}ms が初期 median {earlyMedian:F3}ms から 10% 超劣化しました");
        }
    }
}
