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

        /// <summary>縦書き用の字送り高さ（SixLabors から取得できない場合は AdvanceWidth で代用）</summary>
        public float AdvanceHeight { get; set; }

        /// <summary>元テキスト中の文字インデックス（Per-Character プールの紐付け用）</summary>
        public int CharIndex { get; set; }

        /// <summary>可視文字かどうか（折り返し区切り等の非表示文字は false）</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>縦書き時に 90 度時計回り回転するかどうか（RotateAsciiInVertical が true の ASCII 文字）</summary>
        public bool IsRotated { get; set; } = false;
    }
}
