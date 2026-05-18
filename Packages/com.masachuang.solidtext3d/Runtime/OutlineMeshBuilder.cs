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

        internal static Mesh Build(List<GlyphContour> glyphs, OutlineSettings settings, float bodyExtrusionDepth, float fontSize, DepthAnchor depthAnchor = DepthAnchor.Front)
        {
            return MeshExtruder.CreateMesh(BuildData(glyphs, settings, bodyExtrusionDepth, fontSize, depthAnchor));
        }

        internal static GlyphMeshData BuildData(List<GlyphContour> glyphs, OutlineSettings settings, float bodyExtrusionDepth, float fontSize, DepthAnchor depthAnchor = DepthAnchor.Front)
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

                    float outlineThickness = Mathf.Abs(bodyExtrusionDepth) * settings.Thickness;
                    float frontZ;
                    float backZ;
                    bool includeBackCap = outlineThickness > 0f;
                    bool emitRingBackCap = settings.DisplayMode != OutlineDisplayMode.BackFilled;

                    if (!includeBackCap)
                    {
                        frontZ = 0f;
                        backZ = 0f;
                    }
                    else
                    {
                        ResolveDepthAnchoredRange(bodyExtrusionDepth, outlineThickness, depthAnchor, out frontZ, out backZ);

                        if (settings.DisplayMode == OutlineDisplayMode.BackFilled)
                        {
                            float extrusionDirection = GetExtrusionDirection(bodyExtrusionDepth);
                            float inwardOffset = Mathf.Min(ZFightEpsilon, outlineThickness * 0.5f);
                            backZ -= extrusionDirection * inwardOffset;
                            emitRingBackCap = false;
                        }
                    }

                    var glyphMeshData = MeshExtruder.BuildContourMeshData(profileSet.RingContoursEm, frontZ, backZ, includeBackCap, true, emitRingBackCap);
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

        private static void ResolveDepthAnchoredRange(float bodyExtrusionDepth, float outlineThickness, DepthAnchor depthAnchor, out float frontZ, out float backZ)
        {
            float bodyFrontZ = 0f;
            float bodyBackZ = -bodyExtrusionDepth;
            float extrusionDirection = GetExtrusionDirection(bodyExtrusionDepth);

            switch (depthAnchor)
            {
                case DepthAnchor.Center:
                    float bodyCenterZ = (bodyFrontZ + bodyBackZ) * 0.5f;
                    float halfSpan = extrusionDirection * outlineThickness * 0.5f;
                    frontZ = bodyCenterZ - halfSpan;
                    backZ = bodyCenterZ + halfSpan;
                    break;

                case DepthAnchor.Back:
                    backZ = bodyBackZ;
                    frontZ = bodyBackZ - (extrusionDirection * outlineThickness);
                    break;

                case DepthAnchor.Front:
                default:
                    frontZ = bodyFrontZ;
                    backZ = bodyFrontZ + (extrusionDirection * outlineThickness);
                    break;
            }
        }

        private static float GetExtrusionDirection(float bodyExtrusionDepth)
        {
            float bodyBackZ = -bodyExtrusionDepth;
            if (Mathf.Approximately(bodyBackZ, 0f))
                return -1f;

            return Mathf.Sign(bodyBackZ);
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