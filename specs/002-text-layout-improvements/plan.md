# Implementation Plan: Solid Text 3D — レイアウト・フォント・パフォーマンス改善

**Branch**: `002-text-layout-improvements` | **Date**: 2026-04-21 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/002-text-layout-improvements/spec.md`

## Summary

SolidText3D パッケージに 7 つの改善を加える：  
① 3 軸アンカー指定（垂直 / 水平 / 奥行き）、② 縦書きレイアウト対応、  
③ Per-Character 個別オブジェクトモード（オブジェクトプール）、  
④ フォント参照を文字列型 → Inspector の Object フィールドへ変更（破壊的変更・MAJOR バンプ）、  
⑤ .ttf/.otf → .bytes 自動変換（AssetPostprocessor）、  
⑥ Inspector 入力デバウンスによるレイテンシ解消、  
⑦ フレームごとの GC アロケーション削減（ダーティフラグ + 同一テキスト早期リターン）。

## Technical Context

**Language/Version**: C# (.NET Standard 2.1) / Unity 6 (6000.x LTS)  
**Primary Dependencies**: SixLabors.Fonts（フォント解析）、LibTessDotNet v1.1.15（三角分割）  
**Storage**: N/A（ランタイムはメモリ完結。Editor が .bytes キャッシュを AssetDatabase に書き込む）  
**Testing**: Unity Test Framework — Edit Mode テスト（純粋 C# ロジック）＋ Play Mode テスト（MonoBehaviour）  
**Target Platform**: Unity Editor（全 OS）＋ Unity ビルドターゲット全般（PC / Mobile / WebGL）  
**Project Type**: UPM パッケージ（Asset Store 配布）  
**Performance Goals**: ランタイム時フレームあたり GC Alloc = 0 バイト（テキスト未変更時）、毎フレーム更新シナリオで 60fps 維持  
**Constraints**: ランタイム asmdef は Editor Only アセンブリ参照禁止、.NET Standard 2.1 APIのみ使用  
**Scale/Scope**: 単一 UPM パッケージ、7 機能追加 / 変更、公開 API の MAJOR バンプ（1.x → 2.0.0）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

<!-- SolidText3D 憲法 v2.1.0 に基づくゲート -->

- [x] **I. UPM 構造**: 既存 `Editor/`・`Runtime/`・`Tests/` 構造を維持。新ファイルはすべて各ディレクトリに配置する設計。
- [x] **II. Editor/Runtime 分離**: `FontAssetPostprocessor` と Inspector デバウンスロジックは `Editor/` 配下のみ。ランタイムは `FontData (byte[])` 経由でフォントデータを受け取り、`UnityEditor` 名前空間不使用。
- [x] **III. テストファースト**: spec.md に 7 ユーザーストーリーそれぞれの Acceptance Scenarios を記載済み。Edit Mode テスト（アンカー計算・縦書きレイアウト・プール管理）および Play Mode テスト（入力デバウンス）の計画含む。
- [x] **IV. 後方互換性**: `public string Font` プロパティの削除は spec の Clarification で「即時破壊的変更」として明示的に承認済み。→ **MAJOR バンプ: 1.0.0 → 2.0.0** が必要。`[Obsolete]` ステップはユーザーの明示的指示により省略する。
- [x] **V. パフォーマンス**: `LateUpdate()` 内では `_isDirty` チェックのみ（GC Alloc ゼロ）。テキスト不変時の早期リターン追加。Per-Character プールは Destroy ではなく SetActive(false) で再利用。
- [x] **VI. Asset Store 準拠**: 新規サードパーティ依存なし。既存 SixLabors.Fonts・LibTessDotNet は `Third Party Notices.md` 記載済み。
- [x] **VII. シンプルさ**: アンカー / 書字方向 / モードは enum フィールドで直接制御。不必要なストラテジーパターン・ファクトリー等の抽象化なし。

**⚠️ MAJOR バンプ正当化**: 憲法 IV は原則として `[Obsolete]` 経由を要求するが、フォント参照フィールド（`string _font` → `UnityEngine.Object _fontAsset`）は型が根本的に異なるため `[Obsolete]` で段階移行が技術的に困難。かつ spec Clarification で開発者が破壊的変更を明示承認している。この違反を Complexity Tracking に記録する。

## Project Structure

### Documentation (this feature)

```text
specs/002-text-layout-improvements/
├── plan.md              # このファイル（/speckit.plan コマンド出力）
├── research.md          # Phase 0 出力
├── data-model.md        # Phase 1 出力
├── quickstart.md        # Phase 1 出力
├── contracts/           # Phase 1 出力
│   └── SolidText3DComponent-API.md
└── tasks.md             # Phase 2 出力（/speckit.tasks コマンド — このコマンドでは未作成）
```

### Source Code (repository root)

```text
Packages/com.masachuang.solidtext3d/
├── package.json                          ← version: 1.0.0 → 2.0.0 に更新
├── CHANGELOG.md                          ← 破壊的変更を記録
├── Runtime/
│   ├── SolidText3DComponent.cs           ← 修正: _font(string)削除・_fontAsset追加・アンカー・書字方向・モードフィールド追加
│   ├── MeshGenerationParams.cs           ← 修正: Anchor・WritingMode・WordWrap フィールド追加
│   ├── GlyphMeshBuilder.cs               ← 修正: アンカーオフセット・縦書きレイアウト呼び出し追加
│   ├── LayoutEngine.cs                   ← 新規: 横書き・縦書きレイアウト計算（ApplyLayout の置き換え）
│   ├── CharacterObjectPool.cs            ← 新規: Per-Character モード用オブジェクトプール
│   ├── GlyphContour.cs                   ← 修正: AdvanceHeight フィールド追加（縦書き用）
│   ├── GlyphContourBuilder.cs            ← 読み取り専用（変更なし想定）
│   ├── BezierSubdivider.cs               ← 変更なし
│   ├── MeshExtruder.cs                   ← 変更なし
│   ├── GlyphMeshData.cs                  ← 変更なし
│   └── Plugins/                          ← 変更なし
│       ├── SixLabors.Fonts.dll
│       └── LibTessDotNet.dll
├── Editor/
│   ├── SolidText3DInspector.cs           ← 修正: Object フィールド・デバウンス追加
│   └── FontAssetPostprocessor.cs         ← 新規: .ttf/.otf → .bytes 自動変換
└── Tests/
    ├── Editor/
    │   └── （既存テストファイル + 新規テスト追加）
    └── Runtime/
        └── （既存テストファイル + 新規テスト追加）
```

**Structure Decision**: 既存の単一パッケージ構造を継承。新ファイルはすべて既存の `Runtime/` または `Editor/` に配置する。テスト構造も既存 `Tests/` 配下に追加する形とする。

## Complexity Tracking

| 違反 | 理由 | より単純な代替案を却下した理由 |
| ---- | ---- | ----------------------------- |
| 憲法 IV: `[Obsolete]` ステップ省略（`string Font` 完全削除） | `string` → `UnityEngine.Object` は型が根本的に異なるため、`[Obsolete]` 付きの旧 `string Font` プロパティを残しても自動移行不可。コンパイルエラーで強制移行するほうがユーザーに明確。spec Clarification で開発者が「即時破壊的変更」を明示承認済み。 | `[Obsolete]` 付きの旧プロパティを 1 MINOR 維持する案→ 型変換のスタブコードが複雑になり、かつユーザーが旧 API で実行できてしまうため混乱を招く。 |
