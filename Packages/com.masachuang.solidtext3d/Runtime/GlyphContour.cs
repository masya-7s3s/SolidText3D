using System.Collections.Generic;
using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// 単一グリフの輪郭データを保持する。
    /// </summary>
    public class GlyphContour
    {
        /// <summary>
        /// コンターの配列（1 グリフに複数コンターがある場合に対応、例: 'O', '口'）
        /// </summary>
        public List<List<Vector2>> Contours { get; set; } = new List<List<Vector2>>();

        /// <summary>グリフの送り幅（文字間隔計算に使用）</summary>
        public float AdvanceWidth { get; set; }

        /// <summary>グリフのバウンディングボックス</summary>
        public Rect Bounds { get; set; }

        /// <summary>ApplyLayout() で設定される文字配置オフセット（ワールド空間）</summary>
        public Vector3 Offset { get; set; }
    }
}
