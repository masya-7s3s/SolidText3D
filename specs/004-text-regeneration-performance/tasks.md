# タスク: Text Regeneration Performance

**入力**: `/specs/004-text-regeneration-performance/` 配下の設計資料
**前提資料**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md), [contracts/SolidText3DComponent-RegenerationAPI.md](contracts/SolidText3DComponent-RegenerationAPI.md)

**テスト方針**: 各ユーザーストーリーに対して Unity Test Framework の Edit Mode / Play Mode / Runtime 検証を含める。

**構成方針**: タスクはユーザーストーリーごとに整理し、各ストーリーを単独で実装・検証できるようにする。

## フェーズ 1: セットアップ（共有基盤）

**目的**: deferred regeneration 機能の実装と検証に必要な新規ファイルの雛形を揃える

- [X] T001 Packages/com.masachuang.solidtext3d/Runtime/DisplayResultSignature.cs、Packages/com.masachuang.solidtext3d/Runtime/PreparedDisplayResult.cs、Packages/com.masachuang.solidtext3d/Runtime/PreparedDisplayResultCache.cs、Packages/com.masachuang.solidtext3d/Runtime/DeferredRegenerationState.cs、Packages/com.masachuang.solidtext3d/Runtime/TextStateRequest.cs、Packages/com.masachuang.solidtext3d/Runtime/RegenerationFailureInfo.cs に runtime 用の雛形ファイルを作成する
- [X] T002 [P] Packages/com.masachuang.solidtext3d/Tests/Editor/PreparedDisplayResultCacheTests.cs、Packages/com.masachuang.solidtext3d/Tests/Editor/DeferredRegenerationTests.cs、Packages/com.masachuang.solidtext3d/Tests/Runtime/DeferredRegenerationRuntimeTests.cs にテスト用の雛形ファイルを作成する

---

## フェーズ 2: 基盤整備（全ストーリーの前提）

**目的**: すべてのユーザーストーリーが共有する capture / prepare / apply 基盤を、RED 先行で整える

**⚠️ 重要**: このフェーズではテストを先に追加し、失敗確認後に実装へ進む

### 基盤整備のテスト

- [X] T003 [P] Packages/com.masachuang.solidtext3d/Tests/Editor/PreparedDisplayResultCacheTests.cs と Packages/com.masachuang.solidtext3d/Tests/Editor/DeferredRegenerationTests.cs に TextStateRequest、DisplayResultSignature、PreparedDisplayResult、DeferredRegenerationState の基盤契約テストを追加する
- [X] T004 [P] Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/SolidText3DRuntimeTests.cs に同期再生成契約と deferred 再生成の共通アサーション helper を追加する

### 基盤整備の実装

- [X] T005 Packages/com.masachuang.solidtext3d/Runtime/TextStateRequest.cs、Packages/com.masachuang.solidtext3d/Runtime/DisplayResultSignature.cs、Packages/com.masachuang.solidtext3d/Runtime/PreparedDisplayResult.cs、Packages/com.masachuang.solidtext3d/Runtime/PreparedDisplayResultCache.cs、Packages/com.masachuang.solidtext3d/Runtime/DeferredRegenerationState.cs、Packages/com.masachuang.solidtext3d/Runtime/RegenerationFailureInfo.cs に TextStateRequest、DisplayResultSignature、PreparedDisplayResult、PreparedResultCacheEntry、DeferredRegenerationState、RegenerationFailureInfo を実装する
- [X] T006 [P] Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs、Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs、Packages/com.masachuang.solidtext3d/Runtime/MeshExtruder.cs、Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshData.cs から共有の pure-data preparation helper を抽出する
- [X] T007 Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs に内部の capture / prepare / apply パイプラインと version 管理を実装する

**チェックポイント**: 共有 regeneration パイプラインが使用可能になり、その上で各ユーザーストーリーを実装できる

---

## フェーズ 3: ユーザーストーリー 1 - 高頻度表示でも滑らかに追従する（優先度: P1） 🎯 MVP

**ゴール**: 高頻度更新でも latest-only に追従し、same-display と部分更新では重い再生成を避ける

**独立テスト**: timer 相当の短文更新を連続投入しても、最終表示が最新値に一致し、同一表示要求や一部桁だけの変化で不要な重処理が走らないことを確認する

### ユーザーストーリー 1 のテスト

- [X] T008 [P] [US1] Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs と Packages/com.masachuang.solidtext3d/Tests/Editor/DeferredRegenerationTests.cs に same-display skip、latest-only 集約、部分数値更新の RED テストを追加する
- [X] T009 [P] [US1] Packages/com.masachuang.solidtext3d/Tests/Runtime/DeferredRegenerationRuntimeTests.cs に 12 文字以内の timer 表示を 10 Hz で 60 秒更新して巻き戻りなしを確認する runtime テストを追加する

### ユーザーストーリー 1 の実装

- [X] T010 [US1] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs に RequestRegenerateMesh() と HasPendingRegeneration を XML ドキュメントコメント付きで追加する
- [X] T011 [US1] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs と Packages/com.masachuang.solidtext3d/Runtime/DeferredRegenerationState.cs に latest-only request submit、requestVersion 更新、stale-result discard を実装する
- [X] T012 [US1] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs、Packages/com.masachuang.solidtext3d/Runtime/DisplayResultSignature.cs、Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs で same-display の早期終了と部分数値更新時の reuse path を実装する
- [X] T013 [US1] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs、Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs、Packages/com.masachuang.solidtext3d/Runtime/CharacterObjectPool.cs で single-object と per-character rendering の deferred apply parity を接続する

**チェックポイント**: ユーザーストーリー 1 により、latest-only の高頻度更新と部分更新最適化が滑らかに動作し、単独で検証できる

---

## フェーズ 4: ユーザーストーリー 2 - 明示再生成が他処理を止めない（優先度: P2）

**ゴール**: 明示 API を連続呼び出ししても、他オブジェクトや周辺処理への波及を抑える

**独立テスト**: 重い object と軽い object を同時更新しても、入力や他 object 更新が一律停止せず、観測区間の大半が閾値内に収まることを確認する

### ユーザーストーリー 2 のテスト

- [X] T014 [P] [US2] Packages/com.masachuang.solidtext3d/Tests/Editor/PerformanceTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/PerformanceRuntimeTests.cs に non-blocking submit、10 Hz / 60 秒観測、95% 区間で 50 ms 超過なしのベンチマークを追加する
- [X] T015 [P] [US2] Packages/com.masachuang.solidtext3d/Tests/Runtime/DeferredRegenerationRuntimeTests.cs に heavy-object と light-object の同時更新、および 5 object 同時更新で 90% が 100 ms 以内に反映されることを確認する runtime テストを追加する

### ユーザーストーリー 2 の実装

- [X] T016 [US2] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs、Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs、Packages/com.masachuang.solidtext3d/Runtime/OutlineMeshBuilder.cs、Packages/com.masachuang.solidtext3d/Runtime/MeshExtruder.cs で RegenerateMesh() の同期性を維持したまま、重い preparation を Unity object apply path から切り離す
- [X] T017 [US2] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs と Packages/com.masachuang.solidtext3d/Runtime/DeferredRegenerationState.cs に component-local の実行分離と apply 順序制御を実装し、重い component が他 object を一律停止させないようにする
- [X] T018 [US2] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs と Packages/com.masachuang.solidtext3d/Tests/Editor/PerformanceTests.cs で idle-frame の処理を定数時間に保ち、request submit を allocation-free にする

**チェックポイント**: ユーザーストーリー 2 により、再生成を繰り返してもシーン周辺処理の応答性を維持できる

---

## フェーズ 5: ユーザーストーリー 3 - 繰り返し状態へ素早く戻れる（優先度: P3）

**ゴール**: 同じ見た目の再要求で prepared-result reuse を行い、再表示を速くする

**独立テスト**: 同一表示へ戻す操作で cold build より短い再表示になり、長時間運用後も劣化が閾値内に収まることを確認する

### ユーザーストーリー 3 のテスト

- [X] T019 [P] [US3] Packages/com.masachuang.solidtext3d/Tests/Editor/PreparedDisplayResultCacheTests.cs に cache hit、miss、LRU eviction、満杯後も新規登録が継続し、95% 以上の試行で 1 回の表示更新以内に再表示されるテストを追加する
- [X] T020 [P] [US3] Packages/com.masachuang.solidtext3d/Tests/Editor/PerformanceTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/PerformanceRuntimeTests.cs に cache-hit latency と 10 分運用後のレイテンシ中央値 10% 以内の回帰テストを追加する

### ユーザーストーリー 3 の実装

- [X] T021 [US3] Packages/com.masachuang.solidtext3d/Runtime/PreparedDisplayResultCache.cs に上限付き per-component LRU cache を実装する
- [X] T022 [US3] Packages/com.masachuang.solidtext3d/Runtime/DisplayResultSignature.cs と Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs で見た目に影響する全入力を cache invalidation と reuse signature に含める
- [X] T023 [US3] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs と Packages/com.masachuang.solidtext3d/Runtime/PreparedDisplayResult.cs で sync path と deferred path の両方に prepared result の再利用を導入する

**チェックポイント**: ユーザーストーリー 3 により、視覚的な正しさを保ったまま繰り返し状態を再利用できる

---

## フェーズ 6: ユーザーストーリー 4 - 高負荷でも破綻せず劣化する（優先度: P4）

**ゴール**: 高負荷や失敗時も keep-last-good を維持し、空白化や巻き戻りを防ぐ

**独立テスト**: 失敗、空文字列、更新バーストを含むシナリオで、表示破綻なしに最新状態へ安定追従することを確認する

### ユーザーストーリー 4 のテスト

- [X] T024 [P] [US4] Packages/com.masachuang.solidtext3d/Tests/Editor/DeferredRegenerationTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/DeferredRegenerationRuntimeTests.cs に keep-last-good failure と stale-result discard のテストを追加する
- [X] T025 [P] [US4] Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/SolidText3DRuntimeTests.cs に empty-string clear、burst collapse、失敗通知の edge case テストを追加する

### ユーザーストーリー 4 の実装

- [X] T026 [US4] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs と Packages/com.masachuang.solidtext3d/Runtime/RegenerationFailureInfo.cs に DeferredRegenerationFailed と RegenerationFailureInfo を XML ドキュメントコメント付きで追加する
- [X] T027 [US4] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs と Packages/com.masachuang.solidtext3d/Runtime/DeferredRegenerationState.cs で failed または stale な prepared result を visible mesh を壊さずに破棄する
- [X] T028 [US4] Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs と Packages/com.masachuang.solidtext3d/Runtime/PreparedDisplayResult.cs で空文字列 request に対する安全な clear-display 動作を sync / deferred 両 path で維持する

**チェックポイント**: ユーザーストーリー 4 により、失敗時や更新バースト時でも表示を破綻させない

---

## フェーズ 7: 仕上げと横断対応

**目的**: 公開ドキュメント、契約、最終検証を feature 実装に揃える

- [X] T029 [P] Packages/com.masachuang.solidtext3d/README.md、Packages/com.masachuang.solidtext3d/Documentation~/index.md、Packages/com.masachuang.solidtext3d/CHANGELOG.md に public regeneration API と performance validation 条件の更新を反映する
- [X] T030 [P] specs/004-text-regeneration-performance/quickstart.md と specs/004-text-regeneration-performance/contracts/SolidText3DComponent-RegenerationAPI.md で feature の利用方法、公開 API 契約、閾値付き検証条件を実装済み API と整合させる
- [X] T031 specs/004-text-regeneration-performance/quickstart.md に Edit Mode、Runtime、Profiler、60 秒 10 Hz、10 分劣化試験の検証結果を記録する
- [ ] T032 specs/004-text-regeneration-performance/quickstart.md に Unity 6 LTS（6000.x）の Windows player 手動ビルド確認結果を記録する

---

## 依存関係と実行順序

### フェーズの依存関係

- **フェーズ 1: セットアップ**: 依存なしで開始可能
- **フェーズ 2: 基盤整備**: フェーズ 1 完了後に着手し、RED テストを先に追加してから実装へ進む
- **フェーズ 3-6: ユーザーストーリー**: フェーズ 2 完了後に開始可能。MVP は US1 の完了で成立する
- **フェーズ 7: 仕上げ**: 実施対象のユーザーストーリー完了後に着手する

### ユーザーストーリーの依存関係

- **US1 (P1)**: 基盤整備完了後に開始可能。MVP の最小スコープ
- **US2 (P2)**: 基盤整備完了後に開始可能。US1 の deferred API surface と同じ component を触るため、実作業は US1 後の順次進行を推奨
- **US3 (P3)**: 基盤整備完了後に開始可能。signature と prepare/apply 基盤を共有する
- **US4 (P4)**: 基盤整備完了後に開始可能。deferred path の失敗処理を扱うため、US1 と同じ public surface を共有する

### 各ユーザーストーリー内の進め方

- テストを先に追加し、失敗を確認してから実装に進む
- API / entity / state 変更の後に orchestration を実装する
- orchestration の後に parity / performance / edge-case を締める
- 各ストーリー完了時に、そのストーリーの独立テストを実行する

### 並行実行の候補

- Phase 1 の T002 は T001 と並行で進められる
- Phase 2 の T003 と T004 は並行で進められる
- Phase 2 の T006 は T005 と別ファイル中心のため、RED 失敗確認後に並行化できる
- US1 の T008 と T009 は並行で進められる
- US2 の T014 と T015 は並行で進められる
- US3 の T019 と T020 は並行で進められる
- US4 の T024 と T025 は並行で進められる
- Phase 7 の T029 と T030 は並行で進められる

---

## 並行実行例: ユーザーストーリー 1

```text
Task: "Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs と Packages/com.masachuang.solidtext3d/Tests/Editor/DeferredRegenerationTests.cs に same-display skip、latest-only 集約、部分数値更新の RED テストを追加する"
Task: "Packages/com.masachuang.solidtext3d/Tests/Runtime/DeferredRegenerationRuntimeTests.cs に 12 文字以内の timer 表示を 10 Hz で 60 秒更新して巻き戻りなしを確認する runtime テストを追加する"
```

## 並行実行例: ユーザーストーリー 2

```text
Task: "Packages/com.masachuang.solidtext3d/Tests/Editor/PerformanceTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/PerformanceRuntimeTests.cs に non-blocking submit、10 Hz / 60 秒観測、95% 区間で 50 ms 超過なしのベンチマークを追加する"
Task: "Packages/com.masachuang.solidtext3d/Tests/Runtime/DeferredRegenerationRuntimeTests.cs に heavy-object と light-object の同時更新、および 5 object 同時更新で 90% が 100 ms 以内に反映されることを確認する runtime テストを追加する"
```

## 並行実行例: ユーザーストーリー 3

```text
Task: "Packages/com.masachuang.solidtext3d/Tests/Editor/PreparedDisplayResultCacheTests.cs に cache hit、miss、LRU eviction、満杯後も新規登録が継続するテストを追加する"
Task: "Packages/com.masachuang.solidtext3d/Tests/Editor/PerformanceTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/PerformanceRuntimeTests.cs に cache-hit latency と 10 分運用後のレイテンシ中央値 10% 以内の回帰テストを追加する"
```

## 並行実行例: ユーザーストーリー 4

```text
Task: "Packages/com.masachuang.solidtext3d/Tests/Editor/DeferredRegenerationTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/DeferredRegenerationRuntimeTests.cs に keep-last-good failure と stale-result discard のテストを追加する"
Task: "Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs と Packages/com.masachuang.solidtext3d/Tests/Runtime/SolidText3DRuntimeTests.cs に empty-string clear、burst collapse、失敗通知の edge case テストを追加する"
```

---

## 実装戦略

### MVP 優先（ユーザーストーリー 1 のみ）

1. フェーズ 1: セットアップを完了する
2. フェーズ 2: 基盤整備の RED テストと実装を完了する
3. フェーズ 3: ユーザーストーリー 1 を完了する
4. US1 のテストで高頻度 latest-only 挙動と部分更新最適化を検証する
5. 直近の目的が滑らかな timer 更新であれば、ここで MVP として止める

### 段階的デリバリー

1. セットアップと基盤整備を完了し、共有 regeneration パイプラインを安定化する
2. US1 を提供し、latest-only の滑らかな更新と部分更新最適化を実現する
3. US2 を追加し、繰り返し request 時の scene-wide stall を減らす
4. US3 を追加し、cache reuse により繰り返し表示を高速化する
5. US4 を追加し、失敗時とバースト負荷時の挙動を堅牢化する
6. 最後にフェーズ 7 でドキュメント、閾値検証、Windows build 確認を仕上げる

### 並行開発の進め方

1. 1 人目がフェーズ 1-2 の共有基盤を完了する
2. フェーズ 2 後は、1 人が Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs を中心に US1/US2 を進め、別の人が US3 の cache test と runtime file を準備する
3. 3 人目は public deferred API の形が固まった後に US4 の failure test と フェーズ 7 の documentation update を進められる

---

## 補足

- [P] タスクは、別ファイル中心で進められるか、未完了タスクへの依存なしに安全に分割できるものに限定する
- ユーザーストーリーラベルは [spec.md](spec.md) の優先順ストーリーに対応する
- 各ストーリーには独立したテスト条件があり、フェーズ 2 後に単独検証できる
- 推奨 MVP スコープは フェーズ 3 / US1 のみ
