using System.Collections.Generic;
using SixLabors.Fonts;
using UnityEngine;
using NumericsVector2 = System.Numerics.Vector2;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// SixLabors.Fonts の IGlyphRenderer を実装し、グリフ輪郭データを収集するクラス。
    /// TextRenderer.RenderTextTo() のコールバック先として使用する。
    /// </summary>
    internal sealed class GlyphContourBuilder : IGlyphRenderer
    {
        private readonly float _bezierErrorThreshold;

        // 現在処理中のグリフの輪郭リスト
        private List<List<Vector2>> _currentContours;
        // 現在処理中の輪郭の頂点リスト
        private List<Vector2> _currentContour;
        // BeginFigure で記録した輪郭の先頭点（EndFigure でのクローズに使用）
        private Vector2 _figureStartPoint;
        // 直前の点（Bezier 分割の p0 に使用）
        private Vector2 _lastPoint;

        private FontRectangle _currentBounds;

        /// <summary>
        /// 収集されたグリフ輪郭データのリスト。EndGlyph() が呼ばれるたびに追加される。
        /// </summary>
        public List<GlyphContour> GlyphContours { get; } = new List<GlyphContour>();

        /// <summary>
        /// GlyphContourBuilder を生成する。
        /// </summary>
        /// <param name="bezierErrorThreshold">ベジェ曲線の適応分割誤差閾値。</param>
        // レンダリング用フォントサイズ（72pt 固定）に対するスケール係数
        private readonly float _scale;

        public GlyphContourBuilder(float bezierErrorThreshold, float scale = 1f)
        {
            _bezierErrorThreshold = bezierErrorThreshold;
            _scale = scale;
        }

        /// <inheritdoc/>
        public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            _currentBounds = bounds;
            _currentContours = new List<List<Vector2>>();
            return true;
        }

        /// <inheritdoc/>
        public void BeginFigure()
        {
            if (_currentContours == null)
                _currentContours = new List<List<Vector2>>();
            _currentContour = new List<Vector2>();
        }

        /// <inheritdoc/>
        public void MoveTo(NumericsVector2 point)
        {
            var p = ToUnity(point);
            _figureStartPoint = p;
            _lastPoint = p;
            _currentContour.Add(p);
        }

        /// <inheritdoc/>
        public void LineTo(NumericsVector2 point)
        {
            var p = ToUnity(point);
            _lastPoint = p;
            _currentContour.Add(p);
        }

        /// <inheritdoc/>
        public void QuadraticBezierTo(NumericsVector2 secondControlPoint, NumericsVector2 point)
        {
            var p1 = ToUnity(secondControlPoint);
            var p2 = ToUnity(point);
            BezierSubdivider.SubdivideQuadratic(_lastPoint, p1, p2, _bezierErrorThreshold, _currentContour);
            _lastPoint = p2;
        }

        /// <inheritdoc/>
        public void CubicBezierTo(NumericsVector2 secondControlPoint, NumericsVector2 thirdControlPoint, NumericsVector2 point)
        {
            var p1 = ToUnity(secondControlPoint);
            var p2 = ToUnity(thirdControlPoint);
            var p3 = ToUnity(point);
            BezierSubdivider.SubdivideCubic(_lastPoint, p1, p2, p3, _bezierErrorThreshold, _currentContour);
            _lastPoint = p3;
        }

        /// <inheritdoc/>
        public void EndFigure()
        {
            if (_currentContour != null && _currentContour.Count > 0)
            {
                // SixLabors.Fonts は EndFigure 前に始点への LineTo を呼ぶ場合がある。
                // 始点と終点が重複する場合は終点を除去して縮退エッジを防ぎ、
                // テッセレータの誤動作を防止する。
                while (_currentContour.Count > 1)
                {
                    var first = _currentContour[0];
                    var last = _currentContour[_currentContour.Count - 1];
                    if ((first - last).sqrMagnitude < 1e-10f)
                        _currentContour.RemoveAt(_currentContour.Count - 1);
                    else
                        break;
                }
                if (_currentContour.Count >= 3)
                    _currentContours.Add(_currentContour);
            }
            _currentContour = null;
        }

        /// <inheritdoc/>
        public void EndGlyph()
        {
            var bounds = new Rect(
                _currentBounds.X * _scale,
                _currentBounds.Y * _scale,
                _currentBounds.Width * _scale,
                _currentBounds.Height * _scale);

            var contour = new GlyphContour
            {
                Contours = _currentContours ?? new List<List<Vector2>>(),
                AdvanceWidth = _currentBounds.Width * _scale,
                Bounds = bounds
            };
            GlyphContours.Add(contour);
            _currentContours = null;
        }

        // ── SixLabors.Fonts 1.x IGlyphRenderer 追加メソッド ──────────────

        /// <inheritdoc/>
        public void BeginText(in FontRectangle bounds) { }

        /// <inheritdoc/>
        public void EndText() { }

        /// <inheritdoc/>
        public TextDecorations EnabledDecorations() => TextDecorations.None;

        /// <inheritdoc/>
        public void SetDecoration(TextDecorations decorations, NumericsVector2 start, NumericsVector2 end, float thickness) { }

        private Vector2 ToUnity(NumericsVector2 v)
        {
            // SixLabors.Fonts は Y 軸下向き（スクリーン座標）のため Y を反転する
            return new Vector2(v.X * _scale, -v.Y * _scale);
        }
    }
}
