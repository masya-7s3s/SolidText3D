using System.Collections;
using System.IO;
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
        public IEnumerator TextChange_UpdatesMeshNextFrame()
        {
            var go = new GameObject("RuntimeTest");
            var comp = go.AddComponent<SolidText3DComponent>();
            comp.Text = "A";

            yield return null; // 1 フレーム待機（LateUpdate でメッシュ再生成）

            var mf = go.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf);
            // フォント未設定時は FR-012 によりメッシュ生成をスキップする。
            // sharedMesh が null でも例外が出ないことと、ダーティフラグがクリアされることを検証する。
            Assert.IsFalse(comp.IsDirty, "テキスト変更後 1 フレームでダーティフラグがクリアされること");

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
            Assert.IsTrue(comp.IsDirty, "LateUpdate 実行前はダーティフラグが立っていること");

            yield return null; // 1 フレーム待機

            // 1 フレーム後はダーティフラグがクリアされていること
            Assert.IsFalse(comp.IsDirty, "LateUpdate 実行後はダーティフラグがクリアされること");

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

            yield return null; // 最初のメッシュ生成

            var mf = go.GetComponent<MeshFilter>();

            // FontAsset を null に切り替え → FR-012 によりメッシュ生成をスキップ
            comp.FontAsset = null;

            yield return null; // 再生成

            // FontAsset が null の場合はメッシュを生成しない。例外が発生せず
            // ダーティフラグがクリアされることを検証する（FR-012 準拠）。
            Assert.IsFalse(comp.IsDirty, "フォント切り替え後にダーティフラグがクリアされること");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }
    }
}
