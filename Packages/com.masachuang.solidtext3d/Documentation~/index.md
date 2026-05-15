# Solid Text 3D — ドキュメント

## 概要

Solid Text 3D は TTF/OTF フォントのグリフから 3D ポリゴンメッシュを生成する Unity UPM パッケージです。SixLabors.Fonts で輪郭を抽出し、LibTessDotNet と共有押し出しコアで本体メッシュを生成します。outline は Clipper2 で canonical ring profile を確定してから、Donut / BackFilled の 2 モードで立体化します。

## API リファレンス

### SolidText3DComponent

MonoBehaviour として GameObject に追加して使用します。

| プロパティ/メソッド | 型 | 説明 |
| ------------------ | --- | ---- |
| `Text` | `string` | 表示テキスト。変更時に dirty が立つ |
| `FontAsset` | `UnityEngine.Object` | Inspector で指定するフォントアセット |
| `ExtrusionDepth` | `float` | 本体メッシュの押し出し深さ |
| `OutlineEnabled` | `bool` | outline child の生成・再利用を切り替える |
| `OutlineOffset` | `float` | outline の外側オフセット量 |
| `OutlineThickness` | `float` | outline の厚み |
| `OutlineDisplayMode` | `OutlineDisplayMode` | `Donut` または `BackFilled` |
| `OutlineMaterial` | `Material` | null の場合は本体 sharedMaterial を継承 |
| `LetterSpacing` | `float` | 追加の文字間スペース |
| `LineSpacing` | `float` | 行間係数 |
| `FontSize` | `float` | em スケールの Unity 単位変換 |
| `ObjectMode` | `ObjectMode` | `SingleObject` / `PerCharacter` |
| `IsDirty` | `bool` | 次フレームで再生成が必要かどうか |
| `RegenerateMesh()` | `void` | 即時再生成 |

### MeshGenerationParams

メッシュ生成パラメータをまとめた struct です。

| フィールド | 型 | 説明 |
| ---------- | --- | ---- |
| `Text` | `string` | 表示テキスト |
| `FontData` | `byte[]` | フォントバイト配列 |
| `ExtrusionDepth` | `float` | 押し出し深さ |
| `OutlineWidth` | `float` | 互換用 alias。内部では outline offset として扱う |
| `LetterSpacing` | `float` | 文字間スペース |
| `LineSpacing` | `float` | 行間係数 |
| `FontSize` | `float` | em から Unity 単位へのスケール |
| `BezierErrorThreshold` | `float` | ベジェ曲線の適応分割誤差閾値 |

### OutlineDisplayMode

| 値 | 説明 |
| --- | ---- |
| `Donut` | front silhouette を維持したまま厚みを前後へ均等配分する |
| `BackFilled` | front silhouette を維持し、固定背面を単一の filled cap で閉じる |

## エディタでの使用ガイド

1. GameObject を作成する
2. Solid Text 3D Component を追加する
3. Text と FontAsset を設定する
4. Outline セクションで Enabled をオンにする
5. Offset Amount / Thickness / Display Mode / Material を調整する

### outline の振る舞い

- `OutlineOffset = 0` のときは outline child を維持したまま mesh だけをクリアする
- `Donut` と `BackFilled` は正面シルエットを共有する
- `BackFilled` は固定背面を filled cap で閉じ、厚みは反対側へ伸びる
- `OutlineMaterial = null` のときは本体の sharedMaterial を使用する

## ランタイムでの使用ガイド

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class OutlineRuntimeExample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent _text3D;

    private void Start()
    {
        _text3D.Text = "Hello, Outline";
        _text3D.OutlineEnabled = true;
        _text3D.OutlineOffset = 0.05f;
        _text3D.OutlineThickness = 0.1f;
        _text3D.OutlineDisplayMode = OutlineDisplayMode.Donut;
    }
}
```

## CJK 文字の使用

デフォルト埋め込みフォントは日本語・中国語・韓国語を含む CJK テキストを扱えます。

```csharp
_text3D.Text = "立体文字";
_text3D.Text = "汉字";
_text3D.Text = "한글";
_text3D.Text = "Hello 世界";
```

## トラブルシューティング

### 正面から outline が見えない

- `OutlineEnabled` が true か確認する
- `OutlineOffset` が 0 より大きいか確認する
- `OutlineDisplayMode` に関係なく front silhouette は同一なので、見え方の差は背面側だけか確認する

### フォントが読み込まれない

- FontAsset に有効な TextAsset が割り当たっているか確認する
- ランタイムで直接与える場合は `MeshGenerationParams.FontData` を使用する

### パフォーマンス

- clean frame の LateUpdate は dirty チェックで即 return する
- profiler sample は outline build の各段に追加済み
- 2026-05-13 時点で Edit Mode / Play Mode テスト green を確認済み

## 既知の制限事項

- SixLabors.Fonts は Unity 6 / CoreCLR 前提です
- 複雑な glyph では dirty 時の再生成コストが上がります
- Windows player build の最終手動確認ログは quickstart に記録運用です
