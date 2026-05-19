# Solid Text 3D

Solid Text 3D は、TTF / OTF フォントのグリフから 3D ポリゴンメッシュを生成する Unity 向けパッケージです。  
Unity の GameObject として扱える 3D テキストを生成し、日本語を含む CJK テキスト、縦書き、PerCharacter 出力、独立アウトライン、同期 / deferred の明示再生成 API を提供します。

このリポジトリには、Unity プロジェクト、UPM パッケージ本体、サンプル、GitHub 向けドキュメントが含まれています。

## 特徴

- TTF / OTF から 3D テキストメッシュを生成
- 日本語・中国語・韓国語を含む CJK テキストに対応
- Edit Mode / Play Mode の両方で明示再生成に対応
- 横書き / 縦書きに対応
- 全角 1em / 半角 0.5em の等幅表示モード
- SingleObject / PerCharacter を切り替え可能
- 文字本体と独立したアウトラインメッシュを生成
- Unity 6 系で利用可能

## 主な用途

- ワールド空間に表示する立体タイトル
- ネームプレートやラベル
- スコア、残り時間、状態表示などの動的テキスト
- タイマーや時計のような桁揃えが必要な表示
- 文字単位アニメーションを使う演出

## リポジトリ構成

- [Packages/com.masachuang.solidtext3d](Packages/com.masachuang.solidtext3d): UPM パッケージ本体
- [Assets/Samples](Assets/Samples): Unity に取り込んだサンプル
- [documents](documents): GitHub 用のユーザー向けドキュメント

## 導入方法

### GitHub Release / Git URL から導入する

Unity Package Manager で Git URL を指定して導入できます。

1. Unity で Package Manager を開きます。
2. 「+」 から Add package from git URL... を選びます。
3. 次の URL を入力します。

```text
https://github.com/masya-7s3s/SolidText3D.git?path=/Packages/com.masachuang.solidtext3d
```

過去のリリース版を指定したい場合だけ、URL の末尾に `#vX.Y.Z` を付けてください。

### manifest.json に直接追加する

```json
{
  "dependencies": {
    "com.masachuang.solidtext3d": "https://github.com/masya-7s3s/SolidText3D.git?path=/Packages/com.masachuang.solidtext3d"
  }
}
```

過去のリリース版を固定したい場合だけ、URL の末尾に `#vX.Y.Z` を付けてください。

### ローカルパッケージとして導入する

1. このリポジトリを取得します。
2. Unity で Package Manager を開きます。
3. 「+」 から Add package from disk... を選びます。
4. [Packages/com.masachuang.solidtext3d/package.json](Packages/com.masachuang.solidtext3d/package.json) を指定します。

### ローカル参照を manifest.json に直接追加する

```json
{
  "dependencies": {
    "com.masachuang.solidtext3d": "file:../Packages/com.masachuang.solidtext3d"
  }
}
```

## リリース運用

- [Packages/com.masachuang.solidtext3d/package.json](Packages/com.masachuang.solidtext3d/package.json) の `version` を更新します
- 同じ版の Git tag を `vX.Y.Z` 形式で作成して push します
- GitHub Actions の release workflow が tag と package version の一致を検証し、Release とパッケージ zip を自動生成します

## 最短の使い方

1. GameObject に SolidText3DComponent を追加します。
2. Text を設定します。
3. Font Asset を設定します。未設定の場合はデフォルトフォントが使われます。
4. Extrusion Depth と Font Size を調整します。
5. 必要なら Outline を有効にして Offset Amount / Thickness Ratio / Display Mode を調整します。
6. Regenerate Mesh を実行するか、スクリプトから RegenerateMesh() を呼びます。

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
        text3D.ExtrusionDepth = 0.25f;
        text3D.OutlineEnabled = true;
        text3D.OutlineOffset = 0.05f;
        text3D.OutlineThickness = 0.1f;
        text3D.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
        text3D.OutlineMaterial = outlineMaterial;
        text3D.RegenerateMesh();
    }
}
```

## 実装準拠の要点

- 設定変更だけでは表示は更新されません
- 即時反映したいときは RegenerateMesh() を使います
- 高頻度更新では RequestRegenerateMesh() を使います
- HasPendingRegeneration で deferred path の収束状態を確認できます
- DeferredRegenerationFailed は keep-last-good のまま失敗を通知します
- MonospaceMode を有効にすると、全角は 1em、半角は 0.5em の固定セルで配置され、LetterSpacing は共通の追加間隔として扱われます
- 横書きの Max Width は現行実装では自動折り返しに使われません
- 縦書きの Max Height は列折り返しに使われます
- ビルド済みプレイヤーで FontAsset を差し替えても、その場で新しいフォントデータを自動解決するわけではありません

## サンプル

Package Manager から次の Samples を Import できます。

- Basic Usage
- CJK Example

## ドキュメント

GitHub 上で読むユーザー向けドキュメントは [documents](documents) 配下にあります。

- [documents/README.md](documents/README.md)
- [documents/tutorials/first-3d-text.md](documents/tutorials/first-3d-text.md)
- [documents/how-to/import-fonts.md](documents/how-to/import-fonts.md)
- [documents/how-to/update-from-script.md](documents/how-to/update-from-script.md)
- [documents/how-to/use-outline.md](documents/how-to/use-outline.md)
- [documents/how-to/adjust-layout.md](documents/how-to/adjust-layout.md)
- [documents/reference/solid-text-3d-component.md](documents/reference/solid-text-3d-component.md)
- [documents/explanation/runtime-behavior.md](documents/explanation/runtime-behavior.md)
- [documents/explanation/layout-and-objects.md](documents/explanation/layout-and-objects.md)

UPM パッケージの概要は [Packages/com.masachuang.solidtext3d/README.md](Packages/com.masachuang.solidtext3d/README.md) にあります。  
Package Manager 同梱向けの詳細ガイドは [Packages/com.masachuang.solidtext3d/Documentation~/index.md](Packages/com.masachuang.solidtext3d/Documentation~/index.md) にあります。

## ライセンス

MIT License です。詳細は [LICENSE](LICENSE) を参照してください。  
サードパーティライセンスは [Packages/com.masachuang.solidtext3d/Third Party Notices.md](Packages/com.masachuang.solidtext3d/Third%20Party%20Notices.md) を参照してください。
