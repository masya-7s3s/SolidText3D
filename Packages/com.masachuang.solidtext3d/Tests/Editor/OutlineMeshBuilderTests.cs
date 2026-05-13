using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    public class OutlineMeshBuilderTests
    {
        private static GlyphContour MakeSquareContour(float size = 1f)
        {
            return new GlyphContour
            {
                Contours = new List<List<Vector2>>
                {
                    new List<Vector2>
                    {
                        new Vector2(0f, 0f),
                        new Vector2(size, 0f),
                        new Vector2(size, size),
                        new Vector2(0f, size),
                    }
                },
                AdvanceWidth = size,
                Bounds = new Rect(0f, 0f, size, size)
            };
        }

        private static OutlineSettings MakeSettings(float offset, float thickness)
        {
            return new OutlineSettings
            {
                Enabled = true,
                OffsetAmount = offset,
                Thickness = thickness,
                DisplayMode = OutlineDisplayMode.Donut
            };
        }

        private static Rect GetBounds(List<List<Vector2>> contours)
        {
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            foreach (var contour in contours)
            {
                foreach (var point in contour)
                {
                    minX = Mathf.Min(minX, point.x);
                    minY = Mathf.Min(minY, point.y);
                    maxX = Mathf.Max(maxX, point.x);
                    maxY = Mathf.Max(maxY, point.y);
                }
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static bool HasTriangleAtZ(Mesh mesh, float targetZ)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v0 = vertices[triangles[i + 0]];
                var v1 = vertices[triangles[i + 1]];
                var v2 = vertices[triangles[i + 2]];

                if (Mathf.Approximately(v0.z, targetZ) && Mathf.Approximately(v1.z, targetZ) && Mathf.Approximately(v2.z, targetZ))
                    return true;
            }

            return false;
        }

        private static float GetMinZ(Mesh mesh)
        {
            float minZ = float.PositiveInfinity;
            foreach (var vertex in mesh.vertices)
                minZ = Mathf.Min(minZ, vertex.z);
            return minZ;
        }

        private static float GetMaxZ(Mesh mesh)
        {
            float maxZ = float.NegativeInfinity;
            foreach (var vertex in mesh.vertices)
                maxZ = Mathf.Max(maxZ, vertex.z);
            return maxZ;
        }

        private static float GetFirstTriangleSignedAreaAtZ(Mesh mesh, float targetZ)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v0 = vertices[triangles[i + 0]];
                var v1 = vertices[triangles[i + 1]];
                var v2 = vertices[triangles[i + 2]];

                if (!Mathf.Approximately(v0.z, targetZ) || !Mathf.Approximately(v1.z, targetZ) || !Mathf.Approximately(v2.z, targetZ))
                    continue;

                return Cross(new Vector2(v1.x - v0.x, v1.y - v0.y), new Vector2(v2.x - v0.x, v2.y - v0.y)) * 0.5f;
            }

            Assert.Fail($"Z={targetZ} 平面の三角形が見つかりませんでした");
            return 0f;
        }

        private static bool HasTriangleContainingPointAtZ(Mesh mesh, Vector2 point, float targetZ)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v0 = vertices[triangles[i + 0]];
                var v1 = vertices[triangles[i + 1]];
                var v2 = vertices[triangles[i + 2]];

                if (!Mathf.Approximately(v0.z, targetZ) || !Mathf.Approximately(v1.z, targetZ) || !Mathf.Approximately(v2.z, targetZ))
                    continue;

                if (PointInTriangle(point, new Vector2(v0.x, v0.y), new Vector2(v1.x, v1.y), new Vector2(v2.x, v2.y)))
                    return true;
            }

            return false;
        }

        private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = Cross(b - a, c - a);
            if (Mathf.Approximately(area, 0f))
                return false;

            float s = Cross(b - a, point - a) / area;
            float t = Cross(c - b, point - b) / area;
            float u = Cross(a - c, point - c) / area;
            return s >= -0.0001f && t >= -0.0001f && u >= -0.0001f;
        }

        private static float Cross(Vector2 left, Vector2 right)
        {
            return (left.x * right.y) - (left.y * right.x);
        }

        [Test]
        public void Build_WithPositiveOffset_GeneratesFrontCapTriangles()
        {
            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                MakeSettings(0.25f, 0.4f),
                1f,
                1f);

            Assert.Greater(mesh.vertexCount, 0, "outline mesh が生成されること");
            Assert.IsTrue(HasTriangleAtZ(mesh, -0.3f),
                "body depth 1 / thickness 0.4 の Donut では front cap が body center 基準で +0.2f の Z=-0.3 平面に存在すること");
        }

        [Test]
        public void Build_ThicknessZero_ProducesFrontOnlySinglePlane()
        {
            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                MakeSettings(0.25f, 0f),
                1f,
                1f);

            Assert.Greater(mesh.vertexCount, 0, "thickness 0 でも front outline mesh は生成されること");
            foreach (var vertex in mesh.vertices)
                Assert.AreEqual(0f, vertex.z, 0.0001f, "thickness 0 では front-only の単一 Z 平面になること");
        }

        [Test]
        public void Build_XyBounds_MatchCanonicalRingProfileBounds()
        {
            var glyph = MakeSquareContour();
            var profileSet = OutlineContourBuilder.BuildProfiles(glyph, 0.25f, 1f);
            var expectedBounds = GetBounds(profileSet.RingContoursEm);
            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { glyph },
                MakeSettings(0.25f, 0.4f),
                1f,
                1f);

            Assert.AreEqual(expectedBounds.xMin, mesh.bounds.min.x, 0.02f);
            Assert.AreEqual(expectedBounds.xMax, mesh.bounds.max.x, 0.02f);
            Assert.AreEqual(expectedBounds.yMin, mesh.bounds.min.y, 0.02f);
            Assert.AreEqual(expectedBounds.yMax, mesh.bounds.max.y, 0.02f);
        }

        [Test]
        public void Build_BackFilledMode_GeneratesRearInfillAtBodyAnchoredBackZ()
        {
            var settings = MakeSettings(0.25f, 0.4f);
            settings.DisplayMode = OutlineDisplayMode.BackFilled;

            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                settings,
                1f,
                1f);

            const float expectedBackZ = 0.0001f;
            Assert.AreEqual(expectedBackZ, GetMaxZ(mesh), 0.001f,
                "BackFilled の固定背面は body front + Z_FIGHT_EPSILON に配置されること");
            Assert.IsTrue(HasTriangleContainingPointAtZ(mesh, new Vector2(0.5f, 0.5f), expectedBackZ),
                "BackFilled では背面に単一の rear infill cap があり、元グリフ中心を含む三角形が固定背面 plane に存在すること");
        }

        [Test]
        public void Build_BackFilledMode_RearInfillCreatesNonDegenerateBackPlaneTriangles()
        {
            var settings = MakeSettings(0.25f, 0.4f);
            settings.DisplayMode = OutlineDisplayMode.BackFilled;

            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                settings,
                1f,
                1f);

            const float expectedBackZ = 0.0001f;
            float backArea = GetFirstTriangleSignedAreaAtZ(mesh, expectedBackZ);

            Assert.Greater(backArea, 0f,
                "BackFilled の単一 rear infill cap は固定背面 plane 上で背面側を向く winding を持つこと");
        }

        [Test]
        public void Build_BackFilledMode_FrontPlaneKeepsCenterOpen()
        {
            var settings = MakeSettings(0.25f, 0.4f);
            settings.DisplayMode = OutlineDisplayMode.BackFilled;

            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                settings,
                1f,
                1f);

            const float expectedFrontZ = -0.3999f;
            Assert.IsFalse(HasTriangleContainingPointAtZ(mesh, new Vector2(0.5f, 0.5f), expectedFrontZ),
                "BackFilled の前面 plane はリングのままで、元グリフ中心を埋めないこと");
        }

        [Test]
        public void Build_BackFilledMode_FrontRingCreatesNonDegenerateFrontPlaneTriangles()
        {
            var settings = MakeSettings(0.25f, 0.4f);
            settings.DisplayMode = OutlineDisplayMode.BackFilled;

            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                settings,
                1f,
                1f);

            const float expectedFrontZ = -0.3999f;
            float frontArea = GetFirstTriangleSignedAreaAtZ(mesh, expectedFrontZ);

            Assert.AreNotEqual(0f, frontArea,
                "BackFilled の前面 ring は前面 plane 上で退化していない三角形を持つこと");
        }

        [Test]
        public void Build_DonutAndBackFilledModes_ShareSameProjectedSilhouetteBounds()
        {
            var donutSettings = MakeSettings(0.25f, 0.4f);
            var backFilledSettings = MakeSettings(0.25f, 0.4f);
            backFilledSettings.DisplayMode = OutlineDisplayMode.BackFilled;

            var donutMesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                donutSettings,
                1f,
                1f);
            var backFilledMesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                backFilledSettings,
                1f,
                1f);

            Assert.AreEqual(donutMesh.bounds.min.x, backFilledMesh.bounds.min.x, 0.001f);
            Assert.AreEqual(donutMesh.bounds.max.x, backFilledMesh.bounds.max.x, 0.001f);
            Assert.AreEqual(donutMesh.bounds.min.y, backFilledMesh.bounds.min.y, 0.001f);
            Assert.AreEqual(donutMesh.bounds.max.y, backFilledMesh.bounds.max.y, 0.001f,
                "Donut と BackFilled は正面から見た XY silhouette bounds を共有すること");
        }

        [Test]
        public void Build_DonutMode_CentersThicknessAroundBodyCenter()
        {
            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                MakeSettings(0.25f, 0.4f),
                1f,
                1f);

            Assert.AreEqual(-0.3f, GetMaxZ(mesh), 0.001f,
                "Donut の前面は body center + halfThickness に配置されること");
            Assert.AreEqual(-0.7f, GetMinZ(mesh), 0.001f,
                "Donut の背面は body center - halfThickness に配置されること");
        }

        [Test]
        public void Build_DonutMode_WhenThicknessMatchesBodyDepth_MatchesBodyFrontAndBack()
        {
            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                MakeSettings(0.25f, 1f),
                1f,
                1f);

            Assert.AreEqual(0f, GetMaxZ(mesh), 0.001f,
                "outline thickness が body depth と同じなら Donut 前面は body front と一致すること");
            Assert.AreEqual(-1f, GetMinZ(mesh), 0.001f,
                "outline thickness が body depth と同じなら Donut 背面は body back と一致すること");
        }

        [Test]
        public void Build_PositiveThickness_ProducesMultipleZPlanes()
        {
            var mesh = OutlineMeshBuilder.Build(
                new List<GlyphContour> { MakeSquareContour() },
                MakeSettings(0.25f, 0.4f),
                1f,
                1f);

            Assert.Greater(GetMaxZ(mesh) - GetMinZ(mesh), 0.3f,
                "thickness > 0 のとき outline shell は front/back の複数 Z 平面を持つこと");
        }
    }
}