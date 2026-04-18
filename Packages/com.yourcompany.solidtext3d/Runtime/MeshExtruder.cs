using System.Collections.Generic;
using LibTessDotNet;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// グリフ輪郭データから 3D メッシュを生成する静的クラス。
    /// LibTessDotNet を使用して前面ポリゴンを三角分割し、背面・側面を生成する。
    /// </summary>
    public static class MeshExtruder
    {
        /// <summary>
        /// 複数のグリフ輪郭から結合メッシュを生成する。
        /// </summary>
        /// <param name="glyphs">グリフ輪郭データのリスト。</param>
        /// <param name="p">メッシュ生成パラメータ。</param>
        /// <returns>生成された Unity Mesh。</returns>
        public static Mesh Build(List<GlyphContour> glyphs, MeshGenerationParams p)
        {
            // GC 正当化: このアロケーションはパラメータ変更時（メッシュ再生成時）のみ発生する
            // LateUpdate() 内ではダーティフラグチェックのみを行い、アロケーションは発生しない（憲法 V 準拠）
            var allVertices = new List<Vector3>();
            var allTriangles = new List<int>();
            var allNormals = new List<Vector3>();

            foreach (var glyph in glyphs)
            {
                var data = BuildGlyphMesh(glyph, p.ExtrusionDepth, p.OutlineWidth);
                int indexOffset = allVertices.Count;
                var glyphOffset = glyph.Offset;

                foreach (var v in data.Vertices)
                    allVertices.Add(v + glyphOffset);
                allNormals.AddRange(data.Normals);

                for (int i = 0; i < data.Triangles.Count; i++)
                {
                    allTriangles.Add(data.Triangles[i] + indexOffset);
                }
            }

            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(allVertices);
            mesh.SetNormals(allNormals);
            mesh.SetTriangles(allTriangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 単一グリフ輪郭から 3D メッシュデータを生成する。
        /// </summary>
        /// <param name="glyph">グリフ輪郭データ。</param>
        /// <param name="extrusionDepth">押し出し深さ。</param>
        /// <param name="outlineWidth">アウトライン幅（未使用、将来拡張用）。</param>
        /// <returns>生成されたグリフメッシュデータ。</returns>
        public static GlyphMeshData BuildGlyphMesh(GlyphContour glyph, float extrusionDepth, float outlineWidth)
        {
            Profiler.BeginSample("MeshExtruder.BuildGlyphMesh");
            try
            {
            var data = new GlyphMeshData
            {
                Vertices = new List<Vector3>(),
                Triangles = new List<int>(),
                Normals = new List<Vector3>(),
                Offset = Vector3.zero
            };

            if (glyph.Contours == null || glyph.Contours.Count == 0)
                return data;

            // 前面三角分割（LibTessDotNet EvenOdd WindingRule）
            var tess = new Tess();
            foreach (var contour in glyph.Contours)
            {
                if (contour.Count < 3) continue;
                var tessVertices = new ContourVertex[contour.Count];
                for (int i = 0; i < contour.Count; i++)
                {
                    tessVertices[i] = new ContourVertex
                    {
                        Position = new Vec3 { X = contour[i].x, Y = contour[i].y, Z = 0f }
                    };
                }
                tess.AddContour(tessVertices, ContourOrientation.Original);
            }

            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            int frontStart = data.Vertices.Count;
            // 前面頂点（z=0）
            for (int i = 0; i < tess.VertexCount; i++)
            {
                var v = tess.Vertices[i].Position;
                data.Vertices.Add(new Vector3(v.X, v.Y, 0f));
                data.Normals.Add(Vector3.forward);
            }

            // 前面インデックス
            int elemCount = tess.ElementCount;
            for (int i = 0; i < elemCount; i++)
            {
                int idx0 = tess.Elements[i * 3 + 0] + frontStart;
                int idx1 = tess.Elements[i * 3 + 1] + frontStart;
                int idx2 = tess.Elements[i * 3 + 2] + frontStart;
                data.Triangles.Add(idx0);
                data.Triangles.Add(idx1);
                data.Triangles.Add(idx2);
            }

            if (extrusionDepth <= 0f)
                return data;

            // 背面頂点（z=-extrusionDepth）
            int backStart = data.Vertices.Count;
            for (int i = 0; i < tess.VertexCount; i++)
            {
                var v = tess.Vertices[i].Position;
                data.Vertices.Add(new Vector3(v.X, v.Y, -extrusionDepth));
                data.Normals.Add(Vector3.back);
            }

            // 背面インデックス（前面と逆巻き）
            for (int i = 0; i < elemCount; i++)
            {
                int idx0 = tess.Elements[i * 3 + 0] + backStart;
                int idx1 = tess.Elements[i * 3 + 1] + backStart;
                int idx2 = tess.Elements[i * 3 + 2] + backStart;
                data.Triangles.Add(idx0);
                data.Triangles.Add(idx2);
                data.Triangles.Add(idx1);
            }

            // 側面クワッドを輪郭エッジから生成
            foreach (var contour in glyph.Contours)
            {
                if (contour.Count < 2) continue;
                for (int i = 0; i < contour.Count; i++)
                {
                    int next = (i + 1) % contour.Count;
                    var a = contour[i];
                    var b = contour[next];

                    var aFront = new Vector3(a.x, a.y, 0f);
                    var bFront = new Vector3(b.x, b.y, 0f);
                    var aBack = new Vector3(a.x, a.y, -extrusionDepth);
                    var bBack = new Vector3(b.x, b.y, -extrusionDepth);

                    // エッジ法線（輪郭の外側方向）
                    var edge = new Vector2(b.x - a.x, b.y - a.y);
                    var normal = new Vector3(edge.y, -edge.x, 0f).normalized;

                    int sideBase = data.Vertices.Count;
                    data.Vertices.Add(aFront);
                    data.Vertices.Add(bFront);
                    data.Vertices.Add(bBack);
                    data.Vertices.Add(aBack);
                    data.Normals.Add(normal);
                    data.Normals.Add(normal);
                    data.Normals.Add(normal);
                    data.Normals.Add(normal);

                    // 2 つの三角形でクワッドを構成
                    data.Triangles.Add(sideBase + 0);
                    data.Triangles.Add(sideBase + 1);
                    data.Triangles.Add(sideBase + 2);
                    data.Triangles.Add(sideBase + 0);
                    data.Triangles.Add(sideBase + 2);
                    data.Triangles.Add(sideBase + 3);
                }
            }

            return data;
            }
            finally
            {
                Profiler.EndSample();
            }
        }
    }
}
