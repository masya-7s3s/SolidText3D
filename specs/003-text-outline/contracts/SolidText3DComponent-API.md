# Contract: SolidText3DComponent 公開 API v2.1.0（アウトライン拡張）

**Branch**: `003-text-outline` | **Date**: 2026-04-23  
**SemVer**: `2.0.0 → 2.1.0`（MINOR: 後方互換の機能追加）

---

## 破壊的変更（BREAKING CHANGES）

なし。既存の公開 API はすべて変更なし。

---

## 追加 API（v2.1.0）

### `OutlineDisplayMode` enum（新規）

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>アウトラインの裏面形状モード。</summary>
    public enum OutlineDisplayMode
    {
        /// <summary>
        /// ドーナツモード。アウトラインはリング状で、背面から見たとき中央に文字本体が透けて見える。
        /// </summary>
        Donut,

        /// <summary>
        /// 裏面埋めモード。アウトライン内部が裏面で塗りつぶされた構造になる。
        /// </summary>
        BackFilled
    }
}
```

---

### `SolidText3DComponent` の追加プロパティ

```csharp
// ─── アウトライン設定 ──────────────────────────────────────────────

/// <summary>
/// アウトラインを生成するか。
/// true にすると子 GameObject が生成されメッシュが構築される。
/// false にすると子 GameObject が破棄される（設定値は SerializeField に保持される）。
/// </summary>
public bool OutlineEnabled { get; set; }

/// <summary>
/// アウトラインの外側オフセット量（Unity ワールド単位）。
/// 0 以上。0 のとき文字本体と同一輪郭のアウトラインが生成される。
/// </summary>
public float OutlineOffset { get; set; }

/// <summary>
/// アウトラインの Z 軸方向の厚さ（Unity ワールド単位）。
/// 0 以上。0 のとき表面ポリゴンのみ（厚みなし）が生成される。
/// 文字本体の ExtrusionDepth とは独立して設定できる。
/// </summary>
public float OutlineThickness { get; set; }

/// <summary>
/// アウトライン専用マテリアル。
/// null の場合は文字本体の MeshRenderer の共有マテリアルを参照する。
/// 文字本体の Material を変更しても、このプロパティが非 null の場合はアウトラインに影響しない。
/// </summary>
public Material OutlineMaterial { get; set; }

/// <summary>
/// アウトラインの裏面形状モード。
/// Donut: リング状（背面から中央が透けて見える）
/// BackFilled: 裏面が塗りつぶされた構造
/// 正面からの見た目はどちらのモードでも変わらない。
/// </summary>
public OutlineDisplayMode OutlineDisplayMode { get; set; }
```

---

## 動作仕様サマリー

### アウトライン表面（Z+側）の基準位置

両モードとも `Z = 0`（文字本体の表面と一致）。モード切り替えで表面位置は変化しない。

### アウトライン裏面の Z 計算（BackFilled モード）

```text
back_z = -max(bodyExtrusionDepth, OutlineThickness) - 0.0001f
```

`bodyExtrusionDepth` は `SolidText3DComponent.ExtrusionDepth` の現在値。

### 子 GameObject 構造

```text
[SolidText3DComponent がアタッチされた GO]
└── "__OutlineMesh__"
    ├── MeshFilter    ← OutlineMeshBuilder.Build() の出力
    └── MeshRenderer  ← OutlineMaterial（非 null 時）/ 本体マテリアル（null 時）
```

### マテリアル独立性

- `OutlineMaterial` が非 null: アウトラインの MeshRenderer.sharedMaterial に直接アサイン
- `OutlineMaterial` が null: 文字本体の MeshRenderer.sharedMaterial を参照（コピーではなく参照）

---

## 使用例（コード）

```csharp
// Inspector から設定（推奨）
// → SolidText3DComponent の "Outline" セクションで設定

// コードから設定
var st = GetComponent<SolidText3DComponent>();

// アウトラインを有効化
st.OutlineEnabled    = true;
st.OutlineOffset     = 0.05f;               // 5cm のオフセット（Unity単位）
st.OutlineThickness  = 0.1f;                // 文字本体とは独立した厚さ
st.OutlineMaterial   = outlineMat;          // 専用マテリアル
st.OutlineDisplayMode = OutlineDisplayMode.BackFilled;

// アウトラインを削除する（子 GO が破棄される。設定値は保持される）
st.OutlineEnabled = false;

// アウトラインを再生成する（子 GO が新規生成されメッシュが構築される）
st.OutlineEnabled = true;
// → OutlineOffset / Thickness / Material の前回設定が反映される
```

---

## Inspector UI レイアウト（参考）

```text
[ Outline ]
  ☑ Enabled
  Offset Amount   [0.05]
  Thickness       [0.25]
  Display Mode    [Donut ▼]
  Material        [None (Material) ○]
```
