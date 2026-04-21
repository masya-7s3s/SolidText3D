namespace MasaChuang.SolidText3D
{
    /// <summary>テキストメッシュの水平方向アンカー位置。</summary>
    public enum HorizontalAnchor
    {
        Left,    // テキスト左端が原点
        Center,  // テキスト中心が原点
        Right    // テキスト右端が原点
    }

    /// <summary>テキストメッシュの垂直方向アンカー位置。</summary>
    public enum VerticalAnchor
    {
        Upper,   // テキスト上端が原点
        Middle,  // テキスト中央が原点
        Lower    // テキスト下端が原点
    }

    /// <summary>テキストメッシュの奥行き方向アンカー位置。</summary>
    public enum DepthAnchor
    {
        Front,   // 前面が原点（Z=0）
        Center,  // 中央が原点（Z=-depth/2）
        Back     // 背面が原点（Z=-depth）
    }

    /// <summary>テキストの書字方向。</summary>
    public enum WritingMode
    {
        Horizontal,  // 横書き（デフォルト）
        Vertical     // 縦書き（上→下、右→左の列方向）
    }

    /// <summary>テキストの GameObject 生成モード。</summary>
    public enum ObjectMode
    {
        SingleObject,   // テキスト全体で 1 つの Mesh（デフォルト）
        PerCharacter    // 文字ごとに子 GameObject を生成
    }
}
