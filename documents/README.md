# Solid Text 3D ドキュメント

このドキュメントは、Unityでゲームを開発するユーザー向けに Solid Text 3D の使い方をまとめたものです。  
対象は、シーンに3Dテキストを配置したい人、スクリプトから表示を更新したい人、必要に応じて挙動を理解してカスタマイズしたい人です。

内容は、現在の実装コードを基準に整理しています。Spec Kit 配下の設計文書ではなく、実際のコンポーネント、エディタ拡張、サンプル、テストで確認できる挙動を優先しています。

## このパッケージでできること

- TTF / OTF フォントから3Dメッシュ文字を生成する
- 日本語・中国語・韓国語を含むCJKテキストを表示する
- Play Mode中にテキストを更新する
- 文字本体とは別にアウトラインメッシュを生成する
- 横書きと縦書きを切り替える
- 文字ごとに別GameObjectへ分けて扱う

## 想定環境

- Unity 6系
- パッケージバージョン 2.1.0
- ワールド空間に配置する MeshFilter + MeshRenderer ベースの3Dテキスト

## ドキュメントの読み方

Diátaxis形式で整理しています。

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

## どこから始めるべきか

- まず使いたい: [はじめて3Dテキストを表示する](tutorials/first-3d-text.md)
- フォントを差し替えたい: [独自フォントを使う](how-to/import-fonts.md)
- UIやゲーム中の数値を更新したい: [スクリプトから更新する](how-to/update-from-script.md)
- 配置や見た目を追い込みたい: [アウトラインを設定する](how-to/use-outline.md)、[縦書き・アンカー・文字配置を調整する](how-to/adjust-layout.md)
- 仕組みを理解して応用したい: [更新タイミングと内部動作](explanation/runtime-behavior.md)、[レイアウト・アウトライン・文字単位オブジェクトの仕組み](explanation/layout-and-objects.md)
