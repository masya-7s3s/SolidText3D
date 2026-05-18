# Research: Text Regeneration Performance

**Branch**: `004-text-regeneration-performance` | **Date**: 2026-05-17  
**Status**: Complete

## R-001: 現行コードを正として planning baseline を固定する

**Decision**: この feature の baseline は古い specs ではなく、現行 runtime code と package docs に合わせる。today 時点の正は manual-only / sync-only / no-cache であり、`RegenerateMesh()` が唯一の再生成 API である。

**Rationale**:

- [Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs](../../Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs) の `LateUpdate()` は空で、自動再生成は行われない
- [Packages/com.masachuang.solidtext3d/README.md](../../Packages/com.masachuang.solidtext3d/README.md) と [Packages/com.masachuang.solidtext3d/CHANGELOG.md](../../Packages/com.masachuang.solidtext3d/CHANGELOG.md) も、dirty 状態は明示的な `RegenerateMesh()` まで保持する運用を示している
- 以前の specs には `LateUpdate` 自動再生成や inspector debounce が残っているが、現行コードとは食い違う

**Alternatives considered**:

- 古い specs を baseline として plan を組む案: tasks が現行実装と噛み合わず、不要な後戻りが増えるため却下
- runtime code だけを見て docs を無視する案: 公開 API 契約の文脈を落とすため却下

## R-002: `RegenerateMesh()` は同期 API のまま維持し、別 API を追加する

**Decision**: `RegenerateMesh()` の同期完了セマンティクスは後方互換として維持し、高頻度更新向けには additive な `RequestRegenerateMesh()` を追加する。

**Rationale**:

- spec の clarifications でも sync path の維持が前提になっている
- 現在の package 利用者は `RegenerateMesh()` 復帰時点で mesh が反映済みであることを期待している
- 高頻度更新の問題は sync path を書き換えるより、別経路で coalescing と deferred apply を導入する方が安全

**Alternatives considered**:

- `RegenerateMesh()` 自体を deferred 化する案: 既存呼び出し側の契約破壊になるため却下
- 既存 API に bool 引数を増やして sync/deferred を切り替える案: public surface が曖昧になり、呼び出しミスも増えるため却下

## R-003: 非同期化の境界は Unity `Mesh` 適用の前に置く

**Decision**: 重い準備処理は `TextStateRequest -> PreparedDisplayResult` の pure-data 変換に切り出し、Unity `Mesh`, `MeshFilter`, `MeshRenderer`, child GameObject 更新は main thread に残す。

**Rationale**:

- 現行 `GlyphMeshBuilder.Build()` / `BuildPerCharacter()` は最終的に `Mesh` を生成するため、そのまま worker に載せるには責務が広すぎる
- 一方で [Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshData.cs](../../Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshData.cs) は、Mesh 作成前の頂点・index・normal 集約データとして既に存在する
- `GlyphMeshData` と同等の pure-data 段を body / outline / per-character で共有すれば、deferred path と sync path の visual parity を保ちやすい

**Alternatives considered**:

- `Mesh` 生成まで worker 側で完了する案: Unity API 制約と thread safety の懸念が大きいため却下
- main thread のまま数フレームに分割して小出し実行する案: 実装は単純だが total work を減らせず、複数 object 更新時の fairness も弱いため優先度を下げた

## R-004: queue semantics は component-local latest-only にする

**Decision**: deferred path の queue は component ごとに「進行中 1 件 + 保留 latest 1 件」のみを持つ。古い保留要求は常に上書きし、completed result も version が古ければ apply しない。

**Rationale**:

- spec の clarifications と FR-017 が明示的に latest-only を要求している
- FIFO backlog を積むと timer / score のような用途で古い値の消化に時間を浪費する
- component-local version 管理なら multi-object fairness を壊さず、global scheduler を導入せずに済む

**Alternatives considered**:

- 全要求を順番に処理する案: 最新表示への追従が悪化するため却下
- global queue で全 component を一括制御する案: lock contention と相互干渉が増えるため却下

## R-005: prepared result cache は per-component LRU にする

**Decision**: `DisplayResultSignature` を key にした bounded LRU cache を component ごとに持つ。scope は session 内のみ、capacity は初版では internal に固定する。

**Rationale**:

- spec FR-004 / FR-019 は prepared result の再利用と上限到達時の LRU eviction を求めている
- component-local cache なら、他 object の burst 更新で自 object の hot entry が追い出されにくい
- 公開設定を増やさずに運用でき、憲法 VII の YAGNI に沿う

**Alternatives considered**:

- global shared cache: cross-object reuse の利点はあるが、multi-object burst 時の contention と eviction の説明責務が増えるため却下
- unbounded cache: 長時間運用で spec FR-012 / FR-019 に反するため却下

## R-006: 署名は「same-display skip」と「prepared-result reuse」で分けずに共有する

**Decision**: current display 判定と cache lookup の両方で同じ `DisplayResultSignature` を使う。署名には text、font source、font size、anchor、writing mode、outline、object mode など見た目に影響するすべての入力を含める。

**Rationale**:

- 現行 `ComputeParamHash()` は same-parameter skip のために既に存在し、見た目入力を網羅する方向の責務を持っている
- skip 用ハッシュと cache key を別々に持つと invalidation ミスの温床になる
- per-session / per-component cache であれば、font source は instance id ベースでも十分実用になる

**Alternatives considered**:

- skip と cache で別 key を持つ案: 規約が二重化して不整合を招くため却下
- font bytes 全体の content hash を毎回計算する案: request submit 時のコストが高く、hot path には重いため却下

## R-007: failure policy は keep-last-good + observable notification に統一する

**Decision**: deferred preparation が失敗した場合、直前の completed display は維持し、failed request は破棄し、`DeferredRegenerationFailed` イベントで通知する。

**Rationale**:

- spec FR-018 / SC-007 が明示的に求める振る舞いと一致する
- 現行 sync path でも、フォント欠落時は直前メッシュ維持と warning で処理しており、方向性が揃う
- retry を自動化すると request ordering と latest-only semantics が複雑になる

**Alternatives considered**:

- 失敗時に空メッシュへ切り替える案: UX を悪化させるため却下
- 自動再試行を組み込む案: queue semantics が複雑化し、最新要求優先とも衝突しやすいため却下

## R-008: 検証は cache / queue の unit tests と Unity runtime 観測を併用する

**Decision**: correctness は Edit Mode で囲い、体感性能は Play Mode と Unity Profiler で確認する。既存の `PerformanceTests` / `PerformanceRuntimeTests` は clean-frame 回帰基準として維持し、feature 専用の burst / latest-only 検証を追加する。

**Rationale**:

- 現状の repo に既に editor benchmark と runtime benchmark があるため、回帰比較の土台がある
- この feature の失敗形は compile error より ordering bug や UX regression になりやすい
- Unity batchmode での automated tests と Profiler 手動証跡の両方が必要

**Alternatives considered**:

- Profiler 手動確認だけに頼る案: stale result や LRU eviction の correctness を機械検証できないため却下
- unit tests だけで済ませる案: 実フレームの apply cost や複数 object 同時更新の体感を拾いきれないため却下
