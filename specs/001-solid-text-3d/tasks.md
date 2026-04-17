# タスク: Solid Text 3D

**入力**: `specs/001-solid-text-3d/` の設計ドキュメント群  
**前提条件**: plan.md（必須）, spec.md（必須）, data-model.md, contracts/public-api.md, quickstart.md  
**作成日**: 2026年4月17日

---

## フォーマット: `[ID] [P?] [Story?] 説明 ファイルパス`

- **[P]**: 並行実行可能（異なるファイル、未完了タスクへの依存なし）
- **[Story]**: 所属ユーザーストーリー（例: US1, US2, US3）
- 説明には正確なファイルパスを含める

---

## フェーズ 1: セットアップ（UPM パッケージスケルトン構築）

**目的**: Unity UPM パッケージの基本ディレクトリ構造・設定ファイルをすべて作成し、コアロジック実装の土台を整える

- [ ] T001 パッケージルートディレクトリ構造を作成（`Packages/com.yourcompany.solidtext3d/` 配下に `Runtime/Plugins/`, `Editor/`, `Tests/Editor/`, `Tests/Runtime/`, `Samples~/BasicUsage/`, `Samples~/CJKExample/`, `Documentation~/` を含む全フォルダを作成）
- [ ] T002 `Packages/com.yourcompany.solidtext3d/package.json` を作成（`name`, `version: "1.0.0"`, `unity: "6000.0"`, `description`, `author` を記載）
- [ ] T003 [P] `Packages/com.yourcompany.solidtext3d/Runtime/com.yourcompany.solidtext3d.Runtime.asmdef` を作成（`overrideReferences: true`, `precompiledReferences: ["SixLabors.Fonts.dll", "LibTessDotNet.dll"]` を設定）
- [ ] T004 [P] `Packages/com.yourcompany.solidtext3d/Editor/com.yourcompany.solidtext3d.Editor.asmdef` を作成（`includePlatforms: ["Editor"]`, Runtime asmdef への参照を設定）
- [ ] T005 [P] `Packages/com.yourcompany.solidtext3d/Tests/Editor/com.yourcompany.solidtext3d.Tests.Editor.asmdef` を作成（`optionalUnityReferences: ["TestAssemblies"]`, Runtime asmdef への参照を設定）
- [ ] T006 [P] `Packages/com.yourcompany.solidtext3d/Tests/Runtime/com.yourcompany.solidtext3d.Tests.Runtime.asmdef` を作成（`optionalUnityReferences: ["TestAssemblies"]`, Runtime asmdef への参照を設定）
- [ ] T007 NuGet.org から `SixLabors.Fonts.dll`（MIT、**取得時にバージョンをピン留めし README に記録すること**）と `LibTessDotNet.dll` v1.1.15（SGI Free B v2）を取得し `Packages/com.yourcompany.solidtext3d/Runtime/Plugins/` に配置（各 `.meta` ファイルも含む）（※ quickstart.md ステップ 1-1 参照）
- [ ] T007b デフォルト埋め込みフォント **Noto Sans JP Regular**（SIL Open Font License 1.1）を Google Fonts（<https://fonts.google.com/noto/specimen/Noto+Sans+JP>）から取得し `Packages/com.yourcompany.solidtext3d/Runtime/Plugins/Fonts/NotoSansJP-Regular.ttf` に配置。OFL 1.1 ライセンス全文を `Third Party Notices.md` 用に保存する（spec.md § 前提条件「デフォルト埋め込みフォント」参照）
- [ ] T008 [P] ルートドキュメントプレースホルダーを作成（`Packages/com.yourcompany.solidtext3d/` 直下に `README.md`, `CHANGELOG.md`, `LICENSE.md`, `Third Party Notices.md` を空ファイルとして作成）

> ⚠️ **手動作業 M-1**: T007 完了後、Unity エディタで Plugin Import Settings を開き DLL の Platform 設定を確認する  
> ⚠️ **手動作業 M-2**: Unity エディタで Runtime asmdef の Precompiled References に DLL が正しく登録されていることを確認する

**チェックポイント**: Unity エディタがパッケージを認識し、asmdef による 4 アセンブリのコンパイルが通ること

---

## フェーズ 2: 基盤（ブロッキング前提条件）

**目的**: 全コアロジックが依存するデータ保持型を先に作成する（相互依存なし、並行作業可）

**⚠️ 重要**: このフェーズが完了するまで、いかなるユーザーストーリーの実装も開始できない

- [ ] T009 [P] `Packages/com.yourcompany.solidtext3d/Runtime/MeshGenerationParams.cs` を作成（`Text`, `FontPath`, `FontData`, `ExtrusionDepth`, `OutlineWidth`, `LetterSpacing`, `LineSpacing`, `BezierErrorThreshold` の 8 フィールドを持つ struct。data-model.md § 2 参照）
- [ ] T010 [P] `Packages/com.yourcompany.solidtext3d/Runtime/GlyphContour.cs` を作成（`Contours: List<List<Vector2>>`, `AdvanceWidth: float`, `Bounds: Rect` を持つ class。data-model.md § 3 参照）
- [ ] T011 [P] `Packages/com.yourcompany.solidtext3d/Runtime/GlyphMeshData.cs` を作成（`Vertices: List<Vector3>`, `Triangles: List<int>`, `Normals: List<Vector3>`, `Offset: Vector3` を持つ struct。data-model.md § 9 参照）

**チェックポイント**: 3 つのデータ型がコンパイルエラーなく定義されていること

---

## フェーズ 3: ユーザーストーリー 1 — エディタでの静的 3D テキスト生成（優先度: P1）🎯 MVP

**ゴール**: Unity エディタの Inspector からパラメータを設定し、静的な 3D テキストメッシュをシーンビューに表示する

**独立テスト**: SolidText3DComponent をアタッチしてテキスト・フォント・押し出し深さを設定したとき、指定テキストが 3D メッシュとしてシーンビューに表示される

> **TDD サイクル**: 各コンポーネントはテストを先に作成して FAIL を確認してから実装する  
> ⚠️ **手動作業 M-3**: 各コンポーネント実装後、Unity Test Runner でテストが認識・実行できるか確認する  
> 📌 **CI 注意**: フェーズ 3 の全テスト（T012–T018b）は **`Tests/Editor/` 配下の Edit Mode テスト**。`#if UNITY_EDITOR` 環境（Unity Test Framework の Edit Mode 実行）でのみ実行される。T019 の `#else` スタブ（空メッシュ返却）は非エディタビルド（CI / ランタイムビルド）向けの安全なフォールバックであり、T031 完了後に Noto Sans JP 埋め込みバイトで置き換えられる

### US1 テスト（先行作成・FAIL 確認必須）

- [ ] T012 [P] [US1] `Packages/com.yourcompany.solidtext3d/Tests/Editor/BezierSubdividerTests.cs` を作成（二次ベジェ離散化・三次ベジェ離散化・直線退化・誤差閾値の単体テスト。data-model.md § 5 参照）
- [ ] T014 [P] [US1] `Packages/com.yourcompany.solidtext3d/Tests/Editor/GlyphContourBuilderTests.cs` を作成（`BeginFigure`/`MoveTo`/`LineTo`/`QuadraticBezierTo`/`CubicBezierTo`/`EndFigure`/`EndGlyph` の IGlyphRenderer コールバック動作テスト。data-model.md § 4 参照）
- [ ] T016 [P] [US1] `Packages/com.yourcompany.solidtext3d/Tests/Editor/MeshExtruderTests.cs` を作成（前面ポリゴン三角分割・背面複製・側面クワッド生成・アウトライン帯生成・押し出し深さ 0 の平面メッシュ・全頂点数/インデックス数の検証テスト。data-model.md § 6 参照）
- [ ] T018 [P] [US1] `Packages/com.yourcompany.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` を作成（ASCII 文字 "A"・"Hello" の統合メッシュ生成テスト・頂点数 > 0 の検証・null パラメータ時の安全性テスト。data-model.md § 7 参照）
- [ ] T018b [P] [US1] `Packages/com.yourcompany.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に `LetterSpacing` / `LineSpacing` レイアウト計算テストを追加（`LetterSpacing > 0` 時に各グリフの X オフセットが `LetterSpacing` 分だけ広がることを頂点位置で検証・`LineSpacing` 変更時に改行後グリフの Y オフセットが `LineSpacing × UnitsPerEm` に比例して変化することを検証。FR-013/FR-014 対応。data-model.md § 2 参照）

### US1 実装（各テスト FAIL 確認後に実施）

- [ ] T013 [US1] `Packages/com.yourcompany.solidtext3d/Runtime/BezierSubdivider.cs` を実装（静的クラス。`SubdivideQuadratic(Vector2 p0, p1, p2, float threshold, List<Vector2> output): void` と `SubdivideCubic(Vector2 p0, p1, p2, p3, float threshold, List<Vector2> output): void` を再帰的 De Casteljau 法で実装。research.md 研究結果 3 参照）
- [ ] T015 [US1] `Packages/com.yourcompany.solidtext3d/Runtime/GlyphContourBuilder.cs` を実装（`SixLabors.Fonts.IGlyphRenderer` 実装クラス。7 つのコールバックメソッドで `List<GlyphContour>` を構築。`System.Numerics.Vector2` ↔ `UnityEngine.Vector2` 変換を含む。research.md 研究結果 1 参照）
- [ ] T017 [US1] `Packages/com.yourcompany.solidtext3d/Runtime/MeshExtruder.cs` を実装（静的クラス。`Build(List<GlyphContour>, MeshGenerationParams): Mesh` と `BuildGlyphMesh(GlyphContour, float, float): GlyphMeshData` を実装。LibTessDotNet `AddContour` / `Tessellate`（EvenOdd WindingRule）による前面三角分割・背面頂点複製・側面クワッド生成・アウトライン帯生成を含む。`mesh.indexFormat = IndexFormat.UInt32` を設定。research.md 研究結果 2・4・5 参照）
- [ ] T019 [US1] `Packages/com.yourcompany.solidtext3d/Runtime/GlyphMeshBuilder.cs` を実装（静的クラス。`Build(MeshGenerationParams): Mesh` を実装。`SixLabors.Fonts.FontCollection` でフォントを読み込み、`GlyphContourBuilder` でグリフ輪郭を収集し、`MeshExtruder.Build()` で 3D メッシュを生成。`#if UNITY_EDITOR` ガード内で `UnityEditor.AssetDatabase.GetAssetPath` + `File.ReadAllBytes` でフォントバイト取得、`#else` ブロックでは **スタブとして空メッシュを返却**（Noto Sans JP の埋め込みバイトによる完全実装は T031 で行う）。グリフ欠損時の空グリフスキップ処理を含む。data-model.md § 7 参照）
- [ ] T020 [US1] `Packages/com.yourcompany.solidtext3d/Runtime/SolidText3DComponent.cs` を実装（MonoBehaviour。`_text`, `_font`, `_extrusionDepth`, `_outlineWidth`, `_letterSpacing`, `_lineSpacing` シリアライズフィールド・`_isDirty` / `_meshFilter` 非シリアライズフィールド。`Text`, `Font`, `ExtrusionDepth`, `OutlineWidth`, `LetterSpacing`（FR-013）, `LineSpacing`（FR-014）公開プロパティ（set で `_isDirty = true`）。`Awake()` で MeshFilter/MeshRenderer を GetComponent または AddComponent。`OnValidate()` で `_isDirty = true`。`LateUpdate()` でダーティフラグをチェックし `RegenerateMesh()` を呼び出し。contracts/public-api.md の XML ドキュメントコメントを付与。data-model.md § 1 参照）
- [ ] T021 [US1] `Packages/com.yourcompany.solidtext3d/Editor/SolidText3DInspector.cs` を実装（`[CustomEditor(typeof(SolidText3DComponent))]` 属性付き Editor クラス。`serializedObject.ApplyModifiedProperties()` 後に `EditorApplication.QueuePlayerLoopUpdate()` を呼び出してシーンビューを再描画。`#if UNITY_EDITOR` ガードは asmdef で不要だが `UnityEditor` 名前空間使用を明示。data-model.md § 8 参照）

**チェックポイント**: この時点で US1 の全テストが PASS し、エディタ上でテキスト設定 → 3D メッシュ生成 → シーンビュー表示が動作すること（受け入れシナリオ 1〜4 を手動確認）

---

## フェーズ 4: ユーザーストーリー 2 — ランタイムでの動的テキスト変更（優先度: P2）

**ゴール**: スクリプトからテキストを実行時に変更し、次フレームで 3D メッシュが自動更新される

**独立テスト**: `SolidText3D.Text = "新しい文字列"` を Play Mode で呼び出し、次 `LateUpdate()` でメッシュが更新されることを Unity Test Runner で確認できる

### US2 テスト（先行作成・FAIL 確認必須）

- [ ] T022 [US2] `Packages/com.yourcompany.solidtext3d/Tests/Runtime/SolidText3DRuntimeTests.cs` を作成（テキスト変更後 1 フレーム以内でメッシュが更新される Play Mode テスト（`yield return null;` 後に `Assert.Greater(meshFilter.sharedMesh.vertexCount, 0)` で検証）・空文字列設定時のゼロポリゴン（頂点数 0）テスト・同一フレーム内の複数プロパティ変更が 1 回のメッシュ再生成に集約されるテスト。**前提**: テスト内でフォントを明示的にアサインして `#if UNITY_EDITOR` ブランチの動作を保証すること。data-model.md § 1 ダーティフラグ節参照）

### US2 実装

- [ ] T023 [US2] `Packages/com.yourcompany.solidtext3d/Runtime/SolidText3DComponent.cs` の `RegenerateMesh()` に空文字列時のゼロポリゴン処理を追加（`string.IsNullOrEmpty(_text)` の場合は `meshFilter.sharedMesh.Clear()` を実行してエラーを発生させない）
- [ ] T024 [US2] `Packages/com.yourcompany.solidtext3d/Runtime/SolidText3DComponent.cs` の `Text` setter で `null` 値を空文字に正規化する処理を追加し、Play Mode テスト（T022）がすべて PASS することを確認（**T023 完了後に実施**）

**チェックポイント**: この時点で Play Mode でスクリプトからテキストを変更すると 1 フレーム以内でメッシュが更新され、空文字列でエラーが発生しないこと

---

## フェーズ 5: ユーザーストーリー 3 — CJK および多言語文字への対応（優先度: P2）

**ゴール**: 日本語・中国語・韓国語を含む任意の Unicode 文字が正しく 3D メッシュとして生成される

**独立テスト**: CJK 文字を含むフォントを使用して「立体文字」「한글」「汉字」を設定し、3D メッシュが正しく生成されることを Edit Mode テストで確認できる

### US3 テスト（先行作成・FAIL 確認必須）

- [ ] T025 [P] [US3] `Packages/com.yourcompany.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に日本語（「立体文字」）・中国語（「汉字」）・韓国語（「한글」）の CJK テストケースを追加（各文字の頂点数 > 0・メッシュ生成エラーなしを検証）（**T018 完了後に実行可能**）
- [ ] T026 [P] [US3] `Packages/com.yourcompany.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に混在テキスト（"Hello 世界"）のテストケースを追加（全文字の頂点が含まれることを検証）（**T018 完了後に実行可能**）

### US3 実装

- [ ] T027 [US3] `Packages/com.yourcompany.solidtext3d/Runtime/GlyphMeshBuilder.cs` にフォント未収録グリフのフォールバック処理を追加（グリフが null / 空コンターの場合は `AdvanceWidth` のみ確保して空グリフとしてスキップし、エラー停止しない。data-model.md バリデーションルール参照）
- [ ] T028 [US3] `Packages/com.yourcompany.solidtext3d/Runtime/MeshExtruder.cs` の `LibTessDotNet.Tess` 呼び出しで `WindingRule.EvenOdd` が設定されていることを確認・修正（CJK グリフの穴コンター「口」「O」等を正しく三角分割するため。research.md 研究結果 2 参照）

**チェックポイント**: この時点で CJK 文字・混在テキストの全テストが PASS し、穴を持つ CJK グリフが正しく 3D メッシュ化されること

---

## フェーズ 6: ユーザーストーリー 4 — フォント設定とカスタムフォントの使用（優先度: P3）

**ゴール**: プロジェクトにインポートした TTF/OTF フォントを Inspector に割り当てるだけで、そのフォントのグリフで 3D メッシュが生成される

**独立テスト**: Inspector でフォントフィールドを変更すると、そのフォントのグリフで 3D メッシュが再生成される。フォントが null でもエラーにならずデフォルトフォントで動作する

### US4 テスト（先行作成・FAIL 確認必須）

- [ ] T029 [US4] `Packages/com.yourcompany.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に `FontData = null` 時のデフォルトフォールバックテストを追加（メッシュが返されエラーが発生しないこと・`Debug.LogWarning` が出力されることを検証）

### US4 実装

- [ ] T030 [US4] `Packages/com.yourcompany.solidtext3d/Runtime/SolidText3DComponent.cs` の `Awake()` および `RegenerateMesh()` にフォント null / フォントファイル削除時のデフォルトフォールバックロジックを完全実装（`_font == null` 時に `Debug.LogWarning` を出力し内部デフォルトフォントバイト（埋め込みリソース）を使用。FR-009 参照）
- [ ] T031 [US4] `Packages/com.yourcompany.solidtext3d/Runtime/GlyphMeshBuilder.cs` のフォントバイト取得ロジックを完全実装（エディタ実行時: `#if UNITY_EDITOR` ガード内で `UnityEditor.AssetDatabase.GetAssetPath(font)` + `System.IO.File.ReadAllBytes()` でフォントバイト取得 / ランタイム（ビルド済み）: `#else` ブロックで埋め込みデフォルトフォントバイトを使用してメッシュを生成（FR-009 準拠・エラー停止なし・Warning 出力あり）。data-model.md § 7 実装上の注意参照）
- [ ] T031b [US4] `Packages/com.yourcompany.solidtext3d/Tests/Runtime/SolidText3DRuntimeTests.cs` にランタイムフォント切り替えテストを追加（実行中に `SolidText3DComponent.Font` を別のフォントに変更したとき、`yield return null;` 後に新フォントのグリフでメッシュが再生成されることを検証。spec.md エッジケース「ランタイムでフォント自体を変更した場合、メッシュは正しく再生成されるか？」対応）

**チェックポイント**: この時点でカスタムフォントの割り当て・null フォールバック・全テストの PASS が確認できること

---

## フェーズ 7: ポリッシュ & クロスカッティング

**目的**: 複数ユーザーストーリーにまたがる改善・ドキュメント整備・サンプル作成・最終検証

- [ ] T032 [P] `Packages/com.yourcompany.solidtext3d/README.md` を完成させる（インストール手順・基本的な使い方・主要 API 一覧・カスタムフォントの割り当て方・既知の制限事項を記載。contracts/public-api.md の使用例を含める）
- [ ] T033 `Packages/com.yourcompany.solidtext3d/Third Party Notices.md` に SixLabors.Fonts（MIT ライセンス全文）・LibTessDotNet（SGI Free Software License B v2.0 全文）・Noto Sans JP Regular（SIL Open Font License 1.1 全文）を記載（research.md のライセンス欄・spec.md § 前提条件「デフォルト埋め込みフォント」参照）
- [ ] T034 [P] `Packages/com.yourcompany.solidtext3d/Samples~/BasicUsage/BasicUsageExample.cs` を作成（スコア表示サンプル。スクリプトから `SolidText3DComponent.Text` を変更するコード例。contracts/public-api.md「使用例」参照）
- [ ] T035 [P] `Packages/com.yourcompany.solidtext3d/Samples~/CJKExample/CJKExample.cs` を作成（日本語・中国語・韓国語を含む文字列を SolidText3DComponent に設定するサンプルコード）
- [ ] T036 `Packages/com.yourcompany.solidtext3d/Documentation~/index.md` を作成（API リファレンス・パラメータ一覧・エディタ/ランタイムの使用ガイド・トラブルシューティング（ランタイムでのフォントバイト制限）を記載。quickstart.md を参照）
- [ ] T037 `quickstart.md` の手動検証チェックリスト（M-1〜M-5）を実施し、Unity 6 で正常にビルド・動作することを確認（DLL Platform 設定、Test Runner 動作、シーンへの配置、エディタプレビュー、ビルド通過）
- [ ] T038 [P] `Packages/com.yourcompany.solidtext3d/Tests/Editor/PerformanceTests.cs` に SC-001 ベンチマークテストを作成（50文字テキストのメッシュ生成時間を `System.Diagnostics.Stopwatch` で計測し、`Assert.Less(elapsedMs, 2000)` で 2000ms 未満を検証。メッシュ生成のみを計測し Unity Editor の起動コストを含めない。SC-001 対応）
- [ ] T039 [P] `Packages/com.yourcompany.solidtext3d/Tests/Runtime/PerformanceRuntimeTests.cs` に SC-004 ベンチマークテストを作成（20個の SolidText3DComponent（各50文字）を同一シーンに配置し、`Time.deltaTime` の連続 30 フレーム平均値が 16.7ms 未満（60fps 相当）であることを `Assert.Less` で検証。SC-004 対応）- [ ] T040 `Packages/com.yourcompany.solidtext3d/Tests/` 配下の全テストを Unity Test Runner で実行し、ランタイムロジックのテストカバレッジを確認する（Edit Mode: `BezierSubdivider`, `GlyphContourBuilder`, `MeshExtruder`, `GlyphMeshBuilder`（80%以上を目安）・ Play Mode: `SolidText3DRuntimeTests` が全 PASS であることを確認。カバレッジ計測には `com.unity.test-framework.performance`（Unity Code Coverage パッケージ）の導入を推奨する（未導入の場合は手動目視で主要パスをすべてカバーしていることを確認すること）。不足しているケースがあれば `specs/001-solid-text-3d/tests-coverage-gap.md` に記録する。標準: 憲法第 III 「ランタイムロジック 80% 以上」準拠）

> ⚠️ **手動作業 M-4**: `Samples~/BasicUsage/` と `Samples~/CJKExample/` の `.unity` シーンファイル作成・GameObject 配置・保存は Unity エディタ操作が必要  
> ⚠️ **手動作業 M-5**: Unity 6（6000.x LTS）で PC スタンドアロンビルドを実行して動作確認

**チェックポイント**: Asset Store 提出可能な状態。全テスト PASS・ビルド通過・ドキュメント整備完了・SC-001/SC-004 ベンチマーク PASS・カバレッジ目視確認完了

---

## 依存関係 & 実行順序

### フェーズ依存関係

- **セットアップ（フェーズ 1）**: 依存なし — 即時開始可能
- **基盤（フェーズ 2）**: フェーズ 1 完了後に開始 — **すべてのユーザーストーリーをブロック**
- **US1（フェーズ 3）**: フェーズ 2 完了後に開始 — US2・US3・US4 をブロック
- **US2・US3（フェーズ 4・5）**: フェーズ 3 完了後に並行実行可能（スタッフが複数いる場合）
- **US4（フェーズ 6）**: フェーズ 3 完了後に開始可能
- **ポリッシュ（フェーズ 7）**: 全ユーザーストーリーフェーズ完了後

### ユーザーストーリー依存関係

```text
フェーズ 1（セットアップ）
  └→ フェーズ 2（基盤）
       └→ フェーズ 3（US1: MVP）
            ├→ フェーズ 4（US2: 動的テキスト）
            ├→ フェーズ 5（US3: CJK 対応）
            └→ フェーズ 6（US4: カスタムフォント）
                    └→ フェーズ 7（ポリッシュ）
```

### 各ユーザーストーリー内の順序

1. テストを先に作成して **FAIL** を確認する（TDD Red）
2. 実装を行い、テストを **PASS** させる（TDD Green）
3. データ型（struct/class）→ ユーティリティ（static クラス）→ サービス層 → MonoBehaviour の順に進める
4. 各コンポーネントの全テスト PASS 後に次のコンポーネントへ進む

### 並行実行可能なタスク

- フェーズ 1: T003・T004・T005・T006・T008 は T001・T002 完了後に並行実行可能
- フェーズ 2: T009・T010・T011 は同時に並行実行可能
- フェーズ 3 テスト: T012・T014・T016・T018・T018b は同時に並行作成可能（実装は依存順を守ること）
- フェーズ 5 テスト: T025・T026 は同時に並行実行可能（ただし T018 完了後）
- フェーズ 7: T032・T034・T035・T038・T039 は同時に並行実行可能（T040 は全テスト PASS 後に実施）

---

## 並行実行例: ユーザーストーリー 1（フェーズ 3）

```text
[T009 MeshGenerationParams] ─────────────────────────────────────────────────┐
[T010 GlyphContour        ] ──────────────────────────────────────────────────┤
[T011 GlyphMeshData       ] ──────────────────────────────────────────────────┤
                                                                               ↓
[T012 BezierSubdividerTests] ──→ [T013 BezierSubdivider 実装]
[T014 GlyphContourBuilderTests] ──→ [T015 GlyphContourBuilder 実装] ←─ T013 完了後
[T016 MeshExtruderTests   ] ──→ [T017 MeshExtruder 実装         ] ←─ T010・T011 完了後
[T018 GlyphMeshBuilderTests] ──→ [T019 GlyphMeshBuilder 実装    ] ←─ T015・T017 完了後
                                        ↓
                               [T020 SolidText3DComponent 実装  ] ←─ T019 完了後
                               [T021 SolidText3DInspector 実装  ] ←─ T020 完了後
```

---

## 実装戦略

### MVP（最小実行可能製品）

**フェーズ 1 + 2 + 3（US1）のみで MVP が成立する。**  
US1 完了時点で:

- エディタで 3D テキストを生成できる
- Inspector でリアルタイムプレビューが動作する
- 全 Edit Mode テストが PASS している

### インクリメンタル デリバリー

| イテレーション | フェーズ | デリバラブル |
| --- | --- | --- |
| 1 | フェーズ 1 + 2 | パッケージ骨格・コンパイル確認 |
| 2 | フェーズ 3 | **MVP**: エディタ静的 3D テキスト生成 |
| 3 | フェーズ 4 + 5 | ランタイム動的変更 + CJK 対応 |
| 4 | フェーズ 6 | カスタムフォント対応完了 |
| 5 | フェーズ 7 | Asset Store 提出可能な状態 |

---

## タスク数サマリー

| フェーズ | タスク数 | ユーザーストーリー |
| --- | --- | --- |
| フェーズ 1: セットアップ | 9 タスク（T001–T007b, T008） | — |
| フェーズ 2: 基盤 | 3 タスク（T009–T011） | — |
| フェーズ 3: US1（P1） | 11 タスク（T012–T018b, T019–T021） | US1（MVP） |
| フェーズ 4: US2（P2） | 3 タスク（T022–T024） | US2 |
| フェーズ 5: US3（P2） | 4 タスク（T025–T028） | US3 |
| フェーズ 6: US4（P3） | 3 タスク（T029–T031） | US4 |
| フェーズ 7: ポリッシュ | 9 タスク（T032–T040） | — |
| **合計** | **42 タスク** | 4 ユーザーストーリー |
