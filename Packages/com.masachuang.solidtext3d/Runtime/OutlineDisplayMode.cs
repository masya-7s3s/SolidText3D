namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// アウトラインの奥行き構成モード。
    /// </summary>
    public enum OutlineDisplayMode
    {
        /// <summary>
        /// リング断面を文字本体の中央面を基準に前後へ均等配置する。
        /// </summary>
        /// <example>
        /// <code>
        /// component.OutlineDisplayMode = OutlineDisplayMode.Donut;
        /// </code>
        /// </example>
        Donut,

        /// <summary>
        /// front silhouette を維持したまま、背面側を埋めた構成で表示する。
        /// </summary>
        /// <example>
        /// <code>
        /// component.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
        /// </code>
        /// </example>
        BackFilled
    }
}