# Tasks: テキストアウトライン生成（面生成経路の再設計）

**Input**: Design documents from `/specs/003-text-outline/`  
**Prerequisites**: `plan.md` / `spec.md` / `research.md` / `data-model.md` / `contracts/` / `quickstart.md`  
**Plan Branch**: `003-text-outline` | **Date**: `2026-05-12` | **SemVer**: `2.0.0 → 2.1.0`

**Tests**: 憲法と plan に従い、Phase 2 を含むすべての機能追加は失敗テストを先に追加し、その後に実装して GREEN 化する。  
**Organization**: タスクは Setup → Foundational → User Story ごとの順で整理し、各ストーリーを単独で実装・検証できるようにする。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 並行実行可能（異なるファイルで依存がない）
- **[Story]**: 対応ユーザーストーリー（US1, US2, US3, US4）
- すべての実装タスクは対象ファイルを明記する

## Phase 1: Setup（共有セットアップ）

**Purpose**: 再設計に必要な依存参照と配布メタデータを揃える

- [X] T001 Packages/com.masachuang.solidtext3d/Runtime/com.yourcompany.solidtext3d.Runtime.asmdef、Packages/com.masachuang.solidtext3d/Editor/com.yourcompany.solidtext3d.Editor.asmdef、Packages/com.masachuang.solidtext3d/Tests/Editor/com.yourcompany.solidtext3d.Tests.Editor.asmdef、Packages/com.masachuang.solidtext3d/Tests/Runtime/com.yourcompany.solidtext3d.Tests.Runtime.asmdef をそれぞれ com.masachuang.solidtext3d.* へ rename し、asmdef 内の name / references を更新する
- [X] T002 T001 完了後、Packages/com.masachuang.solidtext3d/Runtime/com.masachuang.solidtext3d.Runtime.asmdef に Clipper2Lib.dll の precompiledReferences を追加または検証する
- [X] T003 [P] Packages/com.masachuang.solidtext3d/Third Party Notices.md に Clipper2 1.4.x の notice を追記または更新する

**Checkpoint**: asmdef 名が package 名と整合し、ランタイム asmdef が Clipper2 を解決でき、配布メタデータが依存関係と一致していること

---

## Phase 2: Foundational（全ストーリー共通の基盤）

**Purpose**: すべてのアウトライン機能が共有する型・内部モデル・押し出しコア・公開契約を先に整える

**⚠️ CRITICAL**: このフェーズでは失敗テストを先に追加し、GREEN 化を確認するまで後続ストーリー実装を開始しない

### Tests for Foundational

- [X] T004 [P] Packages/com.masachuang.solidtext3d/Tests/Editor/MeshExtruderTests.cs に shared cap / side / Z 配置 helper 抽出後も body winding 規約と outline 再利用契約が維持されることを検証する失敗テストを追加する
- [X] T005 [P] Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineContourBuilderTests.cs に Unity 単位入力から em 空間への変換、非負値入力契約、`OutlineOffset = 0` のとき空の `RingContoursEm` を返して outline 形状を生成しないことを検証する失敗テストを追加する
- [X] T006 [P] Packages/com.masachuang.solidtext3d/Tests/Runtime/OutlineChildGOTests.cs に `OutlineEnabled = true` かつ `OutlineOffset = 0` のとき child GameObject は破棄せず mesh のみクリアし、設定値を保持したまま offset 再増加で再生成できることを検証する失敗テストを追加する

### Implementation for Foundational

- [X] T007 [P] Packages/com.masachuang.solidtext3d/Runtime/OutlineDisplayMode.cs に Donut / BackFilled の公開 enum と、該当 public API に対する `<summary>` / `<example>` を含む XML ドキュメントを追加する
- [X] T008 [P] Packages/com.masachuang.solidtext3d/Runtime/OutlineSettings.cs に Enabled / OffsetAmount / Thickness / Material / DisplayMode を持つ公開設定型を追加し、該当 public API に `<summary>` / `<param>` / `<returns>` / `<example>` を含む XML ドキュメントを付与したうえで、OffsetAmount と Thickness が 0 以上の非負値契約であることを明記する
- [X] T009 [P] Packages/com.masachuang.solidtext3d/Runtime/OutlineProfileSet.cs に OriginalFilledContoursEm / OffsetFilledContoursEm / RingContoursEm を持つ internal モデルを追加する
- [X] T010 Packages/com.masachuang.solidtext3d/Runtime/MeshExtruder.cs から outline でも再利用できる cap / side / Z 配置の internal helper を抽出する
- [X] T011 Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs に outline 用 SerializeField、公開プロパティの骨格、`OutlineEnabled` / `OutlineOffset` / `OutlineThickness` / `OutlineMaterial` / `OutlineDisplayMode` の該当 public API に対する `<summary>` / `<param>` / `<returns>` / `<example>` を含む XML ドキュメントコメント、offset 0 の no-geometry 契約、dirty 連携の基盤を追加する
- [X] T012 Packages/com.masachuang.solidtext3d/Tests/Editor と Packages/com.masachuang.solidtext3d/Tests/Runtime の foundational RED テストを GREEN にしつつ、specs/003-text-outline/contracts/SolidText3DComponent-API.md に Outline 系公開 API、0 以上の非負値契約、`OutlineOffset = 0` 時の no-geometry / child 再利用動作、material fallback、mode semantics を反映する

**Checkpoint**: foundational RED テストが先に存在し、outline の公開 API と internal profile/cap 共有基盤と契約書が同じ仕様を指していること

---

## Phase 3: User Story 1 - アウトライン基本表示 (Priority: P1) 🎯 MVP

**Goal**: インスペクターでアウトラインを有効化し、オフセット量を変更すると、文字外周に front face を含む outline が安定して表示され、offset 0 では outline なし相当になるようにする

**Independent Test**: Inspector で outline を有効化して Offset Amount を変更したとき、正面から outline front face が消えず、オフセット増加に応じて外周が更新され、Offset Amount を 0 にすると outline が非表示になることを確認できる

### Tests for User Story 1

- [X] T013 [P] [US1] Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineContourBuilderTests.cs に ring difference、代表グリフセット（英字: O / B / 8、日本語: あ / 回 / 囲）の hole 吸収、`Clipper.Area() < 0` を穴とみなす除去基準、winding parity、オフセット量を 2 倍にした場合の外周長増加率誤差が ±15% 以内であることを検証する失敗テストを追加する
- [X] T014 [P] [US1] Packages/com.masachuang.solidtext3d/Tests/Editor/MeshExtruderTests.cs に canonical な `RingContoursEm` を共有 helper に渡したとき body と同じ cap / side 規約で front cap 可視性が保たれることを検証する失敗テストを追加する
- [X] T015 [P] [US1] Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineMeshBuilderTests.cs に outline front cap の生成、thickness 0 の front-only 動作、front silhouette の基本形状が canonical な `RingContoursEm` と一致することを検証する失敗テストを追加する

### Implementation for User Story 1

- [X] T016 [US1] Packages/com.masachuang.solidtext3d/Runtime/OutlineContourBuilder.cs に Clipper2 の offset と boolean difference を使って canonical な `RingContoursEm` を返す builder を実装し、公開 Unity 単位入力を内部 em 空間へ変換してから演算し、offset 0 では空 profile を返す
- [X] T017 [US1] Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs に `RingContoursEm` を MeshExtruder helper で押し出して outline front cap と side face を生成する実装を追加する
- [X] T018 [US1] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs に outline child の生成再利用と基本メッシュ更新フローを接続し、offset 0 では child を維持したまま mesh をクリアする処理を追加する
- [X] T019 [US1] Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs に Outline セクションと Enabled / Offset Amount の編集 UI を追加する
- [X] T020 [US1] specs/003-text-outline/quickstart.md の基本表示シナリオに沿って Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineContourBuilderTests.cs と Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineMeshBuilderTests.cs を GREEN にし、代表グリフセットでの hole 吸収、外周長増加率 ±15%、front cap 可視性、canonical ring silhouette 一致、offset 0 の no-geometry 動作を確認する

**Checkpoint**: User Story 1 だけで outline の基本表示、オフセット更新、offset 0 の no-geometry 動作が独立して成立すること

---

## Phase 4: User Story 2 - アウトライン表示モード切り替え (Priority: P2)

**Goal**: Donut と BackFilled を切り替えても正面シルエットは一致し、背面の構成と Z アンカーだけが仕様どおり変わるようにする

**Independent Test**: Display Mode を Donut / BackFilled で切り替えたとき、正面からの silhouette が一致し、背面では Donut はリング状、BackFilled は rear infill を持つことを確認できる

### Tests for User Story 2

- [X] T021 [P] [US2] Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineMeshBuilderTests.cs に Donut / BackFilled の front silhouette 一致、XY 投影時の対称差面積 0.5% 以下、BackFilled の rear anchor が `bodyBack - Z_FIGHT_EPSILON` から ±0.001 Unity 単位以内であること、Donut の前後張り出し量差が ±0.001 Unity 単位以内であることを検証する失敗テストを追加する

### Implementation for User Story 2

- [X] T022 [US2] Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs に Donut の中央基準押し出しと BackFilled の rear infill / 背面固定アンカー実装を追加し、SC-004 の ±0.001 Unity 単位制約を満たすようにする
- [X] T023 [US2] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs に OutlineDisplayMode 変更時の dirty 更新と mode 切り替え再生成を実装する
- [X] T024 [US2] Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs に Display Mode の編集 UI を追加する
- [X] T025 [US2] specs/003-text-outline/quickstart.md の mode 切り替えシナリオに沿って Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineMeshBuilderTests.cs を GREEN にする

**Checkpoint**: User Story 2 だけでモード差分が背面構成に閉じ、正面 silhouette が一致していること

---

## Phase 5: User Story 3 - アウトラインの生成/削除切り替え (Priority: P2)

**Goal**: OutlineEnabled のオンオフで child GameObject と outline mesh が破棄・再生成され、設定値が保持されるようにする

**Independent Test**: OutlineEnabled を false にすると outline child が消え、true に戻すと child が再生成され、以前の offset と mode がそのまま反映されることを確認できる

### Tests for User Story 3

- [X] T026 [P] [US3] Packages/com.masachuang.solidtext3d/Tests/Runtime/OutlineChildGOTests.cs に child の生成、破棄、再生成、設定保持を検証する失敗テストを追加する

### Implementation for User Story 3

- [X] T027 [US3] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs に OutlineEnabled の destroy / recreate、OnDestroy 後始末、設定保持ロジックを実装する
- [X] T028 [US3] specs/003-text-outline/quickstart.md のトグル切り替えシナリオに沿って Packages/com.masachuang.solidtext3d/Tests/Runtime/OutlineChildGOTests.cs を GREEN にする

**Checkpoint**: User Story 3 だけで outline child のライフサイクルが独立して検証できること

---

## Phase 6: User Story 4 - アウトラインの厚さとマテリアル設定 (Priority: P2)

**Goal**: outline の厚さとマテリアルを文字本体から独立して設定し、null 時は本体マテリアルへフォールバックできるようにする

**Independent Test**: Outline Thickness と Outline Material を変えると outline だけが更新され、Outline Material が null のときは本体 sharedMaterial を継承することを確認できる

### Tests for User Story 4

- [X] T029 [P] [US4] Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineMeshBuilderTests.cs に thickness 0 / 正値時の Z 配置と shell 生成差分を検証する失敗テストを追加する
- [X] T030 [P] [US4] Packages/com.masachuang.solidtext3d/Tests/Runtime/OutlineChildGOTests.cs に outline material 独立、null fallback、再生成後の設定保持を検証する失敗テストを追加する

### Implementation for User Story 4

- [X] T031 [US4] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs に OutlineThickness / OutlineMaterial の適用、null fallback、再生成時反映を実装する
- [X] T032 [US4] Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs に Thickness / Material の編集 UI を追加する
- [X] T033 [US4] specs/003-text-outline/quickstart.md の厚さ・マテリアル設定シナリオに沿って Packages/com.masachuang.solidtext3d/Tests/Editor/OutlineMeshBuilderTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/OutlineChildGOTests.cs を GREEN にする

**Checkpoint**: User Story 4 だけで outline の厚さとマテリアル独立設定が確認できること

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: バージョン、ドキュメント、性能回帰、最終検証を整える

- [X] T034 [P] Packages/com.masachuang.solidtext3d/package.json を 2.1.0 向けに更新する
- [X] T035 [P] Packages/com.masachuang.solidtext3d/CHANGELOG.md に outline 再設計の変更内容を追加する
- [X] T036 [P] Packages/com.masachuang.solidtext3d/README.md と Packages/com.masachuang.solidtext3d/Documentation~ 配下の説明を outline の設定項目と mode 差分に合わせて更新する
- [X] T037 [P] Packages/com.masachuang.solidtext3d/Tests/Editor/PerformanceTests.cs または Packages/com.masachuang.solidtext3d/Tests/Runtime/PerformanceRuntimeTests.cs に clean frame の GC.Alloc 0 回帰テストを追加する
- [X] T038 [P] Packages/com.masachuang.solidtext3d/Runtime/OutlineContourBuilder.cs と Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs と Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs のホットパスに Profiler.BeginSample / EndSample を追加し、計測結果を specs/003-text-outline/quickstart.md に記録したうえで PR 添付用の Profiler 証跡を残す
- [X] T039 Packages/com.masachuang.solidtext3d/Runtime/OutlineContourBuilder.cs と Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs と Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs を調整して通常フレームで追加アロケーションを発生させないようにする
- [X] T040 specs/003-text-outline/quickstart.md の最終検証シナリオを通しで実施し、Packages/com.masachuang.solidtext3d/Tests/Editor と Packages/com.masachuang.solidtext3d/Tests/Runtime の関連テストを最終確認する
- [ ] T041 specs/003-text-outline/quickstart.md に Unity 6 LTS（6000.x）の Windows player build でサンプルシーンを含む手動ビルド確認結果を記録する

**Checkpoint**: リリース用のドキュメント・性能・Profiler 計測証跡・検証・Unity 6 LTS 手動ビルド確認がそろい、feature を段階的に出荷できる状態になること

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** は即時開始できる
- **Phase 2: Foundational** は Phase 1 完了後に開始し、foundational RED テスト追加と GREEN 化を完了するまで以降の全ユーザーストーリーをブロックする
- **Phase 3: US1** は Phase 2 完了後に開始する MVP フェーズである
- **Phase 4: US2**, **Phase 5: US3**, **Phase 6: US4** は US1 完了後に進める
- **Phase 7: Polish** は必要なユーザーストーリー完了後に進める

### User Story Dependencies

- **US1 (P1)**: Foundational 完了後に単独開始できる
- **US2 (P2)**: US1 の outline profile / mesh build 基盤に依存する
- **US3 (P2)**: US1 の component 接続に依存する
- **US4 (P2)**: US1 の component 接続に依存し、US3 と並行可能である

### Within Each User Story

- 失敗テストを先に追加してから実装する
- contour / profile の確定を先に行い、その後で mesh build と component 接続を進める
- inspector UI は対応する runtime API が確定した後に追加する
- quickstart ベースの独立検証を完了してから次ストーリーへ進む

### Parallel Opportunities

- T001 と T003 は並行実行できる
- T004, T005, T006 は並行実行できる
- T007, T008, T009 は並行実行できる
- US1 の T013, T014, T015 は並行実行できる
- US4 の T029 と T030 は並行実行できる
- T034, T035, T036, T037, T038 は並行実行できる
- T040 と T041 は関連テスト完了後に順次実行する

---

## Parallel Example: User Story 1

```text
T013: OutlineContourBuilderTests.cs に canonical profile の失敗テストを追加する
T014: MeshExtruderTests.cs に shared cap / side 規約の失敗テストを追加する
T015: OutlineMeshBuilderTests.cs に front cap 回帰テストを追加する
```

## Parallel Example: User Story 4

```text
T029: OutlineMeshBuilderTests.cs に thickness の失敗テストを追加する
T030: OutlineChildGOTests.cs に material fallback の失敗テストを追加する
```

---

## Implementation Strategy

### MVP First

1. Phase 1 を完了する
2. Phase 2 を完了して共有基盤と API 契約を確定する
3. Phase 3 の US1 だけを完了する
4. US1 の quickstart とテストを独立検証する
5. front face が安定表示され、offset 0 が no-geometry として振る舞うことを確認してから次のストーリーへ進む

### Incremental Delivery

1. US1 で outline 基本表示と offset 0 の no-geometry 動作を完成させる
2. US2 で mode 差分を追加する
3. US3 と US4 を必要に応じて並行実装する
4. 最後に性能・ドキュメント・リリースメタデータを整える

### Suggested MVP Scope

- **MVP は US1 のみ** とし、front face 不具合の解消、canonical ring profile の安定化、offset 0 の no-geometry 動作を最優先にする

---

## Notes

- `[P]` は異なるファイルに対する独立作業だけに付与している
- 各ストーリーは quickstart.md と対応テストファイルで単独検証できるように分けている
- contract 更新は public API の追加と同一フェーズに固定し、contracts/ を prerequisites に含める理由を明示した
- 旧 tasks.md にあった front tessellation 専用実装タスクは削除し、MeshExtruder 共有コアへの集約方針に置き換えた
- BackFilled と Donut は front silhouette を共有し、差分を Z 配置と rear infill に限定する
