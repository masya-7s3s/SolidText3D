using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using MasaChuang.SolidText3D.Editor;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    /// <summary>
    /// FontAssetPostprocessor のユニットテスト（T008）。
    /// </summary>
    public class FontAssetPostprocessorTests
    {
        private string _tempDir;
        private string _outputDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SolidText3DTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _outputDir = Path.Combine(_tempDir, "SolidText3DFonts");
            Directory.CreateDirectory(_outputDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        /// <summary>
        /// SC-004 検証: .ttf インポートで .bytes ファイルが生成される。
        /// このテストはドラッグ＆ドロップ 1 操作でセットアップが完了するフロー（SC-004）を確認する。
        /// </summary>
        [Test]
        public void ConvertFontToBytes_TtfFile_CreatesBytesFile()
        {
            // Arrange: ダミー .ttf ファイルを作成
            string ttfPath = Path.Combine(_tempDir, "TestFont.ttf");
            File.WriteAllBytes(ttfPath, new byte[] { 0x00, 0x01, 0x00, 0x00 }); // ダミーバイト
            string guid = System.Guid.NewGuid().ToString("N");
            string outputPath = Path.Combine(_outputDir, guid + ".bytes");

            // Act: FontAssetPostprocessor の変換ロジックを呼び出す
            FontAssetPostprocessor.ConvertFontToBytes(ttfPath, outputPath);

            // Assert: .bytes ファイルが生成されること
            Assert.IsTrue(File.Exists(outputPath), ".ttf インポートで .bytes ファイルが生成されること");
            var bytes = File.ReadAllBytes(outputPath);
            Assert.Greater(bytes.Length, 0, "生成された .bytes ファイルが空でないこと");
        }

        /// <summary>
        /// 既存 .bytes ファイルが存在する場合は再変換をスキップする。
        /// </summary>
        [Test]
        public void ConvertFontToBytes_ExistingBytesFile_Skips()
        {
            // Arrange: ダミー .ttf ファイルと既存 .bytes ファイルを作成
            string ttfPath = Path.Combine(_tempDir, "TestFont.ttf");
            File.WriteAllBytes(ttfPath, new byte[] { 0x00, 0x01, 0x00, 0x00 });
            string guid = System.Guid.NewGuid().ToString("N");
            string outputPath = Path.Combine(_outputDir, guid + ".bytes");
            byte[] existingContent = new byte[] { 0xAB, 0xCD };
            File.WriteAllBytes(outputPath, existingContent);
            var lastWriteTime = File.GetLastWriteTimeUtc(outputPath);

            // Act: 既存 .bytes がある場合は変換をスキップ
            FontAssetPostprocessor.ConvertFontToBytes(ttfPath, outputPath);

            // Assert: ファイルが変更されていないこと（内容・更新時刻ともに同じ）
            var afterWrite = File.GetLastWriteTimeUtc(outputPath);
            Assert.AreEqual(lastWriteTime, afterWrite, "既存 .bytes ファイルは再変換されないこと");
        }

        /// <summary>
        /// I/O エラー発生時は例外をスローせず、出力ファイルも生成しない。
        /// </summary>
        [Test]
        public void ConvertFontToBytes_IoError_DoesNotThrowAndNoOutput()
        {
            // Arrange: 存在しない入力ファイル
            string nonExistentPath = Path.Combine(_tempDir, "NonExistent.ttf");
            string outputPath = Path.Combine(_outputDir, "output.bytes");

            // Act + Assert: 例外がスローされないこと
            // LogError が内部で発生するが、テストフレームワークへの影響を避けるため
            // ignoreFailingMessages で抑制し、出力ファイルが生成されないことで動作を確認する
            LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.DoesNotThrow(() =>
                    FontAssetPostprocessor.ConvertFontToBytes(nonExistentPath, outputPath),
                    "I/O エラー時に例外がスローされないこと");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }

            // 出力ファイルが生成されていないこと（エラー時は何も書き出さない）
            Assert.IsFalse(File.Exists(outputPath), "I/O エラー時に出力ファイルが生成されないこと");
        }
    }
}
