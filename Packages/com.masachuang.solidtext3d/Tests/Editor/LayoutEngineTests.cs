using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// LayoutEngine のユニットテスト（T010, T027）。
    /// </summary>
    public class LayoutEngineTests
    {
        private static MeshGenerationParams DefaultParams(string text = "A") =>
            new MeshGenerationParams
            {
                Text = text,
                ExtrusionDepth = 1f,
                FontSize = 1f,
                LetterSpacing = 0f,
                LineSpacing = 1.2f,
                HorizontalAnchor = HorizontalAnchor.Left,
                VerticalAnchor = VerticalAnchor.Lower,
                DepthAnchor = DepthAnchor.Front,
                WritingMode = WritingMode.Horizontal
            };

        // ── T010: 横書きレイアウト基本テスト ──────────────────────────────

        [Test]
        public void ApplyHorizontalLayout_EmptyGlyphs_DoesNotThrow()
        {
            var glyphs = new List<GlyphContour>();
            var p = DefaultParams("");
            Assert.DoesNotThrow(() => LayoutEngine.ApplyHorizontalLayout(glyphs, p));
        }

        [Test]
        public void CalculateAnchorOffset_Center_ReturnsHalfExtents()
        {
            var bounds = new Bounds(new Vector3(1f, 0.5f, 0.5f), new Vector3(2f, 1f, 1f));
            var p = DefaultParams();
            p.HorizontalAnchor = HorizontalAnchor.Center;
            p.VerticalAnchor = VerticalAnchor.Middle;
            p.DepthAnchor = DepthAnchor.Center;

            var offset = LayoutEngine.CalculateAnchorOffset(bounds, p);

            // bounds.min.x = 0, bounds.max.x = 2, width=2 → Center offset = -width/2 = -1.0f (relative to min)
            // 実際の計算: Left=0, Center=-width/2, Right=-width (bounds.min を原点とした相対オフセット)
            // ここでは bounds が (1, 0.5, 0.5) center, (2, 1, 1) size のため
            // min = (0, 0, 0), max = (2, 1, 1)
            Assert.AreEqual(-1f, offset.x, 0.001f, "水平 Center アンカーのオフセットが -width/2 であること");
            Assert.AreEqual(-0.5f, offset.y, 0.001f, "垂直 Middle アンカーのオフセットが -height/2 であること");
            Assert.AreEqual(-0.5f, offset.z, 0.001f, "奥行き Center アンカーのオフセットが -center.z であること");
        }

        // ── SC-005: 全27組み合わせのアンカーテスト ─────────────────────

        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Lower, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Lower, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Lower, DepthAnchor.Back)]
        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Middle, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Middle, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Middle, DepthAnchor.Back)]
        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Upper, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Upper, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Left, VerticalAnchor.Upper, DepthAnchor.Back)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Lower, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Lower, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Lower, DepthAnchor.Back)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Middle, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Middle, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Middle, DepthAnchor.Back)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Upper, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Upper, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Center, VerticalAnchor.Upper, DepthAnchor.Back)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Lower, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Lower, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Lower, DepthAnchor.Back)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Middle, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Middle, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Middle, DepthAnchor.Back)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Upper, DepthAnchor.Front)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Upper, DepthAnchor.Center)]
        [TestCase(HorizontalAnchor.Right, VerticalAnchor.Upper, DepthAnchor.Back)]
        public void CalculateAnchorOffset_AllCombinations_OffsetIsFinite(
            HorizontalAnchor hAnchor, VerticalAnchor vAnchor, DepthAnchor dAnchor)
        {
            // bounds: min=(0,0,0), max=(2,1,1)
            var bounds = new Bounds(new Vector3(1f, 0.5f, 0.5f), new Vector3(2f, 1f, 1f));
            var p = DefaultParams();
            p.HorizontalAnchor = hAnchor;
            p.VerticalAnchor = vAnchor;
            p.DepthAnchor = dAnchor;

            var offset = LayoutEngine.CalculateAnchorOffset(bounds, p);

            Assert.IsFalse(float.IsNaN(offset.x) || float.IsInfinity(offset.x),
                $"X offset は有限値であること (H={hAnchor}, V={vAnchor}, D={dAnchor})");
            Assert.IsFalse(float.IsNaN(offset.y) || float.IsInfinity(offset.y),
                $"Y offset は有限値であること (H={hAnchor}, V={vAnchor}, D={dAnchor})");
            Assert.IsFalse(float.IsNaN(offset.z) || float.IsInfinity(offset.z),
                $"Z offset は有限値であること (H={hAnchor}, V={vAnchor}, D={dAnchor})");

            // アンカー別の期待オフセット検証
            float expectedX = hAnchor == HorizontalAnchor.Left ? -bounds.min.x :
                              hAnchor == HorizontalAnchor.Center ? -bounds.min.x - bounds.size.x / 2f :
                              -bounds.min.x - bounds.size.x;
            float expectedY = vAnchor == VerticalAnchor.Lower ? -bounds.min.y :
                              vAnchor == VerticalAnchor.Middle ? -bounds.min.y - bounds.size.y / 2f :
                              -bounds.min.y - bounds.size.y;
            float expectedZ = dAnchor == DepthAnchor.Front ? -bounds.min.z :
                              dAnchor == DepthAnchor.Center ? -bounds.center.z :
                              -bounds.max.z;

            Assert.AreEqual(expectedX, offset.x, 0.001f);
            Assert.AreEqual(expectedY, offset.y, 0.001f);
            Assert.AreEqual(expectedZ, offset.z, 0.001f);
        }

        // ── FR-005: ダーティフラグ検証 ──────────────────────────────────

        [Test]
        public void AnchorChange_SetsDirtyFlag()
        {
            // FR-005: アンカープロパティの setter が _isDirty = true を設定すること
            var go = new GameObject("TestAnchorDirty");
            var comp = go.AddComponent<SolidText3DComponent>();
            comp.RegenerateMesh(); // ダーティをクリア

            // フォント未設定のため RegenerateMesh 後はダーティはクリアされているはず
            comp.HorizontalAnchor = HorizontalAnchor.Center;
            Assert.IsTrue(comp.IsDirty, "HorizontalAnchor 変更後にダーティフラグが立つこと");

            comp.RegenerateMesh();
            comp.VerticalAnchor = VerticalAnchor.Middle;
            Assert.IsTrue(comp.IsDirty, "VerticalAnchor 変更後にダーティフラグが立つこと");

            comp.RegenerateMesh();
            comp.DepthAnchor = DepthAnchor.Center;
            Assert.IsTrue(comp.IsDirty, "DepthAnchor 変更後にダーティフラグが立つこと");

            UnityEngine.Object.DestroyImmediate(go);
        }

        // ── FR-014: MaxWidth=0 のとき自動折り返しなし ────────────────────

        [Test]
        public void Layout_MaxWidthZero_NoWordWrap()
        {
            // FR-014: MaxWidth が 0 の場合は自動折り返しが行われず、改行コードのみで行が制御される
            var glyphs = CreateDummyGlyphs(10);
            var p = DefaultParams("A B C D E F G H I J");
            p.MaxWidth = 0f; // 無制限

            Assert.DoesNotThrow(() => LayoutEngine.ApplyHorizontalLayout(glyphs, p));
            // MaxWidth=0 のとき全グリフが同一行（Y オフセットが変化しない）であること
            for (int i = 0; i < glyphs.Count; i++)
                Assert.AreEqual(0f, glyphs[i].Offset.y, 0.001f, $"MaxWidth=0 のとき Y オフセットが 0 であること (glyph {i})");
        }


        // ── T027: 縦書きレイアウトテスト ────────────────────────────────

        [Test]
        public void ApplyVerticalLayout_SingleChar_YIsNegative()
        {
            // 縦書き時に2文字目以降の Y 座標が負方向に進むこと
            var glyphs = CreateDummyGlyphs(2);
            var p = DefaultParams("AB");
            p.WritingMode = WritingMode.Vertical;

            LayoutEngine.ApplyVerticalLayout(glyphs, p);

            // 1文字目は Y=0、2文字目は Y<0（下方向）
            Assert.LessOrEqual(glyphs[1].Offset.y, glyphs[0].Offset.y,
                "縦書き時に2文字目の Y 座標が1文字目以下であること");
        }

        [TestCase("あいうえお", 5)]
        [TestCase("ABCDE", 5)]
        [TestCase("日本語", 3)]
        public void ApplyVerticalLayout_MultipleChars_YPositionsDecreasing(string text, int charCount)
        {
            // SC-007: 縦書きで複数文字の Y 座標が前の文字より小さくなること（または同じ列でない場合はリセット）
            var glyphs = CreateDummyGlyphs(charCount);
            var p = DefaultParams(text);
            p.WritingMode = WritingMode.Vertical;
            p.MaxHeight = 0f; // 折り返しなし

            LayoutEngine.ApplyVerticalLayout(glyphs, p);

            for (int i = 1; i < glyphs.Count; i++)
            {
                // 同列内では Y が減少すること
                Assert.LessOrEqual(glyphs[i].Offset.y, glyphs[i - 1].Offset.y,
                    $"縦書き: glyph[{i}].Y ({glyphs[i].Offset.y}) <= glyph[{i-1}].Y ({glyphs[i-1].Offset.y})");
            }
        }

        [Test]
        public void ApplyVerticalLayout_JapanesePunctuation_IsBiasedToTopRightInCell()
        {
            var punctuationGlyphs = CreateDummyGlyphs(1);
            punctuationGlyphs[0].AdvanceWidth = 0.2f;
            punctuationGlyphs[0].AdvanceHeight = 0.2f;
            punctuationGlyphs[0].Bounds = new Rect(0f, 0f, 0.2f, 0.2f);

            var normalGlyphs = CreateDummyGlyphs(1);
            normalGlyphs[0].AdvanceWidth = 0.2f;
            normalGlyphs[0].AdvanceHeight = 0.2f;
            normalGlyphs[0].Bounds = new Rect(0f, 0f, 0.2f, 0.2f);

            var punctuationParams = DefaultParams("。");
            punctuationParams.WritingMode = WritingMode.Vertical;
            punctuationParams.VerticalAnchor = VerticalAnchor.Upper;

            var normalParams = DefaultParams("あ");
            normalParams.WritingMode = WritingMode.Vertical;
            normalParams.VerticalAnchor = VerticalAnchor.Upper;

            LayoutEngine.ApplyVerticalLayout(punctuationGlyphs, punctuationParams);
            LayoutEngine.ApplyVerticalLayout(normalGlyphs, normalParams);

            Assert.Greater(punctuationGlyphs[0].Offset.x, normalGlyphs[0].Offset.x,
                "縦書きの句点は通常文字より右寄せされること");
            Assert.Greater(punctuationGlyphs[0].Offset.y, normalGlyphs[0].Offset.y,
                "縦書きの句点は通常文字より上寄せされること");
            Assert.AreEqual(1.0f, punctuationGlyphs[0].Offset.x, 0.001f,
                "縦書きの句点は従来どおり右寄せされること");
            Assert.AreEqual(-0.16f, punctuationGlyphs[0].Offset.y, 0.001f,
                "縦書きの句点は上端に張り付きすぎない位置に置かれること");
        }

        // ── ヘルパー ───────────────────────────────────────────────────────

        private static List<GlyphContour> CreateDummyGlyphs(int count)
        {
            var list = new List<GlyphContour>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new GlyphContour
                {
                    AdvanceWidth = 1f,
                    AdvanceHeight = 1f,
                    IsVisible = true,
                    CharIndex = i,
                    Bounds = new Rect(0, 0, 1, 1)
                });
            }
            return list;
        }
    }
}
