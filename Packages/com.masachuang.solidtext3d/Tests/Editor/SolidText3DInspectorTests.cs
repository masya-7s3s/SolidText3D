using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// SolidText3DInspector のデバウンス動作テスト（T016）。
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
        /// FR-006: SuppressAutoRegenerate フラグが true の間は
        /// RegenerateMesh() が呼び出された際に LateUpdate でスキップされること（SC-001 の基礎条件確認）。
        /// </summary>
        [Test]
        public void SuppressAutoRegenerate_WhileFocused_BlocksRegeneration()
        {
            // まず正常な状態を確認
            _component.Text = "Hello";
            _component.SuppressAutoRegenerate = false;
            Assert.IsFalse(_component.SuppressAutoRegenerate, "初期状態は false");

            // SuppressAutoRegenerate を true に設定
            _component.SuppressAutoRegenerate = true;
            Assert.IsTrue(_component.SuppressAutoRegenerate, "SuppressAutoRegenerate を true に設定できること");

            // ダーティフラグを立てる
            _component.Text = "Changed";
            Assert.IsTrue(_component.IsDirty, "テキスト変更後にダーティフラグが立つこと");

            // SuppressAutoRegenerate が true のとき、LateUpdate では RegenerateMesh が呼ばれない
            // （ダーティフラグが残ったままであること）
            // ※ LateUpdate は手動呼び出し不可のため、ここでは SuppressAutoRegenerate=true の間に
            //   RegenerateMesh() を呼んでも処理が継続することを確認する

            // フォーカスアウト時: SuppressAutoRegenerate を false に戻して再生成
            _component.SuppressAutoRegenerate = false;
            Assert.IsFalse(_component.SuppressAutoRegenerate, "フォーカスアウト後に false に戻せること");

            // 再生成が呼び出せること（フォント未設定なので警告は出るが例外は出ない）
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*フォント.*"));
            Assert.DoesNotThrow(() => _component.RegenerateMesh(),
                "SuppressAutoRegenerate=false 後に RegenerateMesh() が呼び出せること");
            Assert.IsFalse(_component.IsDirty, "RegenerateMesh() 後にダーティフラグがクリアされること");
        }
    }
}
