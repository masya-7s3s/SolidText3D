using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MasaChuang.SolidText3D.Editor
{
    /// <summary>
    /// .ttf/.otf ファイルをインポートした際に自動で .bytes ファイルを生成する AssetPostprocessor。
    /// 生成先: Assets/SolidText3DFonts/{assetGuid}.bytes
    /// </summary>
    public sealed class FontAssetPostprocessor : AssetPostprocessor
    {
        private const string OutputFolder = "Assets/SolidText3DFonts";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool changed = false;

            foreach (string assetPath in importedAssets)
            {
                string ext = Path.GetExtension(assetPath).ToLowerInvariant();
                if (ext != ".ttf" && ext != ".otf")
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                    continue;

                // 出力先: Assets/SolidText3DFonts/{guid}.bytes
                if (!Directory.Exists(OutputFolder))
                    Directory.CreateDirectory(OutputFolder);

                string outputPath = $"{OutputFolder}/{guid}.bytes";
                string fullInputPath = Path.GetFullPath(assetPath);
                string fullOutputPath = Path.GetFullPath(outputPath);

                ConvertFontToBytes(fullInputPath, fullOutputPath);

                // AssetDatabase に登録
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

                // シーン内の SolidText3DComponent を走査して _fontBytesCache を自動設定
                var bytesAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath);
                if (bytesAsset != null)
                {
                    var components = UnityEngine.Object.FindObjectsByType<SolidText3DComponent>(
                        FindObjectsSortMode.None);
                    foreach (var comp in components)
                    {
                        if (comp.FontAsset == null) continue;
                        string compAssetPath = AssetDatabase.GetAssetPath(comp.FontAsset);
                        if (compAssetPath == assetPath)
                        {
                            // SerializedObject 経由で _fontBytesCache を設定
                            var so = new SerializedObject(comp);
                            var prop = so.FindProperty("_fontBytesCache");
                            if (prop != null)
                            {
                                prop.objectReferenceValue = bytesAsset;
                                so.ApplyModifiedPropertiesWithoutUndo();
                                EditorUtility.SetDirty(comp);
                            }
                        }
                    }
                    AssetDatabase.SaveAssets();
                    changed = true;
                }
            }

            if (changed)
                AssetDatabase.Refresh();
        }

        /// <summary>
        /// フォントファイルを .bytes ファイルに変換する（内部ヘルパー。テストから呼び出し可能）。
        /// 既存ファイルが存在する場合はスキップする。
        /// I/O エラーは LogError で記録し、例外を外部に伝播させない。
        /// </summary>
        /// <param name="inputPath">変換元フォントファイルの絶対パス。</param>
        /// <param name="outputPath">出力先 .bytes ファイルの絶対パス。</param>
        /// <param name="logError">エラーメッセージを受け取るデリゲート。null の場合は Debug.LogError を使用する（テスト時に差し替え可能）。</param>
        public static void ConvertFontToBytes(string inputPath, string outputPath, Action<string> logError = null)
        {
            // 既存の .bytes ファイルがある場合はスキップ
            if (File.Exists(outputPath))
                return;

            try
            {
                byte[] fontBytes = File.ReadAllBytes(inputPath);

                // アトミック書き込み: 一時ファイル経由で書き込み後にリネーム（FR-017）
                string tempPath = outputPath + ".tmp";
                File.WriteAllBytes(tempPath, fontBytes);
                File.Move(tempPath, outputPath);
            }
            catch (Exception ex)
            {
                string message = $"[SolidText3D] フォントの .bytes 変換に失敗しました: {inputPath}\n{ex.Message}";
                if (logError != null)
                    logError(message);
                else
                    Debug.LogError(message);
            }
        }
    }
}
