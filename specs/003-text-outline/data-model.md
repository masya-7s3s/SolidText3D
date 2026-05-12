# Data Model: テキストアウトライン生成

**Branch**: `003-text-outline` | **Date**: 2026-04-23

---

## 新規 Enum 型

### `OutlineDisplayMode`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>アウトラインの裏面形状モード。</summary>
    public enum OutlineDisplayMode
    {
        /// <summary>
        /// ドーナツモード。アウトラインはリング状で、背面から見たとき中央に文字本体が透けて見える。
        /// 裏面ポリゴンは生成しない。
        /// </summary>
        Donut,

        /// <summary>
        /// 裏面埋めモード。アウトライン内部が裏面で塗りつぶされ、文字本体のくぼみが設けられた構造になる。
        /// 裏面ポリゴンにはオフセット外周形状から文字本体形状を差し引いた領域を使用する。
        /// </summary>
        BackFilled
    }
}
```

---

## 新規クラス / 構造体

### `OutlineSettings`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>アウトライン機能の全設定を保持するシリアライズ可能クラス。</summary>
    [System.Serializable]
    public sealed class OutlineSettings
    {
        /// <summary>アウトラインを有効にするか。false の場合は子 GameObject が破棄される。true の場合は子 GameObject が生成されメッシュが構築される。</summary>
        [SerializeField] public bool Enabled = false;

        /// <summary>
        /// 外側へのオフセット量（Unity ワールド単位）。0 以上。
        /// 0 のとき文字本体と同一輪郭になる。
        /// </summary>
        [SerializeField] public float OffsetAmount = 0.05f;

        /// <summary>Z 軸方向の厚さ（Unity ワールド単位）。0 以上。</summary>
        [SerializeField] public float Thickness = 0.25f;

        /// <summary>
        /// アウトライン専用マテリアル。null の場合は文字本体の MeshRenderer の共有マテリアルを使用する。
        /// </summary>
        [SerializeField] public Material Material = null;

        /// <summary>裏面形状モード。ドーナツまたは裏面埋め。</summary>
        [SerializeField] public OutlineDisplayMode DisplayMode = OutlineDisplayMode.Donut;
    }
}
```

**フィールド詳細:**

| フィールド | 型 | デフォルト | 制約 | 説明 |
| --------- | --- | --------- | ---- | ---- |
| `Enabled` | `bool` | `false` | - | アウトライン生成フラグ。false=子 GO 破棄、true=子 GO 生成+メッシュ構築 |
| `OffsetAmount` | `float` | `0.05f` | ≥ 0 | 外側オフセット量（Unity ワールド単位） |
| `Thickness` | `float` | `0.25f` | ≥ 0 | Z 軸方向の厚さ |
| `Material` | `Material` | `null` | - | 専用マテリアル。null = 本体マテリアルを参照 |
| `DisplayMode` | `OutlineDisplayMode` | `Donut` | - | 裏面形状モード |

---

### `OutlineContourBuilder`（内部クラス）

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// Clipper2 を使ってグリフ輪郭を外側へオフセットし、穴除去済みの輪郭リストを返す静的クラス。
    /// </summary>
    internal static class OutlineContourBuilder
    {
        private const long SCALE = 1000L;

        /// <summary>
        /// グリフ輪郭をオフセットして外周パスのリストを返す。穴パスは除外される。
        /// </summary>
        /// <param name="glyphContours">元グリフの輪郭リスト（BezierSubdivider 済み）。</param>
        /// <param name="offsetAmountEm">em 空間でのオフセット量。</param>
        /// <param name="joinType">Clipper2 のジョイン方式。デフォルト: Round。</param>
        /// <returns>穴除去済みの外周パスリスト（em 空間座標）。</returns>
        public static List<List<Vector2>> BuildOffsetContours(
            List<GlyphContour> glyphContours,
            float offsetAmountEm,
            JoinType joinType = JoinType.Round);
    }
}
```

**処理フロー:**

```text
入力: GlyphContour[] (BezierSubdivider 済みポイント列, em 空間)
  ↓ ×SCALE → Paths64
  ↓ ClipperOffset.Execute(offsetAmountEm × SCALE)
  ↓ Clipper.Area() で CCW/CW 判定 → CW（穴）パスを除外
出力: List<List<Vector2>> (外周パスのみ, em 空間 float)
```

**`fontSizeScale`（オフセット量の単位変換）:**

`offsetAmountEm` の計算に使用する `fontSizeScale` は `MeshGenerationParams.FontSize` の値そのもの。  
`FontSize = 1.0f` のとき 1 em = 1 Unity unit。

```csharp
// Unity ワールド単位 → em 空間への変換
float offsetAmountEm = settings.OffsetAmount / params.FontSize;
```

---

### `OutlineContourBuilder` → `OutlineMeshBuilder` パイプライン

`OutlineMeshBuilder.Build()` の内部で `OutlineContourBuilder.BuildOffsetContours()` を呼び出す。  
呼び出し側は元グリフと設定を渡し、オフセット輪郭リストを受け取ってメッシュを構築する。

```text
OutlineMeshBuilder.Build(glyphs, settings, bodyExtrusionDepth)
  ↓
  各グリフごとに:
    offsetContoursEm = OutlineContourBuilder.BuildOffsetContours(
        glyph.Contours,           ← 元グリフの輪郭（BezierSubdivider 済み, em 空間）
        offsetAmountEm,           ← settings.OffsetAmount / params.FontSize
        JoinType.Round            ← デフォルト
    )
    ↓
    [表面] offsetContoursEm + 元グリフ輪郭を穴として LibTessDotNet で三角分割
    [側面] offsetContoursEm の外周に沿ったクワッドストリップ
    [裏面] (BackFilled のみ) offsetContoursEm を LibTessDotNet で三角分割（穴なし）
  ↓
  全グリフ分をマージして Mesh を返す
```

---

### `OutlineMeshBuilder`（内部クラス）

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// オフセット輪郭から 3D アウトラインメッシュを生成する静的クラス。
    /// LibTessDotNet を使用してポリゴンを三角分割する。
    /// </summary>
    internal static class OutlineMeshBuilder
    {
        private const float Z_FIGHT_EPSILON = 0.0001f;
        /// <summary>
        /// アウトラインメッシュを生成する。
        /// </summary>
        /// <param name="glyphs">元グリフの輪郭データリスト（レイアウト適用済み）。</param>
        /// <param name="settings">アウトライン設定。</param>
        /// <param name="bodyExtrusionDepth">文字本体の押し出し深さ（Z ファイティング計算に使用）。</param>
        /// <returns>生成された Unity Mesh。</returns>
        public static Mesh Build(
            List<GlyphContour> glyphs,
            OutlineSettings settings,
            float bodyExtrusionDepth);
    }
}
```

**生成するメッシュ部位:**

| 部位 | 説明 | 生成条件 |
| ---- | ---- | ------- |
| 表面（Front face） | Z=0 のオフセット輪郭を LibTessDotNet で三角分割。元グリフ輪郭を穴として EvenOdd で差し引き（ドーナツ状） | 常に生成 |
| 側面（Side faces） | オフセット輪郭の外周に沿ったクワッドストリップ | `Thickness > 0` のとき |
| 裏面（Back face, 裏面埋めモードのみ） | `Z = -backZ` のオフセット輪郭全体を LibTessDotNet で三角分割（穴なし） | `DisplayMode == BackFilled` のとき |

**Z 座標計算:**

```text
表面 Z    = 0
側面 Z    = 0 〜 -Thickness
裏面 Z    = -max(bodyExtrusionDepth, Thickness) - Z_FIGHT_EPSILON  ※BackFilled のみ
```

---

## 変更される型

### `SolidText3DComponent`（修正）

**追加フィールド:**

| フィールド | 型 | SerializeField | 説明 |
| --------- | --- | :---: | ---- |
| `_outline` | `OutlineSettings` | ✅ | アウトライン設定（インライン展開） |
| `_outlineChild` | `GameObject` | ✅ | アウトライン子 GO への参照 |
| `_outlineMeshFilter` | `MeshFilter` | ✅ | 子 GO の MeshFilter への参照 |
| `_outlineRenderer` | `MeshRenderer` | ✅ | 子 GO の MeshRenderer への参照 |

**追加メソッド（private）:**

| メソッド | 説明 |
| -------- | ---- |
| `CreateOutlineChild()` | 子 GO を新規生成し MeshFilter + MeshRenderer を付与する |
| `DestroyOutlineChildIfExists()` | 子 GO が存在する場合のみ破棄し、参照を null クリアする |
| `UpdateOutlineMesh()` | `_isDirty` 時に呼ばれ、Enabled=false なら破棄、Enabled=true なら生成または更新する |
| `DestroyOutlineChild()` | `OnDestroy` 時に子 GO を無条件破棄する |

**追加プロパティ（public）:**

| プロパティ | 型 | 説明 |
| --------- | --- | ---- |
| `OutlineEnabled` | `bool` | アウトライン生成フラグ（`_outline.Enabled` のラッパー）。false で子 GO 破棄、true で子 GO 生成+メッシュ構築 |
| `OutlineOffset` | `float` | オフセット量（`_outline.OffsetAmount` のラッパー） |
| `OutlineThickness` | `float` | 厚さ（`_outline.Thickness` のラッパー） |
| `OutlineMaterial` | `Material` | マテリアル（`_outline.Material` のラッパー） |
| `OutlineDisplayMode` | `OutlineDisplayMode` | 表示モード（`_outline.DisplayMode` のラッパー） |

**`LateUpdate()` の変更:**  
既存の `RegenerateMesh()` 呼び出しの後に `UpdateOutlineMesh()` を追加する。アウトライン再生成は同一ダーティフラグ `_isDirty` を共有する。

---

## 子 GameObject の名前と構造

```text
[SolidText3DComponent がアタッチされた GO]
└── "__OutlineMesh__" (GameObject)
    ├── MeshFilter    ← アウトラインメッシュを保持
    └── MeshRenderer  ← アウトライン専用マテリアルを使用
```

- 子 GO 名: `"__OutlineMesh__"` （二重アンダースコアで内部実装であることを明示）
- `hideFlags`: `HideFlags.None`（デバッグのためヒエラルキーに表示）
- `transform.localPosition/rotation/scale`: 常に恒等変換

---

## SemVer バンプ

| 変更 | バンプ種別 |
| ---- | --------- |
| `OutlineSettings`, `OutlineDisplayMode`, `OutlineMeshBuilder`, `OutlineContourBuilder` の追加 | MINOR |
| `SolidText3DComponent` へのアウトライン関連プロパティ追加 | MINOR |
| 既存 API への変更なし | — |

**バージョン**: `2.0.0 → 2.1.0`
