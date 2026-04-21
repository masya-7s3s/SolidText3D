# Data Model: Solid Text 3D — レイアウト・フォント・パフォーマンス改善

**Branch**: `002-text-layout-improvements` | **Date**: 2026-04-21

---

## 新規 Enum 型

### `HorizontalAnchor`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>テキストメッシュの水平方向アンカー位置。</summary>
    public enum HorizontalAnchor
    {
        Left,    // テキスト左端が原点
        Center,  // テキスト中心が原点
        Right    // テキスト右端が原点
    }
}
```

### `VerticalAnchor`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>テキストメッシュの垂直方向アンカー位置。</summary>
    public enum VerticalAnchor
    {
        Upper,   // テキスト上端が原点
        Middle,  // テキスト中央が原点
        Lower    // テキスト下端が原点
    }
}
```

### `DepthAnchor`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>テキストメッシュの奥行き方向アンカー位置。</summary>
    public enum DepthAnchor
    {
        Front,   // 前面が原点（Z=0）
        Center,  // 中央が原点（Z=-depth/2）
        Back     // 背面が原点（Z=-depth）
    }
}
```

### `WritingMode`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>テキストの書字方向。</summary>
    public enum WritingMode
    {
        Horizontal,  // 横書き（デフォルト）
        Vertical     // 縦書き（上→下、右→左の列方向）
    }
}
```

### `ObjectMode`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>テキストの GameObject 生成モード。</summary>
    public enum ObjectMode
    {
        SingleObject,   // テキスト全体で 1 つの Mesh（デフォルト）
        PerCharacter    // 文字ごとに子 GameObject を生成
    }
}
```

---

## 変更される型

### `MeshGenerationParams`（修正）

**追加フィールド:**

| フィールド | 型 | デフォルト | 説明 |
| --------- | --- | --------- | ---- |
| `HorizontalAnchor` | `HorizontalAnchor` | `Left` | 水平アンカー |
| `VerticalAnchor` | `VerticalAnchor` | `Lower` | 垂直アンカー |
| `DepthAnchor` | `DepthAnchor` | `Front` | 奥行きアンカー |
| `WritingMode` | `WritingMode` | `Horizontal` | 書字方向 |
| `MaxWidth` | `float` | `0f`（無制限） | 横書き時の最大幅（超えたら折り返し） |
| `MaxHeight` | `float` | `0f`（無制限） | 縦書き時の最大高さ（超えたら次の列へ） |
| `VerticalColumnWidth` | `float` | `0f`（自動） | 縦書き時の列幅（0 の場合 FontSize × 1.1f を使用） |
| `RotateAsciiInVertical` | `bool` | `false` | 縦書き時の ASCII 文字を 90 度回転するか |

**削除フィールド:**

| フィールド | 型 | 削除理由 |
| --------- | --- | ------- |
| `FontPath` | `string` | フォント参照を Object フィールドに変更したため不要。`FontData (byte[])` に一本化。 |

### `SolidText3DComponent`（修正）

**削除フィールド / プロパティ:**

| メンバー | 型 | 削除理由 |
| ------- | --- | ------- |
| `[SerializeField] _font` | `string` | Object フィールドに置き換え |
| `public string Font` | プロパティ | 同上（破壊的変更・MAJOR バンプ） |

**追加フィールド:**

| フィールド | 型 | デフォルト | 説明 |
| --------- | --- | --------- | ---- |
| `_fontAsset` | `UnityEngine.Object` | `null` | Inspector でアタッチされた .ttf/.otf ファイル参照 |
| `_fontBytesCache` | `TextAsset` | `null` | エディタが自動設定する変換済み .bytes（HideInInspector） |
| `_horizontalAnchor` | `HorizontalAnchor` | `Left` | 水平アンカー設定 |
| `_verticalAnchor` | `VerticalAnchor` | `Lower` | 垂直アンカー設定 |
| `_depthAnchor` | `DepthAnchor` | `Front` | 奥行きアンカー設定 |
| `_writingMode` | `WritingMode` | `Horizontal` | 書字方向設定 |
| `_objectMode` | `ObjectMode` | `SingleObject` | 単一/個別オブジェクトモード |
| `_maxWidth` | `float` | `0f` | 横書き自動折り返し幅 |
| `_maxHeight` | `float` | `0f` | 縦書き自動折り返し高さ |
| `_verticalColumnWidth` | `float` | `0f` | 縦書き列幅（0 = FontSize × 1.1f 自動） |
| `_rotateAsciiInVertical` | `bool` | `false` | 縦書き時 ASCII 90 度回転 |
| `_lastParamHash` | `int` | `0` | パラメータの変更検知ハッシュ（内部用）。[research.md § R-007](research.md#r-007-同一テキスト早期リターンパフォーマンス) 参照 |
| `_suppressAutoRegenerate` | `bool` | `false` | デバウンス中に `LateUpdate` の自動再生成を抑制するフラグ。`SolidText3DInspector` が設定する。[research.md § R-003](research.md#r-003-inspector-入力デバウンス) 参照 |
| `_fontMissingWarningIssued` | `bool` | `false` | Missing フォント警告の重複出力防止フラグ（1 回のみ出力） |

**追加プロパティ（公開 API）:**

| プロパティ | 型 | 説明 |
| --------- | --- | ---- |
| `FontAsset` | `UnityEngine.Object` | フォントアセット参照（get/set） |
| `HorizontalAnchor` | `HorizontalAnchor` | 水平アンカー（get/set） |
| `VerticalAnchor` | `VerticalAnchor` | 垂直アンカー（get/set） |
| `DepthAnchor` | `DepthAnchor` | 奥行きアンカー（get/set） |
| `WritingMode` | `WritingMode` | 書字方向（get/set） |
| `ObjectMode` | `ObjectMode` | 生成モード（get/set） |
| `MaxWidth` | `float` | 横書き折り返し幅（get/set） |
| `MaxHeight` | `float` | 縦書き折り返し高さ（get/set） |

### `GlyphContour`（修正）

**追加フィールド:**

| フィールド | 型 | 説明 |
| --------- | --- | ---- |
| `AdvanceHeight` | `float` | 縦書き用の字送り高さ（SixLabors から取得できない場合は AdvanceWidth で代用） |
| `CharIndex` | `int` | 元テキスト中の文字インデックス（Per-Character プールの紐付け用） |
| `IsVisible` | `bool` | 可視文字かどうか（折り返し区切り等の非表示文字は false） |

---

## 新規クラス

### `LayoutEngine`（新規 Runtime クラス）

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// グリフ一覧に対してレイアウト計算（座標割り当て）を行う静的クラス。
    /// 横書き・縦書き両方に対応し、GlyphMeshBuilder から呼び出される。
    /// </summary>
    internal static class LayoutEngine
    {
        /// <summary>横書きレイアウトを適用する。</summary>
        internal static void ApplyHorizontalLayout(List<GlyphContour> glyphs, MeshGenerationParams p);

        /// <summary>縦書きレイアウトを適用する。</summary>
        internal static void ApplyVerticalLayout(List<GlyphContour> glyphs, MeshGenerationParams p);

        /// <summary>アンカーオフセットを計算して返す。</summary>
        internal static Vector3 CalculateAnchorOffset(Bounds meshBounds, MeshGenerationParams p);
    }
}
```

**入力**: `List<GlyphContour>` + `MeshGenerationParams`  
**出力**: 各 `GlyphContour.Offset` を in-place 更新 + アンカーオフセット値を返す  
**バリデーション**: テキストが空の場合は処理をスキップ  
**状態遷移**: なし（純粋な座標計算）  

### `CharacterObjectPool`（新規 Runtime クラス）

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// Per-Character モードで使用する子 GameObject プール。
    /// </summary>
    internal sealed class CharacterObjectPool
    {
        /// <summary>プールを初期化する。</summary>
        internal CharacterObjectPool(Transform parent);

        /// <summary>
        /// 可視文字リストと対応するメッシュリストを使って子 GameObject を同期する。
        /// 不足分は生成・超過分は非アクティブ化する。
        /// </summary>
        /// <param name="visibleGlyphs">IsVisible == true のグリフのみのリスト。</param>
        /// <param name="perCharMeshes">GlyphMeshBuilder.BuildPerCharacter() が返す文字ごとの Mesh リスト（visibleGlyphs と同順）。</param>
        internal void Sync(List<GlyphContour> visibleGlyphs, List<Mesh> perCharMeshes);

        /// <summary>すべての子 GameObject を非アクティブ化する。</summary>
        internal void DeactivateAll();

        /// <summary>プール内のすべての子 GameObject を Destroy する。</summary>
        internal void Destroy();

        /// <summary>現在アクティブな子 GameObject のリスト（読み取り専用）。</summary>
        internal IReadOnlyList<GameObject> ActiveObjects { get; }
    }
}
```

**状態**:

- `_pool: List<GameObject>` — 全プール（アクティブ + 非アクティブ）
- `_parent: Transform` — 子の親となる Transform

**制約**:

- `Sync()` は `LateUpdate()` から呼び出されるが、新規 GameObject 生成は文字数増加時のみ発生（GC 正当化：毎フレームは発生しない）
- 非アクティブ化は `SetActive(false)` のみ（`Destroy` は呼ばない）
- 折り返し区切り文字（`IsVisible = false`）は対象外

### `FontAssetPostprocessor`（新規 Editor クラス）

```csharp
namespace MasaChuang.SolidText3D.Editor
{
    /// <summary>
    /// .ttf / .otf フォントファイルのインポートを検知し、.bytes ファイルを自動生成するポストプロセッサ。
    /// </summary>
    internal sealed class FontAssetPostprocessor : AssetPostprocessor
    {
        // 出力先ディレクトリ（AssetDatabase 管理下）
        private const string OutputDirectory = "Assets/SolidText3DFonts";

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths);
    }
}
```

**処理フロー**:

1. `importedAssets` をフィルター：拡張子 `.ttf` または `.otf` のみ対象
2. 出力先 `Assets/SolidText3DFonts/` が存在しない場合は `Directory.CreateDirectory()` で作成
3. 出力ファイル名 = `{assetGuid}.bytes`（GUID でファイル名を決定）
4. 出力ファイルが既に存在する場合はスキップ
5. 元ファイルの絶対パスを `Path.GetFullPath()` で取得 → `File.ReadAllBytes()`
6. 一時ファイル（`.tmp`）に書き込み → `File.Move()` でリネーム（アトミック書き込み）
7. `AssetDatabase.ImportAsset(outputPath)` で `.bytes` アセットを登録する
8. 開いているシーン内の全 `SolidText3DComponent` を `FindObjectsByType<SolidText3DComponent>(FindObjectsSortMode.None)` で走査し、`_fontAsset` の GUID が一致するものに `_fontBytesCache` を `AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath)` で設定する（`EditorUtility.SetDirty()` + `AssetDatabase.SaveAssets()`）

**制限事項**: 手順 8 は現在開いているシーン内のコンポーネントのみ対象。Prefab アセット内のコンポーネントは次回 Inspector で表示されたとき `OnValidate` 経由で自動再バインドされる（`SolidText3DComponent.OnValidate` に `_fontBytesCache` の再取得ロジックを追加する）。

**エラー処理**:

- I/O 例外: `Debug.LogError()` でコンソールに出力し処理を中断（Silent Fail しない）
- 出力ディレクトリ作成失敗: 同上

---

## `BuildPerCharacter` メソッド設計

`GlyphMeshBuilder.BuildPerCharacter()` は Per-Character モード専用のメソッドで、`Build()` と同じレイアウト計算を行いつつ、グリフごとに独立した `Mesh` を返す。

```csharp
/// <summary>
/// Per-Character モード向けに、可視文字ごとの独立した Mesh リストを生成する。
/// アンカーオフセットは各 Mesh の頂点ではなく、GlyphContour.Offset に格納される。
/// CharacterObjectPool がこのオフセットを子 GameObject のローカル座標として設定する。
/// </summary>
/// <param name="p">メッシュ生成パラメータ。</param>
/// <returns>
///   要素数 = テキスト中の可視文字数（IsVisible == true のグリフ数）。
///   各 Mesh は対応する文字の形状のみを含み、ローカル原点（0,0,0）基準で生成される。
///   文字のワールド/ローカル配置は GlyphContour.Offset として返される（LayoutEngine が設定）。
/// </returns>
public static (List<Mesh> meshes, List<GlyphContour> visibleGlyphs) BuildPerCharacter(MeshGenerationParams p);
```

**実装ポイント**:

- `LayoutEngine.ApplyHorizontalLayout()` / `ApplyVerticalLayout()` で全グリフの配置座標を計算する（`Build()` と共通）
- `IsVisible == false` のグリフ（折り返し区切り等）は結果リストに含めない
- 各グリフに対して `MeshExtruder.BuildGlyphMesh()` を呼び出し、ローカル原点基準の `Mesh` を生成する（`GlyphContour.Offset` は Mesh 頂点に加算しない）
- アンカーオフセット（`LayoutEngine.CalculateAnchorOffset()`）は `GlyphContour.Offset` 全体に加算して返す（子 GameObject のローカル座標として使用）
- 戻り値の `List<Mesh>` と `List<GlyphContour>` は同じインデックスが同じ文字に対応する

---

## データフロー概要

```txt
[Inspector] _fontAsset (UnityEngine.Object)
      ↓ (OnValidate / Inspector 変更)
[Editor] FontAssetPostprocessor が .bytes 生成 → _fontBytesCache (TextAsset) に設定
      ↓
[SolidText3DComponent.RegenerateMesh()]
      ↓
MeshGenerationParams { FontData = _fontBytesCache?.bytes, ... }
      ↓
[GlyphMeshBuilder.Build()]
      ↓
[LayoutEngine.ApplyHorizontalLayout() or ApplyVerticalLayout()]
      ↓
[MeshExtruder.Build()] → UnityEngine.Mesh
      ↓
LayoutEngine.CalculateAnchorOffset() → 頂点全体にオフセット加算
      ↓
[ObjectMode == SingleObject] → MeshFilter.mesh に直接設定
[ObjectMode == PerCharacter] → CharacterObjectPool.Sync() で子 GameObject に分配
```

---

## バリデーションルール

| 条件 | 動作 |
| ---- | ---- |
| `_text == ""` | メッシュをクリア、警告なし |
| `_fontAsset == null` かつ `_fontBytesCache == null` | デフォルトフォント（NotoSansJP-Black）を使用。Inspector に警告表示。 |
| `_fontAsset` が Missing 参照 | 直前のメッシュを維持。コンソールに警告 1 回のみ出力。 |
| `_fontBytesCache` 変換失敗（I/O エラー等） | コンソールに `Debug.LogError()`。描画スキップ。 |
| `_maxWidth > 0 && テキスト幅 > _maxWidth` | 自動折り返しを実行 |
| `_fontSize <= 0` | `Mathf.Max(0.001f, value)` でクランプ |
| `_extrusionDepth < 0` | `Mathf.Max(0f, value)` でクランプ |

---

## 状態遷移: ObjectMode 切り替え

```txt
SingleObject → PerCharacter:
  1. CharacterObjectPool を初期化（parent = this.transform）
  2. Sync() で現在のテキストに対応する子 GameObject を生成
  3. MeshFilter.mesh = null（親 GameObject のメッシュをクリア）

PerCharacter → SingleObject:
  1. CharacterObjectPool.DeactivateAll()
  2. 将来の再利用のため Destroy は行わない
  3. MeshFilter.mesh に単一メッシュを設定
```

（ObjectMode が変更されたとき `_isDirty = true` にセットされ、次の `LateUpdate` で遷移を実行する）
