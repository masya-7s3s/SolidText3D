using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// SolidText3DComponent のライフサイクル・ダーティフラグテスト（T018c）。
    /// </summary>
    public class SolidText3DComponentTests
    {
        private GameObject _go;
        private SolidText3DComponent _component;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestSolidText3D");
            _component = _go.AddComponent<SolidText3DComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        // FR-006: Awake 後のコンポーネント確認 ─────────────────────────

        [Test]
        public void Awake_MeshFilterIsNotNull()
        {
            Assert.IsNotNull(_go.GetComponent<MeshFilter>(),
                "Awake() 後に MeshFilter が存在すること");
        }

        [Test]
        public void Awake_MeshRendererIsNotNull()
        {
            Assert.IsNotNull(_go.GetComponent<MeshRenderer>(),
                "Awake() 後に MeshRenderer が存在すること");
        }

        [Test]
        public void Awake_NoRectTransform()
        {
            Assert.IsNull(_go.GetComponent<RectTransform>(),
                "SolidText3DComponent は World Space コンポーネントであり RectTransform を持たないこと");
        }

        // FR-005: ダーティフラグ検証 ─────────────────────────────────

        [Test]
        public void SetText_MarksDirty()
        {
            // RegenerateMesh を呼んでダーティ状態をリセット
            _component.RegenerateMesh();
            _component.Text = "New Text";
            Assert.IsTrue(_component.IsDirty, "Text 変更後にダーティフラグが立つこと");
        }

        [Test]
        public void SetExtrusionDepth_MarksDirty()
        {
            _component.RegenerateMesh();
            _component.ExtrusionDepth = 2f;
            Assert.IsTrue(_component.IsDirty, "ExtrusionDepth 変更後にダーティフラグが立つこと");
        }

        [Test]
        public void SetOutlineWidth_MarksDirty()
        {
            _component.RegenerateMesh();
            _component.OutlineWidth = 0.1f;
            Assert.IsTrue(_component.IsDirty, "OutlineWidth 変更後にダーティフラグが立つこと");
        }

        [Test]
        public void RegenerateMesh_ClearsDirtyFlag()
        {
            _component.Text = "Test";
            _component.RegenerateMesh();
            Assert.IsFalse(_component.IsDirty, "RegenerateMesh() 後にダーティフラグがクリアされること");
        }
    }
}
