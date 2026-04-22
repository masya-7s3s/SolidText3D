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

            var glyphs = RenderGlyphs(fontBytes, p);
            if (glyphs == null || glyphs.Count == 0)
                return new Mesh();

            // レイアウト適用
            if (p.WritingMode == WritingMode.Vertical)
                LayoutEngine.ApplyVerticalLayout(glyphs, p);
            else
                LayoutEngine.ApplyHorizontalLayout(glyphs, p);

            var mesh = MeshExtruder.Build(glyphs, p);

            // アンカーオフセットを全頂点に加算
            // 横書き: X 方向は各行のレイアウト時に適用済みのため全体オフセットでは無効化
            // 縦書き: Y 方向は各列のレイアウト時に適用済みのため全体オフセットでは無効化
            if (mesh != null && mesh.vertexCount > 0)
            {
                var offset = LayoutEngine.CalculateAnchorOffset(mesh.bounds, p);
                if (p.WritingMode == WritingMode.Horizontal)
                    offset = new Vector3(0f, offset.y, offset.z);
                else if (p.WritingMode == WritingMode.Vertical)
                    offset = new Vector3(offset.x, 0f, offset.z);
                if (offset != Vector3.zero)
                {
                    var verts = mesh.vertices;
                    for (int i = 0; i < verts.Length; i++)
                        verts[i] += offset;
                    mesh.vertices = verts;
                    mesh.RecalculateBounds();
                }
            }

            return mesh;
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// Per-Character モード用のメッシュリストを生成する。
        /// IsVisible == true のグリフのみを対象に、1文字ずつ個別 Mesh を生成する。
        /// </summary>
        /// <param name="p">メッシュ生成パラメータ。</param>
        /// <returns>グリフリストと対応するメッシュリストのペア。</returns>
        public static PerCharacterResult BuildPerCharacter(MeshGenerationParams p)
        {
            if (string.IsNullOrEmpty(p.Text))
                return new PerCharacterResult(new List<GlyphContour>(), new List<Mesh>());

            byte[] fontBytes = GetFontBytes(p);
            if (fontBytes == null || fontBytes.Length == 0)
                return new PerCharacterResult(new List<GlyphContour>(), new List<Mesh>());

            var allGlyphs = RenderGlyphs(fontBytes, p);
            if (allGlyphs == null || allGlyphs.Count == 0)
                return new PerCharacterResult(new List<GlyphContour>(), new List<Mesh>());

            // レイアウト適用（全グリフに座標を割り当て）
            if (p.WritingMode == WritingMode.Vertical)
                LayoutEngine.ApplyVerticalLayout(allGlyphs, p);
            else
                LayoutEngine.ApplyHorizontalLayout(allGlyphs, p);

            // SingleObject と同じアンカーオフセットを算出し各グリフの Offset に反映する
            // これにより PerCharacter と SingleObject の表示位置が一致する
            var tempMesh = MeshExtruder.Build(allGlyphs, p);
            if (tempMesh != null && tempMesh.vertexCount > 0)
            {
                var anchorOffset = LayoutEngine.CalculateAnchorOffset(tempMesh.bounds, p);
                if (p.WritingMode == WritingMode.Horizontal)
                    anchorOffset = new Vector3(0f, anchorOffset.y, anchorOffset.z);
                else if (p.WritingMode == WritingMode.Vertical)
                    anchorOffset = new Vector3(anchorOffset.x, 0f, anchorOffset.z);
                if (anchorOffset != Vector3.zero)
                {
                    foreach (var g in allGlyphs)
                        g.Offset += anchorOffset;
                }
            }

            var visibleGlyphs = new List<GlyphContour>();
            var meshes = new List<Mesh>();

            foreach (var glyph in allGlyphs)
            {
                if (!glyph.IsVisible) continue;

                // 1文字分のパラメータでメッシュを生成
                var singleGlyphList = new List<GlyphContour> { glyph };
                var mesh = MeshExtruder.Build(singleGlyphList, p);
                if (mesh != null && mesh.vertexCount > 0)
                {
                    visibleGlyphs.Add(glyph);
                    meshes.Add(mesh);
                }
            }

            return new PerCharacterResult(visibleGlyphs, meshes);
        }

        private static List<GlyphContour> RenderGlyphs(byte[] fontBytes, MeshGenerationParams p)
        {
            var collection = new FontCollection();
            FontFamily family;
            using (var ms = new MemoryStream(fontBytes))
            {
                family = collection.Add((System.IO.Stream)ms);
            }

            const float renderFontSize = 72f;
            var font = family.CreateFont(renderFontSize);

            // FontSize=1 のとき em スクエア(=renderFontSize)が 1 Unity unit になるようスケーリング
            float scale = (p.FontSize > 0f ? p.FontSize : 1f) / renderFontSize;

            // 縦書きモードの場合、1文字ずつレンダリングして輪郭を原点基準に正規化する
            // (TextRenderer.RenderTextTo は横書き座標で全文字をまとめて出力するため)
            if (p.WritingMode == WritingMode.Vertical)
            {
                var result = new List<GlyphContour>();
                var singleCharOptions = new TextOptions(font);
                for (int i = 0; i < p.Text.Length; i++)
                {
                    // 改行文字は非表示グリフとして登録し、ApplyVerticalLayout で列折り返しに使う
                    if (p.Text[i] == '\n')
                    {
                        result.Add(new GlyphContour
                        {
                            Contours = new System.Collections.Generic.List<System.Collections.Generic.List<Vector2>>(),
                            CharIndex = i,
                            IsVisible = false
                        });
                        continue;
                    }

                    string ch = p.Text[i].ToString();
                    var renderer = new GlyphContourBuilder(p.BezierErrorThreshold, scale);
                    TextRenderer.RenderTextTo(renderer, ch, singleCharOptions);
                    if (renderer.GlyphContours.Count == 0) continue;

                    var g = renderer.GlyphContours[0];
                    // 輪郭を原点基準に正規化する
                    // ToUnity で Y 軸反転済みのため:
                    //   vertex.x 範囲: [Bounds.xMin, Bounds.xMax]  → ox = Bounds.xMin で [0, width]
                    //   vertex.y 範囲: [-Bounds.yMax, -Bounds.yMin] → oy = -Bounds.yMin で [-height, 0]
                    float ox = g.Bounds.xMin;
                    float oy = -g.Bounds.yMin;
                    if (ox != 0f || oy != 0f)
                    {
                        var normalizedContours = new System.Collections.Generic.List<System.Collections.Generic.List<Vector2>>();
                        foreach (var contour in g.Contours)
                        {
                            var nc = new System.Collections.Generic.List<Vector2>(contour.Count);
                            foreach (var v in contour)
                                nc.Add(new Vector2(v.x - ox, v.y - oy));
                            normalizedContours.Add(nc);
                        }
                        g = new GlyphContour
                        {
                            Contours = normalizedContours,
                            AdvanceWidth = g.AdvanceWidth,
                            AdvanceHeight = g.AdvanceHeight,
                            Bounds = new Rect(0f, 0f, g.Bounds.width, g.Bounds.height),
                            CharIndex = i,
                            IsVisible = true
                        };
                    }
                    else
                    {
                        g.CharIndex = i;
                        g.IsVisible = true;
                    }
                    result.Add(g);
                }
                return result;
            }

            var defaultOptions = new TextOptions(font)
            {
                LineSpacing = p.LineSpacing > 0f ? p.LineSpacing : 1f,
            };
            var defaultRenderer = new GlyphContourBuilder(p.BezierErrorThreshold, scale);
            TextRenderer.RenderTextTo(defaultRenderer, p.Text, defaultOptions);

            return defaultRenderer.GlyphContours;
        }

        private static byte[] GetFontBytes(MeshGenerationParams p)
        {
            // FontData が直接指定されている場合は優先して使用
            if (p.FontData != null && p.FontData.Length > 0)
                return p.FontData;

#if UNITY_EDITOR
            // デフォルトの埋め込みフォントをリソースから読み込む
            var textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(
                "Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes");
            if (textAsset != null)
                return textAsset.bytes;

            // フォールバック: ファイルシステムから直接読み込む
            string defaultPath = Path.GetFullPath(
                "Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes");
            if (File.Exists(defaultPath))
                return File.ReadAllBytes(defaultPath);

            return null;
#else
            // ランタイム（ビルド済み）: Resources.Load で Noto Sans JP を取得
            var defaultAsset = Resources.Load<TextAsset>("Fonts/NotoSansJP-Black");
            if (defaultAsset != null)
                return defaultAsset.bytes;

            Debug.LogWarning("[SolidText3D] Resources から NotoSansJP-Black が見つかりませんでした。");
            return null;
#endif
        }
    }

    /// <summary>
    /// BuildPerCharacter の戻り値型。グリフリストと対応するメッシュリストを保持する。
    /// </summary>
    public sealed class PerCharacterResult
    {
        /// <summary>可視グリフのリスト。</summary>
        public List<GlyphContour> Glyphs { get; }
        /// <summary>各グリフに対応するメッシュのリスト。</summary>
        public List<Mesh> Meshes { get; }

        internal PerCharacterResult(List<GlyphContour> glyphs, List<Mesh> meshes)
        {
            Glyphs = glyphs;
            Meshes = meshes;
        }
    }
}
