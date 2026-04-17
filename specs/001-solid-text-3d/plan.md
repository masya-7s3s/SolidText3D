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
- [x] **FR-013/FR-014 対応**: `LetterSpacing`・`LineSpacing` フィールドは data-model.md §1・§2 および contracts/public-api.md に定義済み（spec.md で正式要件として追加）
- [x] **ランタイムフォント制限（v1 設計決定）**: `GlyphMeshBuilder` の `#if UNITY_EDITOR` ガード内フォントバイト取得はエディタ環境専用。ランタイムビルドでは埋め込みデフォルトフォントを使用する。これは憲法 II（Editor/Runtime 分離）に字義上は適合するが、カスタムフォントのランタイム利用は v1 スコープ外として意図的に除外している

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
> 各ステップに「→ 参照:」として記載したリンクで、仕様・アルゴリズム詳細を確認できる。

### フェーズ A: パッケージスケルトン構築

**A-1** ディレクトリツリー作成  
`Packages/com.yourcompany.solidtext3d/` 配下に `Runtime/`, `Runtime/Plugins/`, `Editor/`, `Tests/Editor/`, `Tests/Runtime/`, `Samples~/BasicUsage/`, `Samples~/CJKExample/`, `Documentation~/` を作成。  
→ 参照: [research.md「研究結果 6: UPM パッケージ構造・確定ディレクトリ構造」](research.md)

**A-2** `package.json` 作成  
`name`（逆ドメイン形式）、`version: "1.0.0"`、`unity: "6000.0"`、`description`、`author` を記載。

**A-3** 4 つの `asmdef` ファイル作成・設定

- `Runtime/com.yourcompany.solidtext3d.Runtime.asmdef` — `overrideReferences: true`、`precompiledReferences: ["SixLabors.Fonts.dll", "LibTessDotNet.dll"]`
- `Editor/com.yourcompany.solidtext3d.Editor.asmdef` — `includePlatforms: ["Editor"]`、Runtime asmdef を参照
- `Tests/Editor/com.yourcompany.solidtext3d.Tests.Editor.asmdef` — `optionalUnityReferences: ["TestAssemblies"]`
- `Tests/Runtime/com.yourcompany.solidtext3d.Tests.Runtime.asmdef` — `optionalUnityReferences: ["TestAssemblies"]`

→ 参照: [research.md「研究結果 6: asmdef 設定（JSON 全文）」](research.md)

**A-4** サードパーティ DLL 取得・配置  
`SixLabors.Fonts.dll` と `LibTessDotNet.dll` を `Runtime/Plugins/` に配置。  
→ 参照: [research.md「研究結果 7: NuGet DLL の Unity への組み込み方法」](research.md) / [quickstart.md「ステップ 1-1」](quickstart.md)

> ⚠️ **手動作業 M-1・M-2（Unity エディタ必須）**: DLL 配置後に Plugin Import Settings で Platform 設定確認、asmdef の Precompiled References 確認

---

### フェーズ B: データ構造ファイルの作成

コアロジックが依存するデータ保持型を先に作成する（相互依存がなく並行作業可）。

**B-1** `Runtime/MeshGenerationParams.cs` 作成  
→ 参照: [data-model.md「2. MeshGenerationParams (struct)」](data-model.md)

**B-2** `Runtime/GlyphContour.cs` 作成  
→ 参照: [data-model.md「3. GlyphContour (class)」](data-model.md)

**B-3** `Runtime/GlyphMeshData.cs` 作成  
→ 参照: [data-model.md「9. GlyphMeshData (struct)」](data-model.md)

---

### フェーズ C: コアロジック実装（テストファースト・依存順）

**Red → Green** のサイクルで進める: テスト作成 → 失敗確認 → 実装 → 通過確認。  
各ステップは前のステップの全テストが通過してから開始すること。

> ⚠️ **手動作業 M-3（Unity エディタ必須）**: 各ステップ後に Test Runner でテストが認識・実行できるか確認

**C-1** `BezierSubdivider`（依存: なし）

1. `Tests/Editor/BezierSubdividerTests.cs` 作成  
   → テストケース: [data-model.md「テスト対象エンティティ: BezierSubdivider 行」](data-model.md)
2. `Runtime/BezierSubdivider.cs` 実装  
   → メソッド仕様: [data-model.md「5. BezierSubdivider (static class)」](data-model.md)  
   → アルゴリズム（再帰的 DeCasteljau 法・推奨パラメータ）: [research.md「研究結果 3: Bezier 曲線離散化戦略」](research.md)

**C-2** `GlyphContourBuilder`（依存: C-1 BezierSubdivider、B-2 GlyphContour）

1. `Tests/Editor/GlyphContourBuilderTests.cs` 作成  
   → テストケース: [data-model.md「テスト対象エンティティ: GlyphContourBuilder 行」](data-model.md)
2. `Runtime/GlyphContourBuilder.cs` 実装  
   → コールバックメソッド仕様: [data-model.md「4. GlyphContourBuilder (class, IGlyphRenderer)」](data-model.md)  
   → SixLabors.Fonts の `IGlyphRenderer` 呼び出しパターン: [research.md「研究結果 1: 使用コード（概要）」](research.md)

**C-3** `MeshExtruder`（依存: B-2 GlyphContour、B-3 GlyphMeshData）

1. `Tests/Editor/MeshExtruderTests.cs` 作成  
   → テストケース: [data-model.md「テスト対象エンティティ: MeshExtruder 行」](data-model.md)
2. `Runtime/MeshExtruder.cs` 実装  
   → メソッド仕様・内部処理フロー: [data-model.md「6. MeshExtruder (static class)」](data-model.md)  
   → 前面・背面・側面の押し出しアルゴリズム: [research.md「研究結果 4: 押し出しアルゴリズム」](research.md)  
   → アウトライン（Miter join）生成: [research.md「研究結果 5: アウトライン幾何生成」](research.md)  
   → LibTessDotNet の `AddContour` / `Tessellate` 呼び出し: [research.md「研究結果 2: LibTessDotNet 使用コード」](research.md)

**C-4** `GlyphMeshBuilder`（依存: C-2 GlyphContourBuilder、C-3 MeshExtruder、B-1 MeshGenerationParams）

1. `Tests/Editor/GlyphMeshBuilderTests.cs` 作成  
   → テストケース（CJK 文字・空文字・フォントなし）: [data-model.md「テスト対象エンティティ: GlyphMeshBuilder 行」](data-model.md)
2. `Runtime/GlyphMeshBuilder.cs` 実装  
   → 処理フロー・フォールバック仕様: [data-model.md「7. GlyphMeshBuilder (static class)」](data-model.md)  
   → フォントバイト取得の制約と実装方針: [data-model.md「7. 実装上の注意: フォントバイトの取得方法」](data-model.md)  
   → SixLabors.Fonts `FontCollection` の使い方: [research.md「研究結果 1: 使用コード（概要）」](research.md)

   > ⚠️ **v1 スコープ制限**: フォントバイト取得は `#if UNITY_EDITOR` ガード内の `AssetDatabase.GetAssetPath` + `File.ReadAllBytes` のみ。ランタイムビルドでは埋め込みデフォルトフォントにフォールバックする（spec.md 前提条件参照）

---

### フェーズ D: MonoBehaviour + Editor 統合

**D-1** `Runtime/SolidText3DComponent.cs` 実装  
→ フィールド・プロパティ・ライフサイクルメソッド仕様: [data-model.md「1. SolidText3DComponent (MonoBehaviour)」](data-model.md)  
→ 公開 API 契約（XML ドキュメントコメント文面含む）: [contracts/public-api.md](contracts/public-api.md)  
→ バリデーションルール（クランプ・フォールバック条件）: [data-model.md「バリデーションルール」](data-model.md)

**D-2** Play Mode テスト `Tests/Runtime/SolidText3DRuntimeTests.cs` 作成・実行  
→ テストケース（ダーティフラグ・LateUpdate 再生成）: [data-model.md「テスト対象エンティティ: SolidText3DComponent 行」](data-model.md)

**D-3** `Editor/SolidText3DInspector.cs` 実装  
→ 仕様（CustomEditor 属性・QueuePlayerLoopUpdate 呼び出し）: [data-model.md「8. SolidText3DInspector (Editor class)」](data-model.md)

---

### フェーズ E: ドキュメント・サンプル・パッケージ整備

**E-1** パッケージルートのドキュメントファイル作成  
`README.md`, `CHANGELOG.md`, `LICENSE.md`, `Third Party Notices.md`  
→ ライセンス記載内容: [research.md「研究結果 1・2: ライセンス欄」](research.md)

**E-2** `Samples~/BasicUsage/` C# スクリプト作成（VS Code で作成可）  
→ コード例: [contracts/public-api.md「使用例」](contracts/public-api.md)

**E-3** `Samples~/CJKExample/` C# スクリプト作成（VS Code で作成可）

> ⚠️ **手動作業 M-4（Unity エディタ必須）**: `.unity` シーンファイルの作成・オブジェクト配置・保存はエディタ操作が必要  
> ⚠️ **手動作業 M-5（Unity エディタ必須）**: Unity 6（6000.x LTS）で PC スタンドアロンビルドを実行して動作確認

---

## 手動実行が必要な作業

| # | 作業内容 | タイミング |
| --- | --- | --- |
| M-1 | DLL の Platform 設定確認（Plugin Import Settings） | フェーズ A 完了後 |
| M-2 | asmdef の Precompiled References 確認 | フェーズ A 完了後 |
| M-3 | Test Runner でテストが認識・実行できるか確認 | フェーズ C 各ステップ後 |
| M-4 | サンプルシーン（.unity）の作成・保存 | フェーズ E |
| M-5 | Unity 6（6000.x LTS）での手動ビルド確認 | フェーズ E 完了後 |

---

## 複雑度トラッキング

| 違反 | 必要な理由 | より単純な代替案を却下した理由 |
| --- | --- | --- |
| `LateUpdate` でのメッシュ生成（GC.Alloc 発生） | ランタイムでの動的テキスト変更（FR-007）に対応するため。変更時のみ実行 | 変更検知がない場合は毎フレーム再生成が必要になり、パフォーマンスがより悪化する |
| `GlyphMeshBuilder` の `#if UNITY_EDITOR` によるフォントバイト取得（カスタムフォントのランタイム非対応） | ランタイムビルドで `AssetDatabase` が使用不可のため。v1 ではエディタ環境専用に限定し、ランタイムは埋め込みデフォルトフォントで動作（spec.md 前提条件参照） | ランタイムでのフォント配布には `StreamingAssets` や `Resources` の設計変更が必要であり、v2 以降のスコープとする |

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
