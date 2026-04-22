# Contract: SolidText3DComponent 公開 API v2.0.0

**Branch**: `002-text-layout-improvements` | **Date**: 2026-04-21  
**SemVer**: `1.0.0 → 2.0.0`（MAJOR: 破壊的変更あり）

---

## 破壊的変更（BREAKING CHANGES）

### 削除された API

| API | 旧型 | 削除理由 | 移行方法 |
| --- | ---- | ------- | ------- |
| `public string Font { get; set; }` | `string` | フォント参照を Object フィールドに変更 | `component.FontAsset = fontObject;` を使用 |
| `[SerializeField] _font` | `string` | 同上 | Inspector の「Font Asset」フィールドでアタッチ |
| `MeshGenerationParams.FontPath` | `string` | `FontData (byte[])` に一本化 | `FontData` に直接バイト配列を設定 |

**移行ガイド**: コンパイルエラーが発生した場合、以下の置き換えを行ってください。

```csharp
// 旧 (v1.x)
component.Font = "path/to/font.bytes";

// 新 (v2.x)
// - Inspector でフォントアセットをアタッチする（推奨）
// - コードから設定する場合:
component.FontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Fonts/MyFont.ttf");
```

---

## 公開 API 全体（v2.0.0）

### `SolidText3DComponent` (MonoBehaviour)

#### プロパティ

```csharp
// --- 既存（変更なし）---

/// <summary>表示するテキスト。空文字列の場合はメッシュをクリアする。</summary>
public string Text { get; set; }

/// <summary>押し出し深さ（Z 軸方向）。0 以上の値。</summary>
public float ExtrusionDepth { get; set; }

/// <summary>アウトライン幅。0 以上の値。</summary>
public float OutlineWidth { get; set; }

/// <summary>文字間隔（em 単位）。</summary>
public float LetterSpacing { get; set; }

/// <summary>行間倍率。</summary>
public float LineSpacing { get; set; }

/// <summary>フォントサイズ（Unity ワールド単位）。0.001 以上。</summary>
public float FontSize { get; set; }

/// <summary>ダーティフラグ（テスト・内部デバッグ用）。</summary>
public bool IsDirty { get; }

// --- 新規 (v2.0.0) ---

/// <summary>
/// フォントアセット参照（.ttf / .otf ファイル）。
/// Inspector でドラッグ&ドロップでアタッチする。
/// null の場合はデフォルトフォント（NotoSansJP-Black）を使用。
/// </summary>
public UnityEngine.Object FontAsset { get; set; }

/// <summary>テキストメッシュの水平方向アンカー。</summary>
public HorizontalAnchor HorizontalAnchor { get; set; }

/// <summary>テキストメッシュの垂直方向アンカー。</summary>
public VerticalAnchor VerticalAnchor { get; set; }

/// <summary>テキストメッシュの奥行き方向アンカー。</summary>
public DepthAnchor DepthAnchor { get; set; }

/// <summary>書字方向（横書き / 縦書き）。</summary>
public WritingMode WritingMode { get; set; }

/// <summary>
/// GameObject 生成モード。
/// SingleObject: テキスト全体を 1 Mesh として生成（デフォルト）。
/// PerCharacter: 文字ごとに子 GameObject を生成（アニメーション等に使用）。
/// </summary>
public ObjectMode ObjectMode { get; set; }

/// <summary>
/// 横書き時の自動折り返し幅（Unity ワールド単位）。
/// 0 の場合は折り返しなし（改行コードのみで制御）。
/// </summary>
public float MaxWidth { get; set; }

/// <summary>
/// 縦書き時の自動折り返し高さ（Unity ワールド単位）。
/// 0 の場合は折り返しなし（改行コードのみで制御）。
/// </summary>
public float MaxHeight { get; set; }
```

#### メソッド

```csharp
/// <summary>
/// メッシュを即時再生成する。
/// 通常は LateUpdate() が自動的に呼び出すため、外部から呼ぶ必要はない。
/// </summary>
public void RegenerateMesh();
```

---

### `MeshGenerationParams` (struct)

```csharp
namespace MasaChuang.SolidText3D
{
    public struct MeshGenerationParams
    {
        // --- 既存（変更なし）---
        public string Text;
        public byte[] FontData;             // FontPath は削除。FontData のみ使用。
        public float ExtrusionDepth;
        public float OutlineWidth;
        public float LetterSpacing;
        public float LineSpacing;
        public float BezierErrorThreshold;
        public float FontSize;

        // --- 新規 (v2.0.0) ---
        public HorizontalAnchor HorizontalAnchor;  // デフォルト: Left
        public VerticalAnchor VerticalAnchor;       // デフォルト: Lower
        public DepthAnchor DepthAnchor;             // デフォルト: Front
        public WritingMode WritingMode;             // デフォルト: Horizontal
        public float MaxWidth;                      // デフォルト: 0f（無制限）
        public float MaxHeight;                     // デフォルト: 0f（無制限）
        public float VerticalColumnWidth;           // デフォルト: 0f（FontSize×1.1f を自動使用）
        public bool RotateAsciiInVertical;          // デフォルト: false
    }
}
```

---

### 新規 Enum 型（公開）

```csharp
namespace MasaChuang.SolidText3D
{
    public enum HorizontalAnchor { Left, Center, Right }
    public enum VerticalAnchor   { Upper, Middle, Lower }
    public enum DepthAnchor      { Front, Center, Back }
    public enum WritingMode      { Horizontal, Vertical }
    public enum ObjectMode       { SingleObject, PerCharacter }
}
```

---

### `GlyphMeshBuilder` (static class)

```csharp
namespace MasaChuang.SolidText3D
{
    public static class GlyphMeshBuilder
    {
        /// <summary>
        /// 指定したパラメータに基づいてテキストの 3D メッシュを生成する。
        /// テキストが空またはフォントが読み込めない場合は空メッシュを返す。
        /// アンカーオフセットは生成済みメッシュに適用済み。
        /// </summary>
        /// <param name="p">メッシュ生成パラメータ（v2.0.0 で FontPath を削除）。</param>
        /// <returns>生成された Unity Mesh。</returns>
        public static Mesh Build(MeshGenerationParams p);

        /// <summary>
        /// Per-Character モード向けに、文字ごとのメッシュリストを生成する。
        /// </summary>
        /// <param name="p">メッシュ生成パラメータ。</param>
        /// <returns>可視文字ごとの Mesh リスト（折り返し区切り文字は含まない）。</returns>
        public static List<Mesh> BuildPerCharacter(MeshGenerationParams p);
    }
}
```

---

## Inspector フィールド（エディタ表示）

| フィールド名（Inspector 表示） | 型 | バージョン |
| ---------------------------- | --- | --------- |
| Text | string | v1.0.0 |
| Font Asset | Object (.ttf/.otf) | v2.0.0（新規） |
| Font Size | float | v1.0.0 |
| Extrusion Depth | float | v1.0.0 |
| Outline Width | float | v1.0.0 |
| Letter Spacing | float | v1.0.0 |
| Line Spacing | float | v1.0.0 |
| Bezier Error Threshold | float | v1.0.0 |
| Horizontal Anchor | HorizontalAnchor（ドロップダウン） | v2.0.0（新規） |
| Vertical Anchor | VerticalAnchor（ドロップダウン） | v2.0.0（新規） |
| Depth Anchor | DepthAnchor（ドロップダウン） | v2.0.0（新規） |
| Writing Mode | WritingMode（ドロップダウン） | v2.0.0（新規） |
| Object Mode | ObjectMode（ドロップダウン） | v2.0.0（新規） |
| Max Width | float | v2.0.0（新規） |
| Max Height | float | v2.0.0（新規） |
| Vertical Column Width | float | v2.0.0（新規） |
| Rotate ASCII In Vertical | bool | v2.0.0（新規） |

**非表示フィールド（HideInInspector）**:

- `_fontBytesCache` (TextAsset) — エディタが自動管理

---

## CHANGELOG エントリ（v2.0.0 用）

```markdown
## [2.0.0] - 2026-04-21

### Added
- テキストアンカー: HorizontalAnchor / VerticalAnchor / DepthAnchor の 3 軸独立設定
- 縦書き対応: WritingMode.Vertical（上→下、右→左）
- Per-Character モード: ObjectMode.PerCharacter で文字ごとに子 GameObject を生成
- 自動折り返し: MaxWidth（横書き）/ MaxHeight（縦書き）による折り返し
- フォント Object フィールド: Inspector で .ttf/.otf ファイルを直接アタッチ可能
- .bytes 自動変換: FontAssetPostprocessor による .ttf/.otf → .bytes 自動生成
- Inspector デバウンス: 入力中はメッシュ再生成を抑制し、0.5 秒後に再生成
- パフォーマンス改善: 同一パラメータ時のメッシュ再生成スキップ

### Removed
- `SolidText3DComponent.Font` (string プロパティ) — `FontAsset` (UnityEngine.Object) を使用してください
- `MeshGenerationParams.FontPath` (string フィールド) — `FontData (byte[])` を使用してください

### Migration
1. `component.Font = "path"` → Inspector でフォントをアタッチ、またはコードで `component.FontAsset = ...`
2. `MeshGenerationParams.FontPath` → `MeshGenerationParams.FontData` に byte[] を設定
```
