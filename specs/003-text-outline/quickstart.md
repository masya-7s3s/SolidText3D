# クイックスタートガイド: テキストアウトライン生成（v2.1.0）

**Branch**: `003-text-outline` | **Date**: 2026-04-23

---

## 前提条件

- `SolidText3DComponent` が GameObject にアタッチされ、フォントと文字が設定済みであること
- Unity 6 (6000.x LTS) + URP 環境

---

## Inspector からアウトラインを設定する（推奨）

1. アウトライン対象の GameObject を選択する
2. Inspector の `SolidText3DComponent` コンポーネントを開く
3. **「Outline」セクション**を展開する
4. **「Enabled」チェックボックス**をオンにする
5. 各パラメータを設定する:

   | パラメータ | 推奨初期値 | 説明 |
   | --------- | --------- | ---- |
   | Offset Amount | `0.05` | 文字外周からのオフセット量（Unity ワールド単位） |
   | Thickness | `0.25` | Z 軸方向の厚さ（文字本体と独立） |
   | Display Mode | `Donut` | 裏面形状。ドーナツ状 or 裏面埋め |
   | Material | `None` | 専用マテリアル。未設定時は文字本体と同じマテリアルを使用 |

→ 設定変更と同時にシーンビューのアウトラインがリアルタイム更新される。

---

## コードからアウトラインを制御する

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public class OutlineExample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent _text;
    [SerializeField] private Material _outlineMaterial;

    void Start()
    {
        // アウトラインを有効化
        _text.OutlineEnabled     = true;
        _text.OutlineOffset      = 0.05f;
        _text.OutlineThickness   = 0.1f;
        _text.OutlineMaterial    = _outlineMaterial;   // null = 本体マテリアルを共有
        _text.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
    }

    // ゲームプレイ中にアウトラインを動的に生成・削除する例
    public void ToggleOutline(bool generate)
    {
        _text.OutlineEnabled = generate;
        // generate=false → 子 GameObject が破棄される（設定値は保持）
        // generate=true  → 子 GameObject が新規生成され前回の設定でメッシュが構築される
    }
}
```

---

## 表示モードの選択ガイド

### Donut（ドーナツモード）— デフォルト

```text
正面:  ┌─────────────┐
       │ ┌─────────┐ │
       │ │  文字本体 │ │  ← アウトライン（リング状）
       │ └─────────┘ │
       └─────────────┘

背面:  中央に文字本体が透けて見える
```

**向いているシーン**: 正面からのみ見せる UI テキスト、正面表示のみの 3D UI

---

### BackFilled（裏面埋めモード）

```text
正面:  ドーナツモードと同じ見た目

背面:  └─────────────┘  ← 塗りつぶされた裏面
       （文字本体のくぼみが設けられた構造）
```

**向いているシーン**: あらゆる角度から見せる 3D テキスト、VR / AR コンテンツ

---

## アウトラインと文字本体の厚さ設定例

```csharp
// ケース 1: アウトラインを文字本体より薄くする
_text.ExtrusionDepth  = 0.5f;   // 文字本体: 0.5 Unity単位
_text.OutlineThickness = 0.2f;  // アウトライン: 0.2 Unity単位
// → BackFilled モード時、アウトライン裏面は -0.5 - 0.0001f に配置（Zファイティング回避）

// ケース 2: アウトラインを文字本体より厚くする
_text.ExtrusionDepth  = 0.1f;
_text.OutlineThickness = 0.4f;
// → BackFilled モード時、アウトライン裏面は -0.4 - 0.0001f に配置
```

---

## Clipper2 依存のセットアップ（パッケージ初回インストール時のみ）

> **注**: Unity Package Manager でこのパッケージをインポートすると `Clipper2Lib.dll` が  
> `Runtime/Plugins/` に自動配置されます。手動での DLL コピーは不要です。

---

## トラブルシューティング

| 症状 | 確認事項 |
| ---- | ------- |
| アウトラインが表示されない | Inspector の「Enabled」がオンになっているか確認。`OutlineEnabled = true` がコードから設定されているか確認。Hierarchy で `"__OutlineMesh__"` 子 GO が存在するか確認 |
| アウトライン形状が崩れる | `Offset Amount` が大きすぎて文字内部の穴が外周と融合している可能性がある（仕様動作）。値を小さくして確認 |
| 文字とアウトラインの間に隙間がある | `Offset Amount = 0` に設定すると文字本体と完全一致した輪郭になる |
| 裏面埋めモードで Zファイティングが発生する | `ExtrusionDepth` と `OutlineThickness` の差が非常に小さい場合に生じることがある。`Z_FIGHT_EPSILON`（0.0001f）は自動適用されているため、通常は発生しない |
| アウトライン子 GameObject が複数生成される | `"__OutlineMesh__"` という名前の子 GO が重複している場合は手動で削除する。次回再生成時に 1 つだけ作成される |
