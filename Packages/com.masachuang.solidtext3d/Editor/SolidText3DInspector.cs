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
        private SolidText3DComponent _target;
        private bool _wasEditingTextField;

        private void OnEnable()
        {
            _target = (SolidText3DComponent)target;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_target == null) return;

            bool isEditing = EditorGUIUtility.editingTextField;
            if (_wasEditingTextField && !isEditing)
            {
                // フォーカスアウト: 再生成を実行して抑制を解除
                _target.SuppressAutoRegenerate = false;
                _target.RegenerateMesh();
                EditorApplication.QueuePlayerLoopUpdate();
            }
            _wasEditingTextField = isEditing;
        }

        public override void OnInspectorGUI()
        {
            if (_target == null) return;
            serializedObject.Update();

            // ── フォントアセット ──
            EditorGUI.BeginChangeCheck();
            var newFontAsset = EditorGUILayout.ObjectField(
                "Font Asset",
                _target.FontAsset,
                typeof(UnityEngine.Object),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Font Asset");
                _target.FontAsset = newFontAsset;
                EditorUtility.SetDirty(_target);
            }

            if (_target.FontAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "フォントアセット（.ttf / .otf）を設定してください。",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();

            // ── テキスト（デバウンス対応）──
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Text");
            string newText = EditorGUILayout.TextArea(_target.Text, GUILayout.MinHeight(60f));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Text");
                _target.Text = newText;
                _target.SuppressAutoRegenerate = true; // 入力中は再生成を抑制
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.Space();

            // ── メッシュ設定 ──
            EditorGUI.BeginChangeCheck();
            float newExtrusion = EditorGUILayout.FloatField("Extrusion Depth", _target.ExtrusionDepth);
            float newOutline = EditorGUILayout.FloatField("Outline Width", _target.OutlineWidth);
            float newLetterSpacing = EditorGUILayout.FloatField("Letter Spacing", _target.LetterSpacing);
            float newLineSpacing = EditorGUILayout.FloatField("Line Spacing", _target.LineSpacing);
            float newFontSize = EditorGUILayout.FloatField("Font Size", _target.FontSize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Mesh Params");
                _target.ExtrusionDepth = newExtrusion;
                _target.OutlineWidth = newOutline;
                _target.LetterSpacing = newLetterSpacing;
                _target.LineSpacing = newLineSpacing;
                _target.FontSize = newFontSize;
                EditorUtility.SetDirty(_target);
                // FloatField は Enter/ブラーで値が確定されるため常に即時再生成する
                _target.RegenerateMesh();
                EditorApplication.QueuePlayerLoopUpdate();
            }

            EditorGUILayout.Space();

            // ── アンカー設定 ──
            EditorGUI.BeginChangeCheck();
            var newH = (HorizontalAnchor)EditorGUILayout.EnumPopup("Horizontal Anchor", _target.HorizontalAnchor);
            var newV = (VerticalAnchor)EditorGUILayout.EnumPopup("Vertical Anchor", _target.VerticalAnchor);
            var newD = (DepthAnchor)EditorGUILayout.EnumPopup("Depth Anchor", _target.DepthAnchor);
            float newMaxWidth = EditorGUILayout.FloatField("Max Width (0=∞)", _target.MaxWidth);
            float newMaxHeight = EditorGUILayout.FloatField("Max Height (0=∞)", _target.MaxHeight);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Anchor");
                _target.HorizontalAnchor = newH;
                _target.VerticalAnchor = newV;
                _target.DepthAnchor = newD;
                _target.MaxWidth = newMaxWidth;
                _target.MaxHeight = newMaxHeight;
                EditorUtility.SetDirty(_target);
                _target.RegenerateMesh();
                EditorApplication.QueuePlayerLoopUpdate();
            }

            EditorGUILayout.Space();

            // ── 書字方向 ──
            EditorGUI.BeginChangeCheck();
            var newWritingMode = (WritingMode)EditorGUILayout.EnumPopup("Writing Mode", _target.WritingMode);
            float newColWidth = EditorGUILayout.FloatField("Vertical Column Width (0=auto)", _target.VerticalColumnWidth);
            bool newRotateAscii = EditorGUILayout.Toggle("Rotate ASCII in Vertical", _target.RotateAsciiInVertical);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Writing Mode");
                _target.WritingMode = newWritingMode;
                _target.VerticalColumnWidth = newColWidth;
                _target.RotateAsciiInVertical = newRotateAscii;
                EditorUtility.SetDirty(_target);
                _target.RegenerateMesh();
                EditorApplication.QueuePlayerLoopUpdate();
            }

            EditorGUILayout.Space();

            // ── オブジェクトモード ──
            EditorGUI.BeginChangeCheck();
            var newObjectMode = (ObjectMode)EditorGUILayout.EnumPopup("Object Mode", _target.ObjectMode);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Object Mode");
                _target.ObjectMode = newObjectMode;
                EditorUtility.SetDirty(_target);
                _target.RegenerateMesh();
                EditorApplication.QueuePlayerLoopUpdate();
            }

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
                EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
