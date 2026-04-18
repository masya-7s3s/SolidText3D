using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// GlyphContourBuilder の単体テスト（TDD Red フェーズ: T015 実装前は FAIL）
    /// </summary>
    public class GlyphContourBuilderTests
    {
        private GlyphContourBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _builder = new GlyphContourBuilder(0.0005f);
        }

        [Test]
        public void BeginFigure_Then_EndFigure_CreatesContour()
        {
            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0, 0));
            _builder.LineTo(new System.Numerics.Vector2(1, 0));
            _builder.LineTo(new System.Numerics.Vector2(1, 1));
            _builder.LineTo(new System.Numerics.Vector2(0, 1));
            _builder.EndFigure();
            _builder.EndGlyph();

            var contours = _builder.GlyphContours;
            Assert.AreEqual(1, contours.Count, "1 グリフが作成されるべき");
            Assert.AreEqual(1, contours[0].Contours.Count, "1 コンターが作成されるべき");
            Assert.GreaterOrEqual(contours[0].Contours[0].Count, 3,
                "コンターは少なくとも 3 点を持つべき");
        }

        [Test]
        public void MoveTo_SetsStartPoint()
        {
            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(5, 7));
            _builder.LineTo(new System.Numerics.Vector2(10, 7));
            _builder.EndFigure();
            _builder.EndGlyph();

            var contours = _builder.GlyphContours;
            Assert.AreEqual(1, contours.Count);
            var points = contours[0].Contours[0];
            // 最初の点が MoveTo の位置であるべき
            Assert.AreEqual(5f, points[0].x, 0.001f);
            Assert.AreEqual(7f, points[0].y, 0.001f);
        }

        [Test]
        public void LineTo_AddsLinePoint()
        {
            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0, 0));
            _builder.LineTo(new System.Numerics.Vector2(1, 0));
            _builder.LineTo(new System.Numerics.Vector2(2, 0));
            _builder.EndFigure();
            _builder.EndGlyph();

            var contours = _builder.GlyphContours;
            Assert.GreaterOrEqual(contours[0].Contours[0].Count, 2);
        }

        [Test]
        public void QuadraticBezierTo_CreatesMultiplePoints()
        {
            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0, 0));
            _builder.QuadraticBezierTo(
                new System.Numerics.Vector2(0.5f, 1),
                new System.Numerics.Vector2(1, 0));
            _builder.EndFigure();
            _builder.EndGlyph();

            var contours = _builder.GlyphContours;
            // Bezier 離散化により少なくとも 2 点が追加されるべき
            Assert.GreaterOrEqual(contours[0].Contours[0].Count, 2);
        }

        [Test]
        public void CubicBezierTo_CreatesMultiplePoints()
        {
            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0, 0));
            _builder.CubicBezierTo(
                new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, -1),
                new System.Numerics.Vector2(1, 0));
            _builder.EndFigure();
            _builder.EndGlyph();

            var contours = _builder.GlyphContours;
            Assert.GreaterOrEqual(contours[0].Contours[0].Count, 2);
        }

        [Test]
        public void EndFigure_ClosesContour()
        {
            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0, 0));
            _builder.LineTo(new System.Numerics.Vector2(1, 0));
            _builder.LineTo(new System.Numerics.Vector2(0.5f, 1));
            _builder.EndFigure();
            _builder.EndGlyph();

            var contours = _builder.GlyphContours;
            Assert.AreEqual(1, contours[0].Contours.Count, "EndFigure でコンターが確定するべき");
        }

        [Test]
        public void EndGlyph_ConfirmsGlyph()
        {
            // 2 つのコンター（外輪郭 + ホール）を持つグリフ
            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0, 0));
            _builder.LineTo(new System.Numerics.Vector2(2, 0));
            _builder.LineTo(new System.Numerics.Vector2(2, 2));
            _builder.LineTo(new System.Numerics.Vector2(0, 2));
            _builder.EndFigure();

            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0.5f, 0.5f));
            _builder.LineTo(new System.Numerics.Vector2(1.5f, 0.5f));
            _builder.LineTo(new System.Numerics.Vector2(1.5f, 1.5f));
            _builder.LineTo(new System.Numerics.Vector2(0.5f, 1.5f));
            _builder.EndFigure();

            _builder.EndGlyph();

            var contours = _builder.GlyphContours;
            Assert.AreEqual(1, contours.Count);
            Assert.AreEqual(2, contours[0].Contours.Count, "2 コンター（外 + ホール）が作成されるべき");
        }

        [Test]
        public void MultipleGlyphs_BuildsMultipleContours()
        {
            // 2 グリフ分のコールバック
            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0, 0));
            _builder.LineTo(new System.Numerics.Vector2(1, 0));
            _builder.LineTo(new System.Numerics.Vector2(0.5f, 1));
            _builder.EndFigure();
            _builder.EndGlyph();

            _builder.BeginFigure();
            _builder.MoveTo(new System.Numerics.Vector2(0, 0));
            _builder.LineTo(new System.Numerics.Vector2(1, 0));
            _builder.LineTo(new System.Numerics.Vector2(1, 1));
            _builder.LineTo(new System.Numerics.Vector2(0, 1));
            _builder.EndFigure();
            _builder.EndGlyph();

            var contours = _builder.GlyphContours;
            Assert.AreEqual(2, contours.Count, "2 グリフが作成されるべき");
        }
    }
}
