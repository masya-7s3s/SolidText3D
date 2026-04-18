using UnityEditor;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Editor
{
    /// <summary>
    /// SolidText3DComponent 用カスタム Inspector。
    /// プロパティ変更後にシーンビューのリアルタイム更新を強制する。
    /// </summary>
    [CustomEditor(typeof(SolidText3DComponent))]
    public sealed class SolidText3DInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (serializedObject.ApplyModifiedProperties())
            {
                // Inspector でプロパティが変更された際、シーンビューを強制再描画する
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}
