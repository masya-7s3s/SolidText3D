# Solid Text 3D — ドキュメント

## 概要

Solid Text 3D は、TTF / OTF フォントのグリフから 3D ポリゴンメッシュを生成する Unity UPM パッケージです。  
現在の実装では、Font Asset、outline、縦書き、PerCharacter、deferred regeneration を公開 API と Inspector から扱えます。

## 最初に知っておくとよいこと

- 設定変更だけでは表示は更新されません
- 即時反映したい場合は RegenerateMesh() を使います
- 高頻度更新では RequestRegenerateMesh() を使います
- Font Asset 未設定時は NotoSansJP-Black を使います
- 横書きの MaxWidth は現行実装では自動折り返しに使われません

## SolidText3DComponent

MonoBehaviour として GameObject に追加して使用します。

| プロパティ/メソッド | 型 | 説明 |
| ------------------ | --- | ---- |
| Text | string | 表示テキストです。変更すると dirty 状態になります。 |
| FontAsset | UnityEngine.Object | Font または TextAsset を参照できます。Editor では Font から直接データを読めます。 |
| ExtrusionDepth | float | 本体メッシュの押し出し深さです。 |
| OutlineEnabled | bool | outline の有効化です。false で outline 子オブジェクトを破棄します。 |
| OutlineOffset | float | outline の外側オフセット量です。 |
| OutlineThickness | float | outline の奥行き比率です。1 で本体の ExtrusionDepth と同じ厚さになります。 |
| OutlineDisplayMode | OutlineDisplayMode | Donut または BackFilled を切り替えます。 |
| OutlineMaterial | Material | null の場合は本体 sharedMaterial を使います。 |
| LetterSpacing | float | 追加の文字間隔です。 |
| LineSpacing | float | 横書きでは行間倍率、縦書きでは列幅倍率です。 |
| FontSize | float | em から Unity 単位へのスケールです。 |
| HorizontalAnchor | HorizontalAnchor | 横方向の基準位置です。 |
| VerticalAnchor | VerticalAnchor | 縦方向の基準位置です。 |
| DepthAnchor | DepthAnchor | 奥行き方向の基準位置です。 |
| WritingMode | WritingMode | Horizontal / Vertical を切り替えます。 |
| MonospaceMode | bool | 全角を 1em、半角を 0.5em の固定セルで配置する等幅表示モードです。LetterSpacing は可変幅時と同じ追加間隔として使われます。 |
| ObjectMode | ObjectMode | SingleObject / PerCharacter を切り替えます。 |
| MaxWidth | float | 横書き用の値として保持されますが、現行実装では自動折り返しに使われません。 |
| MaxHeight | float | 縦書きの列折り返し高さです。 |
| RotateAsciiInVertical | bool | 縦書き時に印字可能 ASCII を 90 度回転します。 |
| IsDirty | bool | 再生成が必要な状態かどうかです。 |
| HasPendingRegeneration | bool | deferred path の in-flight / pending / ready 状態が残っているかを表します。 |
| SuppressAutoRegenerate | bool | 公開フラグですが、現行実装ではこの値だけで再生成挙動は変わりません。 |
| RegenerateMesh() | void | 同期的に表示へ反映します。 |
| RequestRegenerateMesh() | void | 高頻度更新向け deferred regeneration を要求します。 |
| DeferredRegenerationFailed | event | deferred regeneration 失敗を通知します。表示は keep-last-good を維持します。 |

## Regeneration API の使い分け

- RegenerateMesh(): 呼び出した時点で表示反映まで完了させたいとき
- RequestRegenerateMesh(): タイマーやスコアのような高頻度更新で使いたいとき
- HasPendingRegeneration: deferred 更新が収束したかを見たいとき

RequestRegenerateMesh() は latest-only で古い request を圧縮し、古い completed result で表示が巻き戻らないように実装されています。

## 等幅表示モード

- MonospaceMode = true で、全角文字は 1em 幅、半角文字は 0.5em 幅の固定セルで配置します
- LetterSpacing は固定セルの外側に加算されるため、可変幅モードと切り替えても字間の印象が極端に変わりにくくなります
- タイマー、時計、スコア表示のように桁揃えを優先したい用途に向いています

## Inspector セクション

- Mesh Update: dirty 状態の表示と Regenerate Mesh
- Text & Font: テキストとフォントの設定
- Geometry: Extrusion Depth、Font Size、Letter Spacing、Line Spacing
- Layout: Writing Mode、Monospace Mode、Anchor、Max Width、Max Height
- Outline: Enabled、Offset Amount、Thickness Ratio (Body=1)、Display Mode、Material
- Output: Object Mode の切り替え

## outline の振る舞い

- outline は __OutlineMesh__ という子オブジェクトで別管理されます
- OutlineEnabled = false で子オブジェクトごと破棄されます
- OutlineOffset = 0 では子オブジェクトを残したままメッシュだけ空になります
- OutlineThickness は本体の ExtrusionDepth を 1 とした相対値として解釈されます
- Donut と BackFilled は正面シルエットを共有し、違いは主に奥行き方向です

## フォントの扱い

フォント解決の優先順は次の通りです。

1. Editor では Font Asset から直接読めたデータ
2. 内部の .bytes キャッシュ
3. パッケージ同梱の NotoSansJP-Black

Editor では .ttf / .otf のインポート時に Assets/SolidText3DFonts/{GUID}.bytes を自動生成します。  
ビルド済みプレイヤーで FontAsset を差し替えても、その場で新しいフォントデータを自動解決するわけではありません。

## ランタイムでの使用例

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class ScoreTickerExample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent text3D;

    private void OnEnable()
    {
        text3D.DeferredRegenerationFailed += OnDeferredRegenerationFailed;
    }

    private void OnDisable()
    {
        text3D.DeferredRegenerationFailed -= OnDeferredRegenerationFailed;
    }

    public void UpdateScore(int score)
    {
        text3D.Text = score.ToString();
        text3D.RequestRegenerateMesh();
    }

    private static void OnDeferredRegenerationFailed(RegenerationFailureInfo info)
    {
        Debug.LogWarning($"Deferred regeneration failed: {info.Message}");
    }
}
```

## CJK テキスト

デフォルトフォントで日本語を含む CJK テキストを扱えます。

```csharp
text3D.Text = "立体文字";
text3D.Text = "Hello 世界";
```

## トラブルシューティング

### outline が見えない

- OutlineEnabled が true か確認します
- OutlineOffset が 0 より大きいか確認します
- 本体と同じ Material で見分けづらくなっていないか確認します

### フォントが読み込まれない

- Editor では Font Asset か .bytes キャッシュを確認します
- ランタイムでは直前の正常表示が残ることがあります

### 横書きで折り返されない

現行実装では MaxWidth を使いません。  
横書きで行を分ける場合は改行文字を入れてください。

## 既知の制限事項

- SixLabors.Fonts は Unity 6 / CoreCLR 前提です
- 複雑な glyph では再生成コストが上がります
- PerCharacter は SingleObject よりオブジェクト数が増えます
