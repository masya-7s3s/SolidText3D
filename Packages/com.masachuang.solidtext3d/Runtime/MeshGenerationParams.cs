namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// GlyphMeshBuilder.Build() に渡すメッシュ生成パラメータ。
    /// </summary>
    public struct MeshGenerationParams
    {
        /// <summary>生成対象テキスト</summary>
        public string Text;

        /// <summary>フォントバイナリデータ</summary>
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

        /// <summary>
        /// フォントサイズ（Unity ワールド単位）。1 = em スクエアの高さが 1 Unity unit。
        /// デフォルト値: 1.0f
        /// </summary>
        public float FontSize;

        /// <summary>水平アンカー位置。デフォルト: Left</summary>
        public HorizontalAnchor HorizontalAnchor;

        /// <summary>垂直アンカー位置。デフォルト: Lower</summary>
        public VerticalAnchor VerticalAnchor;

        /// <summary>奥行きアンカー位置。デフォルト: Front</summary>
        public DepthAnchor DepthAnchor;

        /// <summary>書字方向。デフォルト: Horizontal</summary>
        public WritingMode WritingMode;

        /// <summary>横書き時の最大幅（0 = 無制限）</summary>
        public float MaxWidth;

        /// <summary>縦書き時の最大高さ（0 = 無制限）</summary>
        public float MaxHeight;

        /// <summary>縦書き時の列幅（0 = FontSize × 1.1f 自動）</summary>
        public float VerticalColumnWidth;

        /// <summary>縦書き時に ASCII 英数字を 90 度回転するか</summary>
        public bool RotateAsciiInVertical;
    }
}
