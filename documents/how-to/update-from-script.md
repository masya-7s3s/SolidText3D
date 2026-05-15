# スクリプトから更新する

このガイドでは、Play Mode中にテキストを変更したり、手動で反映したり、文字ごとに扱う方法を説明します。

## もっとも基本的な更新

Text を変更すると、コンポーネントは dirty 状態になります。

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class TimerLabel : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent text3D;

    private void Update()
    {
        text3D.Text = Time.time.ToString("F1");
    }
}
```

見た目へ反映したいタイミングで RegenerateMesh を呼びます。

## 変更を反映する

値を変えた直後に見た目を更新したい場合は、RegenerateMesh を呼びます。

```csharp
text3D.Text = "GO!";
text3D.ExtrusionDepth = 0.2f;
text3D.RegenerateMesh();
```

プロパティ変更のたびに毎フレーム何度も呼ぶと重くなりやすいので、必要な変更をまとめてから呼ぶのが安全です。

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
- ObjectMode
- MaxWidth
- MaxHeight
- RotateAsciiInVertical

これらを変更すると、コンポーネントは内部で再生成が必要な状態になります。

## 文字ごとに別オブジェクトへ分ける

ObjectMode を PerCharacter にすると、可視文字ごとに子GameObjectが作られます。

```csharp
text3D.ObjectMode = ObjectMode.PerCharacter;
text3D.RegenerateMesh();
```

このモードは次の用途に向いています。

- 文字ごとにアニメーションを付ける
- 文字ごとに別マテリアルを当てる
- 文字単位で位置や回転を制御する

実装上のポイントは次の通りです。

- 子オブジェクト名は Char_0, Char_1, Char_2... の形式
- 本体側の MeshRenderer は無効化される
- スペースや改行のような非表示文字は子オブジェクトにならない
- SingleObject に戻すと子オブジェクトは破棄される

## 毎フレーム更新するときの考え方

Solid Text 3D は、変更がないフレームでは何もしないように設計されています。  
ただし、Text を毎フレーム書き換えて毎回 RegenerateMesh を呼べば、そのたびに再生成されます。

ゲーム内のスコアやタイマーのように常時更新する用途でも使えますが、次の点を意識すると扱いやすくなります。

- 本当に表示が変わったときだけ Text を更新する
- 変更をまとめてから RegenerateMesh を 1 回だけ呼ぶ
- 文字数が多い長文を毎フレーム再生成しない
- PerCharacter は SingleObject よりコストが増えやすいので、必要な場所だけ使う

## 高度なカスタマイズが必要な場合

SolidText3DComponent ではなく、GlyphMeshBuilder.Build と MeshGenerationParams を使って自分で Mesh を作ることもできます。  
これは、独自の生成タイミングを持たせたい場合や、byte[] ベースでフォントを扱いたい場合に向いています。

通常のゲームUIやワールドラベルであれば、まずは SolidText3DComponent を使う方が簡単です。
