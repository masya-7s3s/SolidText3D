using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// OutlineContourBuilder の foundational RED テスト。
    /// </summary>
    public class OutlineContourBuilderTests
    {
        private readonly struct RepresentativeGlyphCase
        {
            public RepresentativeGlyphCase(string label, GlyphContour glyph, float offset)
            {
                Label = label;
                Glyph = glyph;
                Offset = offset;
            }

            public string Label { get; }
            public GlyphContour Glyph { get; }
            public float Offset { get; }
        }

        private static GlyphContour MakeSquareContour(float size = 1f)
        {
            return new GlyphContour
            {
                Contours = new List<List<Vector2>>
                {
                    new List<Vector2>
                    {
                        new Vector2(0f, 0f),
                        new Vector2(size, 0f),
                        new Vector2(size, size),
                        new Vector2(0f, size),
                    }
                },
                AdvanceWidth = size,
                Bounds = new Rect(0f, 0f, size, size)
            };
        }

        private static GlyphContour MakeOShapeContour()
        {
            return new GlyphContour
            {
                Contours = new List<List<Vector2>>
                {
                    new List<Vector2>
                    {
                        new Vector2(0f, 0f),
                        new Vector2(4f, 0f),
                        new Vector2(4f, 4f),
                        new Vector2(0f, 4f),
                    },
                    new List<Vector2>
                    {
                        new Vector2(0.8f, 3.2f),
                        new Vector2(3.2f, 3.2f),
                        new Vector2(3.2f, 0.8f),
                        new Vector2(0.8f, 0.8f),
                    }
                },
                AdvanceWidth = 4f,
                Bounds = new Rect(0f, 0f, 4f, 4f)
            };
        }

        private static GlyphContour MakeBShapeContour()
        {
            return new GlyphContour
            {
                Contours = new List<List<Vector2>>
                {
                    new List<Vector2>
                    {
                        new Vector2(0f, 0f),
                        new Vector2(4f, 0f),
                        new Vector2(4f, 6f),
                        new Vector2(0f, 6f),
                    },
                    new List<Vector2>
                    {
                        new Vector2(0.8f, 4.7f),
                        new Vector2(3.1f, 4.7f),
                        new Vector2(3.1f, 3.3f),
                        new Vector2(0.8f, 3.3f),
                    },
                    new List<Vector2>
                    {
                        new Vector2(0.8f, 2.7f),
                        new Vector2(3.1f, 2.7f),
                        new Vector2(3.1f, 1.3f),
                        new Vector2(0.8f, 1.3f),
                    }
                },
                AdvanceWidth = 4f,
                Bounds = new Rect(0f, 0f, 4f, 6f)
            };
        }

        private static GlyphContour MakeEightShapeContour()
        {
            return MakeBShapeContour();
        }

        private static GlyphContour MakeHollowCjkContour(string label)
        {
            float width = label == "あ" ? 5f : 4f;
            float height = label == "あ" ? 5f : 4f;

            return new GlyphContour
            {
                Contours = new List<List<Vector2>>
                {
                    new List<Vector2>
                    {
                        new Vector2(0f, 0f),
                        new Vector2(width, 0f),
                        new Vector2(width, height),
                        new Vector2(0f, height),
                    },
                    new List<Vector2>
                    {
                        new Vector2(0.7f, height - 0.7f),
                        new Vector2(width - 0.7f, height - 0.7f),
                        new Vector2(width - 0.7f, 0.7f),
                        new Vector2(0.7f, 0.7f),
                    }
                },
                AdvanceWidth = width,
                Bounds = new Rect(0f, 0f, width, height)
            };
        }

        private static GlyphContour MakeOverlappingContours()
        {
            return new GlyphContour
            {
                Contours = new List<List<Vector2>>
                {
                    new List<Vector2>
                    {
                        new Vector2(0f, 0f),
                        new Vector2(2f, 0f),
                        new Vector2(2f, 2f),
                        new Vector2(0f, 2f),
                    },
                    new List<Vector2>
                    {
                        new Vector2(1f, 0.5f),
                        new Vector2(3f, 0.5f),
                        new Vector2(3f, 2.5f),
                        new Vector2(1f, 2.5f),
                    }
                },
                AdvanceWidth = 3f,
                Bounds = new Rect(0f, 0f, 3f, 2.5f)
            };
        }

        private static Type RequireOutlineContourBuilderType()
        {
            var type = typeof(SolidText3DComponent).Assembly.GetType("MasaChuang.SolidText3D.OutlineContourBuilder");
            Assert.IsNotNull(type, "OutlineContourBuilder が未実装です。");
            return type;
        }

        private static MethodInfo RequireBuildProfilesMethod()
        {
            var method = RequireOutlineContourBuilderType().GetMethod(
                "BuildProfiles",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(GlyphContour), typeof(float), typeof(float) },
                null);

            Assert.IsNotNull(method,
                "OutlineContourBuilder.BuildProfiles(GlyphContour glyph, float offsetAmount, float fontSize) が必要です。");
            return method;
        }

        private static object InvokeBuildProfiles(GlyphContour glyph, float offsetAmount, float fontSize)
        {
            return RequireBuildProfilesMethod().Invoke(null, new object[] { glyph, offsetAmount, fontSize });
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

        private static float GetFilledArea(List<List<Vector2>> contours)
        {
            float area = 0f;
            foreach (var contour in contours)
                area += GetSignedArea(contour);
            return Mathf.Abs(area);
        }

        private static float GetPerimeter(List<List<Vector2>> contours)
        {
            float perimeter = 0f;
            foreach (var contour in contours)
            {
                for (int i = 0; i < contour.Count; i++)
                {
                    int next = (i + 1) % contour.Count;
                    perimeter += Vector2.Distance(contour[i], contour[next]);
                }
            }

            return perimeter;
        }

        private static IEnumerable<RepresentativeGlyphCase> GetHoleAbsorptionCases()
        {
            yield return new RepresentativeGlyphCase("O", MakeOShapeContour(), 1.4f);
            yield return new RepresentativeGlyphCase("B", MakeBShapeContour(), 1.0f);
            yield return new RepresentativeGlyphCase("8", MakeEightShapeContour(), 1.0f);
            yield return new RepresentativeGlyphCase("あ", MakeHollowCjkContour("あ"), 2.0f);
            yield return new RepresentativeGlyphCase("回", MakeHollowCjkContour("回"), 1.5f);
            yield return new RepresentativeGlyphCase("囲", MakeHollowCjkContour("囲"), 1.5f);
        }

        private static List<List<Vector2>> GetContourProperty(object profileSet, string propertyName)
        {
            Assert.IsNotNull(profileSet, "OutlineProfileSet が返却されること");
            var property = profileSet.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(property, $"OutlineProfileSet.{propertyName} が必要です。");
            var value = property.GetValue(profileSet) as List<List<Vector2>>;
            Assert.IsNotNull(value, $"OutlineProfileSet.{propertyName} は contour list を返すこと");
            return value;
        }

        private static Rect GetBounds(List<List<Vector2>> contours)
        {
            Assert.Greater(contours.Count, 0, "少なくとも 1 つの contour が必要です。");

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            foreach (var contour in contours)
            {
                foreach (var point in contour)
                {
                    minX = Mathf.Min(minX, point.x);
                    minY = Mathf.Min(minY, point.y);
                    maxX = Mathf.Max(maxX, point.x);
                    maxY = Mathf.Max(maxY, point.y);
                }
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        [Test]
        public void BuildProfiles_ZeroOffset_ReturnsEmptyRingContours()
        {
            var profileSet = InvokeBuildProfiles(MakeSquareContour(), 0f, 1f);
            var ringContours = GetContourProperty(profileSet, "RingContoursEm");

            Assert.AreEqual(0, ringContours.Count,
                "OutlineOffset = 0 のとき RingContoursEm は空で、outline 形状を生成しないこと");
        }

        [Test]
        public void BuildProfiles_PositiveUnityOffset_ConvertsToEmSpaceBeforeOffset()
        {
            var profileSet = InvokeBuildProfiles(MakeSquareContour(), 0.5f, 2f);
            var offsetContours = GetContourProperty(profileSet, "OffsetFilledContoursEm");
            var bounds = GetBounds(offsetContours);

            Assert.AreEqual(-0.25f, bounds.xMin, 0.01f,
                "Unity 単位 0.5 / fontSize 2 = 0.25 em の outward offset が xMin に反映されること");
            Assert.AreEqual(1.25f, bounds.xMax, 0.01f,
                "Unity 単位 0.5 / fontSize 2 = 0.25 em の outward offset が xMax に反映されること");
        }

        [Test]
        public void BuildProfiles_RingDifference_AreaMatchesOffsetMinusOriginal()
        {
            var profileSet = InvokeBuildProfiles(MakeSquareContour(), 0.5f, 1f);
            var originalContours = GetContourProperty(profileSet, "OriginalFilledContoursEm");
            var offsetContours = GetContourProperty(profileSet, "OffsetFilledContoursEm");
            var ringContours = GetContourProperty(profileSet, "RingContoursEm");

            float originalArea = GetFilledArea(originalContours);
            float offsetArea = GetFilledArea(offsetContours);
            float ringArea = GetFilledArea(ringContours);

            Assert.AreEqual(offsetArea - originalArea, ringArea, 0.05f,
                "canonical ring profile は offset(originalFilled) - originalFilled の面積差を保持すること");
        }

        [Test]
        public void BuildProfiles_LargeOffset_AbsorbsRepresentativeHolesIntoSingleOffsetProfile()
        {
            foreach (var testCase in GetHoleAbsorptionCases())
            {
                var profileSet = InvokeBuildProfiles(testCase.Glyph, testCase.Offset, 1f);
                var offsetContours = GetContourProperty(profileSet, "OffsetFilledContoursEm");
                var ringContours = GetContourProperty(profileSet, "RingContoursEm");

                Assert.AreEqual(1, offsetContours.Count,
                    $"{testCase.Label} 相当の輪郭は大きめの offset で hole が吸収され、offset filled profile が 1 本に収束すること");
                Assert.Greater(ringContours.Count, 0,
                    $"{testCase.Label} 相当の輪郭でも ring difference 自体は保持されること");
            }
        }

        [Test]
        public void BuildProfiles_RingContours_KeepOuterAndInnerOppositeWinding()
        {
            var profileSet = InvokeBuildProfiles(MakeSquareContour(), 0.5f, 1f);
            var ringContours = GetContourProperty(profileSet, "RingContoursEm");

            Assert.AreEqual(2, ringContours.Count,
                "単純な square ring では outer と inner の 2 contour が必要です");

            float firstArea = GetSignedArea(ringContours[0]);
            float secondArea = GetSignedArea(ringContours[1]);
            Assert.AreEqual(-Mathf.Sign(firstArea), Mathf.Sign(secondArea),
                "ring contour の outer / inner は opposite winding を維持すること");
        }

        [Test]
        public void BuildProfiles_DoublingOffset_KeepsPerimeterGrowthWithinFifteenPercent()
        {
            var smallOffset = InvokeBuildProfiles(MakeSquareContour(), 0.25f, 1f);
            var largeOffset = InvokeBuildProfiles(MakeSquareContour(), 0.5f, 1f);

            float originalPerimeter = GetPerimeter(GetContourProperty(smallOffset, "OriginalFilledContoursEm"));
            float smallPerimeter = GetPerimeter(GetContourProperty(smallOffset, "OffsetFilledContoursEm"));
            float largePerimeter = GetPerimeter(GetContourProperty(largeOffset, "OffsetFilledContoursEm"));

            float smallGrowth = smallPerimeter - originalPerimeter;
            float largeGrowth = largePerimeter - originalPerimeter;
            float growthRatio = largeGrowth / smallGrowth;

            Assert.That(growthRatio, Is.InRange(1.7f, 2.3f),
                "offset を 2 倍にしたとき、外周長の増加率もおおむね 2 倍（±15%）に収まること");
        }

        [Test]
        public void BuildProfiles_OverlappingContours_AreUnionedBeforeRingGeneration()
        {
            var profileSet = InvokeBuildProfiles(MakeOverlappingContours(), 0.25f, 1f);
            var originalContours = GetContourProperty(profileSet, "OriginalFilledContoursEm");
            var offsetContours = GetContourProperty(profileSet, "OffsetFilledContoursEm");
            var ringContours = GetContourProperty(profileSet, "RingContoursEm");

            Assert.AreEqual(1, originalContours.Count,
                "重なり合う contour は original filled profile の段階で union されること");
            Assert.AreEqual(1, offsetContours.Count,
                "重なり合う contour の outward offset も単一 profile に結合されること");
            Assert.AreEqual(2, ringContours.Count,
                "単連結な union shape の outline ring は outer と inner の 2 contour を持つこと");
        }

        [Test]
        public void BuildProfiles_OverlappingContours_RingAreaMatchesOffsetMinusOriginal()
        {
            var profileSet = InvokeBuildProfiles(MakeOverlappingContours(), 0.25f, 1f);
            var originalContours = GetContourProperty(profileSet, "OriginalFilledContoursEm");
            var offsetContours = GetContourProperty(profileSet, "OffsetFilledContoursEm");
            var ringContours = GetContourProperty(profileSet, "RingContoursEm");

            float originalArea = GetFilledArea(originalContours);
            float offsetArea = GetFilledArea(offsetContours);
            float ringArea = GetFilledArea(ringContours);

            Assert.AreEqual(offsetArea - originalArea, ringArea, 0.05f,
                "重なり contour でも outline ring の面積は offset minus original を維持すること");
        }

        [Test]
        public void BuildProfiles_OverlappingContours_RingContours_KeepOppositeWindings()
        {
            var profileSet = InvokeBuildProfiles(MakeOverlappingContours(), 0.25f, 1f);
            var ringContours = GetContourProperty(profileSet, "RingContoursEm");

            bool hasPositiveArea = false;
            bool hasNegativeArea = false;
            foreach (var contour in ringContours)
            {
                float signedArea = GetSignedArea(contour);
                if (signedArea > 0f) hasPositiveArea = true;
                if (signedArea < 0f) hasNegativeArea = true;
            }

            Assert.IsTrue(hasPositiveArea,
                "outline ring には outer contour の winding が含まれること");
            Assert.IsTrue(hasNegativeArea,
                "outline ring には inner contour の winding が含まれること");
        }
    }
}