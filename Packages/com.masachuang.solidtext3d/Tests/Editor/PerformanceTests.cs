using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// SC-001 パフォーマンスベンチマークテスト（T038）。
    /// </summary>
    public class PerformanceTests
    {
        private static string FontPath =>
            Path.GetFullPath("Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes");

        [Test]
        public void Build_50Chars_Under2000ms()
        {
            // 50 文字のテキストを準備
            const string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwx";
            Assert.AreEqual(50, text.Length);

            var p = new MeshGenerationParams
            {
                Text = text,
                FontPath = FontPath,
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
    }
}
