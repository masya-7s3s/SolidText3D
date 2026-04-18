// テスト実行環境: Intel Core i5 第10世代相当または Apple M1 以上
// それ以下のスペックでは [Ignore] 属性でスキップし警告ログを出力する
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Tests.Runtime
{
    /// <summary>
    /// SC-004 ランタイムパフォーマンスベンチマークテスト（T039）。
    /// 20 個の SolidText3DComponent（各 50 文字）を同一シーンに配置し、
    /// 30 フレームの平均フレーム時間が 16.7ms 未満（60fps 相当）であることを検証する。
    /// </summary>
    public class PerformanceRuntimeTests
    {
        private static string FontPath =>
            Path.GetFullPath("Packages/com.MasaChuang.SolidText3D/Runtime/Resources/Fonts/NotoSansJP-Regular.ttf");

        private const int ComponentCount = 20;
        private const int MeasureFrames = 30;
        private const float MaxAverageDeltaMs = 16.7f; // 60fps

        [UnityTest]
        public IEnumerator TwentyComponents_60FPS_Average()
        {
            // 最低動作スペック以下の環境ではスキップ
            if (SystemInfo.processorFrequency < 2000)
            {
                Assert.Ignore("最低動作保証スペック（2GHz 以上の CPU）以下の環境のためテストをスキップします。");
                yield break;
            }

            var objects = new GameObject[ComponentCount];
            const string text50 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwx";

            // 20 個の SolidText3DComponent を生成
            for (int i = 0; i < ComponentCount; i++)
            {
                objects[i] = new GameObject($"PerfTest_{i}");
                var comp = objects[i].AddComponent<SolidText3DComponent>();
                comp.Font = FontPath;
                comp.Text = text50;
            }

            // 最初の数フレームはウォームアップ
            for (int i = 0; i < 5; i++)
                yield return null;

            // 30 フレーム計測
            float totalDelta = 0f;
            for (int frame = 0; frame < MeasureFrames; frame++)
            {
                yield return null;
                totalDelta += Time.deltaTime;
            }

            float averageDeltaMs = (totalDelta / MeasureFrames) * 1000f;
            Debug.Log($"[PerformanceRuntimeTests] 平均フレーム時間: {averageDeltaMs:F2}ms ({1000f / averageDeltaMs:F1}fps) / 20コンポーネント / {MeasureFrames}フレーム平均");

            // クリーンアップ
            for (int i = 0; i < ComponentCount; i++)
            {
                if (objects[i] != null)
                {
#if UNITY_EDITOR
                    Object.DestroyImmediate(objects[i]);
#else
                    Object.Destroy(objects[i]);
#endif
                }
            }

            Assert.Less(averageDeltaMs, MaxAverageDeltaMs,
                $"平均フレーム時間が {averageDeltaMs:F2}ms でした（上限: {MaxAverageDeltaMs}ms）");
        }
    }
}
