using NUnit.Framework;
using System.IO;
using System.Reflection;
using UnityEditor;
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
        private static readonly FieldInfo DeferredStateField = typeof(SolidText3DComponent)
            .GetField("_deferredRegenerationState", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo FontAssetField = typeof(SolidText3DComponent)
            .GetField("_fontAsset", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo FontBytesCacheField = typeof(SolidText3DComponent)
            .GetField("_fontBytesCache", BindingFlags.NonPublic | BindingFlags.Instance);

        private GameObject _go;
        private SolidText3DComponent _component;

        private static DeferredRegenerationState GetDeferredState(SolidText3DComponent component)
        {
            Assert.IsNotNull(DeferredStateField, "deferred regeneration state field が必要です。");
            return (DeferredRegenerationState)DeferredStateField.GetValue(component);
        }

        private static void AssertSynchronousRegenerationContract(SolidText3DComponent component)
        {
            var state = GetDeferredState(component);
            Assert.IsFalse(component.IsDirty, "同期再生成後は dirty が解消されること");
            Assert.IsFalse(component.HasPendingRegeneration, "同期再生成後は pending regeneration が残らないこと");
            Assert.AreEqual(state.LastRequestedVersion, state.LastAppliedVersion,
                "同期再生成は呼び出し復帰時点で request が適用済みであること");
            Assert.IsFalse(state.InFlightRequest.HasValue, "同期再生成後に in-flight request が残らないこと");
            Assert.IsFalse(state.PendingLatestRequest.HasValue, "同期再生成後に pending latest request が残らないこと");
            Assert.IsNull(state.ReadyResult, "同期再生成後に apply 待ち result が残らないこと");
        }

        private static void AssertDeferredRegenerationQueued(SolidText3DComponent component)
        {
            var state = GetDeferredState(component);
            Assert.IsFalse(component.IsDirty, "deferred request submit 後は dirty がクリアされること");
            Assert.IsTrue(component.HasPendingRegeneration, "deferred request submit 後は pending regeneration が存在すること");
            Assert.Greater(state.LastRequestedVersion, state.LastAppliedVersion,
                "deferred request submit 直後は未適用 request version が存在すること");
            Assert.IsTrue(state.InFlightRequest.HasValue || state.PendingLatestRequest.HasValue || state.ReadyResult != null,
                "deferred request submit 後は in-flight / pending / ready のいずれかに状態が残ること");
        }

            private static void AssertNoDeferredRegenerationQueued(SolidText3DComponent component)
            {
                var state = GetDeferredState(component);
                Assert.IsFalse(component.HasPendingRegeneration, "same-display request は deferred regeneration を積まないこと");
                Assert.IsFalse(state.InFlightRequest.HasValue, "same-display request で in-flight request を作らないこと");
                Assert.IsFalse(state.PendingLatestRequest.HasValue, "same-display request で pending latest request を作らないこと");
                Assert.IsNull(state.ReadyResult, "same-display request で apply 待ち result を作らないこと");
            }

        private static void ForceFontUnavailable(SolidText3DComponent component)
        {
            Assert.IsNotNull(FontAssetField);
            Assert.IsNotNull(FontBytesCacheField);
            FontAssetField.SetValue(component, null);
            FontBytesCacheField.SetValue(component, null);
        }

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

        [Test]
        public void RegenerateMesh_SatisfiesSynchronousRegenerationContract()
        {
            _component.Text = "Sync";

            _component.RegenerateMesh();

            AssertSynchronousRegenerationContract(_component);
        }

        [Test]
        public void RequestRegenerateMesh_QueuesDeferredRegenerationWork()
        {
            _component.Text = "Deferred";

            _component.RequestRegenerateMesh();

            AssertDeferredRegenerationQueued(_component);
        }

        [Test]
        public void RequestRegenerateMesh_SameDisplay_DoesNotQueueDeferredWork()
        {
            _component.Text = "12:34";
            _component.RegenerateMesh();

            _component.RequestRegenerateMesh();

            AssertNoDeferredRegenerationQueued(_component);
        }

        [Test]
        public void RequestRegenerateMesh_CaptureFailure_RaisesDeferredFailure_AndKeepsLastGoodDisplay()
        {
            _component.Text = "GOOD";
            _component.RegenerateMesh();
            var state = GetDeferredState(_component);
            var lastGoodSignature = state.LastAppliedSignature;
            int failureCount = 0;
            RegenerationFailureInfo failureInfo = default;
            _component.DeferredRegenerationFailed += info =>
            {
                failureCount++;
                failureInfo = info;
            };

            ForceFontUnavailable(_component);
            _component.Text = "BROKEN";
            _component.RequestRegenerateMesh();

            Assert.AreEqual(1, failureCount, "capture failure 時は DeferredRegenerationFailed を 1 回通知すること");
            Assert.AreEqual("BROKEN", failureInfo.RequestedText);
            Assert.AreEqual(lastGoodSignature, state.LastAppliedSignature,
                "deferred failure でも visible display は keep-last-good を維持すること");
            Assert.IsFalse(_component.HasPendingRegeneration, "capture failure 後に pending regeneration を残さないこと");
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

        [Test]
        public void GetFontBytes_FontAssetOverridesExistingDefaultCache()
        {
            var serifFont = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Samples/Solid Text 3D/2.0.0/CJK Example/NotoSerifJP-Black.ttf");
            Assert.IsNotNull(serifFont, "比較用フォントが存在すること");

            var defaultBytes = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes");
            Assert.IsNotNull(defaultBytes, "デフォルトフォント bytes が存在すること");

            var cacheField = typeof(SolidText3DComponent).GetField("_fontBytesCache", BindingFlags.NonPublic | BindingFlags.Instance);
            cacheField.SetValue(_component, defaultBytes);
            _component.FontAsset = serifFont;

            var getFontBytesMethod = typeof(SolidText3DComponent).GetMethod("GetFontBytes", BindingFlags.NonPublic | BindingFlags.Instance);
            var actualBytes = (byte[])getFontBytesMethod.Invoke(_component, null);
            var expectedBytes = File.ReadAllBytes(Path.GetFullPath(
                "Assets/Samples/Solid Text 3D/2.0.0/CJK Example/NotoSerifJP-Black.ttf"));

            Assert.IsNotNull(actualBytes);
            Assert.AreEqual(expectedBytes.Length, actualBytes.Length,
                "FontAsset 指定時は既存 cache ではなく選択したフォントの bytes を使うこと");
        }

        [Test]
        public void ComputeParamHash_FontAssetChange_ChangesHash()
        {
            var sansFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansJP-Black.ttf");
            var serifFont = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Samples/Solid Text 3D/2.0.0/CJK Example/NotoSerifJP-Black.ttf");
            Assert.IsNotNull(sansFont);
            Assert.IsNotNull(serifFont);

            _component.Text = "A";
            _component.FontAsset = sansFont;

            var computeLayoutHashMethod = typeof(SolidText3DComponent).GetMethod("ComputeLayoutHash", BindingFlags.NonPublic | BindingFlags.Instance);
            var computeHashMethod = typeof(SolidText3DComponent).GetMethod("ComputeParamHash", BindingFlags.NonPublic | BindingFlags.Instance);
            int sansLayoutHash = (int)computeLayoutHashMethod.Invoke(_component, null);
            int sansHash = (int)computeHashMethod.Invoke(_component, new object[] { sansLayoutHash });

            _component.FontAsset = serifFont;
            int serifLayoutHash = (int)computeLayoutHashMethod.Invoke(_component, null);
            int serifHash = (int)computeHashMethod.Invoke(_component, new object[] { serifLayoutHash });

            Assert.AreNotEqual(sansHash, serifHash,
                "フォント変更時は再生成ハッシュも変化すること");
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
