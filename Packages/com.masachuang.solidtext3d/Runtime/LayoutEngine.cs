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
        private const float VerticalPunctuationHorizontalBias = 1f;
        private const float VerticalPunctuationVerticalBias = 0.2f;

        /// <summary>
        /// 横書きレイアウトを適用する。
        /// LetterSpacing の累積加算と MaxWidth による自動折り返しに対応する。
        /// </summary>
        /// <param name="glyphs">レイアウト対象のグリフ一覧。Offset が in-place で更新される。</param>
        /// <param name="p">メッシュ生成パラメータ。</param>
        internal static void ApplyHorizontalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)
        {
            if (glyphs == null || glyphs.Count == 0) return;

            float fontSize = p.FontSize > 0f ? p.FontSize : 1f;
            float lineHeight = fontSize * (p.LineSpacing > 0f ? p.LineSpacing : 1f);
            // 同一行判定閾値: lineHeight * 0.8f
            // コンマなどのディセンダー文字は Bounds.y が大文字と最大 ~0.7*fontSize 程度ズレるが、
            // 行間差（= lineHeight）は必ず threshold より大きいため正しく分類される
            float lineThreshold = lineHeight * 0.8f;

            // Pass 1: Bounds.y の近接度で行グループを構築
            // SixLabors は多行テキストを Y 下向きで配置するため、同一行は Bounds.y がほぼ等しい
            var lines = new List<List<int>>();
            List<int> currentLine = null;
            float currentLineY = float.NaN;

            for (int i = 0; i < glyphs.Count; i++)
            {
                float by = glyphs[i].Bounds.y;
                if (currentLine == null || Mathf.Abs(by - currentLineY) > lineThreshold)
                {
                    currentLine = new List<int>();
                    lines.Add(currentLine);
                    currentLineY = by;
                }
                currentLine.Add(i);
            }

            // Pass 2: 行ごとに HorizontalAnchor 揃えと LetterSpacing を適用
            // Offset.y = 0: SixLabors の頂点座標に Y 位置が既に含まれているため不要
            foreach (var lineIndices in lines)
            {
                if (lineIndices.Count == 0) continue;

                if (p.MonospaceMode)
                {
                    ApplyHorizontalMonospaceLayout(glyphs, lineIndices, p, fontSize);
                    continue;
                }

                float lineWidth = 0f;
                int pos = 0;
                foreach (int gi in lineIndices)
                {
                    var g = glyphs[gi];
                    float right = g.Bounds.xMin + g.Bounds.width + pos * p.LetterSpacing;
                    if (right > lineWidth) lineWidth = right;
                    pos++;
                }

                float alignX = 0f;
                switch (p.HorizontalAnchor)
                {
                    case HorizontalAnchor.Center: alignX = -lineWidth * 0.5f; break;
                    case HorizontalAnchor.Right:  alignX = -lineWidth;        break;
                }

                pos = 0;
                foreach (int gi in lineIndices)
                {
                    var g = glyphs[gi];
                    g.Offset = new Vector3(alignX + pos * p.LetterSpacing, 0f, 0f);
                    glyphs[gi] = g;
                    pos++;
                }
            }
        }

        private static void ApplyHorizontalMonospaceLayout(List<GlyphContour> glyphs, List<int> lineIndices, MeshGenerationParams p, float fontSize)
        {
            float lineWidth = 0f;
            float cursorX = 0f;
            var localOffsets = new float[lineIndices.Count];

            for (int index = 0; index < lineIndices.Count; index++)
            {
                int gi = lineIndices[index];
                var g = glyphs[gi];
                float cellWidth = GetMonospaceCellWidth(g, p, fontSize);
                float horizontalPadding = Mathf.Max(0f, cellWidth - g.Bounds.width) * 0.5f;
                localOffsets[index] = cursorX + horizontalPadding - g.Bounds.xMin;
                lineWidth = cursorX + cellWidth;
                cursorX += cellWidth + p.LetterSpacing;
            }

            float alignX = 0f;
            switch (p.HorizontalAnchor)
            {
                case HorizontalAnchor.Center: alignX = -lineWidth * 0.5f; break;
                case HorizontalAnchor.Right:  alignX = -lineWidth;        break;
            }

            for (int index = 0; index < lineIndices.Count; index++)
            {
                int gi = lineIndices[index];
                var g = glyphs[gi];
                g.Offset = new Vector3(alignX + localOffsets[index], 0f, 0f);
                glyphs[gi] = g;
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

            float fontSize = p.FontSize > 0f ? p.FontSize : 1f;
            // LineSpacing = 列幅の倍率、LetterSpacing = 文字間の追加スペース
            float colWidth = fontSize * (p.LineSpacing > 0f ? p.LineSpacing : 1.1f);
            // 文字送りステップ = FontSize + LetterSpacing（LetterSpacing で文字間を調整）
            float charStep = fontSize + p.LetterSpacing;

            float cursorX = 0f;
            float cursorY = 0f;
            int col = 0;

            // 列ごとのグリフインデックスを記録（第2パス用）
            var columns = new List<List<int>>();
            columns.Add(new List<int>());

            for (int i = 0; i < glyphs.Count; i++)
            {
                var g = glyphs[i];

                // 改行文字 → 次の列へ（縦書きでは右から左へ）
                if (!g.IsVisible && !string.IsNullOrEmpty(p.Text)
                    && g.CharIndex >= 0 && g.CharIndex < p.Text.Length
                    && p.Text[g.CharIndex] == '\n')
                {
                    col++;
                    cursorX = -(col * colWidth);
                    cursorY = 0f;
                    while (columns.Count <= col) columns.Add(new List<int>());
                    continue;
                }

                float charHeight = g.AdvanceHeight > 0f ? g.AdvanceHeight : g.AdvanceWidth;
                if (charHeight <= 0f) charHeight = fontSize;

                // MaxHeight による列折り返し（MaxHeight > 0 のときのみ、先頭以外）
                if (p.MaxHeight > 0f && i > 0 && (-cursorY + charStep) > p.MaxHeight)
                {
                    col++;
                    cursorX = -(col * colWidth);
                    cursorY = 0f;
                    while (columns.Count <= col) columns.Add(new List<int>());
                }

                // 各文字を charStep の固定セル内で垂直・水平中央揃え
                // 正規化後グリフ: X=[0, charWidth], Y=[-charHeight, 0]（上端が 0）
                float charWidth = p.MonospaceMode
                    ? GetMonospaceCellWidth(g, p, fontSize)
                    : (g.AdvanceWidth > 0f ? g.AdvanceWidth : colWidth);

                // RotateAsciiInVertical: 印字可能 ASCII（0x21-0x7E）を 90 度時計回り回転
                bool shouldRotate = p.RotateAsciiInVertical
                    && !string.IsNullOrEmpty(p.Text)
                    && g.CharIndex >= 0 && g.CharIndex < p.Text.Length
                    && p.Text[g.CharIndex] >= '!' && p.Text[g.CharIndex] <= '~';
                g.IsRotated = shouldRotate;
                // 回転時は幅と高さが入れ替わる
                float effectiveWidth  = shouldRotate ? charHeight : charWidth;
                float effectiveHeight = shouldRotate ? charWidth  : charHeight;

                float horizontalPadding = Mathf.Max(0f, colWidth - effectiveWidth);
                float verticalPadding = Mathf.Max(0f, charStep - effectiveHeight);
                bool alignPunctuationToTopRight = !shouldRotate
                    && !string.IsNullOrEmpty(p.Text)
                    && g.CharIndex >= 0
                    && g.CharIndex < p.Text.Length
                    && IsJapaneseVerticalPunctuation(p.Text[g.CharIndex]);

                float charOffsetX = cursorX + (alignPunctuationToTopRight
                    ? horizontalPadding * VerticalPunctuationHorizontalBias
                    : horizontalPadding * 0.5f);
                float charOffsetY = cursorY - (alignPunctuationToTopRight
                    ? verticalPadding * VerticalPunctuationVerticalBias
                    : verticalPadding * 0.5f);
                g.Offset = new Vector3(charOffsetX, charOffsetY, 0f);
                glyphs[i] = g;

                while (columns.Count <= col) columns.Add(new List<int>());
                columns[col].Add(i);
                cursorY -= charStep;
            }

            // Pass 2: VerticalAnchor に応じて列ごとに Y オフセットを適用
            // Upper = 各列の上端が Y=0（デフォルト、追加シフトなし）
            // Middle = 各列の中央が Y=0
            // Lower  = 各列の下端が Y=0
            if (p.VerticalAnchor != VerticalAnchor.Upper)
            {
                foreach (var colIndices in columns)
                {
                    if (colIndices.Count == 0) continue;
                    float colHeight = colIndices.Count * charStep;
                    float deltaY = p.VerticalAnchor == VerticalAnchor.Middle
                        ? colHeight * 0.5f
                        : colHeight; // Lower
                    foreach (int gi in colIndices)
                    {
                        var g = glyphs[gi];
                        g.Offset = new Vector3(g.Offset.x, g.Offset.y + deltaY, g.Offset.z);
                        glyphs[gi] = g;
                    }
                }
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

            // 奥行き: MeshExtruder は front=max.z, back=min.z の規約で面を生成する。
            // Front は min.z を、Back は max.z を原点に合わせる。
            float offsetZ;
            switch (p.DepthAnchor)
            {
                case DepthAnchor.Front:  offsetZ = -meshBounds.min.z; break;
                case DepthAnchor.Center: offsetZ = -meshBounds.center.z; break;
                case DepthAnchor.Back:   offsetZ = -meshBounds.max.z; break;
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

        private static bool IsJapaneseVerticalPunctuation(char c)
        {
            return c == '、' || c == '。';
        }

        private static float GetMonospaceCellWidth(GlyphContour glyph, MeshGenerationParams p, float fontSize)
        {
            char sourceChar;
            if (TryGetSourceCharacter(glyph, p, out sourceChar))
                return IsHalfWidthMonospaceCharacter(sourceChar) ? fontSize * 0.5f : fontSize;

            float inferredAdvance = glyph.AdvanceWidth > 0f ? glyph.AdvanceWidth : glyph.Bounds.width;
            return inferredAdvance <= fontSize * 0.75f ? fontSize * 0.5f : fontSize;
        }

        private static bool TryGetSourceCharacter(GlyphContour glyph, MeshGenerationParams p, out char sourceChar)
        {
            sourceChar = '\0';
            if (string.IsNullOrEmpty(p.Text))
                return false;

            if (glyph.CharIndex < 0 || glyph.CharIndex >= p.Text.Length)
                return false;

            sourceChar = p.Text[glyph.CharIndex];
            return true;
        }

        private static bool IsHalfWidthMonospaceCharacter(char c)
        {
            if (c <= '\u007F') return true;
            if (c >= '\uFF61' && c <= '\uFF9F') return true;
            if (c >= '\uFFA0' && c <= '\uFFDC') return true;
            if (c >= '\uFFE8' && c <= '\uFFEE') return true;
            return false;
        }
    }
}
