# 縦書き・アンカー・文字配置を調整する

このガイドでは、文字の基準位置や並び方を調整する方法を説明します。

## 位置の基準を変える

SolidText3DComponent には3種類のアンカーがあります。

- Horizontal Anchor: Left / Center / Right
- Vertical Anchor: Upper / Middle / Lower
- Depth Anchor: Front / Center / Back

例えば、オブジェクトの原点を文字列の中央に合わせたいなら次のように設定します。

```csharp
text3D.HorizontalAnchor = HorizontalAnchor.Center;
text3D.VerticalAnchor = VerticalAnchor.Middle;
text3D.DepthAnchor = DepthAnchor.Center;
```

タイトルやネームプレートのように、原点基準で配置したい場面ではとても便利です。

## 文字間と行間を調整する

- Letter Spacing: 文字同士の間隔
- Line Spacing: 行間、または縦書き時の列幅の倍率
- Font Size: 文字全体の基準サイズ

横書きでは、Letter Spacing が文字送りに足されます。  
縦書きでは、Line Spacing が列間、Letter Spacing が文字送りに効きます。

## 縦書きにする

```csharp
text3D.WritingMode = WritingMode.Vertical;
```

縦書きでは、文字は上から下に並び、列は右から左へ増えていきます。  
改行文字を入れると次の列へ進みます。

```csharp
text3D.Text = "東京都\n新宿区";
text3D.WritingMode = WritingMode.Vertical;
```

## 縦書きでASCIIを回転する

半角英数字や記号を縦組みに合わせて90度回転したい場合は、Rotate ASCII in Vertical を使います。

```csharp
text3D.WritingMode = WritingMode.Vertical;
text3D.RotateAsciiInVertical = true;
text3D.Text = "HP 100";
```

対象は印字可能なASCII文字です。  
日本語の句読点は別処理で、縦書きセルの右上寄りへ配置されます。

## 縦書きの高さ制限を使う

縦書きでは Max Height を使うと、指定高さを超える前に次の列へ折り返します。

```csharp
text3D.WritingMode = WritingMode.Vertical;
text3D.MaxHeight = 5f;
```

長い縦組みテキストを、一定の高さで複数列にしたいときに使えます。

## 横書きの幅制限について

Inspector には Max Width がありますが、現行実装では横書きの自動折り返しには使われていません。  
横書きで行を分けたい場合は、現在は文字列中に改行を入れて制御してください。

```csharp
text3D.Text = "Stage 1\nBoss Incoming";
```

## PerCharacter と組み合わせる

文字ごとに演出したい場合は、Object Mode を PerCharacter にします。

```csharp
text3D.ObjectMode = ObjectMode.PerCharacter;
text3D.WritingMode = WritingMode.Vertical;
text3D.RegenerateMesh();
```

これで、縦書きの各文字を個別の子オブジェクトとして扱えます。
