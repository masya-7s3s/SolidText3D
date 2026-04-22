# Tasks: Solid Text 3D — レイアウト・フォント・パフォーマンス改善

**Feature**: `002-text-layout-improvements` | **Date**: 2026-04-21  
**Input**: specs/002-text-layout-improvements/{plan.md, spec.md, data-model.md, contracts/, research.md, quickstart.md}  
**SemVer**: `1.0.0 → 2.0.0`（MAJOR: 破壊的変更あり）

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: 並列実行可能（異なるファイル、未完了タスクへの依存なし）
- **[Story]**: 対応ユーザーストーリー（US1〜US7）
- 各タスクに実際のファイルパスを明記

---

## Phase 1: Setup（プロジェクト初期化）

**Purpose**: バージョンバンプと破壊的変更の事前準備

- [X] T001 `Packages/com.masachuang.solidtext3d/package.json` の `version` を `"1.0.0"` から `"2.0.0"` に更新する

---

## Phase 2: Foundational（全ストーリーの前提条件）

**Purpose**: 全ユーザーストーリーが依存する型・クラスの整備。この Phase が完了するまで Phase 3 以降は開始できない。

**⚠️ CRITICAL**: 以下のタスクはすべての US フェーズのブロッカーである。

- [X] T002 [P] `Packages/com.masachuang.solidtext3d/Runtime/TextAnchorEnums.cs` を新規作成し、`HorizontalAnchor`・`VerticalAnchor`・`DepthAnchor`・`WritingMode`・`ObjectMode` の 5 enum を `MasaChuang.SolidText3D` 名前空間で定義する（data-model.md § 新規 Enum 型 参照）
- [X] T003 [P] `Packages/com.masachuang.solidtext3d/Runtime/GlyphContour.cs` を修正し、`AdvanceHeight (float)`・`CharIndex (int)`・`IsVisible (bool)` フィールドを追加する（data-model.md § GlyphContour（修正） 参照）
- [X] T004 `Packages/com.masachuang.solidtext3d/Runtime/MeshGenerationParams.cs` を修正する: `FontPath (string)` フィールドを削除し、`HorizontalAnchor`・`VerticalAnchor`・`DepthAnchor`・`WritingMode`・`MaxWidth`・`MaxHeight`・`VerticalColumnWidth`・`RotateAsciiInVertical` フィールドを追加する（data-model.md § MeshGenerationParams（修正） 参照）
- [X] T005 `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` を修正し、`FontPath` 参照を `FontData = File.ReadAllBytes(...)` に置き換えてコンパイルエラーを解消する（plan.md ステップ 2 注意事項 参照）
- [X] T006 `Packages/com.masachuang.solidtext3d/Runtime/LayoutEngine.cs` を新規作成し、`internal static class LayoutEngine` として `ApplyHorizontalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)` メソッドを実装する（既存 `GlyphMeshBuilder.ApplyLayout()` の横書きロジックを移植。アンカー計算・縦書きは後続タスクで追加）（data-model.md § LayoutEngine 参照）
- [X] T007 `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` を修正し、既存のプライベートメソッド `ApplyLayout()` を削除して `LayoutEngine.ApplyHorizontalLayout()` の呼び出しに置き換え、`GetFontBytes()` から `FontPath` 参照を削除する（`FontData` のみ使用）

**Checkpoint**: Foundational 完了。全 enum・修正済みパラメータクラス・LayoutEngine（横書き）が利用可能。

---

## Phase 3: User Story 1 — フォントのインスペクタアタッチ（Priority: P1）🎯 MVP

**Goal**: Inspector の Object フィールドでフォントを直接アタッチできるようにし、文字列名指定を廃止する。

**Independent Test**: Inspector の「Font Asset」フィールドに .ttf をドラッグ＆ドロップすると、そのフォントで 3D テキストが描画される。フォント未設定時はインスペクタに警告が表示され、何も描画しない。

### Implementation for User Story 1

- [X] T008 [US1] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` を修正する: `[SerializeField] string _font` と `public string Font` プロパティを削除し、`[SerializeField] private UnityEngine.Object _fontAsset`・`[SerializeField, HideInInspector] private TextAsset _fontBytesCache`・`private bool _fontMissingWarningIssued` フィールドを追加する。`public UnityEngine.Object FontAsset { get; set; }` プロパティを実装する。`RegenerateMesh()` にフォント未設定（FR-012）と Missing フォント（FR-016）の分岐を実装する（Missing 時は直前メッシュ維持・LogWarning 1 回のみ・`_fontMissingWarningIssued` フラグ管理）（data-model.md § SolidText3DComponent（修正）、spec FR-012・FR-016 参照）
- [X] T009 [US1] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` を修正し、フォントフィールドを `EditorGUILayout.ObjectField("Font Asset", _fontAsset, typeof(UnityEngine.Object), false)` に変更する。フォント未設定時の警告メッセージ（`EditorGUILayout.HelpBox`）を表示する（research.md § R-001 参照）

### Tests for User Story 1

- [X] T010 [P] [US1] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` に `FontAsset_Missing_MaintainsPreviousMesh` テストを追記する（plan.md ステップ 7 参照）

**Checkpoint**: US1 完了。Inspector でフォントをアタッチして 3D テキストが描画できる。

---

## Phase 4: User Story 2 — フォントファイルの自動 .bytes 変換（Priority: P2）

**Goal**: .ttf/.otf をインポートするだけで .bytes が自動生成され、手動変換が不要になる。

**Independent Test**: .ttf ファイルを Inspector にアタッチし、手動で .bytes を作成せずに 3D テキストが描画される。`Assets/SolidText3DFonts/` に .bytes ファイルが自動生成されている。

### Implementation for User Story 2

- [X] T011 [US2] `Packages/com.masachuang.solidtext3d/Editor/FontAssetPostprocessor.cs` を新規作成する: `internal sealed class FontAssetPostprocessor : AssetPostprocessor` として実装する。`OnPostprocessAllAssets` で .ttf/.otf のインポートを検知し、`Assets/SolidText3DFonts/{guid}.bytes` にアトミック書き込み（一時ファイル → File.Move）で生成する。既存 .bytes が存在する場合はスキップ（FR-003）。`AssetDatabase.ImportAsset()` で登録後、開いているシーン内の全 `SolidText3DComponent` を `FindObjectsByType<SolidText3DComponent>()` で走査し GUID が一致するものに `_fontBytesCache` を設定する（`SerializedObject` 経由、Prefab は対象外）（data-model.md § FontAssetPostprocessor、research.md § R-002、spec FR-002・FR-003・FR-017 参照）
- [X] T012 [US2] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` を修正し、`_fontAsset` が設定されているが `_fontBytesCache` が未設定の場合に `Assets/SolidText3DFonts/{guid}.bytes` を探して自動設定するロジックを `OnEnable` または Inspector 描画時に追加する（Prefab 対応、spec US2 Acceptance Scenario 4 参照）

### Tests for User Story 2

- [X] T013 [P] [US2] `Packages/com.masachuang.solidtext3d/Tests/Editor/FontAssetPostprocessorTests.cs` を新規作成し、`OnPostprocess_TtfFile_CreatesBytesFile`・`OnPostprocess_ExistingBytesFile_Skips`・`OnPostprocess_IoError_LogsError` テストを実装する（plan.md ステップ 8 参照）

**Checkpoint**: US2 完了。.ttf をドラッグするだけでフォントが使用できる。

---

## Phase 5: User Story 3 — テキスト配置のアンカー指定（Priority: P3）

**Goal**: 垂直・水平・奥行きの 3 軸アンカーを Inspector で独立設定でき、テキストが期待位置に配置される。

**Independent Test**: HorizontalAnchor=Center, VerticalAnchor=Middle, DepthAnchor=Center に設定すると、テキストメッシュの重心が GameObject の原点と一致する。

### Implementation for User Story 3

- [X] T014 [US3] `Packages/com.masachuang.solidtext3d/Runtime/LayoutEngine.cs` に `internal static Vector3 CalculateAnchorOffset(Bounds meshBounds, MeshGenerationParams p)` メソッドを追加する。計算式: 水平 Left=0 / Center=-width/2 / Right=-width、垂直 Upper=-height / Middle=-height/2 / Lower=0、奥行き Front=0 / Center=-depth/2 / Back=-depth（research.md § R-005 参照）
- [X] T015 [US3] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` を修正し、`Build()` 内で `MeshExtruder.Build()` 後に `LayoutEngine.CalculateAnchorOffset(mesh.bounds, params)` を呼び出し、全頂点にオフセットを加算する処理を追加する（research.md § R-005 参照）
- [X] T016 [US3] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` を修正し、`_horizontalAnchor`・`_verticalAnchor`・`_depthAnchor` フィールドと対応する公開プロパティ（`HorizontalAnchor`・`VerticalAnchor`・`DepthAnchor`）を追加する。プロパティ setter で `_isDirty = true` をセットする。`BuildParams()` でこれらの値を `MeshGenerationParams` に渡す（data-model.md § SolidText3DComponent（修正）、spec FR-004・FR-005 参照）
- [X] T017 [US3] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` を修正し、`HorizontalAnchor`・`VerticalAnchor`・`DepthAnchor` の 3 つのドロップダウンフィールドを Inspector に追加する（単一 27 択にしない。spec FR-004 参照）

### Tests for User Story 3

- [X] T018 [P] [US3] `Packages/com.masachuang.solidtext3d/Tests/Editor/LayoutEngineTests.cs` を新規作成し、`ApplyHorizontalLayout_EmptyGlyphs_DoesNotThrow`・`CalculateAnchorOffset_Center_ReturnsHalfExtents` テストを実装する（plan.md ステップ 4 参照）
- [X] T019 [P] [US3] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に `Build_WithCenterAnchor_BoundsSymmetric` テストを追記する（plan.md ステップ 5 参照）

**Checkpoint**: US3 完了。3 軸アンカーが独立して機能し、テキストが期待位置に配置される。

---

## Phase 6: User Story 4 — インスペクタ入力レイテンシの改善（Priority: P4）

**Goal**: Inspector のテキストフィールド入力中はメッシュ再生成が走らず、フォーカスアウトまたは Enter キーで確定時にのみ再生成される。

**Independent Test**: テキストフィールドに長い文字列を素早く入力しても入力遅延なく全文字が入力でき、フォーカスアウト時に 3D テキストが更新される。

### Implementation for User Story 4

- [X] T020 [US4] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` を修正し、`internal bool _suppressAutoRegenerate` フィールドを追加する。`LateUpdate()` の `_isDirty` チェック内に `if (_suppressAutoRegenerate) return;` を追加して、デバウンス中の自動再生成を抑制する（data-model.md § SolidText3DComponent（修正）、research.md § R-003 参照）
- [X] T021 [US4] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` を修正し、`EditorApplication.update` フックとタイムスタンプ（`_lastChangeTime`）を使ったデバウンスを実装する: テキストフィールドの変更を `EditorGUI.BeginChangeCheck()`/`EndChangeCheck()` で検知し `_lastChangeTime = EditorApplication.timeSinceStartup` を記録。`EditorApplication.update` で経過時間 >= 0.5 秒になったら `target._suppressAutoRegenerate = false` に戻して `RegenerateMesh()` を呼び出す。変更検知から確定まで `target._suppressAutoRegenerate = true` を維持する（research.md § R-003、spec FR-006 参照）

**Checkpoint**: US4 完了。Inspector でのテキスト入力が快適になる。

---

## Phase 7: User Story 5 — 頻繁な文字列更新に対するパフォーマンス改善（Priority: P5）

**Goal**: テキストが前フレームと同一の場合はメッシュ再生成をスキップし、ランタイム GC Alloc ゼロを維持する。

**Independent Test**: 毎フレーム同一テキストを設定するシナリオで Profiler の GC Alloc が 0 バイトであることを確認できる。

### Implementation for User Story 5

- [X] T022 [US5] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` を修正する: `private int _lastParamHash` フィールドを追加する。`RegenerateMesh()` の冒頭（フォントチェック・空文字列チェックより後）でパラメータハッシュ（`Text.GetHashCode() ^ _fontAsset.GetHashCode() ^ _horizontalAnchor.GetHashCode() ^ _verticalAnchor.GetHashCode() ^ _depthAnchor.GetHashCode() ^ _writingMode.GetHashCode() ^ _objectMode.GetHashCode()`）を計算し、前回と同一なら早期リターンする。`LateUpdate()` 内で `new`・LINQ・文字列連結は使用しない（data-model.md § SolidText3DComponent（修正）、research.md § R-007、spec FR-007 参照）

### Tests for User Story 5

- [X] T023 [P] [US5] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` に `RegenerateMesh_SameParams_SkipsRegeneration` テストを追記する（plan.md ステップ 7 参照）

**Checkpoint**: US5 完了。テキスト未変更時のフレームあたり GC Alloc が 0 バイトになる。

---

## Phase 8: User Story 6 — 文字ごとの個別オブジェクト化（Priority: P6）

**Goal**: Single モードと Per-Character モードを切り替えられ、Per-Character モードでは文字ごとに子 GameObject がオブジェクトプールで管理される。

**Independent Test**: Per-Character モードで "ABC" を設定すると 3 つの子 GameObject が生成され、"AB" に変更すると 3 つ目が非アクティブ化される。

### Implementation for User Story 6

- [X] T024 [US6] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` を修正し、`public List<Mesh> BuildPerCharacter(MeshGenerationParams p)` メソッドを追加する: `IsVisible == true` のグリフのみを対象に各文字の独立した `Mesh` を生成して返す（data-model.md § BuildPerCharacter メソッド設計 参照）
- [X] T025 [US6] `Packages/com.masachuang.solidtext3d/Runtime/CharacterObjectPool.cs` を新規作成する: `internal sealed class CharacterObjectPool` を実装する。コンストラクタで `Transform parent` を受け取る。`Sync(List<GlyphContour> visibleGlyphs, List<Mesh> perCharMeshes)` で子 GameObject を同期する（不足時のみ `new GameObject()` + `MeshFilter` + `MeshRenderer` を追加、超過時は `SetActive(false)`）。`DeactivateAll()`・`Destroy()`・`ActiveObjects` を実装する。`IsVisible == false` のグリフはスキップする（data-model.md § CharacterObjectPool、research.md § R-004、spec FR-009b 参照）
- [X] T026 [US6] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` を修正する: `_objectMode (ObjectMode)` フィールドと `ObjectMode` プロパティを追加する。`CharacterObjectPool _pool` フィールドを追加する。`RegenerateMesh()` に ObjectMode に応じた分岐を実装する: `PerCharacter` 時は `GlyphMeshBuilder.BuildPerCharacter()` の結果を `_pool.Sync()` に渡す。テキストが空文字列の場合は `_pool.DeactivateAll()`。モード切り替え時は `_pool.DeactivateAll()` または `_pool.Destroy()` を適切に処理する（data-model.md § 状態遷移 ObjectMode 切り替え、spec FR-008・FR-009b・FR-015 参照）
- [X] T027 [US6] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` を修正し、`ObjectMode` ドロップダウンフィールドを Inspector に追加する

### Tests for User Story 6

- [X] T028 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/CharacterObjectPoolTests.cs` を新規作成し、`Sync_MoreChars_CreatesNewChildren`・`Sync_FewerChars_DeactivatesExcess`・`Sync_SameCount_ReusesExistingChildren` テストを実装する（plan.md ステップ 6 参照）
- [X] T029 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に `BuildPerCharacter_ThreeChars_ReturnsThreeMeshes` テストを追記する（plan.md ステップ 5 参照）
- [X] T030 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` に `ObjectMode_PerCharacter_CreatesChildObjects` テストを追記する（plan.md ステップ 7 参照）

**Checkpoint**: US6 完了。文字単位のアニメーション・マテリアル制御が可能になる。

---

## Phase 9: User Story 7 — 縦書きテキストの対応（Priority: P7）

**Goal**: 縦書きモードで日本語文字が上から下・右から左に並び、アンカー・折り返し・ASCII 回転が正しく機能する。

**Independent Test**: 縦書きモードで「あいうえお」を設定すると文字が縦に並び、ColumnWidth を超えた場合に次の列（右から左）へ折り返す。

### Implementation for User Story 7

- [X] T031 [US7] `Packages/com.masachuang.solidtext3d/Runtime/LayoutEngine.cs` に `internal static void ApplyVerticalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)` メソッドを追加する: 文字を上から下へ配置（Y 座標を `AdvanceHeight` 分ずつ減算）。列幅 = `p.VerticalColumnWidth > 0 ? p.VerticalColumnWidth : p.FontSize * 1.1f`。列内で水平中央揃え。`MaxHeight` 超過時に次の列へ（X 座標を列幅分だけ左に移動）。`p.RotateAsciiInVertical == true` かつ ASCII 英数字の場合は `GlyphContour` に 90 度回転フラグを設定する（research.md § R-006、spec FR-011・FR-011b・FR-011c 参照）
- [X] T032 [US7] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` を修正し、`Build()` および `BuildPerCharacter()` 内で `params.WritingMode == WritingMode.Vertical` の場合に `LayoutEngine.ApplyVerticalLayout()` を、`Horizontal` の場合に `LayoutEngine.ApplyHorizontalLayout()` を呼び出すように分岐する
- [X] T033 [US7] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` を修正し、`_writingMode (WritingMode)`・`_maxWidth (float)`・`_maxHeight (float)`・`_verticalColumnWidth (float)`・`_rotateAsciiInVertical (bool)` フィールドと対応する公開プロパティを追加する。`BuildParams()` でこれらの値を `MeshGenerationParams` に渡す（data-model.md § SolidText3DComponent（修正）、spec FR-010・FR-011・FR-013・FR-014 参照）
- [X] T034 [US7] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` を修正し、`WritingMode` ドロップダウン・`Max Width`・`Max Height`・`Vertical Column Width`・`Rotate ASCII In Vertical` のフィールドを Inspector に追加する（`WritingMode == Vertical` の場合のみ縦書き専用フィールドを表示する `EditorGUI.indentLevel` グループ推奨）

### Tests for User Story 7

- [X] T035 [P] [US7] `Packages/com.masachuang.solidtext3d/Tests/Editor/LayoutEngineTests.cs` に `ApplyVerticalLayout_SingleChar_YIsNegative`・`ApplyVerticalLayout_ColumnOverflow_MovesToNextColumn` テストを追記する（plan.md ステップ 4 参照）
- [X] T036 [P] [US7] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に `Build_VerticalMode_YDecreases` テストを追記する（plan.md ステップ 5 参照）

**Checkpoint**: US7 完了。縦書きが正しく機能し、全 7 ユーザーストーリーが独立して動作する。

---

## Final Phase: Polish & 破壊的変更ドキュメント

**Purpose**: API ドキュメント整備、バージョン管理、Asset Store 配布準備

- [X] T037 `Packages/com.masachuang.solidtext3d/CHANGELOG.md` を更新し、v2.0.0 セクションに全 7 ユーザーストーリーの変更内容と破壊的変更（`public string Font` 削除・`MeshGenerationParams.FontPath` 削除）を記録する
- [X] T038 `Packages/com.masachuang.solidtext3d/Documentation~/BREAKING_CHANGES.md` を新規作成し、`[Obsolete]` を採用しなかった技術的理由（フォント参照の型が `string` → `UnityEngine.Object` へ根本的に変化し段階移行が技術的に不可能であること）と開発者による明示的承認経緯を詳述する（plan.md 憲法チェック IV 参照）

> **⚠️ 必須**: T038 が完了するまで Final Phase のチェックポイント通過は不可（plan.md 憲法チェック IV より）

**Final Checkpoint**: 全タスク完了。v2.0.0 リリース準備が整った状態。

---

## Dependencies（ユーザーストーリー間の依存関係）

```text
Phase 1 (Setup)
  └─→ Phase 2 (Foundational: T002–T007)
        ├─→ Phase 3 (US1: T008–T010) ← MVP
        │     └─→ Phase 4 (US2: T011–T013)
        ├─→ Phase 5 (US3: T014–T019)  ← Phase 2 完了後に並列実行可
        ├─→ Phase 6 (US4: T020–T021)  ← Phase 2 完了後に並列実行可
        ├─→ Phase 7 (US5: T022–T023)  ← Phase 2 完了後に並列実行可
        ├─→ Phase 8 (US6: T024–T030)  ← Phase 2 完了後に並列実行可
        └─→ Phase 9 (US7: T031–T036)  ← Phase 2 完了後に並列実行可
              └─→ Final Phase (T037–T038)
```

**ストーリー間の並列実行例**（Phase 2 完了後）:

- エージェント A: Phase 3 (US1) → Phase 4 (US2)
- エージェント B: Phase 5 (US3)
- エージェント C: Phase 6 (US4) → Phase 7 (US5)
- エージェント D: Phase 8 (US6)
- エージェント E: Phase 9 (US7)

---

## Implementation Strategy

| フェーズ | 戦略 |
| ------- | ---- |
| **MVP（Phase 1〜3）** | フォント参照方式の変更のみ。US1 単独で動作確認可能 |
| **Increment 2** | US2 追加（自動変換）。US1 + US2 で手動 .bytes 作業が完全不要になる |
| **Increment 3** | US3 + US4 + US5 の並列実装（UI 改善・パフォーマンス） |
| **Increment 4** | US6 + US7 の並列実装（新機能） |
| **リリース** | Final Phase 完了後に v2.0.0 としてリリース |

---

## Summary

| 項目 | 数値 |
| ---- | ---- |
| 総タスク数 | 38 |
| US1（フォントアタッチ） | 3 タスク (T008–T010) |
| US2（自動 .bytes 変換） | 3 タスク (T011–T013) |
| US3（アンカー指定） | 6 タスク (T014–T019) |
| US4（入力デバウンス） | 2 タスク (T020–T021) |
| US5（パフォーマンス改善） | 2 タスク (T022–T023) |
| US6（Per-Character モード） | 7 タスク (T024–T030) |
| US7（縦書き対応） | 6 タスク (T031–T036) |
| Foundational + Setup | 8 タスク (T001–T007) |
| Final Phase | 2 タスク (T037–T038) |
| 並列実行可能タスク [P] | 15 タスク |
| 推奨 MVP スコープ | Phase 1〜3（US1 のみ） |
