using UnityEngine;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// Unity GameObject に 3D テキストメッシュを追加する MonoBehaviour コンポーネント。
    /// Inspector でパラメータを設定すると、次のフレームで自動的にメッシュが再生成される。
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class SolidText3DComponent : MonoBehaviour
    {
        [SerializeField] private string _text = "Hello, World!";
        [SerializeField] private string _font = "";
        [SerializeField] private float _extrusionDepth = 0.1f;
        [SerializeField] private float _outlineWidth = 0f;
        [SerializeField] private float _letterSpacing = 0f;   // FR-013
        [SerializeField] private float _lineSpacing = 1.2f;   // FR-014
        [SerializeField] private float _bezierErrorThreshold = 0.0005f;

        private bool _isDirty = true;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        // ─── 公開プロパティ ──────────────────────────────────────────────

        /// <summary>表示するテキスト。</summary>
        public string Text
        {
            get => _text;
            set { _text = value ?? string.Empty; _isDirty = true; }
        }

        /// <summary>使用するフォントのパスまたは Resources パス。</summary>
        public string Font
        {
            get => _font;
            set { _font = value; _isDirty = true; }
        }

        /// <summary>押し出し深さ（Z 軸方向）。</summary>
        public float ExtrusionDepth
        {
            get => _extrusionDepth;
            set { _extrusionDepth = value; _isDirty = true; }
        }

        /// <summary>アウトライン幅。</summary>
        public float OutlineWidth
        {
            get => _outlineWidth;
            set { _outlineWidth = value; _isDirty = true; }
        }

        /// <summary>文字間スペース（FR-013）。</summary>
        public float LetterSpacing
        {
            get => _letterSpacing;
            set { _letterSpacing = value; _isDirty = true; }
        }

        /// <summary>行間スペース（FR-014）。</summary>
        public float LineSpacing
        {
            get => _lineSpacing;
            set { _lineSpacing = value; _isDirty = true; }
        }

        /// <summary>ダーティフラグ（テスト・内部デバッグ用）。</summary>
        public bool IsDirty => _isDirty;

        // ─── Unity ライフサイクル ────────────────────────────────────────

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();

            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        private void OnValidate()
        {
            _isDirty = true;
        }

        private void LateUpdate()
        {
            if (_isDirty)
                RegenerateMesh();
        }

        // ─── メッシュ生成 ────────────────────────────────────────────────

        /// <summary>
        /// メッシュを再生成する。ダーティフラグをクリアする。
        /// </summary>
        public void RegenerateMesh()
        {
            _isDirty = false;

            var p = new MeshGenerationParams
            {
                Text = _text,
                FontPath = string.IsNullOrEmpty(_font) ? null : _font,
                FontData = null,
                ExtrusionDepth = _extrusionDepth,
                OutlineWidth = _outlineWidth,
                LetterSpacing = _letterSpacing,
                LineSpacing = _lineSpacing,
                BezierErrorThreshold = _bezierErrorThreshold
            };

            // T030: フォント null 時のデフォルトフォールバックロジック（FR-009）
            // フォントパスが指定されていない場合は GlyphMeshBuilder 内でデフォルトフォントが使用される
            if (string.IsNullOrEmpty(_font))
            {
                Debug.LogWarning("[SolidText3D] フォントが指定されていません。デフォルトフォント（Noto Sans JP）を使用します。");
            }

            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if (_meshFilter != null)
            {
                var mesh = GlyphMeshBuilder.Build(p);
                _meshFilter.sharedMesh = mesh;

                // 空文字列の場合はメッシュをクリア（T023: ゼロポリゴン処理）
                if (string.IsNullOrEmpty(_text))
                {
                    _meshFilter.sharedMesh.Clear();
                }
            }
        }
    }
}
