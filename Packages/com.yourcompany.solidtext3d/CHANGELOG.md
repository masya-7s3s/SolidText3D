# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
