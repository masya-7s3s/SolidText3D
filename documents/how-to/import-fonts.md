# 独自フォントを使う

このガイドでは、独自の TTF / OTF フォントを Solid Text 3D で使う方法を説明します。

## Editorで使う手順

1. TTF または OTF ファイルを Assets 配下へ置きます。
2. Unity のインポート完了を待ちます。
3. SolidText3DComponent の Font Asset に、そのフォントを割り当てます。

Inspector の Font Asset では、フォントアセットを選択します。  
実装上は、インポートされた .ttf / .otf を検知して、Assets/SolidText3DFonts 配下に .bytes キャッシュが自動生成されます。

## 自動で行われること

フォントを Assets に入れると、エディタ側で次の処理が行われます。

- フォントファイルを .bytes に変換する
- Assets/SolidText3DFonts/{GUID}.bytes を生成する
- そのフォントを参照している SolidText3DComponent の内部キャッシュを更新する

そのため、通常は自分で .bytes を作る必要はありません。

## どのフォントが使われるか

優先順位は次の通りです。

1. 選択したフォントから読めたデータ
2. エディタで生成または保持されている .bytes キャッシュ
3. パッケージ内のデフォルトフォント NotoSansJP-Black

フォントが未設定でも、デフォルトフォントが見つかれば表示できます。

## スクリプトからフォントを差し替える

Editor上では、FontAsset を差し替えるだけで再生成できます。

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class SwapFontInEditor : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent text3D;
    [SerializeField] private Font fontAsset;

    public void ApplyFont()
    {
        text3D.FontAsset = fontAsset;
        text3D.RegenerateMesh();
    }
}
```

## ビルド済みプレイヤーでの注意点

ビルド後のプレイヤーでは、FontAsset に新しい Font を代入しただけでは、その場で .ttf / .otf を読み直す仕組みはありません。  
プレイヤー実行中にフォント自体を動的に切り替えたい場合は、SolidText3DComponent を使うより、byte[] を用意して GlyphMeshBuilder.Build に渡す方法の方が確実です。

これはゲーム実装で毎回必要になるものではなく、ランタイムカスタマイズを強く行いたい場合の上級者向け手段です。

## うまく表示されないとき

### フォントを設定したのに変わらない

- 一度シーンを保存して、コンポーネントが再生成されるか確認します。
- Font Asset に同じファイルを再指定してみます。
- Assets/SolidText3DFonts に .bytes が作られているか確認します。

### フォントが読めなかったという警告が出る

実装では、フォントデータが読めない場合に直前のメッシュを維持し、警告を出します。  
表示が消えるのではなく、前の状態が残ることがあります。

### とりあえず動く状態に戻したい

Font Asset を外すと、デフォルトフォント NotoSansJP-Black が使われます。
