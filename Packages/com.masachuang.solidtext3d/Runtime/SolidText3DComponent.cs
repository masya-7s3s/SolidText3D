using System;
using System.Collections.Generic;
using System.Threading;
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
        private const int PreparedDisplayResultCacheCapacity = 8;

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
        private volatile bool _deferredPreparationQueued;
        private volatile bool _deferredPreparationCompleted;
        private volatile bool _deferredWorkerStopRequested;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private CharacterObjectPool _characterPool;
        private GameObject _outlineChild;
        private MeshFilter _outlineMeshFilter;
        private MeshRenderer _outlineRenderer;
        private readonly DeferredRegenerationState _deferredRegenerationState = new DeferredRegenerationState();
        private readonly PreparedDisplayResultCache _preparedDisplayResultCache = new PreparedDisplayResultCache(PreparedDisplayResultCacheCapacity);
        private readonly object _deferredPreparationGate = new object();
        private readonly AutoResetEvent _deferredPreparationSignal = new AutoResetEvent(false);
        private PreparedDisplayResult _lastPreparedDisplayResult;
        private PreparedDisplayResult _completedPreparationResult;
        private PreparedDisplayResult _workerReusablePreparedResult;
        private string _completedPreparationFailureMessage;
        private TextStateRequest _workerRequest;
        private Thread _deferredPreparationThread;

        private const string OutlineChildName = "__OutlineMesh__";

        // ─── 公開プロパティ ──────────────────────────────────────────────

        /// <summary>表示するテキスト。</summary>
        public string Text
        {
            get => _text;
            set { _text = value ?? string.Empty; MarkDirty(); }
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
                MarkDirty();
            }
        }

        /// <summary>押し出し深さ（Z 軸方向）。</summary>
        public float ExtrusionDepth
        {
            get => _extrusionDepth;
            set { _extrusionDepth = value; MarkDirty(); }
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
                MarkDirty();
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
                MarkDirty();
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
                MarkDirty();
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
                MarkDirty();
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
                MarkDirty();
            }
        }

        /// <summary>文字間スペース。</summary>
        public float LetterSpacing
        {
            get => _letterSpacing;
            set { _letterSpacing = value; MarkDirty(); }
        }

        /// <summary>行間スペース。</summary>
        public float LineSpacing
        {
            get => _lineSpacing;
            set { _lineSpacing = value; MarkDirty(); }
        }

        /// <summary>フォントサイズ（Unity ワールド単位）。1 = em スクエアの高さが 1 Unity unit。</summary>
        public float FontSize
        {
            get => _fontSize;
            set { _fontSize = Mathf.Max(0.001f, value); MarkDirty(); }
        }

        /// <summary>
        /// 水平方向のアンカー位置。
        /// </summary>
        /// <param name="value">設定する HorizontalAnchor 値。</param>
        /// <returns>現在の水平アンカー設定。</returns>
        public HorizontalAnchor HorizontalAnchor
        {
            get => _horizontalAnchor;
            set { _horizontalAnchor = value; MarkDirty(); }
        }

        /// <summary>
        /// 垂直方向のアンカー位置。
        /// </summary>
        /// <param name="value">設定する VerticalAnchor 値。</param>
        /// <returns>現在の垂直アンカー設定。</returns>
        public VerticalAnchor VerticalAnchor
        {
            get => _verticalAnchor;
            set { _verticalAnchor = value; MarkDirty(); }
        }

        /// <summary>
        /// 奥行き方向のアンカー位置。
        /// </summary>
        /// <param name="value">設定する DepthAnchor 値。</param>
        /// <returns>現在の奥行きアンカー設定。</returns>
        public DepthAnchor DepthAnchor
        {
            get => _depthAnchor;
            set { _depthAnchor = value; MarkDirty(); }
        }

        /// <summary>
        /// 書字方向（横書き / 縦書き）。
        /// </summary>
        /// <param name="value">設定する WritingMode 値。</param>
        /// <returns>現在の書字方向設定。</returns>
        public WritingMode WritingMode
        {
            get => _writingMode;
            set { _writingMode = value; MarkDirty(); }
        }

        /// <summary>
        /// テキストの GameObject 生成モード（SingleObject / PerCharacter）。
        /// </summary>
        public ObjectMode ObjectMode
        {
            get => _objectMode;
            set { if (_objectMode == value) return; _objectMode = value; MarkDirty(); }
        }

        /// <summary>
        /// 横書き時の自動折り返し幅（0 = 無制限）。
        /// </summary>
        /// <param name="value">折り返し幅（Unity ワールド単位）。</param>
        /// <returns>現在の最大幅設定。</returns>
        public float MaxWidth
        {
            get => _maxWidth;
            set { _maxWidth = value; MarkDirty(); }
        }

        /// <summary>
        /// 縦書き時の自動折り返し高さ（0 = 無制限）。
        /// </summary>
        /// <param name="value">折り返し高さ（Unity ワールド単位）。</param>
        /// <returns>現在の最大高さ設定。</returns>
        public float MaxHeight
        {
            get => _maxHeight;
            set { _maxHeight = value; MarkDirty(); }
        }

        /// <summary>
        /// 縦書き時に ASCII 英数字を 90 度回転するかどうか。
        /// </summary>
        /// <param name="value">true の場合、縦書き時に ASCII 英数字を回転する。</param>
        /// <returns>現在の ASCII 回転設定。</returns>
        public bool RotateAsciiInVertical
        {
            get => _rotateAsciiInVertical;
            set { _rotateAsciiInVertical = value; MarkDirty(); }
        }

        /// <summary>ダーティフラグ（テスト・内部デバッグ用）。</summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// deferred regeneration の進行中または待機中 request があるかどうかを返す。
        /// </summary>
        public bool HasPendingRegeneration => _deferredRegenerationState.HasPendingWork || _deferredPreparationQueued || _deferredPreparationCompleted;

        /// <summary>
        /// deferred regeneration が失敗したときに通知する。
        /// </summary>
        public event Action<RegenerationFailureInfo> DeferredRegenerationFailed;

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
            MarkDirty();
        }

        private void OnDestroy()
        {
            StopDeferredWorker();
            DestroyOutlineChildIfExists();
        }

        private void LateUpdate()
        {
            TryFinalizeDeferredPreparation();
            TryApplyDeferredResult();
            TryStartPendingDeferredPreparation();
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
                long requestVersion = _deferredRegenerationState.LastRequestedVersion + 1;
                _deferredRegenerationState.LastRequestedVersion = requestVersion;
                _deferredRegenerationState.PendingLatestRequest = null;
                _deferredRegenerationState.InFlightRequest = null;
                _deferredRegenerationState.ClearReadyResult();

                if (!TryCaptureRequest(requestVersion, out TextStateRequest request, out string failureMessage))
                {
                    IssueFontWarning(failureMessage);
                    return;
                }

                if (ShouldSkipApply(request))
                {
                    MarkRequestSatisfiedWithoutApply(request);
                    return;
                }

                PreparedDisplayResult preparedResult = TryGetCachedPreparedResult(request, out PreparedDisplayResult cached)
                    ? cached
                    : GlyphMeshBuilder.PrepareDisplayResult(request, GetReusablePreparedResult(request));

                ApplyPreparedDisplayResult(preparedResult);
                RecordAppliedResult(preparedResult);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// 高頻度更新向けの deferred regeneration を要求する。
        /// 呼び出し時点では表示反映を保証しない。
        /// </summary>
        public void RequestRegenerateMesh()
        {
            _isDirty = false;

            long requestVersion = _deferredRegenerationState.LastRequestedVersion + 1;
            _deferredRegenerationState.LastRequestedVersion = requestVersion;

            if (!TryCaptureRequest(requestVersion, out TextStateRequest request, out string failureMessage))
            {
                RaiseDeferredFailure(requestVersion, _text, failureMessage);
                return;
            }

            if (ShouldSkipApply(request))
            {
                MarkRequestSatisfiedWithoutApply(request);
                return;
            }

            if (TryGetCachedPreparedResult(request, out PreparedDisplayResult cachedResult))
            {
                _deferredRegenerationState.ReadyResult = cachedResult;
                _deferredRegenerationState.PendingLatestRequest = null;
                return;
            }

            if (_deferredRegenerationState.InFlightRequest.HasValue || _deferredPreparationQueued)
            {
                _deferredRegenerationState.PendingLatestRequest = request;
                return;
            }

            StartDeferredPreparation(request);
        }

        // ─── 内部ヘルパー ────────────────────────────────────────────────

        private bool TryCaptureRequest(long version, out TextStateRequest request, out string failureMessage)
        {
            EnsureOutlineSettingsInitialized();

            var signature = CreateDisplayResultSignature();
            var generationParams = CreateMeshGenerationParams(null);

            if (string.IsNullOrEmpty(_text))
            {
                request = new TextStateRequest(version, signature, string.Empty, null, generationParams, OutlineEnabled, OutlineOffset, OutlineThickness, OutlineMaterial, OutlineDisplayMode, _objectMode);
                failureMessage = null;
                _fontMissingWarningIssued = false;
                return true;
            }

            if (!TryGetFontBytesForCurrentText(out byte[] fontBytes, out failureMessage))
            {
                request = default;
                return false;
            }

            generationParams = CreateMeshGenerationParams(fontBytes);
            request = new TextStateRequest(version, signature, _text, fontBytes, generationParams, OutlineEnabled, OutlineOffset, OutlineThickness, OutlineMaterial, OutlineDisplayMode, _objectMode);
            _fontMissingWarningIssued = false;
            return true;
        }

        private MeshGenerationParams CreateMeshGenerationParams(byte[] fontBytes)
        {
            return new MeshGenerationParams
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
        }

        private DisplayResultSignature CreateDisplayResultSignature()
        {
            int layoutHash = ComputeLayoutHash();
            int fontSourceId = _fontAsset != null
                ? _fontAsset.GetInstanceID()
                : _fontBytesCache != null ? _fontBytesCache.GetInstanceID() : 0;
            return new DisplayResultSignature(_text, ComputeParamHash(layoutHash), layoutHash, fontSourceId, _objectMode);
        }

        private bool TryGetFontBytesForCurrentText(out byte[] fontBytes, out string failureMessage)
        {
            if (_fontAsset == null && _fontBytesCache == null)
            {
                fontBytes = null;
                failureMessage = "[SolidText3D] フォントが設定されていません。FontAsset を Inspector でアタッチしてください。";
                return false;
            }

            fontBytes = GetFontBytes();
            if (fontBytes == null || fontBytes.Length == 0)
            {
                failureMessage = "[SolidText3D] フォントデータを読み込めませんでした。直前のメッシュを維持します。";
                return false;
            }

            failureMessage = null;
            return true;
        }

        private bool ShouldSkipApply(TextStateRequest request)
        {
            return !string.IsNullOrEmpty(request.Text)
                && request.Signature == _deferredRegenerationState.LastAppliedSignature;
        }

        private bool TryGetCachedPreparedResult(TextStateRequest request, out PreparedDisplayResult preparedResult)
        {
            if (_preparedDisplayResultCache.TryGet(request.Signature, out PreparedDisplayResult cached))
            {
                preparedResult = ClonePreparedResult(cached, request.Version, request.Signature);
                return true;
            }

            preparedResult = null;
            return false;
        }

        private static PreparedDisplayResult ClonePreparedResult(PreparedDisplayResult source, long version, DisplayResultSignature signature)
        {
            if (source == null)
                return null;

            return new PreparedDisplayResult
            {
                Version = version,
                Signature = signature,
                BodyMeshData = source.BodyMeshData,
                OutlineMeshData = source.OutlineMeshData,
                PerCharacterMeshData = source.PerCharacterMeshData,
                PerCharacterOffsets = source.PerCharacterOffsets,
                ClearsDisplay = source.ClearsDisplay
            };
        }

        private void StartDeferredPreparation(TextStateRequest request)
        {
            PreparedDisplayResult reusableResult = GetReusablePreparedResult(request);
            _deferredRegenerationState.InFlightRequest = request;
            _deferredRegenerationState.PendingLatestRequest = null;

            lock (_deferredPreparationGate)
            {
                _workerRequest = request;
                _workerReusablePreparedResult = reusableResult;
                _completedPreparationResult = null;
                _completedPreparationFailureMessage = null;
                _deferredPreparationCompleted = false;
                _deferredPreparationQueued = true;
            }

            EnsureDeferredWorkerStarted();
            _deferredPreparationSignal.Set();
        }

        private PreparedDisplayResult GetReusablePreparedResult(TextStateRequest request)
        {
            if (_lastPreparedDisplayResult == null)
                return null;

            if (!_lastPreparedDisplayResult.Signature.CanReusePerCharacterLayoutWith(request.Signature))
                return null;

            return _lastPreparedDisplayResult;
        }

        private void TryFinalizeDeferredPreparation()
        {
            if (!_deferredPreparationCompleted || !_deferredRegenerationState.InFlightRequest.HasValue)
                return;

            TextStateRequest request = _deferredRegenerationState.InFlightRequest.Value;
            try
            {
                PreparedDisplayResult completedResult;
                string completedFailureMessage;
                lock (_deferredPreparationGate)
                {
                    completedResult = _completedPreparationResult;
                    completedFailureMessage = _completedPreparationFailureMessage;
                    _completedPreparationResult = null;
                    _completedPreparationFailureMessage = null;
                    _deferredPreparationCompleted = false;
                }

                if (!string.IsNullOrEmpty(completedFailureMessage))
                {
                    RaiseDeferredFailure(request.Version, request.Text, completedFailureMessage);
                }
                else if (completedResult != null && completedResult.Version >= _deferredRegenerationState.LastRequestedVersion)
                {
                    _deferredRegenerationState.ReadyResult = completedResult;
                }
            }
            finally
            {
                _deferredRegenerationState.InFlightRequest = null;
            }
        }

        private void TryApplyDeferredResult()
        {
            PreparedDisplayResult readyResult = _deferredRegenerationState.ReadyResult;
            if (readyResult == null)
                return;

            _deferredRegenerationState.ClearReadyResult();
            if (readyResult.Version < _deferredRegenerationState.LastRequestedVersion)
                return;

            ApplyPreparedDisplayResult(readyResult);
            RecordAppliedResult(readyResult);
        }

        private void TryStartPendingDeferredPreparation()
        {
            if (_deferredPreparationQueued || !_deferredRegenerationState.PendingLatestRequest.HasValue)
                return;

            TextStateRequest request = _deferredRegenerationState.PendingLatestRequest.Value;
            _deferredRegenerationState.PendingLatestRequest = null;

            if (TryGetCachedPreparedResult(request, out PreparedDisplayResult cachedResult))
            {
                _deferredRegenerationState.ReadyResult = cachedResult;
                return;
            }

            StartDeferredPreparation(request);
        }

        private void EnsureDeferredWorkerStarted()
        {
            if (_deferredPreparationThread != null)
                return;

            lock (_deferredPreparationGate)
            {
                if (_deferredPreparationThread != null)
                    return;

                _deferredWorkerStopRequested = false;
                _deferredPreparationThread = new Thread(DeferredPreparationWorkerLoop)
                {
                    IsBackground = true,
                    Name = "SolidText3D Deferred Worker"
                };
                _deferredPreparationThread.Start();
            }
        }

        private void DeferredPreparationWorkerLoop()
        {
            while (true)
            {
                _deferredPreparationSignal.WaitOne();
                if (_deferredWorkerStopRequested)
                    return;

                PreparedDisplayResult completedResult = null;
                string failureMessage = null;
                TextStateRequest request;
                PreparedDisplayResult reusableResult;

                lock (_deferredPreparationGate)
                {
                    request = _workerRequest;
                    reusableResult = _workerReusablePreparedResult;
                }

                try
                {
                    completedResult = GlyphMeshBuilder.PrepareDisplayResult(request, reusableResult);
                }
                catch (Exception exception)
                {
                    failureMessage = exception.Message;
                }

                lock (_deferredPreparationGate)
                {
                    if (_deferredWorkerStopRequested)
                        return;

                    _workerReusablePreparedResult = null;
                    _completedPreparationResult = completedResult;
                    _completedPreparationFailureMessage = failureMessage;
                    _deferredPreparationQueued = false;
                    _deferredPreparationCompleted = true;
                }
            }
        }

        private void StopDeferredWorker()
        {
            _deferredWorkerStopRequested = true;
            _deferredPreparationSignal.Set();
        }

        private void ApplyPreparedDisplayResult(PreparedDisplayResult preparedResult)
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            if (preparedResult == null)
                return;

            if (preparedResult.ClearsDisplay)
            {
                ClearCurrentDisplay();
                return;
            }

            if (preparedResult.PerCharacterMeshData != null)
            {
                ApplyPerCharacterPreparedResult(preparedResult);
            }
            else
            {
                ApplySingleObjectPreparedResult(preparedResult);
            }

            ApplyPreparedOutlineResult(preparedResult.OutlineMeshData);
        }

        private void ApplySingleObjectPreparedResult(PreparedDisplayResult preparedResult)
        {
            _characterPool?.DestroyAll();
            _characterPool = null;

            if (_meshRenderer != null)
                _meshRenderer.enabled = true;

            if (_meshFilter != null)
                _meshFilter.sharedMesh = MeshExtruder.CreateMesh(preparedResult.BodyMeshData);
        }

        private void ApplyPerCharacterPreparedResult(PreparedDisplayResult preparedResult)
        {
            if (_meshFilter != null)
                _meshFilter.sharedMesh = null;

            if (_meshRenderer != null)
                _meshRenderer.enabled = false;

            if (_characterPool == null)
                _characterPool = new CharacterObjectPool(transform);

            var perCharacterMeshes = new List<Mesh>(preparedResult.PerCharacterMeshData.Count);
            for (int i = 0; i < preparedResult.PerCharacterMeshData.Count; i++)
                perCharacterMeshes.Add(MeshExtruder.CreateMesh(preparedResult.PerCharacterMeshData[i]));

            var sharedMaterial = _meshRenderer != null ? _meshRenderer.sharedMaterial : null;
            _characterPool.Sync(perCharacterMeshes, sharedMaterial);
        }

        private void ApplyPreparedOutlineResult(GlyphMeshData outlineMeshData)
        {
            EnsureOutlineSettingsInitialized();

            if (!_outline.Enabled)
            {
                DestroyOutlineChildIfExists();
                return;
            }

            EnsureOutlineChild();
            ApplyOutlineMaterial();

            if (outlineMeshData.Vertices == null || outlineMeshData.Vertices.Count == 0)
            {
                ClearOutlineMesh();
                return;
            }

            ReplaceOutlineMesh(MeshExtruder.CreateMesh(outlineMeshData));
        }

        private void ClearCurrentDisplay()
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if (_meshFilter != null && _meshFilter.sharedMesh != null)
                _meshFilter.sharedMesh.Clear();

            _characterPool?.DestroyAll();
            _characterPool = null;

            if (_meshRenderer != null)
                _meshRenderer.enabled = _objectMode != ObjectMode.PerCharacter;

            _lastPreparedDisplayResult = null;
            DestroyOutlineChildIfExists();
        }

        private void RecordAppliedResult(PreparedDisplayResult preparedResult)
        {
            _deferredRegenerationState.LastAppliedVersion = preparedResult.Version;
            _deferredRegenerationState.LastAppliedSignature = preparedResult.Signature;
            _lastParamHash = preparedResult.Signature.HashCode;
            _lastPreparedDisplayResult = preparedResult.ClearsDisplay
                ? null
                : ClonePreparedResult(preparedResult, preparedResult.Version, preparedResult.Signature);

            if (!preparedResult.ClearsDisplay)
                _preparedDisplayResultCache.Store(ClonePreparedResult(preparedResult, preparedResult.Version, preparedResult.Signature));
        }

        private void MarkRequestSatisfiedWithoutApply(TextStateRequest request)
        {
            _deferredRegenerationState.LastAppliedVersion = request.Version;
            _deferredRegenerationState.LastAppliedSignature = request.Signature;
            _lastParamHash = request.Signature.HashCode;
        }

        private void RaiseDeferredFailure(long requestVersion, string requestedText, string failureMessage)
        {
            DeferredRegenerationFailed?.Invoke(new RegenerationFailureInfo(requestVersion, requestedText, failureMessage));
        }

        private void IssueFontWarning(string failureMessage)
        {
            if (_fontMissingWarningIssued)
                return;

            Debug.LogWarning(failureMessage);
            _fontMissingWarningIssued = true;
        }

        private void MarkDirty()
        {
            _isDirty = true;
            InvalidatePendingDeferredRegeneration();
        }

        private void InvalidatePendingDeferredRegeneration()
        {
            _deferredRegenerationState.LastRequestedVersion++;
            _deferredRegenerationState.PendingLatestRequest = null;
            _deferredRegenerationState.ClearReadyResult();
        }

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
        private int ComputeParamHash(int layoutHash)
        {
            int hash = layoutHash;
            hash ^= _text != null ? _text.GetHashCode() : 0;
            return hash;
        }

        private int ComputeLayoutHash()
        {
            EnsureOutlineSettingsInitialized();
            int hash = 0;
            hash ^= _horizontalAnchor.GetHashCode();
            hash ^= _verticalAnchor.GetHashCode();
            hash ^= _depthAnchor.GetHashCode();
            hash ^= _writingMode.GetHashCode();
            hash ^= _objectMode.GetHashCode();
            hash ^= _extrusionDepth.GetHashCode();
            hash ^= _fontSize.GetHashCode();
            hash ^= _letterSpacing.GetHashCode();
            hash ^= _lineSpacing.GetHashCode();
            hash ^= _bezierErrorThreshold.GetHashCode();
            hash ^= _maxWidth.GetHashCode();
            hash ^= _maxHeight.GetHashCode();
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
        UnityEngine.Object.DestroyImmediate(previousMesh);
#else
        UnityEngine.Object.Destroy(previousMesh);
#endif
        }

        private void DestroyOutlineChildIfExists()
        {
            if (_outlineChild == null)
                return;

            if (_outlineMeshFilter != null && _outlineMeshFilter.sharedMesh != null)
            {
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(_outlineMeshFilter.sharedMesh);
#else
                UnityEngine.Object.Destroy(_outlineMeshFilter.sharedMesh);
#endif
            }

#if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(_outlineChild);
#else
            UnityEngine.Object.Destroy(_outlineChild);
#endif

            _outlineChild = null;
            _outlineMeshFilter = null;
            _outlineRenderer = null;
        }
    }
}
