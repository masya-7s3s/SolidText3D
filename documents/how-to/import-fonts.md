# 独自フォントを使う

このガイドでは、独自の TTF / OTF フォントを Solid Text 3D で使う方法を説明します。

## Editor で使う手順

1. TTF または OTF ファイルを Assets 配下へ置きます。
2. Unity のインポート完了を待ちます。
3. SolidText3DComponent の Font Asset に、そのフォントを割り当てます。
4. Regenerate Mesh を押して表示を更新します。

Inspector の Font Asset には Font を割り当てます。  
実装では、.ttf / .otf のインポート時に .bytes キャッシュも自動生成します。

## 自動で行われること

フォントを Assets に入れると、Editor 側で次の処理が行われます。

- フォントファイルを .bytes に変換する
- Assets/SolidText3DFonts/{GUID}.bytes を生成する
- 同じフォントを参照している SolidText3DComponent の内部キャッシュを更新する

通常は、自分で .bytes ファイルを作る必要はありません。

## 実際のフォント解決順

現在の実装での優先順位は次の通りです。

1. Editor なら Font Asset から直接読めたフォントデータ
2. 内部で保持している .bytes キャッシュ
3. パッケージ内のデフォルトフォント NotoSansJP-Black

Font Asset を設定していなくても、デフォルトフォントが見つかれば表示できます。

## スクリプトから差し替える

Editor 上では、FontAsset を差し替えると dirty 状態になります。  
表示に反映するときは RegenerateMesh() を呼びます。

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

ビルド後のプレイヤーでは、FontAsset に新しい Font を代入しただけでは、その場で .ttf / .otf を再読込しません。  
実装上、ランタイム側は FontAsset setter で .bytes キャッシュを解決しないためです。

そのため、ビルド済みプレイヤーで動的にフォントを切り替えたい場合は、次のどちらかで考えるのが安全です。

- 事前に Editor で設定した Font Asset を使い分ける
- より低レベルな API で byte 配列を扱う

通常のラベル表示なら、まずは Editor でフォントを設定して使う運用で十分です。

## うまく表示されないとき

### フォントを設定したのに見た目が変わらない

- Regenerate Mesh を押すか、スクリプトから RegenerateMesh() を呼びます
- Font Asset を設定し直します
- Assets/SolidText3DFonts に .bytes が生成されているか確認します

### フォントが読めない警告が出る

現在の表示は keep-last-good で維持されます。  
新しいフォントへの切り替えに失敗しても、直前のメッシュが残ることがあります。

### とりあえず表示を戻したい

Font Asset を外すと、デフォルトフォント NotoSansJP-Black にフォールバックします。
