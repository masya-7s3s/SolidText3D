# Data Model: テキストアウトライン生成（面生成経路の再設計）

**Branch**: `003-text-outline-alt` | **Date**: 2026-05-12

## 公開 Enum

### `OutlineDisplayMode`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>アウトラインの奥行き構成モード。</summary>
    public enum OutlineDisplayMode
    {
        /// <summary>
        /// リング断面を中央基準で前後へ均等に押し出す。
        /// 背面から見ると中央に文字本体が抜けて見える。
        /// </summary>
        Donut,

        /// <summary>
        /// front silhouette は Donut と同一のまま、背面を固定アンカーで閉じる。
        /// 背面から見ると太字状に埋まって見える。
        /// </summary>
        BackFilled
    }
}
```

## 公開設定型

### `OutlineSettings`

```csharp
namespace MasaChuang.SolidText3D
{
    [System.Serializable]
    public sealed class OutlineSettings
    {
        [SerializeField] public bool Enabled = false;
        [SerializeField] public float OffsetAmount = 0.05f;
        [SerializeField] public float Thickness = 0.25f;
        [SerializeField] public Material Material = null;
        [SerializeField] public OutlineDisplayMode DisplayMode = OutlineDisplayMode.Donut;
    }
}
```

| フィールド | 型 | 制約 | 説明 |
| --- | --- | --- | --- |
| `Enabled` | `bool` | - | outline 子 GO の生成 / 破棄を制御する |
| `OffsetAmount` | `float` | `>= 0` | 外側へのオフセット量（Unity 単位） |
| `Thickness` | `float` | `>= 0` | outline の奥行き |
| `Material` | `Material` | null 可 | null 時は本体 `MeshRenderer.sharedMaterial` にフォールバック |
| `DisplayMode` | `OutlineDisplayMode` | - | Donut / BackFilled の切り替え |

## 新規 internal モデル

### `OutlineProfileSet`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// outline 生成に必要な canonical 2D profile 群を保持する内部モデル。
    /// </summary>
    internal sealed class OutlineProfileSet
    {
        public List<List<Vector2>> OriginalFilledContoursEm { get; set; }
        public List<List<Vector2>> OffsetFilledContoursEm { get; set; }
        public List<List<Vector2>> RingContoursEm { get; set; }
    }
}
```

| フィールド | 説明 |
| --- | --- |
| `OriginalFilledContoursEm` | 元文字本体の充填領域。BackFilled rear cap と parity 検査に使う |
| `OffsetFilledContoursEm` | 外側へ拡張した充填領域。bounds 更新や silhouette 検査に使う |
| `RingContoursEm` | `OffsetFilled - OriginalFilled` の最終断面。Donut / BackFilled 共通の front silhouette |

## internal Builder

### `OutlineContourBuilder`

```csharp
namespace MasaChuang.SolidText3D
{
    internal static class OutlineContourBuilder
    {
        public static OutlineProfileSet BuildProfiles(
            GlyphContour glyph,
            float offsetAmountEm);
    }
}
```

**処理フロー**:

```text
GlyphContour (元文字本体, em 空間)
  ↓ 正規化
OriginalFilledContoursEm
  ↓ ClipperOffset (positive delta)
OffsetFilledContoursEm
  ↓ Clipper boolean difference
RingContoursEm = OffsetFilledContoursEm - OriginalFilledContoursEm
  ↓ winding を body と同じ規約へ正規化
OutlineProfileSet
```

**契約**:

- raw offset 外周だけを返してはならない
- 出力 contour の outer / hole 規約は body と一致していなければならない
- hole 吸収後に輪郭数が減ることは許容し、その結果をそのまま canonical profile とする

## internal Mesh Composition

### `OutlineMeshBuilder`

```csharp
namespace MasaChuang.SolidText3D
{
    internal static class OutlineMeshBuilder
    {
        private const float Z_FIGHT_EPSILON = 0.0001f;

        public static Mesh Build(
            List<GlyphContour> glyphs,
            OutlineSettings settings,
            float bodyExtrusionDepth,
            float fontSize);
    }
}
```

**責務**:

- 各 glyph から `OutlineProfileSet` を構築する
- `RingContoursEm` を body と同じ cap/side 生成コアで押し出して ring shell を作る
- mode ごとに Z 配置を決める
- `BackFilled` のときだけ `OriginalFilledContoursEm` を rear infill cap として追加する
- glyph 単位の mesh をマージして最終 `Mesh` を返す

**mode ごとの Z ルール**:

```text
Donut:
  frontZ = +Thickness / 2
  backZ  = -Thickness / 2

BackFilled:
  backZ  = -(bodyExtrusionDepth + Z_FIGHT_EPSILON)
  frontZ = backZ + Thickness
  rear infill cap は backZ に配置

Thickness == 0:
  front cap のみ生成
```

## `MeshExtruder` の再利用方針

`MeshExtruder` は public API としての役割を維持しつつ、outline でも同じ面生成規約を使えるよう internal helper を切り出す。

**想定 helper**:

```csharp
internal static void AppendCap(...)
internal static void AppendSideFaces(...)
internal static void TranslateZRange(...)
```

`OutlineMeshBuilder` はこれら helper を使って ring shell と rear cap を構成する。重要なのは「outline 専用の別 triangulation 規約を持たないこと」であり、helper 名や配置はそれを満たす限り実装時に調整可能。

## `SolidText3DComponent` 変更点

### 追加 SerializeField

| フィールド | 型 | 説明 |
| --- | --- | --- |
| `_outline` | `OutlineSettings` | outline 設定本体 |
| `_outlineChild` | `GameObject` | outline 用 child GO |
| `_outlineMeshFilter` | `MeshFilter` | child の MeshFilter |
| `_outlineRenderer` | `MeshRenderer` | child の MeshRenderer |

### 追加公開プロパティ

| プロパティ | 型 | 説明 |
| --- | --- | --- |
| `OutlineEnabled` | `bool` | outline の生成 / 削除トグル |
| `OutlineOffset` | `float` | 外側オフセット量 |
| `OutlineThickness` | `float` | outline 奥行き |
| `OutlineMaterial` | `Material` | outline 専用 material |
| `OutlineDisplayMode` | `OutlineDisplayMode` | Donut / BackFilled |

### ライフサイクル契約

- `_isDirty == true` のときだけ本体 mesh 再生成後に outline mesh も更新する
- `OutlineEnabled == false` の場合は child GO を破棄する
- `OutlineEnabled == true` の場合は child GO を生成または再利用し、material fallback を適用して mesh を差し替える

## SemVer

| 変更 | バンプ |
| --- | --- |
| 公開 `Outline*` API の追加 | MINOR |
| 今回の面生成再設計 | 内部実装変更のみ |

**Version**: `2.0.0 → 2.1.0`
