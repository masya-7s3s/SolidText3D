# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.2.2] - 2026-05-19

### Added

- GitHub Actions で `vX.Y.Z` tag push 時に Release と UPM パッケージ zip を生成する workflow を追加
- UPM の git URL 導入と GitHub Release 運用手順を README に追加

### Changed

- `package.json` に repository / documentation / changelog / license の GitHub メタデータを追加

## [2.2.1] - 2026-05-18

- 不要なアセットを整理

## [2.2.0] - 2026-05-18

### Added

- `SolidText3DComponent.RequestRegenerateMesh()`、`HasPendingRegeneration`、`DeferredRegenerationFailed` を追加し、高頻度更新向けの deferred regeneration API を公開
- prepared-result cache の hit/miss、LRU eviction、cache-hit latency drift、keep-last-good failure、stale-result discard を検証する Edit Mode / Play Mode テストを追加

### Changed

- `SolidText3DComponent` の dirty 状態は自動再生成せず、`RegenerateMesh()` の明示呼び出しでのみメッシュ更新するよう変更
- `SolidText3DInspector` に `Regenerate Mesh` ボタンを追加し、Inspector 編集時のリアルタイム再生成を廃止
- deferred regeneration の submit path を component-local work item 化し、latest-only queue、stale discard、prepared-result reuse を強化
- パッケージ同梱ドキュメント（`README.md`、`Documentation~/index.md`、`Documentation~/BREAKING_CHANGES.md`、`Third Party Notices.md`）を、明示的な regeneration API、FontAsset の実動作、outline child のライフサイクル、レイアウト制約を含む現在実装準拠の内容へ更新
- パッケージドキュメント上のフォント案内を現行デフォルトの `NotoSansJP-Black` 基準にそろえ、`FontAsset` に `Font` を割り当てる通常運用と `.bytes` キャッシュの補助的な役割を明確化

## [2.1.0] - 2026-05-13

### Added

- `OutlineDisplayMode` (`Donut` / `BackFilled`) と `OutlineSettings` を追加し、outline の表示モード・厚さ・マテリアルを公開 API から制御可能にした
- `OutlineContourBuilder` と `OutlineMeshBuilder` を追加し、canonical ring profile を共有押し出しコアへ渡す outline 再設計を実装した
- outline child GameObject の再利用、`OutlineOffset = 0` 時の no-geometry、`OutlineMaterial = null` 時の本体マテリアル fallback を追加した
- outline の profile / mesh / child lifecycle を検証する Edit Mode / Play Mode テストを追加した

### Changed

- outline front face 生成を outline 専用経路から `MeshExtruder` 共有コアへ統一し、front cap 可視性と winding parity の回帰を防止した
- `SolidText3DInspector` に Outline セクションを追加し、Enabled / Offset Amount / Thickness / Display Mode / Material を編集可能にした
- outline 関連の hot path に profiler sample を追加し、clean frame の追加 GC.Alloc が発生しない回帰検証を補強した

## [2.0.0] - 2026-04-21

### Breaking Changes

- `MeshGenerationParams.FontPath` プロパティを削除。`FontData` (byte[]) を使用してください（BREAKING_CHANGES.md 参照）。
- `SolidText3DComponent.Font` (string) プロパティを削除。`FontAsset` (UnityEngine.Object) を使用してください。

### Added

- **US1**: Inspector でのフォントアセット直接参照（`FontAsset` ObjectField）。TTF/OTF ファイルドラッグ＆ドロップ対応（FR-001）
- **US2**: `.ttf`/`.otf` インポート時に自動で `.bytes` キャッシュを生成する `FontAssetPostprocessor`（FR-002）
- **US3**: `HorizontalAnchor` / `VerticalAnchor` / `DepthAnchor` 3軸アンカー制御（FR-003/004/005）
- **US4**: テキスト入力のデバウンス処理（フォーカスアウトで再生成、Inspector 編集中は抑制）（FR-006）
- **US5**: 同一パラメータ時の再生成スキップ（`ComputeParamHash` ＋ `_lastParamHash` 比較）（FR-007）
- **US6**: Per-Character モード（`ObjectMode.PerCharacter`）— 文字ごとに独立した子 GameObject を生成（FR-009）
- **US7**: 縦書きモード（`WritingMode.Vertical`）— 列折り返し、`MaxHeight`、ASCII 文字回転対応（FR-010/011）
- `LayoutEngine` 静的クラス: 横書き/縦書きレイアウト計算とアンカーオフセット算出
- `CharacterObjectPool`: Per-Character モード用 GameObject プール（Destroy なし、SetActive 制御）
- `TextAnchorEnums.cs`: `HorizontalAnchor`, `VerticalAnchor`, `DepthAnchor`, `WritingMode`, `ObjectMode` enum
- `SolidText3DComponent.SuppressAutoRegenerate` プロパティ: デバウンス制御用フラグ
- `SolidText3DComponent.IsDirty` / `RegenerateMesh()`: 外部からの再生成トリガー API

### Changed

- `MeshGenerationParams` に 8 フィールド追加: `HorizontalAnchor`, `VerticalAnchor`, `DepthAnchor`, `WritingMode`, `MaxWidth`, `MaxHeight`, `VerticalColumnWidth`, `RotateAsciiInVertical`
- `GlyphContour` に `AdvanceHeight`, `CharIndex`, `IsVisible` フィールドを追加
- `GlyphMeshBuilder.Build()` にアンカーオフセット適用を統合

## [1.0.0] - 2026-04-17

### Added

- TTF/OTF フォントのグリフから 3D ポリゴンメッシュを生成する `SolidText3DComponent` MonoBehaviour
- `GlyphMeshBuilder` 静的クラス: フォントとテキストパラメータから Unity `Mesh` を生成
- `MeshExtruder` 静的クラス: グリフ輪郭の前面三角分割・背面複製・側面クワッド生成
- `GlyphContourBuilder`: SixLabors.Fonts `IGlyphRenderer` 実装によるグリフ輪郭収集
- `BezierSubdivider`: 適応 De Casteljau 法による二次・三次ベジェ曲線の離散化
- CJK（日本語・中国語・韓国語）文字の完全サポート
- LibTessDotNet `EvenOdd WindingRule` による穴あきグリフ（「口」「O」等）の正確な三角分割
- `LetterSpacing` プロパティ: 文字間スペースの調整（FR-013）
- `LineSpacing` プロパティ: 行間係数の調整（FR-014）
- デフォルト埋め込みフォント: Noto Sans JP Regular（SIL OFL 1.1）
- エディタ Inspector でのリアルタイムプレビュー（`SolidText3DInspector` カスタムエディタ）
- ダーティフラグ + `LateUpdate` パターンによる効率的なメッシュ再生成
- Unity Profiler マーカー（`GlyphMeshBuilder.Build` / `MeshExtruder.BuildGlyphMesh`）
- Edit Mode テスト一式: BezierSubdivider / GlyphContourBuilder / MeshExtruder / GlyphMeshBuilder / SolidText3DComponent
- Play Mode テスト一式: ランタイム動的テキスト変更 / フォント切り替え / パフォーマンスベンチマーク
- サンプルシーン: BasicUsage / CJKExample
- API ドキュメント（`Documentation~/index.md`）
- Third Party Notices（SixLabors.Fonts / LibTessDotNet / Noto Sans JP）
