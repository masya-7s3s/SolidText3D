using System.Collections.Generic;
using System.IO;
using SixLabors.Fonts;
using UnityEngine;
using UnityEngine.Profiling;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// フォントファイルとテキストパラメータから 3D メッシュを生成する静的ファサードクラス。
    /// </summary>
    public static class GlyphMeshBuilder
    {
        /// <summary>
        /// 指定したパラメータに基づいてテキストの 3D メッシュを生成する。
        /// </summary>
        /// <param name="p">メッシュ生成パラメータ。</param>
        /// <returns>生成された Unity Mesh。テキストが空またはフォントが読み込めない場合は空メッシュを返す。</returns>
        public static Mesh Build(MeshGenerationParams p)
        {
            Profiler.BeginSample("GlyphMeshBuilder.Build");
            try
            {
            // GC 正当化: このアロケーションはパラメータ変更時（メッシュ再生成時）のみ発生する
            // LateUpdate() 内ではダーティフラグチェックのみを行い、アロケーションは発生しない（憲法 V 準拠）
            if (string.IsNullOrEmpty(p.Text))
                return new Mesh();

            byte[] fontBytes = GetFontBytes(p);
            if (fontBytes == null || fontBytes.Length == 0)
            {
                Debug.LogWarning("[SolidText3D] フォントバイト配列が取得できませんでした。空メッシュを返します。");
                return new Mesh();
            }

            var collection = new FontCollection();
            FontFamily family;
            using (var ms = new MemoryStream(fontBytes))
            {
                family = collection.Add((Stream)ms);
            }

            const float renderFontSize = 72f;
            var font = family.CreateFont(renderFontSize);
            var options = new TextOptions(font);

            // FontSize=1 のとき em スクエア(=renderFontSize)が 1 Unity unit になるようスケーリング
            float scale = (p.FontSize > 0f ? p.FontSize : 1f) / renderFontSize;
            var renderer = new GlyphContourBuilder(p.BezierErrorThreshold, scale);
            TextRenderer.RenderTextTo(renderer, p.Text, options);

            var glyphs = renderer.GlyphContours;
            if (glyphs.Count == 0)
                return new Mesh();

            ApplyLayout(glyphs, p, font);

            return MeshExtruder.Build(glyphs, p);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// LetterSpacing / LineSpacing に基づいてグリフの Offset を設定する。
        /// </summary>
        private static void ApplyLayout(List<GlyphContour> glyphs, MeshGenerationParams p, SixLabors.Fonts.Font font)
        {
            float scale = (p.FontSize > 0f ? p.FontSize : 1f) / font.Size;
            float lineHeight = font.Size * scale * p.LineSpacing;
            float cursorX = 0f;
            float cursorY = 0f;
            int lineGlyphIdx = 0;

            // テキストを行ごとに分解してオフセットを計算する
            string text = p.Text ?? string.Empty;
            int glyphIdx = 0;

            for (int charIdx = 0; charIdx < text.Length && glyphIdx < glyphs.Count; charIdx++)
            {
                char c = text[charIdx];
                if (c == '\n')
                {
                    cursorX = 0f;
                    cursorY -= lineHeight;
                    lineGlyphIdx = 0;
                    continue;
                }

                var glyph = glyphs[glyphIdx];
                glyph.Offset = new Vector3(cursorX, cursorY, 0f);
                cursorX += glyph.AdvanceWidth + p.LetterSpacing;
                lineGlyphIdx++;
                glyphIdx++;
            }
        }

        private static byte[] GetFontBytes(MeshGenerationParams p)
        {
            // FontData が直接指定されている場合は優先して使用
            if (p.FontData != null && p.FontData.Length > 0)
                return p.FontData;

#if UNITY_EDITOR
            // エディタ実行時: FontPath からバイトを読み込む
            string path = p.FontPath;
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                    return File.ReadAllBytes(path);

                // Unity アセットパスの場合は絶対パスに変換
                string absPath = Path.GetFullPath(path);
                if (File.Exists(absPath))
                    return File.ReadAllBytes(absPath);
            }

            // デフォルトの埋め込みフォントをリソースから読み込む
            var textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(
                "Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.ttf");
            if (textAsset != null)
                return textAsset.bytes;

            // フォールバック: ファイルシステムから直接読み込む
            string defaultPath = Path.GetFullPath(
                "Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.ttf");
            if (File.Exists(defaultPath))
                return File.ReadAllBytes(defaultPath);

            return null;
#else
            // ランタイム（ビルド済み）: Resources.Load で Noto Sans JP を取得
            // T031 で完全実装される。このスタブは非エディタビルド向けのフォールバック
            if (!string.IsNullOrEmpty(p.FontPath))
            {
                // FontPath を Resources パスとして解釈する試み
                var ta = Resources.Load<TextAsset>(p.FontPath);
                if (ta != null) return ta.bytes;
            }

            var defaultAsset = Resources.Load<TextAsset>("Fonts/NotoSansJP-Black");
            if (defaultAsset != null)
                return defaultAsset.bytes;

            Debug.LogWarning("[SolidText3D] Resources から NotoSansJP-Black が見つかりませんでした。");
            return null;
#endif
        }
    }
}
