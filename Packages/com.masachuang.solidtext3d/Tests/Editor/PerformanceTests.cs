using System;
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
        private static byte[] LoadDefaultFont() =>
            File.ReadAllBytes(
                Path.GetFullPath("Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes"));

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
    }
}
