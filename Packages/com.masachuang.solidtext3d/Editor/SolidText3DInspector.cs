using UnityEditor;
using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Editor
{
    /// <summary>
    /// SolidText3DComponent 用カスタム Inspector。
    /// プロパティ変更を dirty として保持し、明示的な再生成操作を提供する。
    /// </summary>
    [CustomEditor(typeof(SolidText3DComponent))]
    public sealed class SolidText3DInspector : UnityEditor.Editor
    {
        private SolidText3DComponent _target;

        private void OnEnable()
        {
            _target = (SolidText3DComponent)target;
        }

        public override void OnInspectorGUI()
        {
            if (_target == null) return;
            serializedObject.Update();

            // ── フォントアセット (.ttf / .otf 限定) ──
            EditorGUI.BeginChangeCheck();
            // typeof(Font) を指定することで ObjectPicker に .ttf/.otf のみ表示される
            var newFontAsset = EditorGUILayout.ObjectField(
                "Font Asset",
                _target.FontAsset,
                typeof(Font),
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
                    "フォントアセット（.ttf / .otf）を設定するか、未設定の場合は NotoSansJP-Black が使用されます。",
                    MessageType.Info);
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
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.Space();

            // ── メッシュ設定 ──
            EditorGUI.BeginChangeCheck();
            float newExtrusion = EditorGUILayout.FloatField("Extrusion Depth", _target.ExtrusionDepth);
            float newLetterSpacing = EditorGUILayout.FloatField("Letter Spacing", _target.LetterSpacing);
            float newLineSpacing = EditorGUILayout.FloatField("Line Spacing", _target.LineSpacing);
            float newFontSize = EditorGUILayout.FloatField("Font Size", _target.FontSize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Mesh Params");
                _target.ExtrusionDepth = newExtrusion;
                _target.LetterSpacing = newLetterSpacing;
                _target.LineSpacing = newLineSpacing;
                _target.FontSize = newFontSize;
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.Space();

            // ── Outline 設定 ──
            EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool newOutlineEnabled = EditorGUILayout.Toggle("Enabled", _target.OutlineEnabled);
            float newOutlineOffset = EditorGUILayout.FloatField("Offset Amount", _target.OutlineOffset);
            float newOutlineThickness = EditorGUILayout.FloatField("Thickness", _target.OutlineThickness);
            var newOutlineDisplayMode = (OutlineDisplayMode)EditorGUILayout.EnumPopup("Display Mode", _target.OutlineDisplayMode);
            var newOutlineMaterial = (Material)EditorGUILayout.ObjectField("Material", _target.OutlineMaterial, typeof(Material), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Outline Settings");
                _target.OutlineEnabled = newOutlineEnabled;
                _target.OutlineOffset = newOutlineOffset;
                _target.OutlineThickness = newOutlineThickness;
                _target.OutlineDisplayMode = newOutlineDisplayMode;
                _target.OutlineMaterial = newOutlineMaterial;
                EditorUtility.SetDirty(_target);
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
            }

            EditorGUILayout.Space();

            // ── 書字方向 ──
            EditorGUI.BeginChangeCheck();
            var newWritingMode = (WritingMode)EditorGUILayout.EnumPopup("Writing Mode", _target.WritingMode);
            bool newRotateAscii = EditorGUILayout.Toggle("Rotate ASCII in Vertical", _target.RotateAsciiInVertical);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Writing Mode");
                _target.WritingMode = newWritingMode;
                _target.RotateAsciiInVertical = newRotateAscii;
                EditorUtility.SetDirty(_target);
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
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Mesh Update", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_target.IsDirty ? "Status: Dirty" : "Status: Up to date");

                if (GUILayout.Button("Regenerate Mesh"))
                {
                    _target.RegenerateMesh();
                    EditorUtility.SetDirty(_target);
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
                EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
