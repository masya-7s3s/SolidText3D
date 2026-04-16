# 実装計画: Solid Text 3D

**ブランチ**: `001-solid-text-3d` | **日付**: 2026年4月17日 | **仕様**: [spec.md](spec.md)  
**入力**: `specs/001-solid-text-3d/spec.md`

## サマリー

Solid Text 3D は、TTF/OTF フォントのグリフ輪郭から 3D ポリゴンメッシュを生成し、Unity の `GameObject`（`MeshFilter` + `MeshRenderer`）として扱える MonoBehaviour コンポーネントを提供する UPM パッケージである。

**技術的アプローチ**: SixLabors.Fonts（純粋 C#、MIT）でグリフの Bezier 輪郭を抽出 → 適応分割で離散化 → LibTessDotNet で三角形分割 → 押し出し処理で 3D メッシュを生成。ダーティフラグ + `LateUpdate` パターンでエディタプレビュー・ランタイム更新の双方を効率的に処理する。

---

## 技術コンテキスト

**言語/バージョン**: C#（.NET Standard 2.1 / Unity 6 同梱ランタイム）  
**主要依存関係**:

- SixLabors.Fonts（MIT）— フォントパーサー・グリフ輪郭取得
- LibTessDotNet v1.1.15（SGI Free Software License B v2.0）— 三角形分割
- Unity Test Framework — テスト実行基盤

**ストレージ**: N/A（永続化なし。生成メッシュは `MeshFilter.sharedMesh` に保持）  
**テスト**: Unity Test Framework（Edit Mode + Play Mode）  
**対象プラットフォーム**: PC/Console（Windows / macOS / Linux）。モバイルは v1 スコープ外  
**プロジェクト種別**: Unity UPM パッケージ（Asset Store 配布）  
**パフォーマンス目標**:

- 50 文字以内: エディタでパラメータ変更から 2 秒以内でシーンビュー更新（SC-001）
- 同一シーン内に 20 個の SolidText3D（各 50 文字）が存在しても 60 fps 維持（SC-004）
- ランタイムでのテキスト変更が 1 フレーム遅延以内でメッシュ更新（SC-003）

**制約**:

- `LateUpdate()` 内での `new` / LINQ / 文字列連結禁止（憲法 V 準拠）
- ランタイムメッシュ生成はメインスレッドのみ（v1 スコープ）
- `UnityEditor` 名前空間は `#if UNITY_EDITOR` ブロック内のみ使用

---

## 憲法チェック

*GATE: Phase 0 リサーチ前に通過が必須。Phase 1 設計後に再チェック。*

### Phase 0 チェック（設計前）

- [x] **I. UPM 構造**: `Editor/`・`Runtime/`・`Tests/Editor/`・`Tests/Runtime/` の分離と各 asmdef 配置が設計に含まれている
- [x] **II. Editor/Runtime 分離**: ランタイム asmdef は Editor Only アセンブリを参照しない。`SolidText3DInspector` は `Editor/` に配置し `includePlatforms: ["Editor"]` で分離済み
- [x] **III. テストファースト**: Edit Mode テスト（`BezierSubdivider`, `GlyphMeshBuilder`, `MeshExtruder`）と Play Mode テスト（`SolidText3DComponent`）の計画が [data-model.md](data-model.md) に含まれている
- [x] **IV. 後方互換性**: v1.0.0 初回リリースのため破壊的変更なし。公開 API は [contracts/public-api.md](contracts/public-api.md) に定義済み
- [x] **V. パフォーマンス**: `LateUpdate` 内はダーティフラグチェックのみ。GC.Alloc はメッシュ生成時（変更時のみ）に限定。正当化: メッシュ生成は毎フレームではなく変更時のみ実行されるワンショット処理
- [x] **VI. Asset Store 準拠**: SixLabors.Fonts（MIT）・LibTessDotNet（SGI Free B v2）のライセンスを `Third Party Notices.md` に記載予定
- [x] **VII. シンプルさ**: 不要な抽象化なし。`GlyphMeshBuilder` は static クラス（DI 不要）。Rule of Three 適用

### Phase 1 チェック（設計後）

- [x] 全エンティティが `Runtime/` または `Editor/` に明確に配置されている
- [x] 公開 API の XML ドキュメントコメントが [contracts/public-api.md](contracts/public-api.md) に定義済み
- [x] `MeshGenerationParams` を struct として定義し、ヒープアロケーションを最小化
- [x] フォールバック処理（フォントなし・グリフ未収録）が全エンティティで明示されている

---

## プロジェクト構造

### ドキュメント（このフィーチャー）

```text
specs/001-solid-text-3d/
├── plan.md              ← 本ファイル（/speckit.plan 出力）
├── spec.md              ← 機能仕様書
├── research.md          ← Phase 0 出力（/speckit.plan 出力）
├── data-model.md        ← Phase 1 出力（/speckit.plan 出力）
├── quickstart.md        ← Phase 1 出力（/speckit.plan 出力）
├── contracts/
│   └── public-api.md    ← Phase 1 出力（/speckit.plan 出力）
└── tasks.md             ← Phase 2 出力（/speckit.tasks コマンド — 本 plan では未作成）
```

### ソースコード（リポジトリルート）

```text
Packages/
  com.yourcompany.solidtext3d/     ← UPM パッケージルート
    package.json                   ← マニフェスト（name, version, unity, author など）
    README.md
    CHANGELOG.md
    LICENSE.md
    Third Party Notices.md         ← SixLabors.Fonts, LibTessDotNet のライセンス記載
    │
    Runtime/                       ← ランタイムコード（非エディタビルドに含まれる）
    │   com.yourcompany.solidtext3d.Runtime.asmdef
    │   SolidText3DComponent.cs    ← MonoBehaviour メインコンポーネント（公開 API）
    │   MeshGenerationParams.cs    ← メッシュ生成パラメータ（struct）
    │   GlyphContour.cs            ← グリフ輪郭データ（class）
    │   GlyphContourBuilder.cs     ← IGlyphRenderer 実装（SixLabors.Fonts コールバック）
    │   BezierSubdivider.cs        ← Bezier 離散化（static utility）
    │   MeshExtruder.cs            ← 押し出し + 三角形分割（LibTessDotNet 使用）
    │   GlyphMeshBuilder.cs        ← 最上位メッシュ生成サービス（static）
    │   Plugins/                   ← 純粋 C# サードパーティ DLL
    │       SixLabors.Fonts.dll
    │       SixLabors.Fonts.dll.meta
    │       LibTessDotNet.dll
    │       LibTessDotNet.dll.meta
    │
    Editor/                        ← エディタ専用コード（ランタイムビルドに含まれない）
    │   com.yourcompany.solidtext3d.Editor.asmdef
    │   SolidText3DInspector.cs    ← カスタムインスペクター
    │
    Tests/
    │   Editor/
    │   │   com.yourcompany.solidtext3d.Tests.Editor.asmdef
    │   │   BezierSubdividerTests.cs
    │   │   MeshExtruderTests.cs
    │   │   GlyphMeshBuilderTests.cs
    │   └── Runtime/
    │       com.yourcompany.solidtext3d.Tests.Runtime.asmdef
    │       SolidText3DRuntimeTests.cs
    │
    Samples~/                      ← UPM サンプル（ユーザーが任意にインポート）
    │   BasicUsage/
    │   CJKExample/
    │
    Documentation~/
        index.md
```

**構造の決定理由**: Unity UPM パッケージ標準構造（憲法 I 準拠）。`Packages/` 配下にローカルパッケージとして配置することで、Unity エディタが直接認識し、開発中に Asset Store 同等の動作確認が可能。

---

## 実装フェーズ概要

> 詳細タスクは `/speckit.tasks` コマンドで生成される `tasks.md` を参照。

### フェーズ A: パッケージスケルトン構築

1. `Packages/com.yourcompany.solidtext3d/` ディレクトリ作成
2. `package.json` 作成（name, version, unity, description, author）
3. 各ディレクトリ（`Runtime/`, `Editor/`, `Tests/Editor/`, `Tests/Runtime/`, `Samples~/`）作成
4. 各 `asmdef` ファイル作成・設定
5. サードパーティ DLL の取得・`Runtime/Plugins/` への配置

> ⚠️ **手動作業（Unity エディタ必須）**: DLL の Platform 設定確認、asmdef の Precompiled References 確認

### フェーズ B: コアロジック実装（テストファースト）

Edit Mode テスト先行 → 実装の順で進める

1. `BezierSubdivider` テスト → 実装
2. `GlyphContourBuilder` テスト → 実装
3. `MeshExtruder` テスト → 実装（LibTessDotNet を使用した三角形分割・押し出し）
4. `GlyphMeshBuilder` テスト → 実装（統合テスト、CJK 文字含む）

### フェーズ C: MonoBehaviour + Editor 統合

1. `SolidText3DComponent` 実装（`Awake`, `OnValidate`, `LateUpdate`, 公開プロパティ）
2. `SolidText3DInspector` 実装（カスタムインスペクター）
3. Play Mode テスト: ダーティフラグ動作・`LateUpdate` 再生成

### フェーズ D: ドキュメント・サンプル・パッケージ整備

1. `README.md`, `CHANGELOG.md`, `LICENSE.md`, `Third Party Notices.md` 作成
2. `Samples~/BasicUsage/` サンプルシーン作成
3. `Samples~/CJKExample/` サンプルシーン作成

> ⚠️ **手動作業（Unity エディタ必須）**: Unity シーンファイル（`.unity`）の作成・編集はエディタ操作が必要。C# スクリプト部分は VS Code で作成可能。

---

## 手動実行が必要な作業

| # | 作業内容 | タイミング |
| --- | --- | --- |
| M-1 | DLL の Platform 設定確認（Plugin Import Settings） | フェーズ A 完了後 |
| M-2 | asmdef の Precompiled References 確認 | フェーズ A 完了後 |
| M-3 | Test Runner でテストが認識されるか確認 | フェーズ B 各ステップ後 |
| M-4 | サンプルシーン（.unity）の作成・保存 | フェーズ D |
| M-5 | Unity 6 / Unity 2022.3 LTS での手動ビルド確認 | フェーズ D 完了後 |

---

## 複雑度トラッキング

| 違反 | 必要な理由 | より単純な代替案を却下した理由 |
| --- | --- | --- |
| `LateUpdate` でのメッシュ生成（GC.Alloc 発生） | ランタイムでの動的テキスト変更（FR-007）に対応するため。変更時のみ実行 | 変更検知がない場合は毎フレーム再生成が必要になり、パフォーマンスがより悪化する |

---

## リスクと対策

| リスク | 影響度 | 対策 |
| --- | --- | --- |
| SixLabors.Fonts の `IGlyphRenderer` が Unity の `Vector2` と型が合わない | 中 | 独自の `PointF` ↔ `Vector2` 変換を実装（SixLabors.Fonts は `System.Numerics.Vector2` を使用） |
| LibTessDotNet が特定の CJK グリフで不安定 | 中 | EvenOdd WindingRule を使用し、コンター方向の前処理を追加 |
| サードパーティ DLL のビルドサイズ増加 | 低 | SixLabors.Fonts: 約 400 KB、LibTessDotNet: 約 50 KB。合計 450 KB 程度で許容範囲内 |

---

## 生成アーティファクト一覧

| ファイル | 説明 | 状態 |
| --- | --- | --- |
| `specs/001-solid-text-3d/plan.md` | 本ファイル | ✅ 完了 |
| `specs/001-solid-text-3d/research.md` | 技術調査結果 | ✅ 完了 |
| `specs/001-solid-text-3d/data-model.md` | エンティティ定義・データモデル | ✅ 完了 |
| `specs/001-solid-text-3d/contracts/public-api.md` | 公開 C# API コントラクト | ✅ 完了 |
| `specs/001-solid-text-3d/quickstart.md` | セットアップ・使用方法 | ✅ 完了 |
| `specs/001-solid-text-3d/tasks.md` | 実装タスクリスト | ⏳ `/speckit.tasks` で生成 |
