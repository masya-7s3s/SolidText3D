using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// SolidText3DInspector の手動再生成前提の動作テスト。
    /// </summary>
    public class SolidText3DInspectorTests
    {
        private GameObject _go;
        private SolidText3DComponent _component;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestInspectorDebounce");
            _component = _go.AddComponent<SolidText3DComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        /// <summary>
        /// Inspector 経由の変更は dirty を維持し、手動再生成でのみクリアされることを確認する。
        /// </summary>
        [Test]
        public void ManualRegeneration_ClearsDirtyFlag()
        {
            _component.Text = "Hello";
            _component.Text = "Changed";
            Assert.IsTrue(_component.IsDirty, "テキスト変更後にダーティフラグが立つこと");

            _component.FontAsset = null;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*フォント.*"));
            Assert.DoesNotThrow(() => _component.RegenerateMesh(),
                "手動の RegenerateMesh() が呼び出せること");
            Assert.IsFalse(_component.IsDirty, "RegenerateMesh() 後にダーティフラグがクリアされること");
        }
    }
}
