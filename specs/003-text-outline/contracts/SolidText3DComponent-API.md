# Contract: SolidText3DComponent 公開 API v2.1.0（outline 再設計反映）

**Branch**: `003-text-outline-alt` | **Date**: 2026-05-12  
**SemVer**: `2.0.0 → 2.1.0`（MINOR）

## 破壊的変更

なし。今回の再設計は internal な面生成経路の差し替えであり、公開 API には破壊的変更を含まない。

## 追加 API

### `OutlineDisplayMode`

```csharp
namespace MasaChuang.SolidText3D
{
    public enum OutlineDisplayMode
    {
        Donut,
        BackFilled
    }
}
```

### `SolidText3DComponent` の追加プロパティ

```csharp
public bool OutlineEnabled { get; set; }
public float OutlineOffset { get; set; }
public float OutlineThickness { get; set; }
public Material OutlineMaterial { get; set; }
public OutlineDisplayMode OutlineDisplayMode { get; set; }
```

## 動作契約

### 1. 正面シルエット

- `Donut` と `BackFilled` は同じ front silhouette を共有する
- front silhouette は「外側へオフセットした文字充填領域」から「元文字の充填領域」を差し引いた 2D リング断面に基づく
- mode 切り替えで変わるのは奥行き方向の構成だけである

### 2. 奥行き配分

**Donut**:

- 厚みは中央基準で前後へ均等配分される

**BackFilled**:

- 背面位置は文字本体の背面から `0.0001f` 後方に固定される
- 厚みの変更は前方へだけ反映される

### 3. child GameObject

```text
[SolidText3DComponent がアタッチされた GO]
└── "__OutlineMesh__"
    ├── MeshFilter
    └── MeshRenderer
```

- `OutlineEnabled = false` のとき child GO は破棄される
- `OutlineEnabled = true` のとき child GO は生成または再利用される
- `OutlineEnabled = true` かつ `OutlineOffset = 0` のとき child GO は破棄せず、outline mesh をクリアして「outline なし相当」として扱う

### 3.1 入力範囲

- `OutlineOffset` と `OutlineThickness` は 0 以上の非負値を受け付ける
- `OutlineOffset = 0` は有効な入力であり、outline ジオメトリを生成しない特別値として扱う

### 4. material fallback

- `OutlineMaterial != null`: outline child の `MeshRenderer.sharedMaterial` にそのまま適用する
- `OutlineMaterial == null`: 本体 `MeshRenderer.sharedMaterial` を参照する

## 使用例

```csharp
var text = GetComponent<SolidText3DComponent>();

text.OutlineEnabled = true;
text.OutlineOffset = 0.05f;
text.OutlineThickness = 0.10f;
text.OutlineMaterial = outlineMaterial;
text.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
```

```csharp
text.OutlineEnabled = true;
text.OutlineOffset = 0f;
// child GO は維持されるが、outline mesh はクリアされて表示されない
```

## Inspector レイアウト

```text
[ Outline ]
  Enabled
  Offset Amount
  Thickness
  Display Mode
  Material
```

## 実装上の拘束条件

- front face の生成は outline 専用の別規約で実装してはならない
- outline mesh は body mesh と同じ cap/side 生成コアを共有しなければならない
- これにより front face の winding 不一致による非表示を防ぐ
