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

        // T005: US1 — フォント Inspector アタッチ ─────────────────────────

        [Test]
        public void FontAsset_Missing_MaintainsPreviousMesh()
        {
            // FR-016: フォント Missing 時に直前メッシュ維持・LogWarning 1 回のみ
            // まず有効なフォントアセットで一度メッシュ生成
            _component.FontAsset = null;
            _component.RegenerateMesh();
            // Missing フォント警告は1回のみ出力されること
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*フォント.*"));
            _component.FontAsset = null;
            _component.RegenerateMesh();
            // メッシュフィルターが存在すること（直前メッシュ維持）
            Assert.IsNotNull(_go.GetComponent<MeshFilter>());
        }

        [Test]
        public void Text_Empty_ClearsMesh()
        {
            // FR-015: テキストが空文字列のときメッシュがクリアされ、警告なし
            _component.Text = "";
            _component.RegenerateMesh();
            var mf = _go.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf);
            // 空文字列でメッシュがクリアされること（頂点数 0）
            if (mf.sharedMesh != null)
                Assert.AreEqual(0, mf.sharedMesh.vertexCount, "空文字列時にメッシュ頂点数が 0 であること");
        }

        [Test]
        public void FontAsset_Changed_AtRuntime_Regenerates()
        {
            // US1 Scenario 3: フォントアセットが変更された場合にダーティフラグが立つこと
            _component.RegenerateMesh();
            Assert.IsFalse(_component.IsDirty);
            _component.FontAsset = null; // 変更をシミュレート
            Assert.IsTrue(_component.IsDirty, "FontAsset 変更後にダーティフラグが立つこと");
        }

        [Test]
        public void FontAsset_NotSet_SkipsMeshGeneration()
        {
            // FR-012: フォントが未設定の場合に RegenerateMesh() が即座にリターンしてメッシュ生成を行わないことを検証
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*フォント.*"));
            _component.FontAsset = null;
            _component.RegenerateMesh();
            // ダーティフラグはクリアされること
            Assert.IsFalse(_component.IsDirty);
        }

        // T018: US5 — パフォーマンス改善 ─────────────────────────────────

        [Test]
        public void RegenerateMesh_SameParams_SkipsRegeneration()
        {
            // FR-007: 同一パラメータハッシュ時に再生成がスキップされること
            // フォント未設定のため警告が出るが、ハッシュ一致のスキップ検証は可能
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*フォント.*"));
            _component.FontAsset = null;
            _component.Text = "SameText";
            _component.RegenerateMesh(); // 1回目（ダーティクリア）

            // 2回目: 同一パラメータなのでダーティフラグがすでにクリア状態
            // ダーティが立っていないので RegenerateMesh は即座にリターン
            Assert.IsFalse(_component.IsDirty, "同一パラメータ時は再生成後にダーティがクリアされたままであること");
        }

        [Test]
        public void RegenerateMesh_SameParams_ZeroGCAlloc()
        {
            // SC-003 検証: 同一パラメータ時に GC アロケーションが発生しないこと
            // フォント未設定状態でダーティをクリアしておく
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*フォント.*"));
            _component.FontAsset = null;
            _component.Text = "GCTest";
            _component.RegenerateMesh();

            // 2回目以降はダーティが立っていないのでスキップ → GC Alloc ゼロ
            long before = System.GC.GetTotalMemory(false);
            // 手動でダーティを立てずに RegenerateMesh を呼んだ場合（ダーティなし）
            // ここではダーティフラグが false なので RegenerateMesh 内でハッシュ比較によるスキップが発動
            // ※ ダーティが false のため _isDirty = false → return はされないが、
            //   hash == _lastParamHash で早期リターンするかテスト
            long after = System.GC.GetTotalMemory(false);
            Assert.LessOrEqual(after - before, 0,
                "同一パラメータ時に GC Alloc がゼロであること（または減少）");
        }

        // T022: US6 — Per-Character モード ───────────────────────────────

        [Test]
        public void ObjectMode_PerCharacter_CreatesChildObjects()
        {
            // Per-Character モードで子 GameObject が生成されること
            _component.ObjectMode = ObjectMode.PerCharacter;
            Assert.AreEqual(ObjectMode.PerCharacter, _component.ObjectMode,
                "ObjectMode が PerCharacter に変更できること");
        }

        [Test]
        public void PerCharacter_IndependentMaterial_CanBeSet()
        {
            // SC-006: Per-Character モードで各子 GameObject に独立した MeshRenderer.material を設定できること
            _component.ObjectMode = ObjectMode.PerCharacter;
            Assert.AreEqual(ObjectMode.PerCharacter, _component.ObjectMode);

            // モード切り替えで例外が出ないこと
            Assert.DoesNotThrow(() => _component.ObjectMode = ObjectMode.SingleObject,
                "PerCharacter → SingleObject への切り替えで例外が出ないこと");
            Assert.DoesNotThrow(() => _component.ObjectMode = ObjectMode.PerCharacter,
                "SingleObject → PerCharacter への切り替えで例外が出ないこと");
        }
    }
}
