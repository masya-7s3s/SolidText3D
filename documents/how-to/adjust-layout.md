# 縦書き・アンカー・文字配置を調整する

このガイドでは、文字列の基準位置や並び方を調整する方法を説明します。

## アンカーで原点を決める

SolidText3DComponent には 3 種類のアンカーがあります。

- Horizontal Anchor: Left / Center / Right
- Vertical Anchor: Upper / Middle / Lower
- Depth Anchor: Front / Center / Back

例えば、オブジェクトの原点を文字列の中央に合わせたいなら次のように設定します。

```csharp
text3D.HorizontalAnchor = HorizontalAnchor.Center;
text3D.VerticalAnchor = VerticalAnchor.Middle;
text3D.DepthAnchor = DepthAnchor.Center;
text3D.RegenerateMesh();
```

タイトルやネームプレートのように、Transform を基準に揃えたい場面で有効です。

## 文字間と行間を調整する

- Letter Spacing: 追加の文字間隔
- Line Spacing: 横書きでは行間倍率、縦書きでは列幅倍率
- Font Size: 文字全体の基準サイズ

横書きでは Letter Spacing が文字送りに足されます。  
縦書きでは Letter Spacing が上下方向の送り、Line Spacing が列幅に効きます。

## 等幅表示モードを使う

桁揃えを優先したい場合は MonospaceMode を使います。

```csharp
text3D.MonospaceMode = true;
text3D.Text = "HP 128/256";
text3D.RegenerateMesh();
```

このモードでは、全角文字は 1em、半角文字は 0.5em の固定セルで配置されます。
Letter Spacing はセルの外側へ加算されるため、可変幅モードから切り替えても字間の印象が急に崩れにくくなっています。

横書きでは各行を固定セルで並べ直し、縦書きでも同じ半角 / 全角のセル幅判定を列内の配置に使います。
タイマー、時計、スコア、在庫数のように数字や ASCII 記号の並びを見やすくしたい表示に向いています。

## 縦書きにする

```csharp
text3D.WritingMode = WritingMode.Vertical;
text3D.Text = "東京都\n新宿区";
text3D.RegenerateMesh();
```

縦書きでは、文字は上から下へ並び、列は右から左へ増えていきます。  
改行文字を入れると、次の列へ進みます。

MaxHeight が 0 より大きい場合は、改行文字がなくても高さ超過の直前で次の列へ折り返します。

## 縦書きで ASCII を回転する

半角英数字や記号を縦組みに合わせて 90 度回転したい場合は RotateAsciiInVertical を使います。

```csharp
text3D.WritingMode = WritingMode.Vertical;
text3D.RotateAsciiInVertical = true;
text3D.Text = "HP 100";
text3D.RegenerateMesh();
```

対象は印字可能な ASCII 文字です。  
一方、日本語の句読点である 「、」「。」 は別処理で縦書きセルの右上寄りに配置されます。

## 縦書きの高さ制限を使う

縦書きでは MaxHeight を使うと、指定高さを超える前に次の列へ折り返します。

```csharp
text3D.WritingMode = WritingMode.Vertical;
text3D.MaxHeight = 5f;
text3D.RegenerateMesh();
```

長い縦組みテキストを、一定の高さで複数列に分けたいときに便利です。

## 横書きの幅制限について

Inspector には MaxWidth がありますが、現行実装では横書きの自動折り返しには使われていません。  
横書きで行を分けたい場合は、現在は文字列中に改行を入れて制御してください。

```csharp
text3D.Text = "Stage 1\nBoss Incoming";
text3D.RegenerateMesh();
```

## PerCharacter と組み合わせる

文字ごとに演出したい場合は ObjectMode を PerCharacter にします。

```csharp
text3D.ObjectMode = ObjectMode.PerCharacter;
text3D.WritingMode = WritingMode.Vertical;
text3D.RegenerateMesh();
```

これで縦書きの各文字を個別の子オブジェクトとして扱えます。  
SingleObject と PerCharacter で大きく位置がずれないよう、実装は全体 bounds ベースのアンカーオフセットをそろえるように作られています。
