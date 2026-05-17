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
            return MeshExtruder.CreateMesh(BuildData(glyphs, settings, bodyExtrusionDepth, fontSize));
        }

        internal static GlyphMeshData BuildData(List<GlyphContour> glyphs, OutlineSettings settings, float bodyExtrusionDepth, float fontSize)
        {
            Profiler.BeginSample("OutlineMeshBuilder.Build");
            try
            {
                var combined = new GlyphMeshData
                {
                    Vertices = new List<Vector3>(),
                    Triangles = new List<int>(),
                    Normals = new List<Vector3>(),
                    Offset = Vector3.zero
                };

                if (glyphs == null || glyphs.Count == 0 || settings == null || settings.OffsetAmount <= 0f)
                    return combined;

                for (int glyphIndex = 0; glyphIndex < glyphs.Count; glyphIndex++)
                {
                    var glyph = glyphs[glyphIndex];
                    var profileSet = OutlineContourBuilder.BuildProfiles(glyph, settings.OffsetAmount, fontSize);
                    if (profileSet.RingContoursEm == null || profileSet.RingContoursEm.Count == 0)
                        continue;

                    float frontZ;
                    float backZ;
                    bool includeBackCap = settings.Thickness > 0f;
                    bool frontRingFaceForward = true;
                    bool emitRingBackCap = settings.DisplayMode != OutlineDisplayMode.BackFilled;

                    if (!includeBackCap)
                    {
                        frontZ = 0f;
                        backZ = 0f;
                    }
                    else if (settings.DisplayMode == OutlineDisplayMode.BackFilled)
                    {
                        backZ = ZFightEpsilon;
                        frontZ = backZ - settings.Thickness;
                        frontRingFaceForward = false;
                        emitRingBackCap = false;
                    }
                    else
                    {
                        float bodyCenterZ = -bodyExtrusionDepth * 0.5f;
                        float halfThickness = settings.Thickness * 0.5f;
                        frontZ = bodyCenterZ + halfThickness;
                        backZ = bodyCenterZ - halfThickness;
                    }

                    var glyphMeshData = MeshExtruder.BuildContourMeshData(profileSet.RingContoursEm, frontZ, backZ, includeBackCap, frontRingFaceForward, emitRingBackCap);
                    if (includeBackCap && settings.DisplayMode == OutlineDisplayMode.BackFilled)
                    {
                        var rearInfillData = MeshExtruder.BuildCapMeshData(profileSet.OffsetFilledContoursEm, backZ, true);
                        AppendGlyphMeshData(glyphMeshData, rearInfillData);
                    }

                    AppendGlyphMeshData(combined, glyphMeshData, glyph.Offset);
                }

                return combined;
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

        private static void AppendGlyphMeshData(GlyphMeshData target, GlyphMeshData addition, Vector3 offset)
        {
            if (addition.Vertices == null || addition.Vertices.Count == 0)
                return;

            int vertexOffset = target.Vertices.Count;
            for (int i = 0; i < addition.Vertices.Count; i++)
                target.Vertices.Add(addition.Vertices[i] + offset);

            target.Normals.AddRange(addition.Normals);

            for (int i = 0; i < addition.Triangles.Count; i++)
                target.Triangles.Add(addition.Triangles[i] + vertexOffset);
        }
    }
}