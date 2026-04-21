using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// CharacterObjectPool のユニットテスト（T020）。
    /// </summary>
    public class CharacterObjectPoolTests
    {
        private GameObject _parent;
        private CharacterObjectPool _pool;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("TestPool");
            _pool = new CharacterObjectPool(_parent.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_parent != null)
                Object.DestroyImmediate(_parent);
        }

        private static List<GlyphContour> CreateGlyphs(int count)
        {
            var list = new List<GlyphContour>(count);
            for (int i = 0; i < count; i++)
                list.Add(new GlyphContour { IsVisible = true, CharIndex = i });
            return list;
        }

        private static List<Mesh> CreateMeshes(int count)
        {
            var list = new List<Mesh>(count);
            for (int i = 0; i < count; i++)
            {
                var m = new Mesh();
                m.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
                list.Add(m);
            }
            return list;
        }

        [Test]
        public void Sync_MoreChars_CreatesNewChildren()
        {
            var glyphs = CreateGlyphs(3);
            var meshes = CreateMeshes(3);

            _pool.Sync(glyphs, meshes);

            Assert.AreEqual(3, _parent.transform.childCount, "3文字分の子 GameObject が生成されること");
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(_parent.transform.GetChild(i).gameObject.activeSelf, $"子 GameObject[{i}] がアクティブであること");
        }

        [Test]
        public void Sync_FewerChars_DeactivatesExcess()
        {
            // 最初に3つ作成
            _pool.Sync(CreateGlyphs(3), CreateMeshes(3));
            Assert.AreEqual(3, _parent.transform.childCount);

            // 1つに削減
            _pool.Sync(CreateGlyphs(1), CreateMeshes(1));

            // 1つ目はアクティブ、2・3つ目は非アクティブ
            Assert.IsTrue(_parent.transform.GetChild(0).gameObject.activeSelf, "残る文字の GameObject はアクティブであること");
            Assert.IsFalse(_parent.transform.GetChild(1).gameObject.activeSelf, "余剰 GameObject は非アクティブになること");
            Assert.IsFalse(_parent.transform.GetChild(2).gameObject.activeSelf, "余剰 GameObject は非アクティブになること");
        }

        [Test]
        public void Sync_SameCount_ReusesExistingChildren()
        {
            // 最初に2つ作成
            _pool.Sync(CreateGlyphs(2), CreateMeshes(2));
            var firstChild = _parent.transform.GetChild(0).gameObject;
            var secondChild = _parent.transform.GetChild(1).gameObject;

            // 同数で再同期
            _pool.Sync(CreateGlyphs(2), CreateMeshes(2));

            // 同じ GameObject が再利用されること（Destroy されず）
            Assert.AreEqual(firstChild, _parent.transform.GetChild(0).gameObject, "既存 GameObject が再利用されること");
            Assert.AreEqual(secondChild, _parent.transform.GetChild(1).gameObject, "既存 GameObject が再利用されること");
            Assert.AreEqual(2, _parent.transform.childCount, "子 GameObject の総数が変わらないこと");
        }
    }
}
