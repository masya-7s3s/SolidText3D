using System.Collections.Generic;
using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// Per-Character モードで使用する子 GameObject プール。
    /// 文字数増加時のみ新規 GameObject を生成し、文字数減少時は SetActive(false) で非アクティブ化する。
    /// Destroy は行わない（FR-009b）。
    /// </summary>
    internal sealed class CharacterObjectPool
    {
        private readonly Transform _parent;
        private readonly List<GameObject> _pool = new List<GameObject>();

        /// <summary>
        /// プールを初期化する。
        /// </summary>
        /// <param name="parent">子 GameObject の親 Transform。</param>
        internal CharacterObjectPool(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// 可視グリフ数に合わせてプールを同期する。
        /// </summary>
        /// <param name="visibleGlyphs">可視グリフのリスト。</param>
        /// <param name="perCharMeshes">各グリフに対応するメッシュのリスト。</param>
        internal void Sync(List<GlyphContour> visibleGlyphs, List<Mesh> perCharMeshes)
        {
            int count = visibleGlyphs != null ? visibleGlyphs.Count : 0;

            // 文字数増加時: 不足分の GameObject を新規作成
            while (_pool.Count < count)
            {
                var go = new GameObject($"Char_{_pool.Count}");
                go.transform.SetParent(_parent, false);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                _pool.Add(go);
            }

            // 各スロットを更新: アクティブ化 + メッシュ設定
            for (int i = 0; i < _pool.Count; i++)
            {
                var go = _pool[i];
                if (i < count)
                {
                    go.SetActive(true);
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null && i < perCharMeshes.Count)
                        mf.sharedMesh = perCharMeshes[i];
                }
                else
                {
                    // 余剰は非アクティブ化（FR-009b, FR-015）
                    go.SetActive(false);
                }
            }
        }

        /// <summary>
        /// プール内の全 GameObject を非アクティブ化する。
        /// PerCharacter → SingleObject 切り替え時や空文字列時（FR-015）に呼び出す。
        /// </summary>
        internal void DeactivateAll()
        {
            foreach (var go in _pool)
            {
                if (go != null)
                    go.SetActive(false);
            }
        }
    }
}
