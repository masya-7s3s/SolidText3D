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
        private const float RenderFontSize = 72f;
        private static readonly object RenderFontCacheGate = new object();
        private static readonly Dictionary<int, SixLabors.Fonts.Font> RenderFontCache = new Dictionary<int, SixLabors.Fonts.Font>();

        /// <summary>
        /// 指定したパラメータに基づいてテキストの 3D メッシュを生成する。
        /// </summary>
        /// <param name="p">メッシュ生成パラメータ。</param>
        /// <returns>生成された Unity Mesh。テキストが空またはフォントが読み込めない場合は空メッシュを返す。</returns>
        public static Mesh Build(MeshGenerationParams p)
        {
            byte[] fontBytes = GetFontBytes(p);
            if (fontBytes == null || fontBytes.Length == 0)
            {
                Debug.LogWarning("[SolidText3D] フォントバイト配列が取得できませんでした。空メッシュを返します。");
                return new Mesh();
            }

            var request = new TextStateRequest(0L, default, p.Text, fontBytes, p, false, 0f, 0f, null, OutlineDisplayMode.Donut, ObjectMode.SingleObject);
            return MeshExtruder.CreateMesh(PrepareDisplayResult(request).BodyMeshData);
        }

        internal static PreparedDisplayResult PrepareDisplayResult(TextStateRequest request, PreparedDisplayResult reusableResult = null)
        {
            Profiler.BeginSample("GlyphMeshBuilder.Build");
            try
            {
                var prepared = new PreparedDisplayResult
                {
                    Version = request.Version,
                    Signature = request.Signature
                };

                if (string.IsNullOrEmpty(request.Text))
                {
                    prepared.ClearsDisplay = true;
                    return prepared;
                }

                if (request.FontData == null || request.FontData.Length == 0)
                    throw new InvalidDataException("FontData is required to prepare display result.");

                var glyphs = RenderGlyphs(request.Signature.FontSourceId, request.FontData, request.GenerationParams);
                if (glyphs == null || glyphs.Count == 0)
                {
                    prepared.ClearsDisplay = true;
                    return prepared;
                }

                ApplyLayout(glyphs, request.GenerationParams);

                if (request.ObjectMode == ObjectMode.PerCharacter)
                {
                    var combinedBodyData = MeshExtruder.BuildCombinedData(glyphs, request.GenerationParams);
                    var anchorOffset = GetAnchorOffset(combinedBodyData, request.GenerationParams);
                    prepared.PerCharacterMeshData = BuildPerCharacterMeshData(glyphs, request.GenerationParams, anchorOffset, reusableResult, request.Signature, out List<Vector3> perCharacterOffsets);
                    prepared.PerCharacterOffsets = perCharacterOffsets;
                    prepared.BodyMeshData = default;

                    if (request.OutlineEnabled)
                    {
                        prepared.OutlineMeshData = OutlineMeshBuilder.BuildData(glyphs, request.CreateOutlineSettings(), request.GenerationParams.ExtrusionDepth, request.GenerationParams.FontSize, request.GenerationParams.DepthAnchor);
                        MeshExtruder.ApplyOffset(prepared.OutlineMeshData, anchorOffset);
                    }

                    prepared.ClearsDisplay = prepared.PerCharacterMeshData == null || prepared.PerCharacterMeshData.Count == 0;
                    return prepared;
                }

                prepared.BodyMeshData = MeshExtruder.BuildCombinedData(glyphs, request.GenerationParams);
                var bodyOffset = GetAnchorOffset(prepared.BodyMeshData, request.GenerationParams);
                MeshExtruder.ApplyOffset(prepared.BodyMeshData, bodyOffset);

                if (request.OutlineEnabled)
                {
                    prepared.OutlineMeshData = OutlineMeshBuilder.BuildData(glyphs, request.CreateOutlineSettings(), request.GenerationParams.ExtrusionDepth, request.GenerationParams.FontSize, request.GenerationParams.DepthAnchor);
                    MeshExtruder.ApplyOffset(prepared.OutlineMeshData, bodyOffset);
                }

                prepared.ClearsDisplay = prepared.BodyMeshData.Vertices == null || prepared.BodyMeshData.Vertices.Count == 0;
                return prepared;
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

            var allGlyphs = RenderGlyphs(0, fontBytes, p);
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

        private static void ApplyLayout(List<GlyphContour> glyphs, MeshGenerationParams p)
        {
            if (p.WritingMode == WritingMode.Vertical)
                LayoutEngine.ApplyVerticalLayout(glyphs, p);
            else
                LayoutEngine.ApplyHorizontalLayout(glyphs, p);
        }

        private static List<GlyphMeshData> BuildPerCharacterMeshData(
            List<GlyphContour> glyphs,
            MeshGenerationParams p,
            Vector3 anchorOffset,
            PreparedDisplayResult reusableResult,
            DisplayResultSignature currentSignature,
            out List<Vector3> perCharacterOffsets)
        {
            var preparedMeshes = new List<GlyphMeshData>();
            perCharacterOffsets = new List<Vector3>();
            bool canReuse = reusableResult != null
                && reusableResult.PerCharacterMeshData != null
                && reusableResult.PerCharacterOffsets != null
                && reusableResult.Signature.CanReusePerCharacterLayoutWith(currentSignature);

            foreach (var glyph in glyphs)
            {
                if (!glyph.IsVisible)
                    continue;

                int visibleIndex = preparedMeshes.Count;
                Vector3 currentOffset = glyph.Offset + anchorOffset;
                perCharacterOffsets.Add(currentOffset);

                bool reuseCurrentGlyph = canReuse
                    && visibleIndex < reusableResult.PerCharacterMeshData.Count
                    && visibleIndex < reusableResult.PerCharacterOffsets.Count
                    && visibleIndex < reusableResult.Signature.Text.Length
                    && visibleIndex < currentSignature.Text.Length
                    && reusableResult.Signature.Text[visibleIndex] == currentSignature.Text[visibleIndex]
                    && reusableResult.PerCharacterOffsets[visibleIndex] == currentOffset;

                if (reuseCurrentGlyph)
                {
                    preparedMeshes.Add(reusableResult.PerCharacterMeshData[visibleIndex]);
                    continue;
                }

                var meshData = MeshExtruder.BuildGlyphMesh(glyph, p.ExtrusionDepth, p.OutlineWidth);
                MeshExtruder.ApplyOffset(meshData, currentOffset);
                preparedMeshes.Add(meshData);
            }

            return preparedMeshes;
        }

        private static Vector3 GetAnchorOffset(GlyphMeshData meshData, MeshGenerationParams p)
        {
            if (meshData.Vertices == null || meshData.Vertices.Count == 0)
                return Vector3.zero;

            var offset = LayoutEngine.CalculateAnchorOffset(MeshExtruder.CalculateBounds(meshData), p);
            if (p.WritingMode == WritingMode.Horizontal)
                return new Vector3(0f, offset.y, offset.z);

            if (p.WritingMode == WritingMode.Vertical)
                return new Vector3(offset.x, 0f, offset.z);

            return offset;
        }

        private static List<GlyphContour> RenderGlyphs(int fontSourceId, byte[] fontBytes, MeshGenerationParams p)
        {
            var font = GetOrCreateRenderFont(fontSourceId, fontBytes);

            // FontSize=1 のとき em スクエア(=RenderFontSize)が 1 Unity unit になるようスケーリング
            float scale = (p.FontSize > 0f ? p.FontSize : 1f) / RenderFontSize;

            // 縦書きモードの場合、1文字ずつレンダリングして輪郭を原点基準に正規化する
            // (TextRenderer.RenderTextTo は横書き座標で全文字をまとめて出力するため)
            if (p.WritingMode == WritingMode.Vertical)
            {
                var result = new List<GlyphContour>();
                var singleCharOptions = new TextOptions(font);
                for (int i = 0; i < p.Text.Length;)
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
                        i++;
                        continue;
                    }

                    int codeUnitCount = 1;
                    if (i + 1 < p.Text.Length && char.IsSurrogatePair(p.Text[i], p.Text[i + 1]))
                        codeUnitCount = 2;

                    string ch = p.Text.Substring(i, codeUnitCount);
                    var renderer = new GlyphContourBuilder(p.BezierErrorThreshold, scale);
                    TextRenderer.RenderTextTo(renderer, ch, singleCharOptions);
                    if (renderer.GlyphContours.Count == 0)
                    {
                        i += codeUnitCount;
                        continue;
                    }

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
                    i += codeUnitCount;
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

        private static SixLabors.Fonts.Font GetOrCreateRenderFont(int fontSourceId, byte[] fontBytes)
        {
            if (fontBytes == null || fontBytes.Length == 0)
                throw new InvalidDataException("FontData is required to render glyphs.");

            if (fontSourceId != 0)
            {
                lock (RenderFontCacheGate)
                {
                    if (RenderFontCache.TryGetValue(fontSourceId, out SixLabors.Fonts.Font cachedFont))
                        return cachedFont;
                }
            }

            var collection = new FontCollection();
            FontFamily family;
            using (var ms = new MemoryStream(fontBytes))
            {
                family = collection.Add((System.IO.Stream)ms);
            }

            SixLabors.Fonts.Font font = family.CreateFont(RenderFontSize);
            if (fontSourceId != 0)
            {
                lock (RenderFontCacheGate)
                {
                    RenderFontCache[fontSourceId] = font;
                }
            }

            return font;
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
