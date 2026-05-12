using System.Collections.Generic;
using System.Reflection;
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

        private static GlyphContour MakeClockwiseSquareContour(float size = 1f)
        {
            var contour = new List<Vector2>
            {
                new Vector2(0f, size),
                new Vector2(size, size),
                new Vector2(size, 0f),
                new Vector2(0f, 0f),
            };
            return new GlyphContour
            {
                Contours = new List<List<Vector2>> { contour },
                AdvanceWidth = size,
                Bounds = new Rect(0, 0, size, size)
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

        private static MethodInfo RequireSharedContourHelper()
        {
            var method = typeof(MeshExtruder).GetMethod(
                "BuildContourMeshData",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(List<List<Vector2>>), typeof(float), typeof(float), typeof(bool) },
                null);

            Assert.IsNotNull(method,
                "outline が body と同じ cap/side/Z 配置規約を再利用できるよう、MeshExtruder に shared contour helper が必要です。");
            return method;
        }

        private static GlyphMeshData InvokeSharedContourHelper(List<List<Vector2>> contours, float frontZ, float backZ, bool includeBackCap)
        {
            return (GlyphMeshData)RequireSharedContourHelper().Invoke(null, new object[] { contours, frontZ, backZ, includeBackCap });
        }

        private static MethodInfo RequireCapHelper()
        {
            var method = typeof(MeshExtruder).GetMethod(
                "BuildCapMeshData",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(List<List<Vector2>>), typeof(float), typeof(bool) },
                null);

            Assert.IsNotNull(method,
                "BackFilled rear cap も body と同じ cap 規約を再利用できるよう、MeshExtruder に cap helper が必要です。");
            return method;
        }

        private static GlyphMeshData InvokeCapHelper(List<List<Vector2>> contours, float z, bool faceForward)
        {
            return (GlyphMeshData)RequireCapHelper().Invoke(null, new object[] { contours, z, faceForward });
        }

        private static float GetFirstTriangleSignedAreaAtZ(GlyphMeshData data, float targetZ)
        {
            for (int i = 0; i < data.Triangles.Count; i += 3)
            {
                var v0 = data.Vertices[data.Triangles[i + 0]];
                var v1 = data.Vertices[data.Triangles[i + 1]];
                var v2 = data.Vertices[data.Triangles[i + 2]];

                if (!Mathf.Approximately(v0.z, targetZ) || !Mathf.Approximately(v1.z, targetZ) || !Mathf.Approximately(v2.z, targetZ))
                    continue;

                var signedArea = ((v1.x - v0.x) * (v2.y - v0.y)) - ((v2.x - v0.x) * (v1.y - v0.y));
                if (!Mathf.Approximately(signedArea, 0f))
                    return signedArea;
            }

            Assert.Fail($"z={targetZ} の front triangle が見つかりませんでした。");
            return 0f;
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

        [Test]
        public void SharedContourHelper_CustomZPlacement_PreservesFrontWindingParity()
        {
            var contour = MakeDonutContour();
            var baseline = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);
            var helperData = InvokeSharedContourHelper(contour.Contours, 0.25f, -0.75f, true);

            Assert.Greater(helperData.Vertices.Count, 0, "shared helper が outline 用 contour からもメッシュを構築できること");

            bool hasFront = false;
            bool hasBack = false;
            foreach (var vertex in helperData.Vertices)
            {
                if (Mathf.Approximately(vertex.z, 0.25f)) hasFront = true;
                if (Mathf.Approximately(vertex.z, -0.75f)) hasBack = true;
            }

            Assert.IsTrue(hasFront, "shared helper が任意の front Z を維持すること");
            Assert.IsTrue(hasBack, "shared helper が任意の back Z を維持すること");

            var baselineArea = GetFirstTriangleSignedAreaAtZ(baseline, 0f);
            var helperArea = GetFirstTriangleSignedAreaAtZ(helperData, 0.25f);
            Assert.AreEqual(Mathf.Sign(baselineArea), Mathf.Sign(helperArea),
                "body と outline 再利用 helper の front winding parity が一致すること");
        }

        [Test]
        public void SharedContourHelper_CanonicalRingContours_PreserveFrontVisibilityParity()
        {
            var contour = MakeDonutContour();
            var profileSet = OutlineContourBuilder.BuildProfiles(contour, 0.25f, 1f);
            var ringData = InvokeSharedContourHelper(profileSet.RingContoursEm, 0.15f, -0.35f, true);
            var baseline = MeshExtruder.BuildGlyphMesh(contour, 0.5f, 0f);

            Assert.Greater(ringData.Vertices.Count, 0, "canonical ring contour からも shared helper がメッシュ化できること");

            var baselineArea = GetFirstTriangleSignedAreaAtZ(baseline, 0f);
            var ringArea = GetFirstTriangleSignedAreaAtZ(ringData, 0.15f);
            Assert.AreEqual(Mathf.Sign(baselineArea), Mathf.Sign(ringArea),
                "OutlineContourBuilder が返す RingContoursEm でも body と同じ front winding parity を保つこと");
        }

        [Test]
        public void SharedContourHelper_CounterClockwiseOuterContour_BuildsOutwardSideNormals()
        {
            var contour = MakeSquareContour();
            var helperData = InvokeSharedContourHelper(contour.Contours, 0.25f, -0.25f, true);

            Assert.GreaterOrEqual(helperData.Normals.Count, 12, "cap + side normal が生成されること");

            for (int i = 8; i < 12; i++)
                Assert.AreEqual(Vector3.down, helperData.Normals[i],
                    "CCW outer contour の最初の side quad は外側 (-Y) を向くこと");
        }

        [Test]
        public void SharedContourHelper_ClockwiseContour_BuildsHoleFacingSideNormals()
        {
            var contour = MakeClockwiseSquareContour();
            var helperData = InvokeSharedContourHelper(contour.Contours, 0.25f, -0.25f, true);

            Assert.GreaterOrEqual(helperData.Normals.Count, 12, "cap + side normal が生成されること");

            for (int i = 8; i < 12; i++)
                Assert.AreEqual(Vector3.down, helperData.Normals[i],
                    "CW hole contour の最初の side quad は穴の内側 (-Y) を向くこと");
        }

        [Test]
        public void BuildGlyphMesh_DonutContour_InnerWallFacesHoleInterior()
        {
            var contour = MakeDonutContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);

            Assert.GreaterOrEqual(data.Normals.Count, 28, "ドーナツ形状の cap + side normal が生成されること");

            for (int i = 24; i < 28; i++)
                Assert.AreEqual(Vector3.down, data.Normals[i],
                    "最初の hole side quad は穴の内側を向くこと");
        }

        [Test]
        public void BuildGlyphMesh_ClockwiseSourceContour_IsNormalizedBeforeSideGeneration()
        {
            var contour = MakeClockwiseSquareContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);

            Assert.GreaterOrEqual(data.Normals.Count, 12, "cap + side normal が生成されること");

            for (int i = 8; i < 12; i++)
                Assert.AreEqual(Vector3.down, data.Normals[i],
                    "raw glyph contour が clockwise でも body 側で正規化され、最初の outer side quad は外側 (-Y) を向くこと");
        }

        [Test]
        public void BuildGlyphMesh_SquareContour_FrontAndBackCapsUseOppositeFacing()
        {
            var contour = MakeSquareContour();
            var data = MeshExtruder.BuildGlyphMesh(contour, 1f, 0f);

            var frontArea = GetFirstTriangleSignedAreaAtZ(data, 0f);
            var backArea = GetFirstTriangleSignedAreaAtZ(data, -1f);

            Assert.Greater(frontArea, 0f, "正規化済み body front cap は可視側を向く winding を維持すること");
            Assert.Less(backArea, 0f, "正規化済み body back cap は front と逆向きの winding を維持すること");
        }

        [Test]
        public void SharedCapHelper_FaceDirectionMatchesBodyCapConvention()
        {
            var contour = MakeSquareContour();
            var frontCap = InvokeCapHelper(contour.Contours, 0.25f, true);
            var backCap = InvokeCapHelper(contour.Contours, -0.25f, false);

            var frontArea = GetFirstTriangleSignedAreaAtZ(frontCap, 0.25f);
            var backArea = GetFirstTriangleSignedAreaAtZ(backCap, -0.25f);

            Assert.Greater(frontArea, 0f, "faceForward=true の cap helper は body front と同じ winding を使うこと");
            Assert.Less(backArea, 0f, "faceForward=false の cap helper は body back と同じ winding を使うこと");
        }
    }
}
