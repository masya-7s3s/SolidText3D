using System.Collections.Generic;
using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// outline 生成に必要な canonical 2D profile 群を保持する内部モデル。
    /// </summary>
    internal sealed class OutlineProfileSet
    {
        public List<List<Vector2>> OriginalFilledContoursEm { get; set; } = new List<List<Vector2>>();

        public List<List<Vector2>> OffsetFilledContoursEm { get; set; } = new List<List<Vector2>>();

        public List<List<Vector2>> RingContoursEm { get; set; } = new List<List<Vector2>>();
    }
}