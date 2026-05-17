using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Runtime
{
    /// <summary>
    /// SolidText3DComponent の Play Mode テスト（T022）。
    /// </summary>
    public class SolidText3DRuntimeTests
    {
        private static readonly FieldInfo DeferredStateField = typeof(SolidText3DComponent)
            .GetField("_deferredRegenerationState", BindingFlags.NonPublic | BindingFlags.Instance);

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

        private static IEnumerator WaitForDeferredRegenerationToSettle(SolidText3DComponent component, int maxFrames = 120)
        {
            var state = GetDeferredState(component);
            float deadline = Time.realtimeSinceStartup + 5f;
            int frame = 0;

            while (component.HasPendingRegeneration && Time.realtimeSinceStartup < deadline)
            {
                frame++;
                yield return null;
            }

            Assert.IsFalse(component.HasPendingRegeneration,
                $"deferred regeneration が制限時間内に収束すること (frames={frame}, lastRequested={state.LastRequestedVersion}, lastApplied={state.LastAppliedVersion}, inFlight={state.InFlightRequest.HasValue}, pending={state.PendingLatestRequest.HasValue}, ready={state.ReadyResult != null})");
        }

        [UnityTest]
        public IEnumerator TextChange_StaysDirtyUntilManualRegeneration()
        {
            var go = new GameObject("RuntimeTest");
            var comp = go.AddComponent<SolidText3DComponent>();
            comp.Text = "A";

            yield return null; // 自動再生成しないため dirty のまま維持される

            var mf = go.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf);
            Assert.IsTrue(comp.IsDirty, "テキスト変更後は明示的に再生成するまでダーティフラグが維持されること");

            comp.RegenerateMesh();
            Assert.IsFalse(comp.IsDirty, "RegenerateMesh() 実行後にダーティフラグがクリアされること");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }

    [UnityTest]
    public IEnumerator RegenerateMesh_SatisfiesSynchronousRegenerationContract()
    {
        var go = new GameObject("RuntimeSyncContract");
        var comp = go.AddComponent<SolidText3DComponent>();
        comp.Text = "Sync";

        comp.RegenerateMesh();
        yield return null;

        AssertSynchronousRegenerationContract(comp);

#if UNITY_EDITOR
        Object.DestroyImmediate(go);
#else
        Object.Destroy(go);
#endif
    }

    [UnityTest]
    public IEnumerator RequestRegenerateMesh_QueuesAndSettlesDeferredRegeneration()
    {
        var go = new GameObject("RuntimeDeferredContract");
        var comp = go.AddComponent<SolidText3DComponent>();
        comp.Text = "Deferred";

        comp.RequestRegenerateMesh();
        AssertDeferredRegenerationQueued(comp);

        yield return WaitForDeferredRegenerationToSettle(comp);
        AssertSynchronousRegenerationContract(comp);

#if UNITY_EDITOR
        Object.DestroyImmediate(go);
#else
        Object.Destroy(go);
#endif
    }

    [UnityTest]
    public IEnumerator RequestRegenerateMesh_PerCharacter_AppliesChildrenParity()
    {
    var go = new GameObject("RuntimeDeferredPerCharacter");
    var comp = go.AddComponent<SolidText3DComponent>();
    comp.ObjectMode = ObjectMode.PerCharacter;
    comp.Text = "AB";

    comp.RequestRegenerateMesh();
    yield return WaitForDeferredRegenerationToSettle(comp);

    var meshRenderer = go.GetComponent<MeshRenderer>();
    Assert.IsNotNull(meshRenderer);
    Assert.IsFalse(meshRenderer.enabled, "PerCharacter deferred apply 後は本体 MeshRenderer が無効になること");
    Assert.IsNotNull(go.transform.Find("Char_0"), "PerCharacter deferred apply 後に 1 文字目 child が生成されること");
    Assert.IsNotNull(go.transform.Find("Char_1"), "PerCharacter deferred apply 後に 2 文字目 child が生成されること");

#if UNITY_EDITOR
    Object.DestroyImmediate(go);
#else
    Object.Destroy(go);
#endif
    }

    [UnityTest]
    public IEnumerator RequestRegenerateMesh_EmptyText_ClearsDisplayAfterDeferredApply()
    {
    var go = new GameObject("RuntimeDeferredEmpty");
    var comp = go.AddComponent<SolidText3DComponent>();
    comp.Text = "AB";
    comp.RegenerateMesh();

    comp.Text = "";
    comp.RequestRegenerateMesh();
    yield return WaitForDeferredRegenerationToSettle(comp);

    var meshFilter = go.GetComponent<MeshFilter>();
    int vertexCount = meshFilter.sharedMesh != null ? meshFilter.sharedMesh.vertexCount : 0;
    Assert.AreEqual(0, vertexCount, "deferred empty-string apply 後は表示が安全にクリアされること");

#if UNITY_EDITOR
    Object.DestroyImmediate(go);
#else
    Object.Destroy(go);
#endif
    }

    [UnityTest]
    public IEnumerator RequestRegenerateMesh_BurstCollapse_AppliesLatestOnly()
    {
    var go = new GameObject("RuntimeDeferredBurstCollapse");
    var comp = go.AddComponent<SolidText3DComponent>();

    comp.Text = "A";
    comp.RequestRegenerateMesh();
    comp.Text = "B";
    comp.RequestRegenerateMesh();
    comp.Text = "C";
    comp.RequestRegenerateMesh();

    yield return WaitForDeferredRegenerationToSettle(comp);

    Assert.AreEqual("C", GetDeferredState(comp).LastAppliedSignature.Text,
        "更新バーストでも最後の request だけが最終表示に残ること");

#if UNITY_EDITOR
    Object.DestroyImmediate(go);
#else
    Object.Destroy(go);
#endif
    }

        [UnityTest]
        public IEnumerator EmptyText_ZeroPolygon()
        {
            var go = new GameObject("RuntimeTestEmpty");
            var comp = go.AddComponent<SolidText3DComponent>();
            comp.Text = "";

            yield return null;

            var mf = go.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf);
            // フォント未設定時は sharedMesh が null になりうる。null も頂点数 0 と同義とみなす。
            int vertexCount = mf.sharedMesh != null ? mf.sharedMesh.vertexCount : 0;
            Assert.AreEqual(0, vertexCount, "空テキストで頂点数が 0 であること");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }

        [UnityTest]
        public IEnumerator MultiplePropertyChanges_SingleMeshRegeneration()
        {
            var go = new GameObject("RuntimeTestMulti");
            var comp = go.AddComponent<SolidText3DComponent>();

            // 同一フレームで複数プロパティを変更
            comp.Text = "A";
            comp.ExtrusionDepth = 0.5f;
            comp.LetterSpacing = 5f;

            // 変更後、再生成前は IsDirty が true であること
            Assert.IsTrue(comp.IsDirty, "手動再生成前はダーティフラグが立っていること");

            yield return null; // 自動再生成しない

            Assert.IsTrue(comp.IsDirty, "フレーム経過後も手動再生成まではダーティフラグが維持されること");

            comp.RegenerateMesh();
            Assert.IsFalse(comp.IsDirty, "RegenerateMesh() 実行後はダーティフラグがクリアされること");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }

        // T031b: ランタイムフォント切り替えテスト ─────────────────────────

        [UnityTest]
        public IEnumerator FontSwitch_RegeneratesMeshWithNewFont()
        {
            var go = new GameObject("RuntimeTestFontSwitch");
            var comp = go.AddComponent<SolidText3DComponent>();
            comp.Text = "A";

            comp.RegenerateMesh();

            var mf = go.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf);

            // FontAsset を null に切り替え → FR-012 によりメッシュ生成をスキップ
            comp.FontAsset = null;

            yield return null;

            Assert.IsTrue(comp.IsDirty, "フォント切り替え後は手動再生成までダーティフラグが維持されること");

            comp.RegenerateMesh();
            Assert.IsFalse(comp.IsDirty, "手動再生成後にダーティフラグがクリアされること");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }
    }
}
