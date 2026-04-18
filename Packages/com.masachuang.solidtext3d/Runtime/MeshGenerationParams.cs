using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// GlyphMeshBuilder.Build() に渡すメッシュ生成パラメータ。
    /// </summary>
    public struct MeshGenerationParams
    {
        /// <summary>生成対象テキスト</summary>
        public string Text;

        /// <summary>フォントファイルの絶対パス（FontData と排他）</summary>
        public string FontPath;

        /// <summary>フォントバイナリデータ（FontPath と排他）</summary>
        public byte[] FontData;

        /// <summary>押し出し深さ（0 以上）</summary>
        public float ExtrusionDepth;

        /// <summary>アウトライン幅（0 以上）</summary>
        public float OutlineWidth;

        /// <summary>文字間隔（em 単位）</summary>
        public float LetterSpacing;

        /// <summary>行間倍率</summary>
        public float LineSpacing;

        /// <summary>Bezier 離散化誤差（省略時: 0.0005f）</summary>
        public float BezierErrorThreshold;
    }
}
