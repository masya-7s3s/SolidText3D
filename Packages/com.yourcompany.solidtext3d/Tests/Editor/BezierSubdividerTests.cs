using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// BezierSubdivider の単体テスト（TDD Red フェーズ: T013 実装前は FAIL）
    /// </summary>
    public class BezierSubdividerTests
    {
        private const float DefaultThreshold = 0.0005f;

        // ─── SubdivideQuadratic ───────────────────────────────────────────

        [Test]
        public void SubdivideQuadratic_StraightLine_ReturnsTwoPoints()
        {
            // 直線退化: p0→p1→p2 が一直線の場合、終点のみ追加される
            var output = new List<Vector2>();
            var p0 = new Vector2(0, 0);
            var p1 = new Vector2(1, 0);
            var p2 = new Vector2(2, 0);

            BezierSubdivider.SubdivideQuadratic(p0, p1, p2, DefaultThreshold, output);

            Assert.GreaterOrEqual(output.Count, 1, "直線退化でも終点は出力されるべき");
            Assert.AreEqual(p2.x, output[output.Count - 1].x, 0.001f);
            Assert.AreEqual(p2.y, output[output.Count - 1].y, 0.001f);
        }

        [Test]
        public void SubdivideQuadratic_CurvedArc_ProducesMultiplePoints()
        {
            // 曲率の大きい二次ベジェは複数の頂点を生成する
            var output = new List<Vector2>();
            var p0 = new Vector2(0, 0);
            var p1 = new Vector2(0.5f, 1f);  // 大きく曲がる制御点
            var p2 = new Vector2(1, 0);

            BezierSubdivider.SubdivideQuadratic(p0, p1, p2, DefaultThreshold, output);

            Assert.Greater(output.Count, 1, "曲率が大きいとき複数の頂点が生成されるべき");
        }

        [Test]
        public void SubdivideQuadratic_EndpointIsLastElement()
        {
            var output = new List<Vector2>();
            var p2 = new Vector2(3, 4);

            BezierSubdivider.SubdivideQuadratic(
                new Vector2(0, 0), new Vector2(1.5f, 5), p2, DefaultThreshold, output);

            Assert.AreEqual(p2.x, output[output.Count - 1].x, 0.001f);
            Assert.AreEqual(p2.y, output[output.Count - 1].y, 0.001f);
        }

        [Test]
        public void SubdivideQuadratic_LargeThreshold_FewerPoints()
        {
            // 誤差閾値が大きいほど生成される頂点数が少なくなるべき
            var highThreshold = new List<Vector2>();
            var lowThreshold = new List<Vector2>();

            var p0 = new Vector2(0, 0);
            var p1 = new Vector2(0.5f, 1f);
            var p2 = new Vector2(1, 0);

            BezierSubdivider.SubdivideQuadratic(p0, p1, p2, 0.1f, highThreshold);
            BezierSubdivider.SubdivideQuadratic(p0, p1, p2, 0.0001f, lowThreshold);

            Assert.LessOrEqual(highThreshold.Count, lowThreshold.Count,
                "閾値が大きいほど頂点数が少ないか同じであるべき");
        }

        // ─── SubdivideCubic ──────────────────────────────────────────────

        [Test]
        public void SubdivideCubic_StraightLine_ReturnsFewPoints()
        {
            // 三次ベジェの直線退化
            var output = new List<Vector2>();
            BezierSubdivider.SubdivideCubic(
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(2, 0),
                new Vector2(3, 0),
                DefaultThreshold, output);

            Assert.GreaterOrEqual(output.Count, 1);
            Assert.AreEqual(3f, output[output.Count - 1].x, 0.001f);
            Assert.AreEqual(0f, output[output.Count - 1].y, 0.001f);
        }

        [Test]
        public void SubdivideCubic_SCurve_ProducesMultiplePoints()
        {
            // S 字カーブ
            var output = new List<Vector2>();
            BezierSubdivider.SubdivideCubic(
                new Vector2(0, 0),
                new Vector2(0, 1f),
                new Vector2(1, -1f),
                new Vector2(1, 0),
                DefaultThreshold, output);

            Assert.Greater(output.Count, 1);
        }

        [Test]
        public void SubdivideCubic_EndpointIsLastElement()
        {
            var output = new List<Vector2>();
            var p3 = new Vector2(5, 7);

            BezierSubdivider.SubdivideCubic(
                new Vector2(0, 0),
                new Vector2(1, 3),
                new Vector2(4, -1),
                p3,
                DefaultThreshold, output);

            Assert.AreEqual(p3.x, output[output.Count - 1].x, 0.001f);
            Assert.AreEqual(p3.y, output[output.Count - 1].y, 0.001f);
        }

        [Test]
        public void SubdivideCubic_LargeThreshold_FewerPoints()
        {
            var highThreshold = new List<Vector2>();
            var lowThreshold = new List<Vector2>();

            BezierSubdivider.SubdivideCubic(
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, -1), new Vector2(1, 0),
                0.1f, highThreshold);
            BezierSubdivider.SubdivideCubic(
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, -1), new Vector2(1, 0),
                0.0001f, lowThreshold);

            Assert.LessOrEqual(highThreshold.Count, lowThreshold.Count);
        }
    }
}
