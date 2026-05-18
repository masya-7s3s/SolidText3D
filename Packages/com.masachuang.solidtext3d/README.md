# Solid Text 3D

TTF / OTF フォントから 3D テキストメッシュを生成する Unity UPM パッケージです。  
横書き、縦書き、CJK テキスト、独立 outline、PerCharacter 出力、deferred regeneration を扱えます。

## 特徴

- Font Asset から 3D テキストを生成
- Font Asset 未設定時は NotoSansJP-Black を使用
- outline を Donut / BackFilled の 2 モードで生成
- SingleObject / PerCharacter を切り替え可能
- RegenerateMesh() と RequestRegenerateMesh() を用途別に使い分け可能
- Edit Mode / Play Mode の両方で明示再生成に対応

## インストール

### GitHub Release / Git URL から導入する

1. Unity Package Manager を開きます
2. 追加メニューから Add package from git URL... を選びます
3. 次の URL を入力します

```text
https://github.com/masya-7s3s/SolidText3D.git?path=/Packages/com.masachuang.solidtext3d#v2.2.3
```

`#v2.2.3` は使いたい Release tag に置き換えます。  
このリポジトリは Unity プロジェクト全体を含むため、`?path=/Packages/com.masachuang.solidtext3d` を付けます。

### ローカル package.json から導入する

1. Unity Package Manager を開きます
2. 追加メニューから Add package from disk... を選びます
3. Packages/com.masachuang.solidtext3d/package.json を指定します

manifest.json に直接記述する場合の例です。

```json
{
  "dependencies": {
    "com.masachuang.solidtext3d": "https://github.com/masya-7s3s/SolidText3D.git?path=/Packages/com.masachuang.solidtext3d#v2.2.3"
  }
}
```

ローカル参照で使う場合の例です。

```json
{
  "dependencies": {
    "com.masachuang.solidtext3d": "file:../Packages/com.masachuang.solidtext3d"
  }
}
```

## 最短の使い方

1. GameObject に SolidText3DComponent を追加します
2. Text & Font で Text と Font Asset を設定します
3. Geometry で Extrusion Depth と Font Size を調整します
4. 必要なら Layout / Outline / Output を調整します
5. Mesh Update の Regenerate Mesh を押すか、スクリプトから RegenerateMesh() を呼びます

設定変更だけでは表示は更新されません。  
表示に反映するには Regenerate Mesh か RequestRegenerateMesh() が必要です。

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class OutlineSample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent text3D;
    [SerializeField] private Material outlineMaterial;

    private void Start()
    {
        text3D.Text = "Solid Text 3D";
        text3D.OutlineEnabled = true;
        text3D.OutlineOffset = 0.05f;
        text3D.OutlineThickness = 0.1f; // body depth に対する 10%
        text3D.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
        text3D.OutlineMaterial = outlineMaterial;
        text3D.RegenerateMesh();
    }
}
```

## 更新 API の使い分け

- RegenerateMesh(): 呼び出した時点で同期的に表示へ反映します
- RequestRegenerateMesh(): 高頻度更新向けの deferred API です
- HasPendingRegeneration: deferred path の進行中状態を確認できます
- DeferredRegenerationFailed: deferred 失敗時に通知します。表示は keep-last-good を維持します

RequestRegenerateMesh() は latest-only で古い request を圧縮し、古い ready result で表示が巻き戻らないように実装されています。

## 実装準拠の注意点

- 横書きの MaxWidth は現行実装では自動折り返しに使われません
- 縦書きの MaxHeight は列折り返しに使われます
- outline は __OutlineMesh__ という子オブジェクトで別管理されます
- OutlineThickness は本体の Extrusion Depth を 1 とした相対値です
- OutlineOffset が 0 のときは outline 子オブジェクトを残したままメッシュだけ空になります
- PerCharacter では可視文字ごとに Char_0, Char_1... の子オブジェクトを生成し、余剰分は非アクティブ化で再利用します
- 空文字列を再生成すると本体・outline・PerCharacter 子オブジェクトをクリアします

## カスタムフォント

1. TTF / OTF を Assets 配下へ配置します
2. SolidText3DComponent の Font Asset に Font を割り当てます
3. 必要なら RegenerateMesh() で反映します

Editor では .ttf / .otf のインポート時に Assets/SolidText3DFonts 配下へ .bytes キャッシュを自動生成します。  
ビルド済みプレイヤーで FontAsset を差し替えても、その場で新しいフォントデータを自動解決するわけではありません。

## 詳細ドキュメント

- Documentation~/index.md: パッケージ同梱の詳細ガイド
- CHANGELOG.md: 変更履歴
- Third Party Notices.md: サードパーティライセンス

リポジトリ版の長文ドキュメントは documents 配下にあります。

## ライセンス

MIT License です。詳細は LICENSE.md を参照してください。
