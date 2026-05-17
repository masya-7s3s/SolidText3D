using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using MasaChuang.SolidText3D;
using UnityEngine;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// SC-001 パフォーマンスベンチマークテスト（T038）。
    /// </summary>
    public class PerformanceTests
    {
        private const int DeferredSubmitSamples = 600;
        private const double DeferredSubmitMaxMilliseconds95 = 50d;
        private const double CacheDriftToleranceRatio = 1.10d;

        private static byte[] LoadDefaultFont() =>
            File.ReadAllBytes(
                Path.GetFullPath("Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes"));

        private static double GetPercentileMilliseconds(List<double> samples, double percentile)
        {
            Assert.IsNotEmpty(samples, "percentile 計算対象の sample が必要です。");
            samples.Sort();
            int index = Mathf.Clamp((int)Math.Ceiling(samples.Count * percentile) - 1, 0, samples.Count - 1);
            return samples[index];
        }

        private static double GetMedianMilliseconds(List<double> samples)
        {
            return GetPercentileMilliseconds(samples, 0.5d);
        }

        private static Action CreateLateUpdateInvoker(SolidText3DComponent component)
        {
            var method = typeof(SolidText3DComponent).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "SolidText3DComponent.LateUpdate が必要です。");
            return (Action)Delegate.CreateDelegate(typeof(Action), component, method);
        }

        [Test]
        public void Build_50Chars_Under2000ms()
        {
            // 50 文字のテキストを準備
            const string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwx";
            Assert.AreEqual(50, text.Length);

            var p = new MeshGenerationParams
            {
                Text = text,
                FontData = LoadDefaultFont(),
                ExtrusionDepth = 1f,
                OutlineWidth = 0f,
                BezierErrorThreshold = 0.0005f,
                LetterSpacing = 0f,
                LineSpacing = 1.2f
            };

            // ウォームアップ（JIT コスト含む 1 回目は計測対象外）
            GlyphMeshBuilder.Build(p);

            // 2 回目以降を計測（SC-001: 2000ms 未満）
            var sw = Stopwatch.StartNew();
            GlyphMeshBuilder.Build(p);
            sw.Stop();

            UnityEngine.Debug.Log($"[PerformanceTests] 50文字メッシュ生成: {sw.ElapsedMilliseconds}ms");

            Assert.Less(sw.ElapsedMilliseconds, 2000,
                $"50文字のメッシュ生成が {sw.ElapsedMilliseconds}ms かかりました（上限: 2000ms）");
        }

        [Test]
        public void OutlineEnabled_CleanLateUpdate_AllocatesZeroBytes()
        {
            var go = new GameObject("OutlinePerfTest");
            try
            {
                var component = go.AddComponent<SolidText3DComponent>();
                component.Text = "Outline";
                component.OutlineEnabled = true;
                component.OutlineOffset = 0.05f;
                component.OutlineThickness = 0.1f;
                component.RegenerateMesh();

                Assert.IsFalse(component.IsDirty, "初回再生成後は dirty がクリアされていること");

                var lateUpdate = CreateLateUpdateInvoker(component);
                lateUpdate();

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 16; i++)
                    lateUpdate();
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

                Assert.AreEqual(0L, allocatedBytes,
                    $"clean frame の LateUpdate で追加 GC.Alloc が発生しました: {allocatedBytes} bytes");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RequestRegenerateMesh_SubmitP95_Under50Milliseconds()
        {
            var go = new GameObject("DeferredSubmitPerfTest");
            try
            {
                var component = go.AddComponent<SolidText3DComponent>();
                component.Text = "0000";

                var samples = new List<double>(DeferredSubmitSamples);
                var stopwatch = new Stopwatch();
                for (int index = 0; index < DeferredSubmitSamples; index++)
                {
                    component.Text = index.ToString("D4");
                    stopwatch.Restart();
                    component.RequestRegenerateMesh();
                    stopwatch.Stop();
                    samples.Add(stopwatch.Elapsed.TotalMilliseconds);
                }

                double p95 = GetPercentileMilliseconds(samples, 0.95d);
                UnityEngine.Debug.Log($"[PerformanceTests] RequestRegenerateMesh submit p95: {p95:F3}ms / samples={DeferredSubmitSamples}");

                Assert.LessOrEqual(p95, DeferredSubmitMaxMilliseconds95,
                    $"RequestRegenerateMesh submit の 95 percentile が {p95:F3}ms でした（上限: {DeferredSubmitMaxMilliseconds95}ms）");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RequestRegenerateMesh_WhileInFlight_AllocatesZeroBytesOnSubmit()
        {
            var go = new GameObject("DeferredSubmitAllocTest");
            try
            {
                var component = go.AddComponent<SolidText3DComponent>();
                var pendingTexts = new[]
                {
                    "0001", "0002", "0003", "0004",
                    "0005", "0006", "0007", "0008"
                };

                component.Text = new string('W', 128);
                component.RequestRegenerateMesh();

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int index = 0; index < pendingTexts.Length; index++)
                {
                    component.Text = pendingTexts[index];
                    component.RequestRegenerateMesh();
                }

                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0L, allocatedBytes,
                    $"in-flight 中の RequestRegenerateMesh submit で追加 GC.Alloc が発生しました: {allocatedBytes} bytes");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RegenerateMesh_CacheHit_IsFasterThanColdBuild()
        {
            var go = new GameObject("PreparedResultCacheHitPerf");
            try
            {
                var component = go.AddComponent<SolidText3DComponent>();
                var stopwatch = new Stopwatch();

                component.Text = "CACHE-A";
                stopwatch.Start();
                component.RegenerateMesh();
                stopwatch.Stop();
                double coldMs = stopwatch.Elapsed.TotalMilliseconds;

                component.Text = "CACHE-B";
                component.RegenerateMesh();

                stopwatch.Restart();
                component.Text = "CACHE-A";
                component.RegenerateMesh();
                stopwatch.Stop();
                double cacheHitMs = stopwatch.Elapsed.TotalMilliseconds;

                Assert.LessOrEqual(cacheHitMs, coldMs,
                    $"cache hit の再表示時間 {cacheHitMs:F3}ms が cold build {coldMs:F3}ms を上回りました");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RegenerateMesh_CacheHitMedian_DoesNotDriftMoreThan10PercentAfterLongRun()
        {
            var go = new GameObject("PreparedResultCacheDriftPerf");
            try
            {
                var component = go.AddComponent<SolidText3DComponent>();
                var samples = new List<double>(240);
                var stopwatch = new Stopwatch();

                component.Text = "CACHE-A";
                component.RegenerateMesh();
                component.Text = "CACHE-B";
                component.RegenerateMesh();

                for (int iteration = 0; iteration < 240; iteration++)
                {
                    component.Text = iteration % 2 == 0 ? "CACHE-A" : "CACHE-B";
                    stopwatch.Restart();
                    component.RegenerateMesh();
                    stopwatch.Stop();
                    samples.Add(stopwatch.Elapsed.TotalMilliseconds);
                }

                var earlySamples = samples.GetRange(0, 40);
                var lateSamples = samples.GetRange(samples.Count - 40, 40);
                double earlyMedian = GetMedianMilliseconds(earlySamples);
                double lateMedian = GetMedianMilliseconds(lateSamples);

                Assert.LessOrEqual(lateMedian, earlyMedian * CacheDriftToleranceRatio,
                    $"長時間運用後の cache-hit median {lateMedian:F3}ms が初期 median {earlyMedian:F3}ms から 10% 超劣化しました");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
