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

            // IsRotated: 縦書き ASCII 90 度時計回り回転
            // 正規化後グリフ中心 (cx, cy) = (w/2, -h/2) 周りで回転
            // 90° CW: (dx,dy) → (dy, -dx)  ⟹  new_x = cx + (y-cy),  new_y = cy - (x-cx)
            var sourceContours = glyph.Contours;
            if (glyph.IsRotated && sourceContours.Count > 0)
            {
                float cx = glyph.Bounds.width * 0.5f;
                float cy = -glyph.Bounds.height * 0.5f;
                var rotated = new List<List<Vector2>>(sourceContours.Count);
                foreach (var contour in sourceContours)
                {
                    var rc = new List<Vector2>(contour.Count);
                    foreach (var v in contour)
                        rc.Add(new Vector2(cx + (v.y - cy), cy - (v.x - cx)));
                    rotated.Add(rc);
                }
                sourceContours = rotated;
            }

            return BuildContourMeshData(NormalizeContours(sourceContours), 0f, -extrusionDepth, extrusionDepth > 0f);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        internal static GlyphMeshData BuildContourMeshData(List<List<Vector2>> contours, float frontZ, float backZ, bool includeBackCap, bool frontFaceForward = true, bool emitBackCapSurface = true)
        {
            var data = new GlyphMeshData
            {
                Vertices = new List<Vector3>(),
                Triangles = new List<int>(),
                Normals = new List<Vector3>(),
                Offset = Vector3.zero
            };

            if (contours == null || contours.Count == 0)
                return data;

            // 前面三角分割（LibTessDotNet EvenOdd WindingRule）
            var tess = new Tess();
            foreach (var contour in contours)
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

            tess.Tessellate(WindingRule.NonZero, ElementType.Polygons, 3);

            int frontStart = data.Vertices.Count;
            // 前面頂点（z=0）
            for (int i = 0; i < tess.VertexCount; i++)
            {
                var v = tess.Vertices[i].Position;
                data.Vertices.Add(new Vector3(v.X, v.Y, frontZ));
                data.Normals.Add(frontFaceForward ? Vector3.forward : Vector3.back);
            }

            // 前面インデックス
            // 正規化済み contour（outer=CCW, hole=CW）に対しては
            // LibTessDotNet の出力順をそのまま使うと front cap が可視側を向く。
            int elemCount = tess.ElementCount;
            for (int i = 0; i < elemCount; i++)
            {
                int e0 = tess.Elements[i * 3 + 0];
                int e1 = tess.Elements[i * 3 + 1];
                int e2 = tess.Elements[i * 3 + 2];
                if (e0 < 0 || e1 < 0 || e2 < 0) continue;
                data.Triangles.Add(e0 + frontStart);
                data.Triangles.Add((frontFaceForward ? e1 : e2) + frontStart);
                data.Triangles.Add((frontFaceForward ? e2 : e1) + frontStart);
            }

            if (!includeBackCap || Mathf.Approximately(frontZ, backZ))
                return data;

            // 背面頂点（z=-extrusionDepth）
            int backStart = data.Vertices.Count;
            for (int i = 0; i < tess.VertexCount; i++)
            {
                var v = tess.Vertices[i].Position;
                data.Vertices.Add(new Vector3(v.X, v.Y, backZ));
                data.Normals.Add(Vector3.back);
            }

            if (emitBackCapSurface)
            {
                // 背面インデックス（前面と逆巻き）
                for (int i = 0; i < elemCount; i++)
                {
                    int e0 = tess.Elements[i * 3 + 0];
                    int e1 = tess.Elements[i * 3 + 1];
                    int e2 = tess.Elements[i * 3 + 2];
                    if (e0 < 0 || e1 < 0 || e2 < 0) continue;
                    data.Triangles.Add(e0 + backStart);
                    data.Triangles.Add(e2 + backStart);
                    data.Triangles.Add(e1 + backStart);
                }
            }

            bool reverseSideWinding = frontZ < backZ;

            // 側面クワッドを輪郭エッジから生成
            foreach (var contour in contours)
            {
                if (contour.Count < 2) continue;
                for (int i = 0; i < contour.Count; i++)
                {
                    int next = (i + 1) % contour.Count;
                    var a = contour[i];
                    var b = contour[next];

                    var aFront = new Vector3(a.x, a.y, frontZ);
                    var bFront = new Vector3(b.x, b.y, frontZ);
                    var aBack = new Vector3(a.x, a.y, backZ);
                    var bBack = new Vector3(b.x, b.y, backZ);

                    var edge = new Vector2(b.x - a.x, b.y - a.y);
                    var normal = new Vector3(edge.y, -edge.x, 0f).normalized;

                    int sideBase = data.Vertices.Count;
                    data.Vertices.Add(aFront);  // 0
                    data.Vertices.Add(bFront);  // 1
                    data.Vertices.Add(bBack);   // 2
                    data.Vertices.Add(aBack);   // 3
                    data.Normals.Add(normal);
                    data.Normals.Add(normal);
                    data.Normals.Add(normal);
                    data.Normals.Add(normal);

                    if (!reverseSideWinding)
                    {
                        data.Triangles.Add(sideBase + 0);
                        data.Triangles.Add(sideBase + 2);
                        data.Triangles.Add(sideBase + 1);
                        data.Triangles.Add(sideBase + 0);
                        data.Triangles.Add(sideBase + 3);
                        data.Triangles.Add(sideBase + 2);
                    }
                    else
                    {
                        data.Triangles.Add(sideBase + 0);
                        data.Triangles.Add(sideBase + 1);
                        data.Triangles.Add(sideBase + 2);
                        data.Triangles.Add(sideBase + 0);
                        data.Triangles.Add(sideBase + 2);
                        data.Triangles.Add(sideBase + 3);
                    }
                }
            }

            return data;
        }

        private static List<List<Vector2>> NormalizeContours(List<List<Vector2>> contours)
        {
            var normalized = new List<List<Vector2>>();
            if (contours == null)
                return normalized;

            var filtered = new List<List<Vector2>>();
            foreach (var contour in contours)
            {
                if (contour == null || contour.Count < 3)
                    continue;

                if (Mathf.Abs(GetSignedArea(contour)) <= 0.0001f)
                    continue;

                filtered.Add(new List<Vector2>(contour));
            }

            filtered.Sort((left, right) => Mathf.Abs(GetSignedArea(right)).CompareTo(Mathf.Abs(GetSignedArea(left))));
            for (int index = 0; index < filtered.Count; index++)
            {
                var contour = filtered[index];
                bool isHole = IsHoleContour(filtered, index);
                float signedArea = GetSignedArea(contour);

                if (!isHole && signedArea < 0f)
                    contour.Reverse();
                else if (isHole && signedArea > 0f)
                    contour.Reverse();

                normalized.Add(contour);
            }

            return normalized;
        }

        private static bool IsHoleContour(List<List<Vector2>> sortedContours, int index)
        {
            int containingCount = 0;
            var samplePoint = GetCentroid(sortedContours[index]);
            for (int i = 0; i < index; i++)
            {
                if (ContainsPoint(sortedContours[i], samplePoint))
                    containingCount++;
            }

            return (containingCount & 1) == 1;
        }

        private static float GetSignedArea(List<Vector2> contour)
        {
            float signedArea = 0f;
            for (int i = 0; i < contour.Count; i++)
            {
                int next = (i + 1) % contour.Count;
                signedArea += (contour[i].x * contour[next].y) - (contour[next].x * contour[i].y);
            }

            return signedArea * 0.5f;
        }

        private static Vector2 GetCentroid(List<Vector2> contour)
        {
            float signedArea = GetSignedArea(contour);
            if (Mathf.Abs(signedArea) <= 0.0001f)
                return contour[0];

            float centroidX = 0f;
            float centroidY = 0f;
            for (int i = 0; i < contour.Count; i++)
            {
                int next = (i + 1) % contour.Count;
                float cross = (contour[i].x * contour[next].y) - (contour[next].x * contour[i].y);
                centroidX += (contour[i].x + contour[next].x) * cross;
                centroidY += (contour[i].y + contour[next].y) * cross;
            }

            float factor = 1f / (6f * signedArea);
            return new Vector2(centroidX * factor, centroidY * factor);
        }

        private static bool ContainsPoint(List<Vector2> contour, Vector2 point)
        {
            bool inside = false;
            int lastIndex = contour.Count - 1;
            for (int i = 0, j = lastIndex; i < contour.Count; j = i++)
            {
                bool intersects = ((contour[i].y > point.y) != (contour[j].y > point.y)) &&
                    (point.x < ((contour[j].x - contour[i].x) * (point.y - contour[i].y) / (contour[j].y - contour[i].y)) + contour[i].x);
                if (intersects)
                    inside = !inside;
            }

            return inside;
        }

        internal static GlyphMeshData BuildCapMeshData(List<List<Vector2>> contours, float z, bool faceForward)
        {
            var data = new GlyphMeshData
            {
                Vertices = new List<Vector3>(),
                Triangles = new List<int>(),
                Normals = new List<Vector3>(),
                Offset = Vector3.zero
            };

            if (contours == null || contours.Count == 0)
                return data;

            var tess = new Tess();
            foreach (var contour in contours)
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

            tess.Tessellate(WindingRule.NonZero, ElementType.Polygons, 3);

            var normal = faceForward ? Vector3.forward : Vector3.back;
            for (int i = 0; i < tess.VertexCount; i++)
            {
                var vertex = tess.Vertices[i].Position;
                data.Vertices.Add(new Vector3(vertex.X, vertex.Y, z));
                data.Normals.Add(normal);
            }

            for (int i = 0; i < tess.ElementCount; i++)
            {
                int e0 = tess.Elements[i * 3 + 0];
                int e1 = tess.Elements[i * 3 + 1];
                int e2 = tess.Elements[i * 3 + 2];
                if (e0 < 0 || e1 < 0 || e2 < 0) continue;

                if (faceForward)
                {
                    data.Triangles.Add(e0);
                    data.Triangles.Add(e1);
                    data.Triangles.Add(e2);
                }
                else
                {
                    data.Triangles.Add(e0);
                    data.Triangles.Add(e2);
                    data.Triangles.Add(e1);
                }
            }

            return data;
        }
    }
}
