using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;
using UnityEngine.Profiling;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// Clipper2 を使って outline 用の canonical profile を構築する。
    /// </summary>
    internal static class OutlineContourBuilder
    {
        private const double Scale = 1000.0;
        private const float AreaEpsilon = 0.0001f;

        internal static OutlineProfileSet BuildProfiles(GlyphContour glyph, float offsetAmount, float fontSize)
        {
            Profiler.BeginSample("OutlineContourBuilder.BuildProfiles");
            try
            {
                var profileSet = new OutlineProfileSet();
                if (glyph == null || glyph.Contours == null || glyph.Contours.Count == 0)
                    return profileSet;

                profileSet.OriginalFilledContoursEm = CanonicalizeFilledContours(CloneContours(glyph.Contours));
                if (offsetAmount <= 0f || fontSize <= 0f)
                    return profileSet;

                var originalPaths = ToPaths64(profileSet.OriginalFilledContoursEm);
                if (originalPaths.Count == 0)
                    return profileSet;

                float offsetAmountEm = offsetAmount / fontSize;
                var clipperOffset = new ClipperOffset();
                clipperOffset.AddPaths(originalPaths, JoinType.Round, EndType.Polygon);

                var offsetPaths = new Paths64();
                clipperOffset.Execute(offsetAmountEm * Scale, offsetPaths);
                offsetPaths = Clipper.Union(offsetPaths, FillRule.NonZero);
                profileSet.OffsetFilledContoursEm = NormalizeContours(ToContours(offsetPaths));

                var ringPaths = Clipper.Difference(offsetPaths, originalPaths, FillRule.NonZero);
                profileSet.RingContoursEm = NormalizeContours(ToContours(ringPaths));
                return profileSet;
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static List<List<Vector2>> CanonicalizeFilledContours(List<List<Vector2>> contours)
        {
            var normalizedContours = NormalizeContours(contours);
            var normalizedPaths = ToPaths64(normalizedContours);
            if (normalizedPaths.Count == 0)
                return new List<List<Vector2>>();

            var unionPaths = Clipper.Union(normalizedPaths, FillRule.NonZero);
            return NormalizeContours(ToContours(unionPaths));
        }

        private static List<List<Vector2>> CloneContours(List<List<Vector2>> contours)
        {
            var cloned = new List<List<Vector2>>(contours.Count);
            foreach (var contour in contours)
            {
                var clone = new List<Vector2>(contour.Count);
                for (int i = 0; i < contour.Count; i++)
                    clone.Add(contour[i]);
                cloned.Add(clone);
            }

            return cloned;
        }

        private static Paths64 ToPaths64(List<List<Vector2>> contours)
        {
            var paths = new Paths64();
            foreach (var contour in contours)
            {
                if (contour == null || contour.Count < 3)
                    continue;

                var path = new Path64(contour.Count);
                for (int i = 0; i < contour.Count; i++)
                {
                    path.Add(new Point64(
                        (long)System.Math.Round(contour[i].x * Scale),
                        (long)System.Math.Round(contour[i].y * Scale)));
                }

                paths.Add(path);
            }

            return paths;
        }

        private static List<List<Vector2>> ToContours(Paths64 paths)
        {
            var contours = new List<List<Vector2>>(paths.Count);
            foreach (var path in paths)
            {
                var contour = new List<Vector2>(path.Count);
                for (int i = 0; i < path.Count; i++)
                {
                    contour.Add(new Vector2(
                        (float)(path[i].X / Scale),
                        (float)(path[i].Y / Scale)));
                }

                contours.Add(contour);
            }

            return contours;
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

                if (Mathf.Abs(GetSignedArea(contour)) <= AreaEpsilon)
                    continue;

                filtered.Add(new List<Vector2>(contour));
            }

            if (filtered.Count == 0)
                return normalized;

            filtered.Sort((left, right) => Mathf.Abs(GetSignedArea(right)).CompareTo(Mathf.Abs(GetSignedArea(left))));

            bool reverseAllContours = GetSignedArea(filtered[0]) < 0f;
            for (int index = 0; index < filtered.Count; index++)
            {
                var contour = filtered[index];
                if (reverseAllContours)
                    contour.Reverse();

                normalized.Add(contour);
            }

            return normalized;
        }

        private static bool IsHoleContour(List<List<Vector2>> sortedContours, int index)
        {
            int containingCount = 0;
            var samplePoint = GetInteriorPoint(sortedContours[index]);
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
            if (Mathf.Abs(signedArea) <= AreaEpsilon)
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

        private static Vector2 GetInteriorPoint(List<Vector2> contour)
        {
            for (int i = 0; i < contour.Count; i++)
            {
                int previous = (i + contour.Count - 1) % contour.Count;
                int next = (i + 1) % contour.Count;
                var a = contour[previous];
                var b = contour[i];
                var c = contour[next];

                if (Mathf.Abs(Cross(b - a, c - a)) <= AreaEpsilon)
                    continue;

                var centroid = (a + b + c) / 3f;
                if (ContainsPoint(contour, centroid))
                    return centroid;
            }

            return GetCentroid(contour);
        }

        private static float Cross(Vector2 left, Vector2 right)
        {
            return (left.x * right.y) - (left.y * right.x);
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
    }
}