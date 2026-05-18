# レイアウト・アウトライン・文字単位オブジェクトの仕組み

このページでは、見た目の配置がどう決まるか、outline がどう管理されるか、PerCharacter がどう動くかを説明します。

## 横書きのレイアウト

横書きでは、グリフを行ごとにまとめたうえで、各行に対して HorizontalAnchor と LetterSpacing を適用します。  
そのあと、メッシュ全体の bounds を使って VerticalAnchor と DepthAnchor を反映します。

実装上のポイントは次の通りです。

- 行のまとまりは glyph bounds の Y 近接で判定する
- LetterSpacing は各行の X 方向へ加算される
- HorizontalAnchor は行単位で効く
- VerticalAnchor と DepthAnchor は全体 bounds を使って最終調整される

## 縦書きのレイアウト

縦書きでは、文字は上から下へ進み、列は右から左へ増えます。  
改行文字または MaxHeight 超過で次の列へ進みます。

実装上のポイントは次の通りです。

- LetterSpacing は上下方向の送り量に使う
- LineSpacing は列幅倍率として使う
- VerticalAnchor は列ごとに効く
- HorizontalAnchor と DepthAnchor は最終的な bounds から反映する

このため、横書きと縦書きではアンカーが効くタイミングが少し異なります。

## 縦書きの句読点と ASCII 回転

日本語の 「、」「。」 は、縦書き時に通常文字と同じ中央配置ではなく、セルの右上寄りへ寄せて配置されます。  
これは縦組みらしい見え方を保つための補正です。

一方、RotateAsciiInVertical が true のときは、印字可能 ASCII 文字を 90 度回転します。  
この回転処理と句読点補正は別です。

## MaxHeight は効くが、MaxWidth は使われない

縦書きでは MaxHeight による列折り返しが実装されています。  
高さを超えそうになった時点で次の列へ進みます。

一方で、横書きの MaxWidth は現在のレイアウト処理では参照されていません。  
そのため、横書きの改行は明示的な改行文字で制御する前提です。

## outline は別の子オブジェクト

outline は本体メッシュに混ぜ込まれず、__OutlineMesh__ という子オブジェクトで別管理されます。  
この設計には次の利点があります。

- 本体と別 Material を割り当てやすい
- outline のオンオフを分かりやすく制御できる
- outline だけ空メッシュにする挙動を扱いやすい

挙動は次のようになっています。

- OutlineEnabled = false で子オブジェクトを破棄する
- OutlineEnabled = true で子オブジェクトを生成または再利用する
- OutlineOffset = 0 では子オブジェクトを残し、メッシュだけ空にする
- OutlineThickness は本体の ExtrusionDepth に対する比率として扱う

## PerCharacter は再利用前提のプール方式

ObjectMode を PerCharacter にすると、可視文字ごとに子オブジェクトを持ちます。  
毎回全部作り直すのではなく、既存オブジェクトをできるだけ再利用します。

基本動作は次の通りです。

- 文字数が増えたときだけ不足分を新規作成する
- 余った子オブジェクトは削除せず非アクティブ化する
- SingleObject に戻したときや空文字列ではまとめて破棄する

これにより、文字単位アニメーションはやりやすくなりますが、Object 数が増えるぶん SingleObject より軽いとは限りません。

## SingleObject と PerCharacter の位置をそろえる仕組み

PerCharacter でも SingleObject と大きく位置がずれないよう、内部では一度全体メッシュ相当の bounds からアンカーオフセットを計算し、それを各文字へ反映しています。  
さらに、同じ文字が同じ位置にある場合は prepared result の再利用も行われます。
