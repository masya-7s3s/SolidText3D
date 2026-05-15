# Solid Text 3D

Solid Text 3D は、TTF / OTF フォントのグリフから 3D ポリゴンメッシュを生成する Unity 向けパッケージです。  
Unity の GameObject として扱える 3D テキストを生成し、日本語を含む CJK テキスト、ランタイム更新、縦書き、Per-Character 配置、独立アウトラインに対応します。

このリポジトリには、Unity プロジェクト、UPM パッケージ本体、サンプル、ユーザー向けドキュメントが含まれています。

## 特徴

- TTF / OTF から 3D テキストメッシュを生成
- 日本語・中国語・韓国語を含む CJK テキストに対応
- Play Mode 中のテキスト更新に対応
- 横書き / 縦書きに対応
- 文字単位の GameObject 分割に対応
- 文字本体と独立したアウトラインメッシュを生成
- Unity 6 系で利用可能

## 主な用途

- ワールド空間に表示する立体タイトル
- ネームプレートやラベル
- スコア、残り時間、状態表示などの動的テキスト
- 文字単位アニメーションを使う演出

## リポジトリ構成

- Packages/com.masachuang.solidtext3d: UPM パッケージ本体
- Assets/Samples: Unity に取り込んだサンプル
- documents: GitHub 用のユーザー向けドキュメント

## 導入方法

### ローカルパッケージとして導入する

1. このリポジトリを取得します。
2. Unity で Package Manager を開きます。
3. + から Add package from disk... を選びます。
4. Packages/com.masachuang.solidtext3d/package.json を指定します。

### manifest.json に直接追加する

```json
{
  "dependencies": {
    "com.masachuang.solidtext3d": "file:../Packages/com.masachuang.solidtext3d"
  }
}
```

## 最短の使い方

1. GameObject に SolidText3DComponent を追加します。
2. Text を設定します。
3. Font Asset を設定します。未設定の場合はデフォルトフォントが使われます。
4. Extrusion Depth で厚みを調整します。
5. 必要なら Outline を有効にして Offset Amount / Thickness / Display Mode を調整します。

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class SolidText3DSample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent text3D;
    [SerializeField] private Material outlineMaterial;

    private void Start()
    {
        text3D.Text = "Solid Text 3D";
        text3D.OutlineEnabled = true;
        text3D.OutlineOffset = 0.05f;
        text3D.OutlineThickness = 0.1f;
        text3D.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
        text3D.OutlineMaterial = outlineMaterial;
    }
}
```

## サンプル

Package Manager から次の Samples を Import できます。

- Basic Usage
- CJK Example

## ドキュメント

GitHub 上で読むユーザー向けドキュメントは documents 配下にあります。

- documents/README.md
- documents/tutorials/first-3d-text.md
- documents/how-to/import-fonts.md
- documents/how-to/update-from-script.md
- documents/how-to/use-outline.md
- documents/how-to/adjust-layout.md
- documents/reference/solid-text-3d-component.md
- documents/explanation/runtime-behavior.md
- documents/explanation/layout-and-objects.md

パッケージ内 README は UPM パッケージの概要用です。

- Packages/com.masachuang.solidtext3d/README.md

## 既知の注意点

- 現行実装では、縦書きの Max Height は列折り返しに使われます
- 一方で、横書きの Max Width は現時点では自動折り返しに使われません
- 複雑なグリフや長文を高頻度で更新すると再生成コストが増えます

## ライセンス

MIT License です。詳細は LICENSE を参照してください。  
サードパーティライセンスは Packages/com.masachuang.solidtext3d/Third Party Notices.md を参照してください。
