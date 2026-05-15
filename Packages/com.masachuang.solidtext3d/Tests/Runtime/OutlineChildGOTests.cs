using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Runtime
{
    /// <summary>
    /// Outline child GameObject の foundational RED テスト。
    /// </summary>
    public class OutlineChildGOTests
    {
        private static IEnumerator RegenerateAndWait(SolidText3DComponent component)
        {
            component.RegenerateMesh();
            yield return null;
        }

        private static Material CreateTestMaterial()
        {
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Assert.IsNotNull(shader, "テスト用 Material を生成できる Shader が必要です。");
            return new Material(shader);
        }

        private static PropertyInfo RequireOutlineProperty(string propertyName)
        {
            var property = typeof(SolidText3DComponent).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, $"SolidText3DComponent.{propertyName} が必要です。");
            return property;
        }

        [UnityTest]
        public IEnumerator OutlineOffsetZero_KeepsChildAndClearsMeshUntilOffsetReturns()
        {
            var go = new GameObject("OutlineChildRuntimeTest");
            var component = go.AddComponent<SolidText3DComponent>();
            component.Text = "O";

            var outlineEnabled = RequireOutlineProperty("OutlineEnabled");
            var outlineOffset = RequireOutlineProperty("OutlineOffset");

            outlineEnabled.SetValue(component, true);
            outlineOffset.SetValue(component, 0.05f);
            yield return RegenerateAndWait(component);

            var initialChild = go.transform.Find("__OutlineMesh__");
            Assert.IsNotNull(initialChild, "outline 有効化後に child GameObject が生成されること");

            outlineOffset.SetValue(component, 0f);
            yield return RegenerateAndWait(component);

            var reusedChild = go.transform.Find("__OutlineMesh__");
            Assert.IsNotNull(reusedChild, "OutlineOffset = 0 でも child GameObject を破棄しないこと");
            Assert.AreSame(initialChild.gameObject, reusedChild.gameObject,
                "OutlineOffset = 0 は child を再利用し、Enabled のみがライフサイクルを制御すること");

            var meshFilter = reusedChild.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, "outline child に MeshFilter が存在すること");
            Assert.IsTrue(meshFilter.sharedMesh == null || meshFilter.sharedMesh.vertexCount == 0,
                "OutlineOffset = 0 のとき outline mesh のみクリアされること");

            outlineOffset.SetValue(component, 0.05f);
            yield return RegenerateAndWait(component);

            var rebuiltChild = go.transform.Find("__OutlineMesh__");
            Assert.IsNotNull(rebuiltChild, "offset 再増加後も outline child が残っていること");
            Assert.AreSame(initialChild.gameObject, rebuiltChild.gameObject,
                "offset 再増加時も同じ child GameObject を再利用すること");

            meshFilter = rebuiltChild.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter.sharedMesh, "offset 再増加後に outline mesh が再生成されること");
            Assert.Greater(meshFilter.sharedMesh.vertexCount, 0, "offset 再増加後に outline mesh が空ではないこと");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }

        [UnityTest]
        public IEnumerator OutlineEnabled_FalseDestroysChild_TrueRecreatesItWithRetainedOffset()
        {
            var go = new GameObject("OutlineToggleRuntimeTest");
            var component = go.AddComponent<SolidText3DComponent>();
            component.Text = "O";

            var outlineEnabled = RequireOutlineProperty("OutlineEnabled");
            var outlineOffset = RequireOutlineProperty("OutlineOffset");

            outlineOffset.SetValue(component, 0.05f);
            outlineEnabled.SetValue(component, true);
            yield return RegenerateAndWait(component);

            Assert.IsNotNull(go.transform.Find("__OutlineMesh__"), "outline 有効時に child が生成されること");

            outlineEnabled.SetValue(component, false);
            yield return RegenerateAndWait(component);

            Assert.IsNull(go.transform.Find("__OutlineMesh__"), "OutlineEnabled=false で child が破棄されること");

            outlineEnabled.SetValue(component, true);
            yield return RegenerateAndWait(component);

            Assert.IsNotNull(go.transform.Find("__OutlineMesh__"), "OutlineEnabled=true で child が再生成されること");
            Assert.AreEqual(0.05f, (float)outlineOffset.GetValue(component), 0.0001f,
                "再有効化後も OutlineOffset 設定値が保持されること");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }

        [UnityTest]
        public IEnumerator OutlineMaterial_UsesCustomMaterialOrFallsBackToBodyRenderer()
        {
            var go = new GameObject("OutlineMaterialRuntimeTest");
            var component = go.AddComponent<SolidText3DComponent>();
            component.Text = "O";

            var bodyRenderer = go.GetComponent<MeshRenderer>();
            if (bodyRenderer == null)
                bodyRenderer = go.AddComponent<MeshRenderer>();

            var bodyMaterial = CreateTestMaterial();
            var outlineMaterial = CreateTestMaterial();
            bodyRenderer.sharedMaterial = bodyMaterial;

            var outlineEnabled = RequireOutlineProperty("OutlineEnabled");
            var outlineOffset = RequireOutlineProperty("OutlineOffset");
            var outlineMaterialProperty = RequireOutlineProperty("OutlineMaterial");

            outlineOffset.SetValue(component, 0.05f);
            outlineEnabled.SetValue(component, true);
            outlineMaterialProperty.SetValue(component, null);
            yield return RegenerateAndWait(component);

            var outlineChild = go.transform.Find("__OutlineMesh__");
            Assert.IsNotNull(outlineChild, "outline child が生成されること");

            var childRenderer = outlineChild.GetComponent<MeshRenderer>();
            Assert.AreSame(bodyMaterial, childRenderer.sharedMaterial,
                "OutlineMaterial=null のとき本体 sharedMaterial へフォールバックすること");

            outlineMaterialProperty.SetValue(component, outlineMaterial);
            yield return RegenerateAndWait(component);

            Assert.AreSame(outlineMaterial, childRenderer.sharedMaterial,
                "OutlineMaterial 指定時は outline child に専用 material が適用されること");

#if UNITY_EDITOR
            Object.DestroyImmediate(bodyMaterial);
            Object.DestroyImmediate(outlineMaterial);
            Object.DestroyImmediate(go);
#else
            Object.Destroy(bodyMaterial);
            Object.Destroy(outlineMaterial);
            Object.Destroy(go);
#endif
        }
    }
}