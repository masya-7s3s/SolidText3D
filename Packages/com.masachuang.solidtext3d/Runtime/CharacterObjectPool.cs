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
        /// プールを初期化する。ドメインリロード後などに残留した孤立子オブジェクトも破棄する。
        /// </summary>
        /// <param name="parent">子 GameObject の親 Transform。</param>
        internal CharacterObjectPool(Transform parent)
        {
            _parent = parent;
            DestroyOrphanedChildren();
        }

        /// <summary>
        /// グリフ数に合わせて子 GameObject を同期する。
        /// - 既存 GameObject は再利用してメッシュ・マテリアルを更新する（Destroy しない）。
        /// - 余剰 GameObject は SetActive(false) で非アクティブ化する。
        /// - 不足分のみ新規 GameObject を生成する（FR-009b）。
        /// </summary>
        internal void Sync(List<GlyphContour> visibleGlyphs, List<Mesh> perCharMeshes, Material sharedMaterial = null)
        {
            Sync(perCharMeshes, sharedMaterial);
        }

        internal void Sync(List<Mesh> perCharMeshes, Material sharedMaterial = null)
        {
            int count = perCharMeshes != null ? perCharMeshes.Count : 0;

            // 既存プールエントリを再利用・更新
            for (int i = 0; i < count; i++)
            {
                if (i < _pool.Count)
                {
                    var go = _pool[i];
                    go.SetActive(true);
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null && i < perCharMeshes.Count) mf.sharedMesh = perCharMeshes[i];
                    if (sharedMaterial != null)
                    {
                        var mr = go.GetComponent<MeshRenderer>();
                        if (mr != null) mr.sharedMaterial = sharedMaterial;
                    }
                }
                else
                {
                    var go = new GameObject($"Char_{i}");
                    go.transform.SetParent(_parent, false);
                    var mf = go.AddComponent<MeshFilter>();
                    var mr = go.AddComponent<MeshRenderer>();
                    if (i < perCharMeshes.Count) mf.sharedMesh = perCharMeshes[i];
                    if (sharedMaterial != null) mr.sharedMaterial = sharedMaterial;
                    _pool.Add(go);
                }
            }

            // 余剰 GameObject を非アクティブ化
            for (int i = count; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                    _pool[i].SetActive(false);
            }
        }

        /// <summary>
        /// プール内の全 GameObject を破棄してプールを空にする。
        /// PerCharacter → SingleObject 切り替え時や空文字列時に呼び出す。
        /// </summary>
        internal void DestroyAll()
        {
            foreach (var go in _pool)
            {
                if (go != null)
                    DestroyGameObject(go);
            }
            _pool.Clear();
        }

        /// <summary>
        /// 親の子オブジェクトのうち "Char_" で始まる孤立オブジェクトを破棄する。
        /// ドメインリロード後のプール再生成時に呼び出す。
        /// </summary>
        private void DestroyOrphanedChildren()
        {
            var toDestroy = new List<GameObject>();
            for (int i = 0; i < _parent.childCount; i++)
            {
                var child = _parent.GetChild(i);
                if (child != null && child.name.StartsWith("Char_"))
                    toDestroy.Add(child.gameObject);
            }
            foreach (var go in toDestroy)
                DestroyGameObject(go);
        }

        private static void DestroyGameObject(GameObject go)
        {
#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }
    }
}
