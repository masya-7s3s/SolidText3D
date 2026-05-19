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
        private const float SectionSpacing = 6f;

        private SolidText3DComponent _target;

        private void OnEnable()
        {
            _target = (SolidText3DComponent)target;
        }

        private static void DrawSectionHeader(string title, string description)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(description))
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(2f);
        }

        private static void DrawDirtyStatusPill(bool isDirty)
        {
            var style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 4, 4)
            };

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = isDirty
                ? new Color(0.95f, 0.72f, 0.25f)
                : new Color(0.58f, 0.8f, 0.56f);

            GUILayout.Label(isDirty ? "Dirty" : "Up to date", style, GUILayout.Width(96f));
            GUI.backgroundColor = previousColor;
        }

        private void DrawMeshUpdateSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("Mesh Update", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(
                            _target.IsDirty
                                ? "パラメータ変更が未反映です。内容を確認してから再生成してください。"
                                : "現在の設定はメッシュに反映済みです。",
                            EditorStyles.wordWrappedMiniLabel);
                    }

                    GUILayout.FlexibleSpace();
                    DrawDirtyStatusPill(_target.IsDirty);
                }

                EditorGUILayout.Space(4f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Regenerate Mesh", GUILayout.Height(28f)))
                    {
                        Undo.RecordObject(_target, "Regenerate SolidText3D Mesh");
                        _target.RegenerateMesh();
                        EditorUtility.SetDirty(_target);
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
            }
        }

        private void DrawFontAndTextSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSectionHeader("Text & Font", "テキスト内容とフォントを調整するセクションです。");

                EditorGUI.BeginChangeCheck();
                var newFontAsset = EditorGUILayout.ObjectField(
                    "Font Asset",
                    _target.FontAsset,
                    typeof(Font),
                    false);

                EditorGUILayout.LabelField("Text", EditorStyles.miniBoldLabel);
                string newText = EditorGUILayout.TextArea(_target.Text, GUILayout.MinHeight(72f));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Change Text And Font");
                    _target.FontAsset = newFontAsset;
                    _target.Text = newText;
                    EditorUtility.SetDirty(_target);
                }

                if (_target.FontAsset == null)
                {
                    EditorGUILayout.HelpBox(
                        "Font Asset 未設定時は NotoSansJP-Black を使用します。専用フォントを使う場合はここで差し替えてください。",
                        MessageType.Info);
                }
            }
        }

        private void DrawGeometrySection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSectionHeader("Geometry", "文字の厚みと基本的な組版スケールを調整します。");

                EditorGUI.BeginChangeCheck();
                float newExtrusion = EditorGUILayout.FloatField("Extrusion Depth", _target.ExtrusionDepth);
                float newFontSize = EditorGUILayout.FloatField("Font Size", _target.FontSize);
                float newLetterSpacing = EditorGUILayout.FloatField("Letter Spacing", _target.LetterSpacing);
                float newLineSpacing = EditorGUILayout.FloatField("Line Spacing", _target.LineSpacing);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Change Geometry");
                    _target.ExtrusionDepth = newExtrusion;
                    _target.FontSize = newFontSize;
                    _target.LetterSpacing = newLetterSpacing;
                    _target.LineSpacing = newLineSpacing;
                    EditorUtility.SetDirty(_target);
                }
            }
        }

        private void DrawLayoutSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSectionHeader("Layout", "配置方向、アンカー、折り返し範囲をまとめています。");

                EditorGUI.BeginChangeCheck();
                var newWritingMode = (WritingMode)EditorGUILayout.EnumPopup("Writing Mode", _target.WritingMode);
                bool newMonospaceMode = EditorGUILayout.Toggle("Monospace Mode", _target.MonospaceMode);
                bool newRotateAscii = EditorGUILayout.Toggle("Rotate ASCII in Vertical", _target.RotateAsciiInVertical);

                if (newWritingMode != WritingMode.Vertical)
                {
                    EditorGUILayout.HelpBox("Rotate ASCII in Vertical は縦書き時のみ有効です。", MessageType.None);
                }

                var newHorizontalAnchor = (HorizontalAnchor)EditorGUILayout.EnumPopup("Horizontal Anchor", _target.HorizontalAnchor);
                var newVerticalAnchor = (VerticalAnchor)EditorGUILayout.EnumPopup("Vertical Anchor", _target.VerticalAnchor);
                var newDepthAnchor = (DepthAnchor)EditorGUILayout.EnumPopup("Depth Anchor", _target.DepthAnchor);
                float newMaxWidth = EditorGUILayout.FloatField("Max Width", _target.MaxWidth);
                float newMaxHeight = EditorGUILayout.FloatField("Max Height", _target.MaxHeight);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Change Layout");
                    _target.WritingMode = newWritingMode;
                    _target.MonospaceMode = newMonospaceMode;
                    _target.RotateAsciiInVertical = newRotateAscii;
                    _target.HorizontalAnchor = newHorizontalAnchor;
                    _target.VerticalAnchor = newVerticalAnchor;
                    _target.DepthAnchor = newDepthAnchor;
                    _target.MaxWidth = newMaxWidth;
                    _target.MaxHeight = newMaxHeight;
                    EditorUtility.SetDirty(_target);
                }
            }
        }

        private void DrawOutlineSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSectionHeader("Outline", "アウトライン生成が必要なときだけ詳細を開いて調整します。");

                EditorGUI.BeginChangeCheck();
                bool newOutlineEnabled = EditorGUILayout.Toggle("Enabled", _target.OutlineEnabled);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Toggle Outline");
                    _target.OutlineEnabled = newOutlineEnabled;
                    EditorUtility.SetDirty(_target);
                }

                using (new EditorGUI.DisabledScope(!_target.OutlineEnabled))
                {
                    EditorGUI.BeginChangeCheck();
                    float newOutlineOffset = EditorGUILayout.FloatField("Offset Amount", _target.OutlineOffset);
                    float newOutlineThickness = EditorGUILayout.FloatField("Thickness Ratio (Body=1)", _target.OutlineThickness);
                    var newOutlineDisplayMode = (OutlineDisplayMode)EditorGUILayout.EnumPopup("Display Mode", _target.OutlineDisplayMode);
                    var newOutlineMaterial = (Material)EditorGUILayout.ObjectField("Material", _target.OutlineMaterial, typeof(Material), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_target, "Change Outline Settings");
                        _target.OutlineOffset = newOutlineOffset;
                        _target.OutlineThickness = newOutlineThickness;
                        _target.OutlineDisplayMode = newOutlineDisplayMode;
                        _target.OutlineMaterial = newOutlineMaterial;
                        EditorUtility.SetDirty(_target);
                    }

                    EditorGUILayout.HelpBox("Thickness Ratio は本体の Extrusion Depth を 1 とした相対値です。1 で本体と同じ厚さになります。", MessageType.None);
                }

                if (!_target.OutlineEnabled)
                {
                    EditorGUILayout.HelpBox("Outline を有効にすると、太さ・表示モード・専用 Material を調整できます。", MessageType.None);
                }
            }
        }

        private void DrawOutputSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSectionHeader("Output", "生成結果の出し方に関わる設定です。");

                EditorGUI.BeginChangeCheck();
                var newObjectMode = (ObjectMode)EditorGUILayout.EnumPopup("Object Mode", _target.ObjectMode);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Change Object Mode");
                    _target.ObjectMode = newObjectMode;
                    EditorUtility.SetDirty(_target);
                }

                EditorGUILayout.LabelField(
                    newObjectMode == ObjectMode.PerCharacter
                        ? "各文字を個別 GameObject として生成します。"
                        : "1つのメッシュとしてまとめて生成します。",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        public override void OnInspectorGUI()
        {
            if (_target == null) return;
            serializedObject.Update();

            DrawMeshUpdateSection();
            EditorGUILayout.Space(SectionSpacing);
            DrawFontAndTextSection();
            EditorGUILayout.Space(SectionSpacing);
            DrawGeometrySection();
            EditorGUILayout.Space(SectionSpacing);
            DrawLayoutSection();
            EditorGUILayout.Space(SectionSpacing);
            DrawOutlineSection();
            EditorGUILayout.Space(SectionSpacing);
            DrawOutputSection();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
                EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
