using UnityEngine;
using UnityEngine.Profiling;

namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// Unity GameObject に 3D テキストメッシュを追加する MonoBehaviour コンポーネント。
    /// Inspector やスクリプトでパラメータを変更すると dirty になり、明示的な再生成でメッシュを更新する。
    /// </summary>
    /// <remarks>
    /// [ExecuteAlways] により Edit Mode でもライフサイクルは維持されるが、
    /// メッシュ更新は RegenerateMesh() の明示呼び出しでのみ行われる。
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class SolidText3DComponent : MonoBehaviour
    {
        private const string DefaultFontBytesAssetPath = "Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes";
        private const string GeneratedFontBytesFolder = "Assets/SolidText3DFonts";

        [SerializeField] private string _text = "Hello, World!";
        [SerializeField] private UnityEngine.Object _fontAsset;
        [SerializeField, HideInInspector] private TextAsset _fontBytesCache;
        [SerializeField] private float _extrusionDepth = 0.25f;
        [SerializeField] private float _outlineWidth = 0f;
        [SerializeField] private OutlineSettings _outline = new OutlineSettings();
        [SerializeField] private float _letterSpacing = 0f;
        [SerializeField] private float _lineSpacing = 1f;
        [SerializeField] private float _bezierErrorThreshold = 0.0005f;
        [SerializeField] private float _fontSize = 1f;
        [SerializeField] private HorizontalAnchor _horizontalAnchor = HorizontalAnchor.Left;
        [SerializeField] private VerticalAnchor _verticalAnchor = VerticalAnchor.Lower;
        [SerializeField] private DepthAnchor _depthAnchor = DepthAnchor.Front;
        [SerializeField] private WritingMode _writingMode = WritingMode.Horizontal;
        [SerializeField] private ObjectMode _objectMode = ObjectMode.SingleObject;
        [SerializeField] private float _maxWidth = 0f;
        [SerializeField] private float _maxHeight = 0f;
        [SerializeField] private bool _rotateAsciiInVertical = false;

        private bool _isDirty = true;
        private bool _suppressAutoRegenerate = false;
        private bool _fontMissingWarningIssued = false;
        private int _lastParamHash = 0;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private CharacterObjectPool _characterPool;
        private GameObject _outlineChild;
        private MeshFilter _outlineMeshFilter;
        private MeshRenderer _outlineRenderer;

        private const string OutlineChildName = "__OutlineMesh__";

        // ─── 公開プロパティ ──────────────────────────────────────────────

        /// <summary>表示するテキスト。</summary>
        public string Text
        {
            get => _text;
            set { _text = value ?? string.Empty; _isDirty = true; }
        }

        /// <summary>
        /// Inspector でアタッチされた .ttf/.otf フォントアセット参照。
        /// </summary>
        /// <param name="value">UnityEngine.Object 型のフォントアセット。null の場合はフォント未設定扱いとなる。</param>
        /// <returns>現在設定されているフォントアセット。</returns>
        public UnityEngine.Object FontAsset
        {
            get => _fontAsset;
            set
            {
                _fontAsset = value;
#if UNITY_EDITOR
                _fontBytesCache = ResolveEditorFontBytesCache(value);
#else
                if (value == null) _fontBytesCache = null;
#endif
                _fontMissingWarningIssued = false;
                _isDirty = true;
            }
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
            get => OutlineOffset;
            set => OutlineOffset = value;
        }

        /// <summary>
        /// outline を有効化するかどうか。
        /// </summary>
        /// <example>
        /// <code>
        /// component.OutlineEnabled = true;
        /// </code>
        /// </example>
        public bool OutlineEnabled
        {
            get => _outline != null && _outline.Enabled;
            set
            {
                EnsureOutlineSettingsInitialized();
                if (_outline.Enabled == value)
                    return;

                _outline.Enabled = value;
                _isDirty = true;
            }
        }

        /// <summary>
        /// outline の外側オフセット量。0 以上。
        /// </summary>
        /// <example>
        /// <code>
        /// component.OutlineOffset = 0.05f;
        /// </code>
        /// </example>
        public float OutlineOffset
        {
            get => _outline != null ? _outline.OffsetAmount : 0f;
            set
            {
                EnsureOutlineSettingsInitialized();
                float sanitized = Mathf.Max(0f, value);
                if (Mathf.Approximately(_outline.OffsetAmount, sanitized))
                    return;

                _outline.OffsetAmount = sanitized;
                _outlineWidth = sanitized;
                _isDirty = true;
            }
        }

        /// <summary>
        /// outline の奥行き。0 以上。
        /// </summary>
        /// <example>
        /// <code>
        /// component.OutlineThickness = 0.1f;
        /// </code>
        /// </example>
        public float OutlineThickness
        {
            get => _outline != null ? _outline.Thickness : 0f;
            set
            {
                EnsureOutlineSettingsInitialized();
                float sanitized = Mathf.Max(0f, value);
                if (Mathf.Approximately(_outline.Thickness, sanitized))
                    return;

                _outline.Thickness = sanitized;
                _isDirty = true;
            }
        }

        /// <summary>
        /// outline 専用マテリアル。null の場合は本体 sharedMaterial を使用する。
        /// </summary>
        /// <example>
        /// <code>
        /// component.OutlineMaterial = outlineMaterial;
        /// </code>
        /// </example>
        public Material OutlineMaterial
        {
            get => _outline != null ? _outline.Material : null;
            set
            {
                EnsureOutlineSettingsInitialized();
                if (_outline.Material == value)
                    return;

                _outline.Material = value;
                _isDirty = true;
            }
        }

        /// <summary>
        /// outline の表示モード。
        /// </summary>
        /// <example>
        /// <code>
        /// component.OutlineDisplayMode = OutlineDisplayMode.Donut;
        /// </code>
        /// </example>
        public OutlineDisplayMode OutlineDisplayMode
        {
            get => _outline != null ? _outline.DisplayMode : OutlineDisplayMode.Donut;
            set
            {
                EnsureOutlineSettingsInitialized();
                if (_outline.DisplayMode == value)
                    return;

                _outline.DisplayMode = value;
                _isDirty = true;
            }
        }

        /// <summary>文字間スペース。</summary>
        public float LetterSpacing
        {
            get => _letterSpacing;
            set { _letterSpacing = value; _isDirty = true; }
        }

        /// <summary>行間スペース。</summary>
        public float LineSpacing
        {
            get => _lineSpacing;
            set { _lineSpacing = value; _isDirty = true; }
        }

        /// <summary>フォントサイズ（Unity ワールド単位）。1 = em スクエアの高さが 1 Unity unit。</summary>
        public float FontSize
        {
            get => _fontSize;
            set { _fontSize = Mathf.Max(0.001f, value); _isDirty = true; }
        }

        /// <summary>
        /// 水平方向のアンカー位置。
        /// </summary>
        /// <param name="value">設定する HorizontalAnchor 値。</param>
        /// <returns>現在の水平アンカー設定。</returns>
        public HorizontalAnchor HorizontalAnchor
        {
            get => _horizontalAnchor;
            set { _horizontalAnchor = value; _isDirty = true; }
        }

        /// <summary>
        /// 垂直方向のアンカー位置。
        /// </summary>
        /// <param name="value">設定する VerticalAnchor 値。</param>
        /// <returns>現在の垂直アンカー設定。</returns>
        public VerticalAnchor VerticalAnchor
        {
            get => _verticalAnchor;
            set { _verticalAnchor = value; _isDirty = true; }
        }

        /// <summary>
        /// 奥行き方向のアンカー位置。
        /// </summary>
        /// <param name="value">設定する DepthAnchor 値。</param>
        /// <returns>現在の奥行きアンカー設定。</returns>
        public DepthAnchor DepthAnchor
        {
            get => _depthAnchor;
            set { _depthAnchor = value; _isDirty = true; }
        }

        /// <summary>
        /// 書字方向（横書き / 縦書き）。
        /// </summary>
        /// <param name="value">設定する WritingMode 値。</param>
        /// <returns>現在の書字方向設定。</returns>
        public WritingMode WritingMode
        {
            get => _writingMode;
            set { _writingMode = value; _isDirty = true; }
        }

        /// <summary>
        /// テキストの GameObject 生成モード（SingleObject / PerCharacter）。
        /// </summary>
        public ObjectMode ObjectMode
        {
            get => _objectMode;
            set { if (_objectMode == value) return; _objectMode = value; _isDirty = true; }
        }

        /// <summary>
        /// 横書き時の自動折り返し幅（0 = 無制限）。
        /// </summary>
        /// <param name="value">折り返し幅（Unity ワールド単位）。</param>
        /// <returns>現在の最大幅設定。</returns>
        public float MaxWidth
        {
            get => _maxWidth;
            set { _maxWidth = value; _isDirty = true; }
        }

        /// <summary>
        /// 縦書き時の自動折り返し高さ（0 = 無制限）。
        /// </summary>
        /// <param name="value">折り返し高さ（Unity ワールド単位）。</param>
        /// <returns>現在の最大高さ設定。</returns>
        public float MaxHeight
        {
            get => _maxHeight;
            set { _maxHeight = value; _isDirty = true; }
        }

        /// <summary>
        /// 縦書き時に ASCII 英数字を 90 度回転するかどうか。
        /// </summary>
        /// <param name="value">true の場合、縦書き時に ASCII 英数字を回転する。</param>
        /// <returns>現在の ASCII 回転設定。</returns>
        public bool RotateAsciiInVertical
        {
            get => _rotateAsciiInVertical;
            set { _rotateAsciiInVertical = value; _isDirty = true; }
        }

        /// <summary>ダーティフラグ（テスト・内部デバッグ用）。</summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// true の間、LateUpdate による自動メッシュ再生成を抑制する。
        /// SolidText3DInspector が入力デバウンス制御に使用する。
        /// </summary>
        public bool SuppressAutoRegenerate
        {
            get => _suppressAutoRegenerate;
            set => _suppressAutoRegenerate = value;
        }

        // ─── Unity ライフサイクル ────────────────────────────────────────

        private void Awake()
        {
            EnsureOutlineSettingsInitialized();
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();

            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();

            // URP プロジェクトではデフォルトマテリアル（Standard シェーダー）が紫色になるため
            // null・エラーシェーダー・ビルトインデフォルトの場合は URP 互換マテリアルに差し替える
            var mat = _meshRenderer.sharedMaterial;
            bool needsDefault = mat == null
                || mat.shader == null
                || mat.shader.name.StartsWith("Hidden/")
                || mat.name == "Default-Material";

            if (needsDefault)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Standard");
                if (shader != null)
                    _meshRenderer.sharedMaterial = new Material(shader) { name = "SolidText3D Default" };
            }

            // フォント未設定時はパッケージ内の NotoSansJP-Black をデフォルトフォントとして自動設定
            if (_fontAsset == null && _fontBytesCache == null)
            {
#if UNITY_EDITOR
                var defaultFont = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    DefaultFontBytesAssetPath);
                if (defaultFont != null)
                    _fontBytesCache = defaultFont as TextAsset;
#else
                var defaultAsset = Resources.Load<TextAsset>("Fonts/NotoSansJP-Black");
                if (defaultAsset != null)
                    _fontBytesCache = defaultAsset;
#endif
            }

            if (_objectMode == ObjectMode.PerCharacter)
                _characterPool = new CharacterObjectPool(transform);
        }

        private void OnValidate()
        {
            EnsureOutlineSettingsInitialized();
#if UNITY_EDITOR
            if (_fontAsset != null)
                _fontBytesCache = ResolveEditorFontBytesCache(_fontAsset);
#endif
            _isDirty = true;
        }

        private void OnDestroy()
        {
            DestroyOutlineChildIfExists();
        }

        private void LateUpdate()
        {
            // 自動再生成は行わない。dirty 状態は明示的な RegenerateMesh() 呼び出しまで維持する。
        }

        // ─── メッシュ生成 ────────────────────────────────────────────────

        /// <summary>
        /// メッシュを再生成する。ダーティフラグをクリアする。
        /// </summary>
        public void RegenerateMesh()
        {
            Profiler.BeginSample("SolidText3DComponent.RegenerateMesh");
            try
            {
                _isDirty = false;

                // FR-012: フォント未設定時はメッシュ生成をスキップ（警告を1回のみ出力）
                if (_fontAsset == null && _fontBytesCache == null)
                {
                    if (!_fontMissingWarningIssued)
                    {
                        Debug.LogWarning("[SolidText3D] フォントが設定されていません。FontAsset を Inspector でアタッチしてください。");
                        _fontMissingWarningIssued = true;
                    }
                    return;
                }

                // FR-015: 空文字列時はメッシュをクリアし、PerCharacter 全子 GameObject を破棄
                if (string.IsNullOrEmpty(_text))
                {
                    if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
                    if (_meshFilter != null && _meshFilter.sharedMesh != null)
                        _meshFilter.sharedMesh.Clear();
                    _characterPool?.DestroyAll();
                    _characterPool = null;
                    return;
                }

                // FR-016: フォントデータが取得できない場合は直前メッシュを維持し警告を1回のみ出力
                byte[] fontBytes = GetFontBytes();
                if (fontBytes == null || fontBytes.Length == 0)
                {
                    if (!_fontMissingWarningIssued)
                    {
                        Debug.LogWarning("[SolidText3D] フォントデータを読み込めませんでした。直前のメッシュを維持します。");
                        _fontMissingWarningIssued = true;
                    }
                    return;
                }

                // FR-007: 同一パラメータ時は再生成をスキップ（空文字列は常にダーティ扱い）
                int currentHash = ComputeParamHash();
                if (currentHash == _lastParamHash)
                    return;
                _lastParamHash = currentHash;

                var p = new MeshGenerationParams
                {
                    Text = _text,
                    FontData = fontBytes,
                    ExtrusionDepth = _extrusionDepth,
                    OutlineWidth = _outlineWidth,
                    LetterSpacing = _letterSpacing,
                    LineSpacing = _lineSpacing,
                    BezierErrorThreshold = _bezierErrorThreshold,
                    FontSize = _fontSize,
                    HorizontalAnchor = _horizontalAnchor,
                    VerticalAnchor = _verticalAnchor,
                    DepthAnchor = _depthAnchor,
                    WritingMode = _writingMode,
                    MaxWidth = _maxWidth,
                    MaxHeight = _maxHeight,
                    RotateAsciiInVertical = _rotateAsciiInVertical
                };

                if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();

                if (_objectMode == ObjectMode.PerCharacter)
                {
                    // SingleObject のメッシュを非表示にして PerCharacter と重複しないようにする
                    if (_meshFilter != null) _meshFilter.sharedMesh = null;
                    if (_meshRenderer != null) _meshRenderer.enabled = false;

                    // 毎回再生成（テキスト変更時に子を作り直す・MissingReference 防止）
                    if (_characterPool == null)
                        _characterPool = new CharacterObjectPool(transform);

                    var mat = _meshRenderer != null ? _meshRenderer.sharedMaterial : null;
                    var perCharResult = GlyphMeshBuilder.BuildPerCharacter(p);
                    _characterPool.Sync(perCharResult.Glyphs, perCharResult.Meshes, mat);
                    UpdateOutlineMesh(p);
                }
                else
                {
                    // SingleObject に切り替わった際に子オブジェクトを破棄
                    _characterPool?.DestroyAll();
                    _characterPool = null;

                    if (_meshRenderer != null) _meshRenderer.enabled = true;
                    if (_meshFilter != null)
                        _meshFilter.sharedMesh = GlyphMeshBuilder.Build(p);

                    UpdateOutlineMesh(p);
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        // ─── 内部ヘルパー ────────────────────────────────────────────────

        private byte[] GetFontBytes()
        {
#if UNITY_EDITOR
            byte[] fontBytes = TryReadEditorFontBytesFromAsset(_fontAsset);
            if (fontBytes != null && fontBytes.Length > 0)
                return fontBytes;

            if (_fontBytesCache != null)
                return _fontBytesCache.bytes;

            var textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(
                DefaultFontBytesAssetPath);
            if (textAsset != null)
                return textAsset.bytes;
#else
            if (_fontBytesCache != null)
                return _fontBytesCache.bytes;

            var defaultAsset = Resources.Load<TextAsset>("Fonts/NotoSansJP-Black");
            if (defaultAsset != null)
                return defaultAsset.bytes;
#endif
            return null;
        }

        // GC Alloc ゼロのハッシュ計算（XOR 結合のみ、new/LINQ/文字列連結なし）
        private int ComputeParamHash()
        {
            EnsureOutlineSettingsInitialized();
            int hash = _text != null ? _text.GetHashCode() : 0;
            hash ^= _horizontalAnchor.GetHashCode();
            hash ^= _verticalAnchor.GetHashCode();
            hash ^= _depthAnchor.GetHashCode();
            hash ^= _writingMode.GetHashCode();
            hash ^= _objectMode.GetHashCode();
            hash ^= _extrusionDepth.GetHashCode();
            hash ^= _fontSize.GetHashCode();
            hash ^= _letterSpacing.GetHashCode();
            hash ^= _lineSpacing.GetHashCode();
            hash ^= _rotateAsciiInVertical.GetHashCode();
            hash ^= _fontAsset != null ? _fontAsset.GetInstanceID() : 0;
            hash ^= _fontBytesCache != null ? _fontBytesCache.GetInstanceID() : 0;
            hash ^= _outline.Enabled.GetHashCode();
            hash ^= _outline.OffsetAmount.GetHashCode();
            hash ^= _outline.Thickness.GetHashCode();
            hash ^= _outline.DisplayMode.GetHashCode();
            hash ^= _outline.Material != null ? _outline.Material.GetInstanceID() : 0;
            return hash;
        }

#if UNITY_EDITOR
        private static TextAsset ResolveEditorFontBytesCache(UnityEngine.Object fontAsset)
        {
            if (fontAsset == null)
                return null;

            if (fontAsset is TextAsset textAsset)
                return textAsset;

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(fontAsset);
            if (string.IsNullOrEmpty(assetPath))
                return null;

            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                return null;

            return UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>($"{GeneratedFontBytesFolder}/{guid}.bytes");
        }

        private static byte[] TryReadEditorFontBytesFromAsset(UnityEngine.Object fontAsset)
        {
            if (fontAsset == null)
                return null;

            if (fontAsset is TextAsset textAsset)
                return textAsset.bytes;

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(fontAsset);
            if (string.IsNullOrEmpty(assetPath))
                return null;

            string fullPath = System.IO.Path.GetFullPath(assetPath);
            if (!System.IO.File.Exists(fullPath))
                return null;

            return System.IO.File.ReadAllBytes(fullPath);
        }
#endif

        private void EnsureOutlineSettingsInitialized()
        {
            if (_outline == null)
                _outline = new OutlineSettings();

            if (_outline.OffsetAmount <= 0f && _outlineWidth > 0f)
                _outline.OffsetAmount = _outlineWidth;
        }

        private void UpdateOutlineMesh(MeshGenerationParams p)
        {
            Profiler.BeginSample("SolidText3DComponent.UpdateOutlineMesh");
            try
            {
                EnsureOutlineSettingsInitialized();

                if (!_outline.Enabled)
                {
                    DestroyOutlineChildIfExists();
                    return;
                }

                EnsureOutlineChild();
                ApplyOutlineMaterial();

                if (_outline.OffsetAmount <= 0f)
                {
                    ClearOutlineMesh();
                    return;
                }

                var outlineGlyphs = GlyphMeshBuilder.BuildPerCharacter(p).Glyphs;
                if (outlineGlyphs == null || outlineGlyphs.Count == 0)
                {
                    ClearOutlineMesh();
                    return;
                }

                if (_outlineMeshFilter == null)
                    return;

                ReplaceOutlineMesh(OutlineMeshBuilder.Build(outlineGlyphs, _outline, _extrusionDepth, _fontSize));
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private void EnsureOutlineChild()
        {
            if (_outlineChild != null)
                return;

            var existing = transform.Find(OutlineChildName);
            if (existing != null)
            {
                _outlineChild = existing.gameObject;
            }
            else
            {
                _outlineChild = new GameObject(OutlineChildName);
                _outlineChild.transform.SetParent(transform, false);
            }

            _outlineMeshFilter = _outlineChild.GetComponent<MeshFilter>();
            if (_outlineMeshFilter == null)
                _outlineMeshFilter = _outlineChild.AddComponent<MeshFilter>();

            _outlineRenderer = _outlineChild.GetComponent<MeshRenderer>();
            if (_outlineRenderer == null)
                _outlineRenderer = _outlineChild.AddComponent<MeshRenderer>();
        }

        private void ApplyOutlineMaterial()
        {
            if (_outlineRenderer == null)
                return;

            _outlineRenderer.sharedMaterial = _outline.Material != null ? _outline.Material : _meshRenderer.sharedMaterial;
        }

        private void ClearOutlineMesh()
        {
            if (_outlineMeshFilter == null)
                return;

            if (_outlineMeshFilter.sharedMesh == null)
                return;

            _outlineMeshFilter.sharedMesh.Clear();
        }

        private void ReplaceOutlineMesh(Mesh mesh)
        {
            if (_outlineMeshFilter == null)
                return;

            var previousMesh = _outlineMeshFilter.sharedMesh;
            _outlineMeshFilter.sharedMesh = mesh;

            if (previousMesh == null || ReferenceEquals(previousMesh, _meshFilter != null ? _meshFilter.sharedMesh : null))
                return;

#if UNITY_EDITOR
            Object.DestroyImmediate(previousMesh);
#else
            Object.Destroy(previousMesh);
#endif
        }

        private void DestroyOutlineChildIfExists()
        {
            if (_outlineChild == null)
                return;

            if (_outlineMeshFilter != null && _outlineMeshFilter.sharedMesh != null)
            {
#if UNITY_EDITOR
                Object.DestroyImmediate(_outlineMeshFilter.sharedMesh);
#else
                Object.Destroy(_outlineMeshFilter.sharedMesh);
#endif
            }

#if UNITY_EDITOR
            Object.DestroyImmediate(_outlineChild);
#else
            Object.Destroy(_outlineChild);
#endif

            _outlineChild = null;
            _outlineMeshFilter = null;
            _outlineRenderer = null;
        }
    }
}
