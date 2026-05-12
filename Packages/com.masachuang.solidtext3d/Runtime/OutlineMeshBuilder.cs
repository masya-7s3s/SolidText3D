using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// outline 用の ring contour からメッシュを構築する。
    /// </summary>
    internal static class OutlineMeshBuilder
    {
        private const float ZFightEpsilon = 0.0001f;

        internal static Mesh Build(List<GlyphContour> glyphs, OutlineSettings settings, float bodyExtrusionDepth, float fontSize)
        {
            Profiler.BeginSample("OutlineMeshBuilder.Build");
            try
            {
                var mesh = new Mesh();
                mesh.indexFormat = IndexFormat.UInt32;

                if (glyphs == null || glyphs.Count == 0 || settings == null || settings.OffsetAmount <= 0f)
                    return mesh;

                var allVertices = new List<Vector3>();
                var allTriangles = new List<int>();
                var allNormals = new List<Vector3>();

                for (int glyphIndex = 0; glyphIndex < glyphs.Count; glyphIndex++)
                {
                    var glyph = glyphs[glyphIndex];
                    var profileSet = OutlineContourBuilder.BuildProfiles(glyph, settings.OffsetAmount, fontSize);
                    if (profileSet.RingContoursEm == null || profileSet.RingContoursEm.Count == 0)
                        continue;

                    float frontZ;
                    float backZ;
                    bool includeBackCap = settings.Thickness > 0f;

                    if (!includeBackCap)
                    {
                        frontZ = 0f;
                        backZ = 0f;
                    }
                    else if (settings.DisplayMode == OutlineDisplayMode.BackFilled)
                    {
                        backZ = -(bodyExtrusionDepth + ZFightEpsilon);
                        frontZ = backZ + settings.Thickness;
                    }
                    else
                    {
                        float bodyCenterZ = -bodyExtrusionDepth * 0.5f;
                        float halfThickness = settings.Thickness * 0.5f;
                        frontZ = bodyCenterZ + halfThickness;
                        backZ = bodyCenterZ - halfThickness;
                    }

                    bool frontRingFaceForward = true;
                    bool emitRingBackCap = settings.DisplayMode != OutlineDisplayMode.BackFilled;
                    var glyphMeshData = MeshExtruder.BuildContourMeshData(profileSet.RingContoursEm, frontZ, backZ, includeBackCap, frontRingFaceForward, emitRingBackCap);
                    if (includeBackCap && settings.DisplayMode == OutlineDisplayMode.BackFilled)
                    {
                        var rearInfillData = MeshExtruder.BuildCapMeshData(profileSet.OffsetFilledContoursEm, backZ, false);
                        AppendGlyphMeshData(glyphMeshData, rearInfillData);
                    }

                    int vertexOffset = allVertices.Count;

                    for (int i = 0; i < glyphMeshData.Vertices.Count; i++)
                        allVertices.Add(glyphMeshData.Vertices[i] + glyph.Offset);

                    allNormals.AddRange(glyphMeshData.Normals);

                    for (int i = 0; i < glyphMeshData.Triangles.Count; i++)
                        allTriangles.Add(glyphMeshData.Triangles[i] + vertexOffset);
                }

                mesh.SetVertices(allVertices);
                mesh.SetNormals(allNormals);
                mesh.SetTriangles(allTriangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static void AppendGlyphMeshData(GlyphMeshData target, GlyphMeshData addition)
        {
            if (addition.Vertices == null || addition.Vertices.Count == 0)
                return;

            int vertexOffset = target.Vertices.Count;
            target.Vertices.AddRange(addition.Vertices);
            target.Normals.AddRange(addition.Normals);

            for (int i = 0; i < addition.Triangles.Count; i++)
                target.Triangles.Add(addition.Triangles[i] + vertexOffset);
        }
    }
}