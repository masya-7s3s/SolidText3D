using System.Collections.Generic;
using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// グリフ一覧に対してレイアウト計算（座標割り当て）を行う静的クラス。
    /// 横書き・縦書き両方に対応し、GlyphMeshBuilder から呼び出される。
    /// </summary>
    internal static class LayoutEngine
    {
        /// <summary>
        /// 横書きレイアウトを適用する。
        /// LetterSpacing の累積加算と MaxWidth による自動折り返しに対応する。
        /// </summary>
        /// <param name="glyphs">レイアウト対象のグリフ一覧。Offset が in-place で更新される。</param>
        /// <param name="p">メッシュ生成パラメータ。</param>
        internal static void ApplyHorizontalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)
        {
            if (glyphs == null || glyphs.Count == 0) return;

            float cursorX = 0f;
            float cursorY = 0f;
            float lineHeight = p.FontSize > 0f ? p.FontSize * (p.LineSpacing > 0f ? p.LineSpacing : 1f) : 1f;
            float extraX = 0f;

            for (int i = 0; i < glyphs.Count; i++)
            {
                var g = glyphs[i];

                // MaxWidth による自動折り返し（MaxWidth > 0 のときのみ）
                if (p.MaxWidth > 0f && i > 0)
                {
                    float charWidth = g.AdvanceWidth > 0f ? g.AdvanceWidth : p.FontSize;
                    bool isCjk = IsCjkCharacter(i < (p.Text?.Length ?? 0) ? p.Text[i] : '\0');
                    bool isWordBoundary = isCjk; // CJK は文字単位で折り返し

                    if (isWordBoundary && (cursorX + charWidth) > p.MaxWidth)
                    {
                        cursorX = 0f;
                        cursorY -= lineHeight;
                        extraX = 0f;
                    }
                }

                g.Offset = new Vector3(cursorX + extraX, cursorY, 0f);
                glyphs[i] = g;
                extraX += p.LetterSpacing;
            }
        }

        /// <summary>
        /// 縦書きレイアウトを適用する。
        /// 各文字を上から下（Y は減少方向）に並べ、MaxHeight を超えたら次列（X は左方向）へ折り返す。
        /// </summary>
        /// <param name="glyphs">レイアウト対象のグリフ一覧。Offset が in-place で更新される。</param>
        /// <param name="p">メッシュ生成パラメータ。</param>
        internal static void ApplyVerticalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)
        {
            if (glyphs == null || glyphs.Count == 0) return;

            float colWidth = p.VerticalColumnWidth > 0f ? p.VerticalColumnWidth : p.FontSize * 1.1f;
            float lineHeight = p.FontSize > 0f ? p.FontSize * (p.LineSpacing > 0f ? p.LineSpacing : 1f) : 1f;

            float cursorX = 0f;
            float cursorY = 0f;
            int col = 0;

            for (int i = 0; i < glyphs.Count; i++)
            {
                var g = glyphs[i];
                float charHeight = g.AdvanceHeight > 0f ? g.AdvanceHeight : g.AdvanceWidth;
                if (charHeight <= 0f) charHeight = lineHeight;

                // MaxHeight による列折り返し（MaxHeight > 0 のときのみ、先頭以外）
                if (p.MaxHeight > 0f && i > 0 && (-cursorY + charHeight) > p.MaxHeight)
                {
                    col++;
                    cursorX = -(col * colWidth);
                    cursorY = 0f;
                }

                // 列内で水平中央揃え
                float charCenterX = cursorX + colWidth * 0.5f;
                g.Offset = new Vector3(charCenterX, cursorY, 0f);
                glyphs[i] = g;

                cursorY -= charHeight;
            }
        }

        /// <summary>
        /// メッシュの bounds とパラメータからアンカーオフセットを計算して返す。
        /// </summary>
        /// <param name="meshBounds">メッシュのバウンディングボックス。</param>
        /// <param name="p">メッシュ生成パラメータ（アンカー設定を参照）。</param>
        /// <returns>全頂点に加算すべきオフセットベクトル。</returns>
        internal static Vector3 CalculateAnchorOffset(Bounds meshBounds, MeshGenerationParams p)
        {
            float width = meshBounds.size.x;
            float height = meshBounds.size.y;
            float depth = meshBounds.size.z;

            // 水平: Left=min を原点, Center=-width/2 相対, Right=-width 相対
            float offsetX;
            switch (p.HorizontalAnchor)
            {
                case HorizontalAnchor.Left:   offsetX = -meshBounds.min.x; break;
                case HorizontalAnchor.Center: offsetX = -meshBounds.min.x - width * 0.5f; break;
                case HorizontalAnchor.Right:  offsetX = -meshBounds.min.x - width; break;
                default: offsetX = -meshBounds.min.x; break;
            }

            // 垂直: Lower=min を原点, Middle=-height/2 相対, Upper=-height 相対
            float offsetY;
            switch (p.VerticalAnchor)
            {
                case VerticalAnchor.Lower:  offsetY = -meshBounds.min.y; break;
                case VerticalAnchor.Middle: offsetY = -meshBounds.min.y - height * 0.5f; break;
                case VerticalAnchor.Upper:  offsetY = -meshBounds.min.y - height; break;
                default: offsetY = -meshBounds.min.y; break;
            }

            // 奥行き: Front=min を原点, Center=-depth/2 相対, Back=-depth 相対
            float offsetZ;
            switch (p.DepthAnchor)
            {
                case DepthAnchor.Front:  offsetZ = -meshBounds.min.z; break;
                case DepthAnchor.Center: offsetZ = -meshBounds.min.z - depth * 0.5f; break;
                case DepthAnchor.Back:   offsetZ = -meshBounds.min.z - depth; break;
                default: offsetZ = -meshBounds.min.z; break;
            }

            return new Vector3(offsetX, offsetY, offsetZ);
        }

        /// <summary>
        /// 指定した文字が CJK（文字単位折り返し対象）かどうかを判定する。
        /// </summary>
        private static bool IsCjkCharacter(char c)
        {
            // CJK統合漢字・ひらがな・カタカナ等
            if (c >= '\u2E80' && c <= '\u9FFF') return true;
            // ハングル音節
            if (c >= '\uAC00' && c <= '\uD7AF') return true;
            // 全角英数字・記号
            if (c >= '\uFF00' && c <= '\uFF60') return true;
            return false;
        }
    }
}
