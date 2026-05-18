# Solid Text 3D ドキュメント

このドキュメントは、Solid Text 3D を Unity プロジェクトで使う人のための実装準拠ガイドです。  
設計メモではなく、現在のコンポーネント、Inspector、Editor 拡張、テストで確認できる挙動を正としてまとめています。

## このパッケージでできること

- TTF / OTF フォントから 3D テキストメッシュを生成する
- 日本語を含む CJK テキストを表示する
- 横書きと縦書きを切り替える
- outline を文字本体とは別メッシュで生成する
- 文字ごとに子 GameObject へ分けて出力する
- Edit Mode / Play Mode の両方で同期または deferred に再生成する

## 最初に知っておくとよいこと

- プロパティを変更しただけでは表示は更新されません
- 即時反映したいときは RegenerateMesh() を使います
- 高頻度更新では RequestRegenerateMesh() を使います
- Font Asset 未設定時はデフォルトの NotoSansJP-Black を使います
- 横書きの MaxWidth は現行実装では保持だけされ、自動折り返しには使われません
- ビルド済みプレイヤーで FontAsset を差し替えても、その場で新しいフォントデータを自動解決するわけではありません

## ドキュメント一覧

### Tutorials

- [はじめて3Dテキストを表示する](tutorials/first-3d-text.md)

### How-to Guides

- [独自フォントを使う](how-to/import-fonts.md)
- [スクリプトから更新する](how-to/update-from-script.md)
- [アウトラインを設定する](how-to/use-outline.md)
- [縦書き・アンカー・文字配置を調整する](how-to/adjust-layout.md)

### Reference

- [SolidText3DComponent リファレンス](reference/solid-text-3d-component.md)

### Explanation

- [更新タイミングと内部動作](explanation/runtime-behavior.md)
- [レイアウト・アウトライン・文字単位オブジェクトの仕組み](explanation/layout-and-objects.md)

## 読み始めの目安

- まず表示してみたい: [はじめて3Dテキストを表示する](tutorials/first-3d-text.md)
- フォントを差し替えたい: [独自フォントを使う](how-to/import-fonts.md)
- 数値やラベルをスクリプトから更新したい: [スクリプトから更新する](how-to/update-from-script.md)
- 見た目を詰めたい: [アウトラインを設定する](how-to/use-outline.md) と [縦書き・アンカー・文字配置を調整する](how-to/adjust-layout.md)
- 挙動の理由まで把握したい: [更新タイミングと内部動作](explanation/runtime-behavior.md) と [レイアウト・アウトライン・文字単位オブジェクトの仕組み](explanation/layout-and-objects.md)
