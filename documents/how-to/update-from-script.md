# スクリプトから更新する

このガイドでは、Play Mode 中にテキストを変更し、用途に応じて同期更新と deferred 更新を使い分ける方法を説明します。

## 基本ルール

SolidText3DComponent は、プロパティを変更しただけでは表示を更新しません。  
変更後にどちらかを呼びます。

- RegenerateMesh(): 即時に反映したいとき
- RequestRegenerateMesh(): 高頻度更新でフレームを詰まらせたくないとき

## 即時に反映する

ボタン押下、会話ウィンドウの見出し、演出の開始時など、呼んだ直後に見た目を更新したい場合は RegenerateMesh() を使います。

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class RoundStartLabel : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent text3D;

    public void ShowRound(int round)
    {
        text3D.Text = $"ROUND {round}";
        text3D.ExtrusionDepth = 0.18f;
        text3D.RegenerateMesh();
    }
}
```

複数の設定を変える場合は、全部変更してから最後に 1 回だけ RegenerateMesh() を呼ぶのが安全です。

## 高頻度更新では RequestRegenerateMesh() を使う

タイマー、スコア、HP 表示のように短い間隔で何度も更新する場合は RequestRegenerateMesh() が向いています。  
この API は non-blocking で request を投入し、古い request を latest-only で圧縮します。

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class TimerLabel : MonoBehaviour
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

    private void Update()
    {
        text3D.Text = Time.time.ToString("F1");
        text3D.RequestRegenerateMesh();
    }

    private static void OnDeferredRegenerationFailed(RegenerationFailureInfo info)
    {
        Debug.LogWarning($"Deferred regeneration failed: {info.Message}");
    }
}
```

この方式では、呼んだその瞬間に必ず表示が変わるわけではありません。  
ただし、完了した古い結果で表示が巻き戻らないように実装されています。  
途中で Text や Layout を変えると、未適用の ready result や待機中 request は無効化されます。

## 進行状況を知りたいとき

deferred 更新中かどうかは HasPendingRegeneration で確認できます。

```csharp
if (!text3D.HasPendingRegeneration)
{
    Debug.Log("最新の deferred request がいったん収束しています。");
}
```

待機中、実行中、適用待ち ready result のいずれかが残っている間は true です。

## よく使う調整項目

スクリプトから変更しやすい項目は次の通りです。

- Text
- FontAsset
- ExtrusionDepth
- OutlineEnabled
- OutlineOffset
- OutlineThickness
- OutlineDisplayMode
- OutlineMaterial
- LetterSpacing
- LineSpacing
- FontSize
- HorizontalAnchor
- VerticalAnchor
- DepthAnchor
- WritingMode
- MonospaceMode
- ObjectMode
- MaxWidth
- MaxHeight
- RotateAsciiInVertical

これらを変更すると dirty 状態になります。  
ただし MaxWidth は現行の横書き自動折り返しには使われません。

## 文字ごとに別オブジェクトへ分ける

ObjectMode を PerCharacter にすると、可視文字ごとに子 GameObject を作ります。

```csharp
text3D.ObjectMode = ObjectMode.PerCharacter;
text3D.RegenerateMesh();
```

このモードは次の用途に向いています。

- 文字ごとにアニメーションを付ける
- 文字ごとに個別の位置や回転を制御する
- 一部の文字だけ差し替える演出を行う

実装上のポイントは次の通りです。

- 子オブジェクト名は Char_0, Char_1, Char_2... です
- 親側の MeshRenderer は無効化されます
- 非表示文字は子オブジェクトになりません
- 余った子オブジェクトは再利用のため非アクティブ化されます
- SingleObject に戻したときや空文字列を再生成したときは、既存の子オブジェクトを破棄します

## 毎フレーム更新で気をつけること

- 同期反映が必要ない限り RegenerateMesh() を毎フレーム呼ばない
- 表示内容が本当に変わったときだけ Text を更新する
- 文字数の多い長文を高頻度で更新しない
- PerCharacter は SingleObject よりオブジェクト数が増えるので必要な場所だけ使う

同梱 Samples の Basic Usage / CJK Example も、Text を更新したあとに RegenerateMesh() を呼ぶ構成です。
どちらのサンプルもスペースキー入力は Input System / Legacy Input Manager の両方に対応しています。

## ランタイムでフォントを差し替えたいとき

ビルド済みプレイヤーでは、FontAsset を差し替えただけで新しいフォントデータが自動解決されるわけではありません。  
そのため、動的なフォント切り替えが必要なら設計を一段下げて扱う必要があります。

## 表示を消したいとき

Text を空文字列にしてから RegenerateMesh() または RequestRegenerateMesh() を呼ぶと、現在の表示をクリアできます。

```csharp
text3D.Text = string.Empty;
text3D.RegenerateMesh();
```

このとき、SingleObject の本体メッシュだけでなく、outline 子オブジェクトや PerCharacter の子オブジェクトも片付けられます。
