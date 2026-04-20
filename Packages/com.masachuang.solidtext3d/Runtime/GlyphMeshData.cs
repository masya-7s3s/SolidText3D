using System.Collections.Generic;
using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// 単一グリフの 3D メッシュ構成データを保持する一時的な値型。
    /// MeshExtruder.BuildGlyphMesh() が返し、MeshExtruder.Build() 内で
    /// 全グリフ分を結合して最終 UnityEngine.Mesh を生成する際に使用される。
    /// </summary>
    /// <remarks>
    /// GC 正当化: List&lt;&gt; フィールドはメッシュ生成時（パラメータ変更時のみ）に
    /// 一度だけアロケーションが発生する。LateUpdate() 内では _isDirty チェックのみを行い、
    /// false の場合はアロケーションが一切発生しない（憲法 V 準拠）。
    /// </remarks>
    public struct GlyphMeshData
    {
        /// <summary>グリフの頂点リスト（前面・背面・側面をすべて含む）</summary>
        public List<Vector3> Vertices;

        /// <summary>三角形インデックスリスト（3 要素ごとに 1 三角形）</summary>
        public List<int> Triangles;

        /// <summary>各頂点の法線ベクトル</summary>
        public List<Vector3> Normals;

        /// <summary>
        /// 文字配置計算で決定したこのグリフの配置オフセット（他グリフとの結合時に加算）
        /// </summary>
        public Vector3 Offset;
    }
}
