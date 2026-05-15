using System.Collections;
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
