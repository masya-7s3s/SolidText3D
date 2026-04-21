# タスク: Solid Text 3D — レイアウト・フォント・パフォーマンス改善

**Input**: `specs/002-text-layout-improvements/` の設計文書  
**Branch**: `002-text-layout-improvements`  
**Date**: 2026-04-21  
**Prerequisites**: plan.md ✅ / spec.md ✅ / data-model.md ✅ / contracts/ ✅ / quickstart.md ✅

---

## フォーマット: `[ID] [P?] [Story?] 説明`

- **[P]**: 並列実行可（異なるファイル・未完了タスクに非依存）
- **[Story]**: 対応するユーザーストーリー（US1〜US7）
- 各タスクの説明には対象ファイルの正確なパスを記載

---

## Phase 1: セットアップ（共有インフラ）

**目的**: 既存の UPM パッケージ構造を前提とするため、追加のプロジェクト初期化は不要。
この機能では新規ファイルはすべて既存の `Runtime/`・`Editor/`・`Tests/` に配置する。

> _（新規プロジェクトスキャフォールドなし — Phase 2 の基盤タスクへ直接移行）_

---

## Phase 2: 基盤（全ユーザーストーリーの前提条件）

**目的**: 全ユーザーストーリーが依存する型定義・データモデル変更を先行実装する。

**⚠️ 重要**: このフェーズが完了するまで、いかなるユーザーストーリーも着手できない。

- [ ] T001 `Packages/com.masachuang.solidtext3d/Runtime/TextAnchorEnums.cs` を新規作成し、`HorizontalAnchor`・`VerticalAnchor`・`DepthAnchor`・`WritingMode`・`ObjectMode` の 5 つの enum を `MasaChuang.SolidText3D` 名前空間に定義する（data-model.md § 新規 Enum 型 参照）
- [ ] T002 `Packages/com.masachuang.solidtext3d/Runtime/MeshGenerationParams.cs` を更新する: `FontPath (string)` フィールドを削除し、`HorizontalAnchor`・`VerticalAnchor`・`DepthAnchor`・`WritingMode`・`MaxWidth`・`MaxHeight`・`VerticalColumnWidth`・`RotateAsciiInVertical` の 8 フィールドを追加する（data-model.md § MeshGenerationParams 参照）
- [ ] T003 [P] `Packages/com.masachuang.solidtext3d/Runtime/GlyphContour.cs` を更新する: `AdvanceHeight (float)`・`CharIndex (int)`・`IsVisible (bool)` の 3 フィールドを追加する（data-model.md § GlyphContour 参照）
- [ ] T004 `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` を修正する: `FontPath` 参照をすべて `FontData = File.ReadAllBytes(...)` に置き換え、T002 の破壊的変更に対応させる

**チェックポイント**: 基盤完了 — 以降の各フェーズは独立して着手可能。

---

## Phase 3: ユーザーストーリー 1 — フォントのインスペクタアタッチ (P1) 🎯 MVP

**目標**: インスペクタの Object フィールドに `.ttf`/`.otf` ファイルをアタッチするだけでフォントを指定できる。旧 `string Font` API を完全削除し、MAJOR バンプ（1.0.0 → 2.0.0）とする。

**独立テスト**: フォントフィールドにアセットをアタッチすると 3D テキストがそのフォントで表示される。フォント Missing 時は直前メッシュを維持し、警告を 1 回だけ出力することを単独で確認できる。

### ユーザーストーリー 1 のテスト

- [ ] T005 [P] [US1] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` に `FontAsset_Missing_MaintainsPreviousMesh` テストを追加する（FR-016: フォント Missing 時に直前メッシュ維持・LogWarning 1 回のみ）

### ユーザーストーリー 1 の実装

- [ ] T006 [US1] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` を修正する: `[SerializeField] string _font` と `public string Font` プロパティを完全削除し、`_fontAsset (UnityEngine.Object)`・`_fontBytesCache (TextAsset, HideInInspector)`・`_fontMissingWarningIssued (bool)` を追加する。`FontAsset` プロパティ（get/set）を実装し、`RegenerateMesh()` 内にフォント未設定（FR-012: メッシュ生成スキップ）・Missing（FR-016: 直前メッシュ維持・LogWarning 1 回）の処理を追加する（data-model.md § SolidText3DComponent 参照）
- [ ] T007 [US1] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` を修正する: `DrawDefaultInspector()` を廃止して手動描画に切り替え、`EditorGUILayout.ObjectField("Font Asset", ..., typeof(UnityEngine.Object), false)` でフォントフィールドを追加し、フォント未アタッチ時に `EditorGUILayout.HelpBox()` で警告を表示する（research.md § R-001 参照）

**チェックポイント**: ユーザーストーリー 1 完了 — インスペクタ Object フィールドでのフォント指定と Missing 動作を単独で検証可能。

---

## Phase 4: ユーザーストーリー 2 — フォントファイルの自動 .bytes 変換 (P2)

**目標**: `.ttf`/`.otf` ファイルをプロジェクトにインポートするだけで `.bytes` ファイルが自動生成される。ユーザーは手動変換不要。

**独立テスト**: `.ttf` ファイルをインポートして `.bytes` ファイルが `Assets/SolidText3DFonts/` に生成されることを単独で確認できる。

### ユーザーストーリー 2 のテスト

- [ ] T008 [P] [US2] `Packages/com.masachuang.solidtext3d/Tests/Editor/FontAssetPostprocessorTests.cs` を新規作成し、以下 3 テストを実装する: `OnPostprocess_TtfFile_CreatesBytesFile`（.ttf インポートで .bytes 生成）・`OnPostprocess_ExistingBytesFile_Skips`（既存 .bytes 再変換スキップ）・`OnPostprocess_IoError_LogsError`（I/O エラー時の LogError）

### ユーザーストーリー 2 の実装

- [ ] T009 [US2] `Packages/com.masachuang.solidtext3d/Editor/FontAssetPostprocessor.cs` を新規作成する: `AssetPostprocessor` を継承し、`OnPostprocessAllAssets` で `.ttf`/`.otf` を検知する。`Assets/SolidText3DFonts/{assetGuid}.bytes` へ一時ファイル経由のアトミック書き込み（FR-017）を実装し、`AssetDatabase.ImportAsset()` で登録する。シーン内の `SolidText3DComponent` を走査して `_fontBytesCache` を自動設定し `EditorUtility.SetDirty()` + `AssetDatabase.SaveAssets()` でシリアライズする（data-model.md § FontAssetPostprocessor 参照、research.md § R-002 参照）

**チェックポイント**: ユーザーストーリー 2 完了 — .ttf アタッチから .bytes 自動生成までの流れを単独で検証可能。

---

## Phase 5: ユーザーストーリー 3 — テキスト配置のアンカー指定 (P3)

**目標**: Horizontal / Vertical / Depth の 3 軸アンカーをそれぞれ独立したフィールドで設定し、メッシュをアンカー位置に合わせてオフセットできる。

**独立テスト**: 各軸のアンカーを切り替え、メッシュの bounds が原点に対して期待どおりにオフセットされることを単独で確認できる。

### ユーザーストーリー 3 のテスト

- [ ] T010 [P] [US3] `Packages/com.masachuang.solidtext3d/Tests/Editor/LayoutEngineTests.cs` を新規作成し、以下のテストを実装する: `ApplyHorizontalLayout_EmptyGlyphs_DoesNotThrow`・`CalculateAnchorOffset_Center_ReturnsHalfExtents`
- [ ] T011 [P] [US3] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に `Build_WithCenterAnchor_BoundsSymmetric` テストを追加する

### ユーザーストーリー 3 の実装

- [ ] T012 [US3] `Packages/com.masachuang.solidtext3d/Runtime/LayoutEngine.cs` を新規作成する: `internal static class LayoutEngine` に `ApplyHorizontalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)` と `CalculateAnchorOffset(Bounds meshBounds, MeshGenerationParams p)` を実装する。アンカーオフセット計算式（水平: Left=0, Center=-width/2, Right=-width; 垂直: Upper=-height, Middle=-height/2, Lower=0; 奥行き: Front=0, Center=-depth/2, Back=-depth）を適用する。既存 `GlyphMeshBuilder.ApplyLayout()` のロジックを移植し、`MaxWidth` による自動折り返しに対応する（research.md § R-005 参照）
- [ ] T013 [US3] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` を修正する: `ApplyLayout()` プライベートメソッドを削除して `LayoutEngine.ApplyHorizontalLayout()` 呼び出しに置き換え、`GetFontBytes()` から `FontPath` 参照を削除する。`Build()` 内で `MeshExtruder.Build()` 後に `LayoutEngine.CalculateAnchorOffset()` を呼び出して全頂点にオフセットを加算する（data-model.md § データフロー概要 参照）
- [ ] T014 [US3] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` を修正する: `_horizontalAnchor`・`_verticalAnchor`・`_depthAnchor` の 3 フィールドとそれぞれのプロパティ（`HorizontalAnchor`・`VerticalAnchor`・`DepthAnchor`）、`_maxWidth`・`_maxHeight` フィールドと `MaxWidth`・`MaxHeight` プロパティを追加する。プロパティ setter で `_isDirty = true` を設定する
- [ ] T015 [US3] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` に `HorizontalAnchor`・`VerticalAnchor`・`DepthAnchor` の 3 つのドロップダウンフィールドと `MaxWidth`・`MaxHeight` の数値フィールドを手動描画で追加する（FR-004 参照）

**チェックポイント**: ユーザーストーリー 3 完了 — 3 軸アンカー設定とメッシュ位置オフセットを単独で検証可能。

---

## Phase 6: ユーザーストーリー 4 — インスペクタ入力レイテンシの改善 (P4)

**目標**: テキストフィールド入力中はメッシュ再生成を抑制し、Enter キーまたはフォーカスアウトのタイミングでのみ再生成する。

**独立テスト**: テキストフィールドに長い文字列を素早く入力し、入力中に再生成が走らないことを確認できる。

### ユーザーストーリー 4 の実装

- [ ] T016 [US4] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` に `_suppressAutoRegenerate (bool)` フィールドと `SuppressAutoRegenerate` プロパティ（get/set）を追加し、`LateUpdate()` 内で `_suppressAutoRegenerate` が true の場合は `RegenerateMesh()` を呼び出さないよう制御を追加する（FR-006、research.md § R-003 参照）
- [ ] T017 [US4] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` のテキストフィールドに `EditorGUI.BeginChangeCheck()` / `EndChangeCheck()` + `EditorApplication.update` によるデバウンスを実装する: 入力検知時に `_target.SuppressAutoRegenerate = true` を設定し、フォーカスアウトまたは Enter キー確定後に `RegenerateMesh()` を呼び出して `SuppressAutoRegenerate = false` に戻す（research.md § R-003 参照）

**チェックポイント**: ユーザーストーリー 4 完了 — 入力デバウンスによるレイテンシ改善を単独で確認可能（SC-001: 50ms 未満の入力遅延）。

---

## Phase 7: ユーザーストーリー 5 — 頻繁な文字列更新のパフォーマンス改善 (P5)

**目標**: テキストが毎フレーム変更される UI ユースケースで GC Alloc を抑制し、同一パラメータ時のメッシュ再生成をスキップして 60fps を維持する。

**独立テスト**: 毎フレームテキストを変更するシナリオで再生成コストを計測し、同一テキスト時にスキップが発動することを単独で確認できる。

### ユーザーストーリー 5 のテスト

- [ ] T018 [P] [US5] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` に `RegenerateMesh_SameParams_SkipsRegeneration` テストを追加する（同一パラメータハッシュ時に再生成がスキップされること）

### ユーザーストーリー 5 の実装

- [ ] T019 [US5] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` に `_lastParamHash (int)` フィールドを追加し、`RegenerateMesh()` の冒頭で現在パラメータのハッシュと `_lastParamHash` を比較して同一の場合は早期リターンするロジックを実装する（FR-007、research.md § R-007 参照）

**チェックポイント**: ユーザーストーリー 5 完了 — テキスト未変更時の GC Alloc ゼロおよびフレームレート維持を単独で検証可能。

---

## Phase 8: ユーザーストーリー 6 — 文字ごとの個別オブジェクト化 (P6)

**目標**: `ObjectMode.PerCharacter` 時に各文字に対応する子 GameObject を生成しオブジェクトプールで管理する。可視文字のみが対象で、切り替え時はプールを正しく制御する。

**独立テスト**: Per-Character モードで "ABC" を設定し、3 つの子 GameObject が生成されることを単独で確認できる。

### ユーザーストーリー 6 のテスト

- [ ] T020 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/CharacterObjectPoolTests.cs` を新規作成し、以下 3 テストを実装する: `Sync_MoreChars_CreatesNewChildren`・`Sync_FewerChars_DeactivatesExcess`・`Sync_SameCount_ReusesExistingChildren`
- [ ] T021 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に `BuildPerCharacter_ThreeChars_ReturnsThreeMeshes` テストを追加する
- [ ] T022 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` に `ObjectMode_PerCharacter_CreatesChildObjects` テストを追加する

### ユーザーストーリー 6 の実装

- [ ] T023 [US6] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` に `BuildPerCharacter(MeshGenerationParams p)` メソッドを追加する: `IsVisible == true` のグリフのみを対象に、1 文字ずつ `GlyphContourBuilder` + `MeshExtruder` で個別 Mesh を生成して `List<Mesh>` として返す（data-model.md § BuildPerCharacter 設計 参照）
- [ ] T024 [US6] `Packages/com.masachuang.solidtext3d/Runtime/CharacterObjectPool.cs` を新規作成する: `internal sealed class CharacterObjectPool` に `Sync(List<GlyphContour> visibleGlyphs, List<Mesh> perCharMeshes)` を実装する。文字数増加時のみ新規 GameObject を生成（`MeshFilter` + `MeshRenderer` を `AddComponent`）し、文字数減少時は余剰を `SetActive(false)` で非アクティブ化する。Destroy は行わない（FR-009b、research.md § R-004 参照）
- [ ] T025 [US6] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` に `_objectMode (ObjectMode)` フィールドと `ObjectMode` プロパティを追加し、`CharacterObjectPool` インスタンスを保持する `_characterPool` フィールドを追加する。`ObjectMode` 変更時の切り替えロジック（PerCharacter → SingleObject 切り替え時に子 GameObject を全て非アクティブ化、SingleObject → PerCharacter 切り替え時にプールを初期化）を実装する（data-model.md § 状態遷移: ObjectMode 切り替え 参照）
- [ ] T026 [US6] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` に `ObjectMode` の選択ドロップダウンを手動描画で追加する（FR-008 参照）

**チェックポイント**: ユーザーストーリー 6 完了 — Per-Character モードの子 GameObject 生成・プール管理・モード切り替えを単独で検証可能。

---

## Phase 9: ユーザーストーリー 7 — 縦書きテキストの対応 (P7)

**目標**: `WritingMode.Vertical` 時に文字を上から下・列を右から左に並べる縦書きレイアウトを実現する。アンカー指定・Per-Character モードとの同時使用に対応する。

**独立テスト**: 縦書きモードで「あいうえお」を設定し、文字が縦に並ぶことを単独で確認できる。

### ユーザーストーリー 7 のテスト

- [ ] T027 [P] [US7] `Packages/com.masachuang.solidtext3d/Tests/Editor/LayoutEngineTests.cs` に以下テストを追加する: `ApplyVerticalLayout_SingleChar_YIsNegative`（縦書き時に Y 座標が負方向に進むこと）
- [ ] T028 [P] [US7] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` に `Build_VerticalMode_YDecreases` テストを追加する

### ユーザーストーリー 7 の実装

- [ ] T029 [US7] `Packages/com.masachuang.solidtext3d/Runtime/LayoutEngine.cs` に `ApplyVerticalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)` を実装する: 各文字を上から下（Y は減少方向）に並べ、`MaxHeight` を超えたら次の列（X は左方向）へ折り返す。各列内の文字は `VerticalColumnWidth`（0 の場合は `FontSize × 1.1f`）を基準に水平中央揃えとする。`RotateAsciiInVertical` が true の場合は ASCII 英数字グリフの回転フラグを設定する。複数行（改行コード）は次列への折り返しとして処理する（FR-010、FR-011、FR-011b、FR-011c、research.md § R-006 参照）
- [ ] T030 [US7] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` を修正する: `Build()` / `BuildPerCharacter()` 内で `MeshGenerationParams.WritingMode` を参照し、`Vertical` の場合は `LayoutEngine.ApplyVerticalLayout()` を呼び出すよう分岐を追加する
- [ ] T031 [US7] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` に `_writingMode (WritingMode)` フィールドと `WritingMode` プロパティ、`_verticalColumnWidth (float)` フィールド、`_rotateAsciiInVertical (bool)` フィールドを追加する。プロパティ setter で `_isDirty = true` を設定する
- [ ] T032 [US7] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` に `WritingMode` ドロップダウン、`VerticalColumnWidth` 数値フィールド、`RotateAsciiInVertical` トグルを手動描画で追加する（FR-010、FR-011b、FR-011c 参照）

**チェックポイント**: ユーザーストーリー 7 完了 — 縦書きレイアウト、列折り返し、ASCII 回転オプション、アンカーとの組み合わせを単独で検証可能。

---

## Final Phase: ポリッシュ・横断的関心事

**目的**: バージョン更新、CHANGELOG 記載、パッケージ最終整備。

- [ ] T033 [P] `Packages/com.masachuang.solidtext3d/package.json` の `"version"` を `"1.0.0"` から `"2.0.0"` に更新する（破壊的変更: `string Font` 削除、contracts/SolidText3DComponent-API.md 参照）
- [ ] T034 [P] `Packages/com.masachuang.solidtext3d/CHANGELOG.md` に v2.0.0 エントリを追記する: 破壊的変更（`Font (string)` 削除・`FontAsset (Object)` 追加）、追加機能（アンカー・縦書き・Per-Character・自動 .bytes 変換・デバウンス・パフォーマンス改善）を記載する（contracts/SolidText3DComponent-API.md § CHANGELOG エントリ 参照）

---

## 依存関係グラフ（ユーザーストーリー間）

```text
Phase 2 (基盤: T001-T004)
│
├──► Phase 3: US1 フォント Inspector (T005-T007) ← MVP 候補
│        │
│        └──► Phase 4: US2 .bytes 自動変換 (T008-T009)
│
├──► Phase 5: US3 アンカー指定 (T010-T015)
│        │
│        ├──► Phase 6: US4 入力デバウンス (T016-T017)
│        │
│        ├──► Phase 7: US5 パフォーマンス (T018-T019)
│        │
│        ├──► Phase 8: US6 Per-Character (T020-T026)
│        │
│        └──► Phase 9: US7 縦書き (T027-T032)
│
└──► Final Phase: ポリッシュ (T033-T034) ← 全フェーズ完了後
```

**独立実行可能なフェーズ**: Phase 3 (US1) と Phase 5 (US3) は Phase 2 完了後に並列着手可能。
Phase 4・5・6・7 (US4-US7) はそれぞれ Phase 5 (US3) の完了を前提とする。

---

## 並列実行例（各ユーザーストーリー内）

### Phase 2（基盤）内の並列実行

```text
T001 (TextAnchorEnums.cs 新規作成)
    ↓
T002 (MeshGenerationParams.cs 更新) ┐ 並列実行可
T003 (GlyphContour.cs 更新)         ┘
    ↓（両方完了後）
T004 (GlyphMeshBuilderTests.cs 修正)
```

### Phase 5（US3）内の並列実行

```text
T010 (LayoutEngineTests.cs 新規) ┐ 並列実行可
T011 (GlyphMeshBuilderTests 追記) ┘
    ↓（テスト作成後）
T012 (LayoutEngine.cs 新規作成)
    ↓
T013 (GlyphMeshBuilder.cs 修正)
    ↓
T014 (SolidText3DComponent アンカーフィールド追加) ┐ 並列実行可
T015 (SolidText3DInspector アンカー UI 追加)       ┘
```

### Phase 8（US6）内の並列実行

```text
T020 (CharacterObjectPoolTests 新規) ┐
T021 (GlyphMeshBuilderTests 追記)    ├ 並列実行可
T022 (SolidText3DComponentTests 追記) ┘
    ↓（テスト作成後）
T023 (GlyphMeshBuilder.BuildPerCharacter 追加)
    ↓
T024 (CharacterObjectPool.cs 新規作成) ┐ 並列実行可
T025 (SolidText3DComponent ObjectMode追加) ┘
    ↓
T026 (SolidText3DInspector ObjectMode UI 追加)
```

---

## 実装戦略

### MVP スコープ（推奨最初のデリバリー）

**Phase 2 + Phase 3（US1）** のみで MVP として成立する:

1. `TextAnchorEnums.cs` の定義（T001）
2. `MeshGenerationParams` の破壊的変更対応（T002-T004）
3. `SolidText3DComponent` の Font フィールド変更（T005-T006）
4. `SolidText3DInspector` の Object フィールド追加（T007）

これだけで「文字列型フォント名 → Object フィールドへの移行」という最大の UX 課題が解消される。

### インクリメンタルデリバリー順序

| リリース候補 | 含まれるフェーズ | 提供価値 |
| ----------- | --------------- | ------- |
| v2.0.0-alpha.1 | Phase 2 + 3 | フォント Inspector アタッチ（破壊的変更） |
| v2.0.0-alpha.2 | + Phase 4 | .bytes 自動変換 |
| v2.0.0-beta.1 | + Phase 5 + 6 | アンカー指定 + デバウンス |
| v2.0.0-beta.2 | + Phase 7 + 8 | パフォーマンス + Per-Character |
| v2.0.0 | + Phase 9 + Final | 縦書き + CHANGELOG + バージョン更新 |

---

## フォーマット検証

全タスクが以下の形式に準拠していることを確認:

- ✅ すべてのタスクが `- [ ]` チェックボックスで始まる
- ✅ 全タスクに T001〜T034 の連番 ID がある
- ✅ 並列実行可能タスクには `[P]` マーカーがある
- ✅ ユーザーストーリーフェーズのタスクには `[US1]`〜`[US7]` ラベルがある
- ✅ 全タスクに正確なファイルパスが含まれる
- ✅ セットアップ・基盤フェーズのタスクにはストーリーラベルがない
- ✅ 最終フェーズのタスクにはストーリーラベルがない
