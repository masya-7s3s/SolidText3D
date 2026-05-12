# Tasks: テキストアウトライン生成

**Input**: Design documents from `/specs/003-text-outline/`
**Prerequisites**: plan.md ✅ / spec.md ✅ / research.md ✅ / data-model.md ✅ / contracts/ ✅ / quickstart.md ✅
**Branch**: `003-text-outline` | **Date**: 2026-04-23 | **SemVer**: `2.0.0 → 2.1.0`

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: 並行実行可能（異なるファイル、依存なし）
- **[Story]**: 対応ユーザーストーリー（US1〜US4）
- **🔧 手作業**: Unity Editor や外部ツールでの手動操作が必要なタスク

---

## Phase 1: Setup（Clipper2 依存のセットアップ）

**Purpose**: Clipper2 DLL をプロジェクトに追加し参照を設定する

> ⚠️ **フェーズ 1 はすべて Phase 2 以降の前提条件**。完了前に次フェーズへ進まないこと。

- [X] T001 🔧 NuGet から `Clipper2` v1.4.x パッケージをダウンロードし `Clipper2Lib.dll` を抽出する（手順: `dotnet add package Clipper2` または NuGet サイトから `.nupkg` を取得し `lib/netstandard2.0/Clipper2Lib.dll` を展開）
- [X] T002 🔧 抽出した `Clipper2Lib.dll` を `Packages/com.masachuang.solidtext3d/Runtime/Plugins/Clipper2Lib.dll` にコピーし、Unity Editor を開いてインポートエラーがないことを確認する
- [X] T003 `Packages/com.masachuang.solidtext3d/Runtime/com.masachuang.solidtext3d.Runtime.asmdef` の `precompiledReferences` 配列に `"Clipper2Lib.dll"` を追加する
- [X] T004 `Packages/com.masachuang.solidtext3d/Third Party Notices.md` に Clipper2 ライセンスブロックを追記する（バージョン: 1.4.x、Boost Software License 1.0、URL: <https://github.com/AngusJohnson/Clipper2）>（ファイルが存在しない場合は新規作成する）

**Checkpoint**: Unity Editor でコンパイルエラーがなく、`Clipper2Lib` が Plugins フォルダに表示されること

---

## Phase 2: Foundational（共通データ型）

**Purpose**: US1〜US4 すべてが依存する enum とデータクラスを先行作成する

> ⚠️ **CRITICAL**: このフェーズ完了前に US フェーズを開始しないこと

- [X] T005 [P] `OutlineDisplayMode` enum を新規作成する（`Donut`（ドーナツモード） / `BackFilled`（裏面埋めモード）の 2 値、XML ドキュメントコメント付き）: `Packages/com.masachuang.solidtext3d/Runtime/OutlineDisplayMode.cs`
- [X] T006 [P] `OutlineSettings` シリアライズ可能クラスを新規作成する（`Enabled`, `OffsetAmount`, `Thickness`, `Material`, `DisplayMode` フィールド、data-model.md のデフォルト値・制約に従う）: `Packages/com.masachuang.solidtext3d/Runtime/OutlineSettings.cs`

**Checkpoint**: Unity でコンパイルが通り、`OutlineSettings` と `OutlineDisplayMode` が Inspector に表示されること

---

## Phase 3: User Story 1 — アウトライン基本表示（Priority: P1）🎯 MVP

**Goal**: インスペクターでアウトラインを有効化しオフセット量を設定すると、文字の外側にアウトライン形状が表示される

**Independent Test**: Inspector で Enabled=true・OffsetAmount=0.05 に設定してシーンを再生し、`"__OutlineMesh__"` 子 GO が生成されてアウトライン形状が表示されること

### テスト先行記述（テストファースト — 実装前に RED 確認必須）

- [X] T007 [US1] `OutlineContourBuilderTests.cs` を新規作成する。以下の 6 テストケースを記述し、実装前に失敗することを確認できる状態にする: ①正方形グリフへの正オフセットで結果パス面積が増加する、②「O」字形グリフへの大オフセットで穴パスが除去され外周 1 本のみが返る、③`offsetAmountEm=0` で元グリフと同一形状が返る、④オフセット量を 2 倍にすると結果輪郭の外周長増加率が ±15% 誤差以内で比例する（SC-001 比例性検証）、⑤em 空間スケール係数 ×1000 で変換したオフセット量が正しい Unity 単位値に戻ること（FR-011 変換精度）、⑥日本語グリフ（多重輪郭、例: 「あ」）への大オフセットで外周パスが 1 本のみ返る（SC-002 日本語グリフカバレッジ）: `Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineContourBuilderTests.cs`
- [ ] T008 🔧 [US1] Unity Editor の **Window > General > Test Runner** を開き、`OutlineContourBuilderTests` の 6 テストがすべて **RED（失敗）** であることを確認する（実装前であるため失敗は期待通り）

### OutlineContourBuilder 実装

- [X] T009 [US1] `OutlineContourBuilder` 静的クラスを新規実装する（`Clipper2Lib.ClipperOffset` + `JoinType.Round` + `EndType.Polygon` でオフセット演算、`Clipper.Area()` による CCW/CW 判定で穴パス除去、em 空間 ×1000 スケール係数の変換ロジックを含む）: `Packages/com.masachuang.solidtext3d/Runtime/OutlineContourBuilder.cs`
- [ ] T010 🔧 [US1] Test Runner で `OutlineContourBuilderTests` の 6 テストがすべて **GREEN（成功）** であることを確認する

### OutlineMeshBuilder テスト先行記述（テストファースト — 実装前に RED 確認必須）

- [X] T034 [US1] `OutlineMeshBuilderTests.cs` を新規作成する。以下の 4 テストケースを記述し、実装前に失敗することを確認できる状態にする: ①有効な輪郭入力に対して `OutlineMeshBuilder.Build()` が頂点数 > 0 のメッシュを返す、②`Thickness > 0` のとき側面頂点の Z 座標範囲が `[0, -Thickness]` に一致する、③`Thickness=0` のとき側面・裏面頂点が生成されない（spec.md Edge Case 準拠）、④`DisplayMode.Donut`（デフォルト）時に裏面ポリゴンが生成されない（FR-006 基本動作確認）: `Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineMeshBuilderTests.cs`
- [ ] T035 🔧 [US1] Unity Editor の **Window > General > Test Runner** を開き、`OutlineMeshBuilderTests` の 4 テストがすべて **RED（失敗）** であることを確認する（実装前であるため失敗は期待通り）

### OutlineMeshBuilder 実装

- [X] T011 [US1] `OutlineMeshBuilder` 静的クラスを新規実装する（表面ポリゴン: `Z=0`、オフセット輪郭 + 元グリフ輪郭を穴として EvenOdd で LibTessDotNet 三角分割；側面: オフセット輪郭外周に沿ったクワッドストリップ `Z=0〜-Thickness`）: `Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs`

### SolidText3DComponent ライフサイクル テスト先行記述（テストファースト — 実装前に RED 確認必須）

- [X] T036 [US1] `OutlineChildGOTests.cs` を新規作成する。以下の 2 テストケースを記述し、実装前に失敗することを確認できる状態にする（Phase 5 で残り 2 テストケースを追記）: ①`OutlineEnabled=true` 後に `transform.Find("__OutlineMesh__")` が非 null を返す、②`OutlineEnabled=false` 後に `"__OutlineMesh__"` 子 GO が消滅する: `Packages/com.masachuang.solidtext3d/Tests/Runtime/OutlineChildGOTests.cs`
- [ ] T037 🔧 [US1] Unity Editor の **Window > General > Test Runner** を開き、`OutlineChildGOTests` の 2 テストがすべて **RED（失敗）** であることを確認する（実装前であるため失敗は期待通り）

### SolidText3DComponent 実装

- [X] T012 [US1] `SolidText3DComponent` に `_outline`（`OutlineSettings`）・`_outlineChild`・`_outlineMeshFilter`・`_outlineRenderer` SerializeField を追加し、`CreateOutlineChild()`・`DestroyOutlineChildIfExists()`・`UpdateOutlineMesh()`・`DestroyOutlineChild()` プライベートメソッドを実装する（子 GO 名: `"__OutlineMesh__"`、R-004 ライフサイクル仕様に従う）: `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs`
- [X] T013 [US1] `SolidText3DComponent` に `OutlineEnabled` / `OutlineOffset` パブリックプロパティを追加し、`LateUpdate()` の `RegenerateMesh()` 呼び出し直後に `UpdateOutlineMesh()` を追加する（既存の `_isDirty` フラグを共有）: `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs`
- [ ] T014 🔧 [US1] Unity Editor でシーンを再生し、Inspector から Enabled=true・OffsetAmount=0.05 を設定して `"__OutlineMesh__"` 子 GO が Hierarchy に現れ、アウトライン形状が文字の外側に表示されることを目視確認する

**Checkpoint**: US1 独立動作確認 — アウトライン基本表示が機能すること

---

## Phase 4: User Story 2 — アウトライン表示モード切り替え（Priority: P2）

**Goal**: ドーナツモードと裏面埋めモードを切り替えると、裏面の形状が正しく変化する。正面の見た目は両モードで変わらない。

**Independent Test**: DisplayMode を `Donut` / `BackFilled` それぞれで設定し、オブジェクトを回転させて裏面の外観が仕様と一致することを確認できること

### テスト先行記述（テストファースト — 実装前に RED 確認必須）

- [X] T029 [US2] `OutlineMeshBuilderTests.cs` にモード切り替え検証テストケースを追記する。以下の 4 テストケースを記述し、実装前に失敗することを確認できる状態にする: ①Donut モード時に裏面ポリゴンが生成されない（BackFilled 時と比較して三角形数が少ない）、②BackFilled モード時に裏面ポリゴンが生成される、③両モードで表面頂点の最大 Z 座標が同一（SC-003）、④BackFilled モードで任意の厚さ組み合わせ時にアウトライン裏面 Z 座標が `-(max(bodyExtrusionDepth, thickness) + Z_FIGHT_EPSILON)` と一致する（SC-004）: `Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineMeshBuilderTests.cs`
- [ ] T030 🔧 [US2] Unity Editor の **Window > General > Test Runner** を開き、`OutlineMeshBuilderTests` の追記した 4 テストがすべて **RED（失敗）** であることを確認する（実装前であるため失敗は期待通り）

### 実装

- [X] T015 [US2] `OutlineMeshBuilder` に `BackFilled` モードの裏面メッシュ生成ロジックを追加する（裏面ポリゴン: Z=-backZ、オフセット輪郭全体を穴なし LibTessDotNet 三角分割、`backZ = max(bodyExtrusionDepth, Thickness) + Z_FIGHT_EPSILON(0.0001f)` の計算式に従う）: `Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs`
- [X] T016 [US2] `SolidText3DComponent` に型 `OutlineDisplayMode` のパブリックプロパティ `OutlineDisplayMode` を追加する（contracts/SolidText3DComponent-API.md の API 仕様参照）: `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs`
- [ ] T017 🔧 [US2] Unity Editor で ①Donut モードでオブジェクトを背面から見てリング状（中央が透ける）であること、②BackFilled モードで背面が塗りつぶされていること、③両モードで正面の表面形状・基準位置が一致していることを目視確認する。BackFilled モードで Zファイティングが発生しないことも確認する（SC-003・SC-004）

**Checkpoint**: US2 独立動作確認 — 両モードの裏面形状が仕様通りであること

---

## Phase 5: User Story 3 — アウトラインの生成/削除切り替え（Priority: P2）

**Goal**: インスペクターのトグルをオフにすると子 GO が破棄され、再度オンにすると子 GO が新たに生成される。設定値は保持される。

**Independent Test**: Inspector でトグルをオフ → Hierarchy から `"__OutlineMesh__"` が消え、再度オン → 新たに生成され前回の設定が反映されること

### テスト記述

- [X] T018 [US3] `OutlineChildGOTests.cs` に残り 2 テストケースを追記する（T036 で①②を先行作成済み）: ③子 GO の `MeshRenderer.sharedMaterial` が文字本体の `MeshRenderer.sharedMaterial` と異なるインスタンスである（`OutlineMaterial` 設定時）、④`OutlineEnabled` を false→true 切り替え後に前回の `OutlineOffset` 値が維持されている: `Packages/com.masachuang.solidtext3d/Tests/Runtime/OutlineChildGOTests.cs`

### 実装確認・補完

- [X] T019 [US3] T036/T037 で先行作成した `OutlineChildGOTests` の①②テストを GREEN にする最終確認と、T012/T013 で実装した `SolidText3DComponent` のライフサイクルロジックの補完を行う。`OutlineEnabled` セッターが以下を実装していることをテスト結果で検証し、不足があれば補完する：`false` 時に `DestroyOutlineChildIfExists()` 呼び出し、`true` 時に `CreateOutlineChild()` + `UpdateOutlineMesh()` 呼び出し、`OnDestroy()` での `DestroyOutlineChild()` 呼び出し: `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs`
- [ ] T020 🔧 [US3] Test Runner で `OutlineChildGOTests` の 4 テストがすべて **GREEN** であることを確認する
- [ ] T021 🔧 [US3] Unity Editor で Inspector のトグルをオフ/オンを繰り返し、Hierarchy の子 GO が消え・再生成されることを目視確認する

**Checkpoint**: US3 独立動作確認 — 生成/削除切り替えが正しく機能すること

---

## Phase 6: User Story 4 — アウトラインの厚さとマテリアル設定（Priority: P2）

**Goal**: アウトラインの Z 軸方向の厚さとマテリアルを文字本体と独立して設定できる

**Independent Test**: `OutlineThickness` を文字本体と異なる値に設定し専用マテリアルを割り当てると、アウトラインが独自の外観で表示されること。文字本体のマテリアルを変更してもアウトラインには影響しないこと。

### テスト先行記述（テストファースト — 実装前に RED 確認必須）

- [X] T031 [US4] `OutlinePropertyTests.cs` を新規作成する。以下の 4 テストケースを記述し、実装前に失敗することを確認できる状態にする: ①`OutlineSettings.Thickness` に任意の値を設定すると `OutlineMeshBuilder.Build()` の生成メッシュの側面頂点 Z 範囲が指定厚さと一致する、②`OutlineMaterial` に専用マテリアルを設定すると `_outlineRenderer.sharedMaterial` が同インスタンスを指す、③`OutlineMaterial` が null のとき `_outlineRenderer.sharedMaterial` が文字本体の `sharedMaterial` にフォールバックされる、④`Thickness=0` のとき `OutlineMeshBuilder.Build()` が側面・裏面頂点を含まないメッシュを返す（spec.md Edge Case 準拠）: `Packages/com.masachuang.solidtext3d/Tests/Editor/OutlinePropertyTests.cs`
- [ ] T032 🔧 [US4] Unity Editor の **Window > General > Test Runner** を開き、`OutlinePropertyTests` の 4 テストがすべて **RED（失敗）** であることを確認する（実装前であるため失敗は期待通り）

### 実装

- [X] T022 [P] [US4] `SolidText3DComponent` に `OutlineThickness` パブリックプロパティを追加する（セッターでダーティフラグを立てる）: `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs`
- [X] T023 [P] [US4] `SolidText3DComponent` に `OutlineMaterial` パブリックプロパティを追加する（セッターで `_outlineRenderer.sharedMaterial` を更新し、null 時は文字本体の MeshRenderer.sharedMaterial を参照するフォールバックロジックを含む）: `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs`
- [ ] T024 🔧 [US4] Unity Editor で ①`OutlineThickness` を文字本体の `ExtrusionDepth` と異なる値に設定してアウトライン厚さが独立して反映されること、②専用マテリアルを割り当てて正しく適用されること、③文字本体のマテリアルを変更してもアウトラインのマテリアルが変化しないことを目視確認する（SC-005）

**Checkpoint**: US4 独立動作確認 — 厚さとマテリアルが文字本体と独立して制御できること

---

## Phase 7: Polish & 横断的関心事

**Purpose**: Inspector UI の整備、バージョン管理、最終動作確認

- [X] T025 [P] `SolidText3DInspector.cs` の Inspector 描画部に「Outline」折りたたみセクションを追加する（`Enabled` チェックボックス、`OffsetAmount`・`Thickness` フィールド、`DisplayMode` ドロップダウン、`Material` オブジェクトフィールドの順に表示、contracts/SolidText3DComponent-API.md の Inspector UI レイアウト参照）: `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs`
- [X] T026 [P] `package.json` のバージョンフィールドを `"2.0.0"` → `"2.1.0"` に変更する: `Packages/com.masachuang.solidtext3d/package.json`
- [X] T027 [P] `CHANGELOG.md` に v2.1.0 エントリを追記する（追加: `OutlineSettings`・`OutlineDisplayMode`・`OutlineContourBuilder`・`OutlineMeshBuilder` の新規追加、`SolidText3DComponent` へのアウトライン関連プロパティ追加、Clipper2 依存追加；ファイルが存在しない場合は新規作成する）: `Packages/com.masachuang.solidtext3d/CHANGELOG.md`
- [X] T033 [P] `OutlineContourBuilder` および `OutlineMeshBuilder` のメッシュ再生成ホットパスに `Profiler.BeginSample` / `Profiler.EndSample` を追加し、Unity Editor のプロファイラーで**メッシュ再生成が行われない通常フレーム（`_isDirty=false` パス）における GC アロケーションがゼロ**であることを計測・確認して結果をソースコードコメントに記録する（憑法 V 準拠：メッシュ生成時は頂点配列確保のため GC アロケーションが発生することは許容される）: `Packages/com.masachuang.solidtext3d/Runtime/OutlineContourBuilder.cs`, `Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs`
- [ ] T038 🔧 Unity Test Framework のカバレッジレポートまたは手動集計で、ランタイムロジックのテストカバレッジが **80% 以上** であることを確認する（憲法 III 準拠）
- [ ] T028 🔧 `quickstart.md` に記載されているシナリオ（Inspector からの設定・コードからの設定・Donut/BackFilled モードの動作・厚さ設定ケース 1 & 2）をすべて Unity Editor で実施し、期待通りに動作することを最終確認する

**Checkpoint**: 全ユーザーストーリー完了・バージョン 2.1.0 リリース準備完了

---

## Dependencies & Execution Order

### フェーズ間依存関係

```text
Phase 1: Setup
  ↓ （Clipper2 DLL が存在しないと後続全フェーズでコンパイルエラー）
Phase 2: Foundational
  ↓ （OutlineDisplayMode / OutlineSettings がないと US フェーズでコンパイルエラー）
Phase 3: US1 (P1 — MVP)
Phase 4: US2 (P2 — US1 の OutlineMeshBuilder に追加実装)
Phase 5: US3 (P2 — US1 の SolidText3DComponent に追加実装)
Phase 6: US4 (P2 — US1 の SolidText3DComponent に追加実装)
  ↓
Phase 7: Polish
```

### ユーザーストーリー間依存関係

| フェーズ | 依存 |
| ------- | ---- |
| US1 (Phase 3) | Phase 1 + Phase 2 完了後に開始可能 |
| US2 (Phase 4) | Phase 3 完了後（`OutlineMeshBuilder` への追記のため） |
| US3 (Phase 5) | Phase 3 完了後（`SolidText3DComponent` への追記のため） |
| US4 (Phase 6) | Phase 3 完了後（`SolidText3DComponent` への追記のため。US3 と並行可能） |
| Polish (Phase 7) | US1〜US4 完了後 |

### フェーズ内並行実行

- T005 と T006 は独立しており並行実行可能 (Phase 2)
- T022 と T023 は独立しており並行実行可能 (Phase 6)
- T025、T026、T027 は独立しており並行実行可能 (Phase 7)

---

## 手作業タスク一覧（🔧）

| タスク ID | 作業内容 | ツール |
| --------- | ------- | ------ |
| T001 | Clipper2 NuGet DLL 抽出 | `dotnet` CLI または NuGet サイト |
| T002 | DLL 配置 + Unity Editor でインポート確認 | Unity Editor（エラーなし確認） |
| T008 | Test Runner で T007 テストが RED であることを確認 | Unity Test Runner |
| T010 | Test Runner で T009 テストが GREEN であることを確認 | Unity Test Runner |
| T035 | Test Runner で T034 テストが RED であることを確認 | Unity Test Runner |
| T037 | Test Runner で T036 テストが RED であることを確認 | Unity Test Runner |
| T014 | アウトライン基本表示を目視確認 | Unity Editor（シーンビュー） |
| T017 | ドーナツ/裏面埋めモードを目視確認 | Unity Editor（シーンビュー） |
| T030 | Test Runner で T029 テストが RED であることを確認 | Unity Test Runner |
| T020 | Test Runner で T018 テストが GREEN であることを確認 | Unity Test Runner |
| T021 | トグル切り替えを Hierarchy で目視確認 | Unity Editor（Hierarchy ビュー） |
| T024 | 厚さ・マテリアル独立設定を目視確認 | Unity Editor（シーンビュー） |
| T032 | Test Runner で T031 テストが RED であることを確認 | Unity Test Runner |
| T028 | quickstart.md シナリオの最終確認 | Unity Editor（総合） |
| T038 | ランタイムロジック テストカバレッジ 80% 以上を確認 | Unity Test Framework（カバレッジレポート） |

---

## Parallel Example: Phase 2

```text
# Phase 2 の 2 タスクは同時実行可能:
T005: OutlineDisplayMode.cs を作成
T006: OutlineSettings.cs を作成（T005 に依存しない）
```

## Parallel Example: Phase 7

```text
# Phase 7 の 4 タスクは同時実行可能:
T025: SolidText3DInspector.cs にアウトライン UI セクションを追加
T026: package.json のバージョンを 2.1.0 に変更
T027: CHANGELOG.md に v2.1.0 エントリを追記
T033: Profiler サンプリングをホットパスに追加
```

---

## Implementation Strategy

### MVP First（US1 のみ）

1. **Phase 1**: Clipper2 セットアップ（T001〜T004）
2. **Phase 2**: データ型作成（T005〜T006）
3. **Phase 3**: US1 実装（T007〜T014）
4. **STOP & VALIDATE**: US1 独立動作確認
5. 検証後に Phase 4〜7 へ進む

### Incremental Delivery

1. Phase 1 + Phase 2 完了 → 基盤準備完了
2. Phase 3（US1）完了 → アウトライン基本表示 **MVP！**
3. Phase 4（US2）完了 → 表示モード切り替え追加
4. Phase 5（US3）+ Phase 6（US4）完了 → 生成/削除トグル + 厚さ/マテリアル設定追加（並行可）
5. Phase 7 完了 → v2.1.0 リリース準備完了

---

## Notes

- `[P]` タスクは異なるファイル・依存関係なし → 並行実行可能
- `🔧` タスクは LLM による自動実装が不可能な手動操作
- テストはテストファースト原則（憲法 III）に従い RED 確認後に実装する
- `com.masachuang.solidtext3d.Runtime.asmdef` は `overrideReferences: true` を使用 → DLL 参照は `precompiledReferences` 配列に追加する（`references` 配列ではない）
- Clipper2 の `EndType.Polygon`（閉じたポリゴン）と `JoinType.Round`（自然なコーナー）を必ず指定すること
- Z ファイティング防止定数 `Z_FIGHT_EPSILON = 0.0001f` は `OutlineMeshBuilder` 内に定数として定義する
