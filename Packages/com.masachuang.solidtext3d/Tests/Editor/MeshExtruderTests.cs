using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// MeshExtruder の単体テスト。
    /// </summary>
    public class MeshExtruderTests
    {
        // 正方形の輪郭を作成するヘルパー
        private static GlyphContour MakeSquareContour(float size = 1f)
        {
            var contour = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(size, 0f),
                new Vector2(size, size),
                new Vector2(0f, size),
            };
            var glyphContour = new GlyphContour
            {
                Contours = new List<List<Vector2>> { contour },
                AdvanceWidth = size,
                Bounds = new Rect(0, 0, size, size)
            };
            return glyphContour;
        }

        // 外側の正方形と内側の穴（小さな正方形）を持つ輪郭（ドーナツ形状）
        private static GlyphContour MakeDonutContour()
        {
            var outer = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(2f, 0f),
                new Vector2(2f, 2f),
                new Vector2(0f, 2f),
            };
            // NonZero WindingRule で穴を生成するには内側コンターを CW（時計回り）にする必要がある
            // outer: CCW（反時計回り、winding=+1）、inner: CW（時計回り、winding=-1）→ 穴の内部は winding=0
            var inner = new List<Vector2>
            {
                new Vector2(0.5f, 1.5f),
                new Vector2(1.5f, 1.5f),
                new Vector2(1.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
            };
            return new GlyphContour
            {
                Contours = new List<List<Vector2>> { outer, inner },
                AdvanceWidth = 2f,
                Bounds = new Rect(0, 0, 2, 2)
            };
        }

        private static MeshGenerationParams DefaultParams(float depth = 1f)
        {
            return new MeshGenerationParams
            {
                Text = "A",
                ExtrusionDepth = depth,
                OutlineWidth = 0f,
                BezierErrorThreshold = 0.0005f,
                LetterSpacing = 0f,
                LineSpacing = 1.2f
            };
        }

        [Test]
        public void BuildGlyphMesh_SquareContour_HasVertices()
        {
            var contour = MakeSquareContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);

            Assert.IsNotNull(data.Vertices);
            Assert.Greater(data.Vertices.Count, 0, "頂点数は 0 より大きいこと");
        }

        [Test]
        public void BuildGlyphMesh_SquareContour_HasTriangles()
        {
            var contour = MakeSquareContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);

            Assert.IsNotNull(data.Triangles);
            Assert.Greater(data.Triangles.Count, 0, "インデックス数は 0 より大きいこと");
            Assert.AreEqual(0, data.Triangles.Count % 3, "インデックス数は 3 の倍数であること");
        }

        [Test]
        public void BuildGlyphMesh_SquareContour_HasNormals()
        {
            var contour = MakeSquareContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);

            Assert.IsNotNull(data.Normals);
            Assert.AreEqual(data.Vertices.Count, data.Normals.Count, "法線数は頂点数と一致すること");
        }

        [Test]
        public void BuildGlyphMesh_ZeroDepth_FlatMesh()
        {
            var contour = MakeSquareContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 0f, 0f);

            Assert.Greater(data.Vertices.Count, 0, "押し出し深さ 0 でも頂点が存在すること");
            // 全頂点の Z 値が 0 であること
            foreach (var v in data.Vertices)
            {
                Assert.AreEqual(0f, v.z, 0.0001f, "押し出し深さ 0 では全頂点の Z が 0 であること");
            }
        }

        [Test]
        public void BuildGlyphMesh_WithDepth_FrontAndBackFaces()
        {
            var contour = MakeSquareContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);

            bool hasFront = false;
            bool hasBack = false;
            foreach (var v in data.Vertices)
            {
                if (Mathf.Approximately(v.z, 0f)) hasFront = true;
                if (Mathf.Approximately(v.z, -1f)) hasBack = true;
            }
            Assert.IsTrue(hasFront, "前面頂点（z=0）が存在すること");
            Assert.IsTrue(hasBack, "背面頂点（z=-depth）が存在すること");
        }

        [Test]
        public void BuildGlyphMesh_DonutContour_HoleHandled()
        {
            var contour = MakeDonutContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);

            // 穴あり形状でも頂点・インデックスが生成されること
            Assert.Greater(data.Vertices.Count, 0, "ドーナツ形状で頂点が生成されること");
            Assert.Greater(data.Triangles.Count, 0, "ドーナツ形状でインデックスが生成されること");
        }

        [Test]
        public void Build_SingleGlyph_ReturnsMesh()
        {
            var contour = MakeSquareContour();
            var glyphs = new List<GlyphContour> { contour };
            var p = DefaultParams(1f);

            var mesh = MeshExtruder.Build(glyphs, p);

            Assert.IsNotNull(mesh, "Mesh が null でないこと");
            Assert.Greater(mesh.vertexCount, 0, "頂点数が 0 より大きいこと");
        }

        [Test]
        public void Build_MultipleGlyphs_ReturnsCombinedMesh()
        {
            var glyphs = new List<GlyphContour>
            {
                MakeSquareContour(),
                MakeSquareContour(),
            };
            var p = DefaultParams(1f);

            var mesh = MeshExtruder.Build(glyphs, p);

            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);
        }

        [Test]
        public void Build_EmptyGlyphList_ReturnsEmptyMesh()
        {
            var mesh = MeshExtruder.Build(new List<GlyphContour>(), DefaultParams());
            Assert.IsNotNull(mesh);
            Assert.AreEqual(0, mesh.vertexCount);
        }

        // T028: CJK 穴ありグリフの EvenOdd テスト ─────────────────────────

        [Test]
        public void BuildGlyphMesh_DonutContour_EvenOddHoleCorrect()
        {
            // 「口」「O」相当の外枠 + 内側の穴を持つ形状
            // depth=0 で前面のみ生成（側面なし）し、面積で EvenOdd 穴を検証する
            var contour = MakeDonutContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 0f, 0f);

            Assert.Greater(data.Vertices.Count, 0, "穴あり形状で頂点が生成されること");
            Assert.Greater(data.Triangles.Count, 0, "穴あり形状でインデックスが生成されること");

            // 前面三角形の総面積を計算
            float frontArea = 0f;
            for (int i = 0; i < data.Triangles.Count; i += 3)
            {
                var v0 = data.Vertices[data.Triangles[i + 0]];
                var v1 = data.Vertices[data.Triangles[i + 1]];
                var v2 = data.Vertices[data.Triangles[i + 2]];
                // 2D 三角形面積 = 0.5 × |外積|
                float area = Mathf.Abs(
                    (v1.x - v0.x) * (v2.y - v0.y) -
                    (v2.x - v0.x) * (v1.y - v0.y)) * 0.5f;
                frontArea += area;
            }

            // 外側正方形面積 = 2×2 = 4
            // 内側穴面積    = 1×1 = 1
            // EvenOdd リング面積 = 4 - 1 = 3 < 4
            const float outerArea = 4f;
            Assert.Less(frontArea, outerArea,
                $"EvenOdd WindingRule で穴がくり抜かれ、前面面積（{frontArea:F3}）が外側正方形面積（{outerArea}）より小さいこと");
        }
    }
}
