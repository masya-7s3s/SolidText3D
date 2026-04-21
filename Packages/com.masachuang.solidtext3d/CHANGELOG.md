# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
