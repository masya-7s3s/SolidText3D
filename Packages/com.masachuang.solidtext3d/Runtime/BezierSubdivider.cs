using System.Collections.Generic;
using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// 二次・三次 Bezier 曲線を適応分割アルゴリズム（再帰的 DeCasteljau 法）で
    /// 頂点列に変換するユーティリティクラス。
    /// </summary>
    public static class BezierSubdivider
    {
        private const int MaxRecursionDepth = 8;

        /// <summary>
        /// 二次 Bezier 曲線を適応分割して頂点列に変換する。
        /// </summary>
        /// <param name="p0">始点</param>
        /// <param name="p1">制御点</param>
        /// <param name="p2">終点</param>
        /// <param name="threshold">誤差閾値（大きいほど粗い近似）</param>
        /// <param name="output">変換結果を追加するリスト</param>
        public static void SubdivideQuadratic(
            Vector2 p0, Vector2 p1, Vector2 p2,
            float threshold, List<Vector2> output)
        {
            SubdivideQuadraticRecursive(p0, p1, p2, threshold, output, 0);
        }

        /// <summary>
        /// 三次 Bezier 曲線を適応分割して頂点列に変換する。
        /// </summary>
        /// <param name="p0">始点</param>
        /// <param name="p1">第 1 制御点</param>
        /// <param name="p2">第 2 制御点</param>
        /// <param name="p3">終点</param>
        /// <param name="threshold">誤差閾値（大きいほど粗い近似）</param>
        /// <param name="output">変換結果を追加するリスト</param>
        public static void SubdivideCubic(
            Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
            float threshold, List<Vector2> output)
        {
            SubdivideCubicRecursive(p0, p1, p2, p3, threshold, output, 0);
        }

        // ─── Private Recursive Implementations ──────────────────────────

        private static void SubdivideQuadraticRecursive(
            Vector2 p0, Vector2 p1, Vector2 p2,
            float threshold, List<Vector2> output, int depth)
        {
            // DeCasteljau t=0.5 の中点
            Vector2 mid = 0.25f * p0 + 0.5f * p1 + 0.25f * p2;
            Vector2 chord = (p0 + p2) * 0.5f;

            if (depth >= MaxRecursionDepth || Vector2.Distance(mid, chord) <= threshold)
            {
                // 精度 OK → 終点追加
                output.Add(p2);
                return;
            }

            // 分割
            Vector2 q0 = (p0 + p1) * 0.5f;
            Vector2 q1 = (p1 + p2) * 0.5f;
            Vector2 q2 = (q0 + q1) * 0.5f;

            SubdivideQuadraticRecursive(p0, q0, q2, threshold, output, depth + 1);
            SubdivideQuadraticRecursive(q2, q1, p2, threshold, output, depth + 1);
        }

        private static void SubdivideCubicRecursive(
            Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
            float threshold, List<Vector2> output, int depth)
        {
            // 曲線の平坦度チェック: 制御点と弦の最大距離
            Vector2 d1 = p1 - (p0 * 2f / 3f + p3 * 1f / 3f);
            Vector2 d2 = p2 - (p0 * 1f / 3f + p3 * 2f / 3f);
            float flatness = Mathf.Max(d1.magnitude, d2.magnitude);

            if (depth >= MaxRecursionDepth || flatness <= threshold)
            {
                output.Add(p3);
                return;
            }

            // DeCasteljau t=0.5 分割
            Vector2 q0 = (p0 + p1) * 0.5f;
            Vector2 q1 = (p1 + p2) * 0.5f;
            Vector2 q2 = (p2 + p3) * 0.5f;
            Vector2 r0 = (q0 + q1) * 0.5f;
            Vector2 r1 = (q1 + q2) * 0.5f;
            Vector2 s  = (r0 + r1) * 0.5f;

            SubdivideCubicRecursive(p0, q0, r0, s,  threshold, output, depth + 1);
            SubdivideCubicRecursive(s,  r1, q2, p3, threshold, output, depth + 1);
        }
    }
}
