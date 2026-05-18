# はじめて 3D テキストを表示する

このチュートリアルでは、シーンに Solid Text 3D を追加し、文字を立体表示するところまでを行います。

## ゴール

- Scene 上に 3D テキストを表示できる
- Text、Font、厚みを調整できる
- 必要なら outline を追加できる

## 1. パッケージを追加する

ローカルパッケージとして使う場合は、Unity Package Manager で次の手順を行います。

1. Window > Package Manager を開きます。
2. 左上の + を押します。
3. Add package from disk... を選びます。
4. Packages/com.masachuang.solidtext3d/package.json を指定します。

サンプルも確認したい場合は、Package Manager の Samples から取り込めます。

- Basic Usage
- CJK Example

## 2. GameObject を作る

1. Hierarchy で空の GameObject を作ります。
2. Inspector で SolidText3DComponent を追加します。

このコンポーネントは MeshFilter と MeshRenderer を使うワールド空間向け 3D オブジェクトです。  
UI 用の RectTransform ベースではありません。  
必要な MeshFilter / MeshRenderer が欠けていても Awake 時に自動で補われます。

## 3. 文字とフォントを設定する

Inspector の Text & Font と Geometry で次を設定します。

- Text
- Font Asset
- Extrusion Depth
- Font Size

Font Asset を空のままでも、パッケージ同梱の NotoSansJP-Black が使われます。  
まずは次のようなテキストで動作を確認すると分かりやすいです。

```text
Hello, World!
立体文字
Hello 世界
```

## 4. Material を設定する

見た目を整えたい場合は、MeshRenderer の Material を設定してください。  
何も設定されていない場合、実装は URP/Lit または Standard のデフォルト材質を自動で探して設定しますが、最終的な質感や色は自分で割り当てた方が分かりやすいです。

## 5. メッシュを生成する

Text や Font Size を設定しただけでは、まだ表示は更新されません。  
Mesh Update セクションの Regenerate Mesh を押して反映します。  
これは Edit Mode でも Play Mode でも同じです。

これが Solid Text 3D の基本です。

1. 設定を変える
2. dirty 状態になる
3. Regenerate Mesh で表示へ反映する

## 6. outline を付ける

必要なら Outline セクションで次を設定します。

- Enabled を On
- Offset Amount を 0.03 から 0.08 程度
- Thickness を 0.05 から 0.15 程度
- Display Mode を Donut または BackFilled
- Material は必要なら専用のものを設定

Thickness は本体の Extrusion Depth を 1 とした相対値です。  
例えば Extrusion Depth が 0.25 で Thickness が 0.1 のとき、outline の厚みは 0.025 相当になります。

最初は次の組み合わせから始めると調整しやすいです。

- Offset Amount = 0.05
- Thickness = 0.1
- Display Mode = Donut

変更後はもう一度 Regenerate Mesh を押します。

## 7. スクリプトから更新する

テキストはスクリプトから変更できます。  
表示を確実に更新したいときは RegenerateMesh() を呼びます。

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
        text3D.RegenerateMesh();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        score += 100;
        text3D.Text = $"Score: {score}";
        text3D.RegenerateMesh();
    }
}
```

Text を代入しただけでは dirty になるだけで、見た目は変わりません。  
高頻度更新で使う RequestRegenerateMesh() については [スクリプトから更新する](../how-to/update-from-script.md) で詳しく説明します。

## 次に読むもの

- フォントを差し替えたい: [独自フォントを使う](../how-to/import-fonts.md)
- スクリプト更新を使い分けたい: [スクリプトから更新する](../how-to/update-from-script.md)
- 縦書きやアンカーを調整したい: [縦書き・アンカー・文字配置を調整する](../how-to/adjust-layout.md)
