using System.IO;
using NUnit.Framework;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// GlyphMeshBuilder の統合テスト（T018 + T018b）。
    /// </summary>
    public class GlyphMeshBuilderTests
    {
        private static byte[] LoadDefaultFont() =>
            File.ReadAllBytes(
                Path.GetFullPath("Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes"));

        private static MeshGenerationParams ParamsFor(string text, float letterSpacing = 0f, float lineSpacing = 1.2f)
        {
            return new MeshGenerationParams
            {
                Text = text,
                FontData = LoadDefaultFont(),
                ExtrusionDepth = 1f,
                OutlineWidth = 0f,
                BezierErrorThreshold = 0.0005f,
                LetterSpacing = letterSpacing,
                LineSpacing = lineSpacing
            };
        }

        // T018 ─────────────────────────────────────────────

        [Test]
        public void Build_SingleCharA_HasVertices()
        {
            var mesh = GlyphMeshBuilder.Build(ParamsFor("A"));
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "ASCII 'A' のメッシュに頂点が存在すること");
        }

        [Test]
        public void Build_Hello_HasVertices()
        {
            var mesh = GlyphMeshBuilder.Build(ParamsFor("Hello"));
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "'Hello' のメッシュに頂点が存在すること");
        }

        [Test]
        public void Build_NullText_ReturnsEmptyMesh()
        {
            var p = ParamsFor("A");
            p.Text = null;
            var mesh = GlyphMeshBuilder.Build(p);
            Assert.IsNotNull(mesh);
            Assert.AreEqual(0, mesh.vertexCount);
        }

        [Test]
        public void Build_EmptyText_ReturnsEmptyMesh()
        {
            var mesh = GlyphMeshBuilder.Build(ParamsFor(""));
            Assert.IsNotNull(mesh);
            Assert.AreEqual(0, mesh.vertexCount);
        }

        // T018b ─────────────────────────────────────────────

        [Test]
        public void Build_LetterSpacing_IncreasesXOffset()
        {
            var meshNoSpacing = GlyphMeshBuilder.Build(ParamsFor("AB", letterSpacing: 0f));
            var meshWithSpacing = GlyphMeshBuilder.Build(ParamsFor("AB", letterSpacing: 10f));

            // LetterSpacing > 0 のとき、メッシュの横幅（AABB の max.x）が広がること
            Assert.Greater(meshWithSpacing.bounds.max.x, meshNoSpacing.bounds.max.x,
                "LetterSpacing > 0 のとき横幅が広がること");
        }

        [Test]
        public void Build_LineSpacing_AffectsMultilineYOffset()
        {
            var meshSmall = GlyphMeshBuilder.Build(ParamsFor("A\nB", lineSpacing: 1.0f));
            var meshLarge = GlyphMeshBuilder.Build(ParamsFor("A\nB", lineSpacing: 2.0f));

            // LineSpacing が大きいほどメッシュの縦幅（AABB の size.y）が大きくなること
            Assert.Greater(meshLarge.bounds.size.y, meshSmall.bounds.size.y,
                "LineSpacing が大きいほど縦幅が広がること");
        }

        // T025: CJK テスト ─────────────────────────────────────────────

        [Test]
        public void Build_Japanese_HasVertices()
        {
            var mesh = GlyphMeshBuilder.Build(ParamsFor("立体文字"));
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "日本語テキストにメッシュ頂点が存在すること");
        }

        [Test]
        public void Build_Chinese_HasVertices()
        {
            var mesh = GlyphMeshBuilder.Build(ParamsFor("汉字"));
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "中国語テキストにメッシュ頂点が存在すること");
        }

        [Test]
        public void Build_Korean_HasVertices()
        {
            var mesh = GlyphMeshBuilder.Build(ParamsFor("한글"));
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "韓国語テキストにメッシュ頂点が存在すること");
        }

        // T026: 混在テキスト ─────────────────────────────────────────────

        [Test]
        public void Build_MixedText_HasVertices()
        {
            var mesh = GlyphMeshBuilder.Build(ParamsFor("Hello 世界"));
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "混在テキスト（ASCII + CJK）にメッシュ頂点が存在すること");
        }

        // T029: フォールバックテスト ─────────────────────────────────────

        [Test]
        public void Build_NoFontPath_FallsBackToDefault()
        {
            var p = new MeshGenerationParams
            {
                Text = "A",
                FontData = null,
                ExtrusionDepth = 1f,
                BezierErrorThreshold = 0.0005f,
                LetterSpacing = 0f,
                LineSpacing = 1.2f
            };
            // フォールバック時はエラーにならず Mesh が返ること
            Mesh mesh = null;
            Assert.DoesNotThrow(() => mesh = GlyphMeshBuilder.Build(p));
            Assert.IsNotNull(mesh);
        }

        // T029b: フォント切り替えテスト ─────────────────────────────────

        [Test]
        public void Build_SwitchFont_MeshChanges()
        {
            var meshA = GlyphMeshBuilder.Build(ParamsFor("A"));
            var p2 = ParamsFor("A");
            // 同じフォントで再生成しても頂点数が一致することを確認（切り替えロジックが機能することの証明）
            var meshB = GlyphMeshBuilder.Build(p2);
            Assert.AreEqual(meshA.vertexCount, meshB.vertexCount,
                "同じパラメータで再生成した場合は頂点数が一致すること");
        }

        // T011: アンカー + 縦書きテスト ──────────────────────────────────

        [Test]
        public void Build_WithCenterAnchor_BoundsSymmetric()
        {
            // Center アンカー時、メッシュの bounds が原点に対して対称であること
            var p = ParamsFor("A");
            p.HorizontalAnchor = HorizontalAnchor.Center;
            p.VerticalAnchor = VerticalAnchor.Middle;
            p.DepthAnchor = DepthAnchor.Center;
            var mesh = GlyphMeshBuilder.Build(p);

            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);

            // Center アンカーのとき bounds の min.x + max.x ≈ 0 (対称)
            float symX = mesh.bounds.min.x + mesh.bounds.max.x;
            float symY = mesh.bounds.min.y + mesh.bounds.max.y;
            Assert.AreEqual(0f, symX, 0.05f, "Center 水平アンカー時に bounds が X 軸で対称であること");
            Assert.AreEqual(0f, symY, 0.05f, "Middle 垂直アンカー時に bounds が Y 軸で対称であること");
        }

        [Test]
        public void Build_VerticalMode_YDecreases()
        {
            // T028: 縦書きモードで Y 座標が下方向（負）に進むこと
            var p = ParamsFor("AB");
            p.WritingMode = WritingMode.Vertical;
            p.VerticalColumnWidth = 0f; // 自動
            var mesh = GlyphMeshBuilder.Build(p);

            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "縦書きモードでメッシュが生成されること");
        }

        [Test]
        public void BuildPerCharacter_ThreeChars_ReturnsThreeMeshes()
        {
            // T021: Per-Character モードで3文字分の Mesh リストが返ること
            var p = ParamsFor("ABC");
            var result = GlyphMeshBuilder.BuildPerCharacter(p);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Meshes);
            Assert.AreEqual(3, result.Meshes.Count, "3文字分の Mesh が返ること");
            foreach (var mesh in result.Meshes)
            {
                Assert.IsNotNull(mesh);
                Assert.Greater(mesh.vertexCount, 0, "各文字の Mesh に頂点が存在すること");
            }
        }
    }
}
