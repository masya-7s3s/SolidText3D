using System;
using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// アウトライン機能の設定を保持するシリアライズ可能クラス。
    /// </summary>
    /// <example>
    /// <code>
    /// var settings = new OutlineSettings
    /// {
    ///     Enabled = true,
    ///     OffsetAmount = 0.05f,
    ///     Thickness = 0.25f,
    ///     DisplayMode = OutlineDisplayMode.Donut
    /// };
    /// </code>
    /// </example>
    [Serializable]
    public sealed class OutlineSettings
    {
        /// <summary>アウトラインを有効にするかどうか。</summary>
        public bool Enabled = false;

        /// <summary>アウトラインの外側オフセット量。0 以上。</summary>
        public float OffsetAmount = 0.05f;

        /// <summary>アウトラインの奥行き。0 以上。</summary>
        public float Thickness = 0.25f;

        /// <summary>アウトライン専用マテリアル。null の場合は本体 sharedMaterial を使用する。</summary>
        public Material Material = null;

        /// <summary>アウトラインの表示モード。</summary>
        public OutlineDisplayMode DisplayMode = OutlineDisplayMode.Donut;
    }
}