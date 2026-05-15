# はじめて3Dテキストを表示する

このチュートリアルでは、シーンに Solid Text 3D を追加し、文字を立体表示するところまでを行います。

## ゴール

- GameObjectに3Dテキストを表示できる
- テキスト内容と厚みを調整できる
- 必要ならアウトラインも追加できる

## 1. パッケージをプロジェクトに入れる

ローカルパッケージとして使う場合は、Unity Package Manager で次の手順を行います。

1. Window > Package Manager を開きます。
2. 左上の + を押します。
3. Add package from disk... を選びます。
4. Packages/com.masachuang.solidtext3d/package.json を指定します。

サンプルも確認したい場合は、Package Manager の Samples から次を Import できます。

- Basic Usage
- CJK Example

## 2. 3Dテキスト用のGameObjectを作る

1. Hierarchy で空のGameObjectを作ります。
2. Inspector で SolidText3DComponent を追加します。

このコンポーネントは自動で MeshFilter と MeshRenderer を利用します。UI用の RectTransform ではなく、ワールド空間に置く3Dオブジェクトです。

## 3. テキストとフォントを設定する

Inspector で次を設定します。

- Font Asset
- Text
- Extrusion Depth
- Font Size

最初に何もフォントを設定しなくても、パッケージに含まれる NotoSansJP-Black が使われます。  
そのままでも日本語を含むテキストを試せます。

試しに以下のような文字列を入れてみてください。

```text
Hello, World!
立体文字
Hello 世界
```

## 4. 見た目を整える

まずは次の2つを調整すると、見た目の変化が分かりやすいです。

- Extrusion Depth: 文字の厚み
- Font Size: 文字全体の大きさ

必要なら MeshRenderer の Material も設定してください。  
URP環境では、適切なマテリアルがない場合に URP/Lit か Standard へ自動で合わせようとしますが、最終的な見た目は自分のマテリアルを割り当てた方が分かりやすいです。

## 5. アウトラインを付ける

Inspector の Outline セクションで次を設定します。

- Enabled: オン
- Offset Amount: 0.03 から 0.08 くらい
- Thickness: 0.05 から 0.15 くらい
- Display Mode: Donut または BackFilled
- Material: 必要なら専用マテリアル

最初は次の組み合わせが試しやすい設定です。

- Offset Amount = 0.05
- Thickness = 0.1
- Display Mode = Donut

## 6. スクリプトから文字を変える

テキストはスクリプトから変更できます。  
次の例では、Play Mode中にスペースキーを押すたびに表示を更新します。

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class ScoreLabel : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent text3D;
    private int score;

    private void Start()
    {
        text3D.Text = "Score: 0";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            score += 100;
            text3D.Text = $"Score: {score}";
        }
    }
}
```

通常は Text プロパティを変更するだけで、次のフレームで自動更新されます。

## 次に読むもの

- 独自フォントを使いたい: [独自フォントを使う](../how-to/import-fonts.md)
- スクリプトから即時反映や文字単位制御をしたい: [スクリプトから更新する](../how-to/update-from-script.md)
- 縦書きやアンカー配置を使いたい: [縦書き・アンカー・文字配置を調整する](../how-to/adjust-layout.md)
