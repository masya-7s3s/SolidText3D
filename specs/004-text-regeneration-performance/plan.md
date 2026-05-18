# Implementation Plan: Text Regeneration Performance

**Branch**: `004-text-regeneration-performance` | **Date**: 2026-05-17 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-text-regeneration-performance/spec.md`

## Summary

現行実装の正は `Packages/com.masachuang.solidtext3d` 配下のコードと現行 `README.md` / `CHANGELOG.md` であり、再生成は today 時点で manual-only / sync-only / no-cache である。この feature ではその前提を崩さず、`RegenerateMesh()` の同期完了セマンティクスを維持したまま、別の明示 API による latest-only な遅延再生成経路、署名ベースの prepared-result 再利用、失敗時の直前表示維持、複数オブジェクト更新時の波及抑制を追加する。重い準備処理は main thread の Mesh/GameObject 適用から切り離し、Unity API を触る最終反映だけを main thread に残す。

## Technical Context

**Language/Version**: C# (.NET Standard 2.1) / Unity 6 (6000.x LTS)  
**Primary Dependencies**: SixLabors.Fonts 2.1.3、LibTessDotNet 1.1.15、Clipper2 1.4.x、UnityEngine / Unity Test Framework  
**Storage**: N/A（ランタイムはメモリ完結。Editor のみ `.bytes` キャッシュを AssetDatabase に保持）  
**Testing**: Unity Test Framework（Edit Mode / Play Mode）、Unity batchmode `-runTests`、Profiler 手動観測  
**Target Platform**: Unity 6 Editor + Runtime（Windows 必須、macOS / Linux は任意）  
**Project Type**: UPM パッケージ（Runtime ライブラリ + Editor 拡張 + Unity tests）  
**Performance Goals**: 12 文字以内の高頻度表示で 10 Hz 更新時も最新値追従を維持し、`RegenerateMesh()` 後方互換を壊さず、clean frame の追加 GC.Alloc は 0 を維持する  
**Constraints**: `RegenerateMesh()` は同期完了 API のまま維持、`LateUpdate()` は no-op または定数時間、Update 系で `new` / LINQ / 文字列連結禁止、Mesh / GameObject 操作は main thread 限定、古い spec より現行実装を正とする  
**Scale/Scope**: 単一 package 内の再生成経路再設計。主変更対象は `Runtime/` 5〜8 ファイル、`Tests/` 4〜6 ファイル、feature docs と API contract 一式

## Constitution Check

*GATE: Phase 0 前に確認し、Phase 1 設計反映後に再確認する。*

- [x] **I. UPM 構造**: 実装は `Packages/com.masachuang.solidtext3d/Runtime/` を中心に行い、Inspector や Editor 補助が必要なら `Editor/`、検証は `Tests/Editor/` と `Tests/Runtime/` に分離する。
- [x] **II. Editor/Runtime 分離**: deferred preparation、cache、signature、latest-only queue は runtime に閉じる。`UnityEditor` 参照は既存 inspector / editor helper に限定する。
- [x] **III. テストファースト**: queue collapse、stale result discard、cache hit / LRU eviction、sync API 後方互換、failure retain-last-display、multi-object fairness を RED から追加する前提で進める。
- [x] **IV. 後方互換性**: `RegenerateMesh()` は継続し、追加は別の明示 API とイベントに限定する。既存公開 API の削除は行わないため SemVer は MINOR（`2.1.0 → 2.2.0`）を想定する。
- [x] **V. パフォーマンス**: hot path は request capture・version 更新・ready result 適用のみに絞り、clean frame の追加 GC.Alloc 0 を維持する。重い準備は worker に寄せるか request 間で amortize する。
- [x] **VI. Asset Store 準拠**: 新規サードパーティ依存は追加しない。公開 API 追加に伴う `CHANGELOG.md` / `README.md` / `Documentation~` の更新を tasks に含める。
- [x] **VII. シンプルさ**: general-purpose scheduler や複数段キューは導入せず、「進行中 1 件 + 待機 latest 1 件 + per-component LRU cache」の最小構成に留める。

**Post-Design Re-check**: Phase 1 の設計では runtime 内の純粋データ準備境界を追加するだけで、Editor/Runtime 分離・後方互換・GC 制約のいずれも破らない。手動ビルド確認と docs 更新は tasks 側の必須ゲートとして明示する。

## Project Structure

### Documentation (this feature)

```text
specs/004-text-regeneration-performance/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── SolidText3DComponent-RegenerationAPI.md
└── tasks.md
```

### Source Code (repository root)

```text
Packages/com.masachuang.solidtext3d/
├── Runtime/
│   ├── SolidText3DComponent.cs
│   ├── GlyphMeshBuilder.cs
│   ├── MeshExtruder.cs
│   ├── GlyphMeshData.cs
│   ├── CharacterObjectPool.cs
│   ├── DisplayResultSignature.cs                # new
│   ├── PreparedDisplayResult.cs                 # new
│   ├── PreparedDisplayResultCache.cs            # new
│   └── DeferredRegenerationState.cs             # new
├── Editor/
│   └── SolidText3DInspector.cs
└── Tests/
    ├── Editor/
    │   ├── SolidText3DComponentTests.cs
    │   ├── PerformanceTests.cs
    │   ├── PreparedDisplayResultCacheTests.cs   # new
    │   └── DeferredRegenerationTests.cs         # new
    └── Runtime/
        ├── PerformanceRuntimeTests.cs
        └── DeferredRegenerationRuntimeTests.cs  # new
```

**Structure Decision**: 既存 UPM 構造を維持し、再生成性能の責務を `SolidText3DComponent` の巨大メソッドへさらに押し込まず、署名・prepared result・cache・request state を internal 型へ分離する。`GlyphMeshBuilder` / `MeshExtruder` には「Unity Mesh を作る前の純粋データ準備」境界を抽出し、deferred path と sync path が同じ準備ロジックを共有する。

## Phase 0: Research Output

Phase 0 では次を確定した。

- 現行ベースラインは manual-only / sync-only / no-cache であり、`LateUpdate()` の自動再生成や既存 deferred API は存在しない。古い specs より runtime code と changelog を正とする。
- `RegenerateMesh()` は後方互換対象として維持し、high-frequency 用の振る舞いは別 API で追加する。
- 非同期化の境界は `Mesh` / `GameObject` 適用の前に置く。`GlyphMeshData` のような pure data を worker で準備し、Unity オブジェクト生成だけを main thread へ戻す。
- 再利用は global cache ではなく per-component LRU cache とし、request ordering は component-local な version と latest-only queue で解決する。
- 失敗時は「最後に完成済みだった表示を維持し、失敗イベントを通知する」ポリシーを採る。

詳細は [research.md](research.md) を参照。

## Phase 1: Design Decisions

### 1. 実装を基準に同期 API を固定する

- 現行 `SolidText3DComponent.RegenerateMesh()` は「呼び出し復帰時点で mesh が反映済み」という契約を持つため、ここは feature 後も同期 API のまま維持する。
- 既存の dirty フラグ、空文字列処理、フォント欠落時の直前表示維持、same-signature skip はすべて残す。
- 旧 spec にあった `LateUpdate()` 自動再生成や inspector debounce はベースラインから除外し、今回の設計判断では参照しない。

### 2. 再生成を request capture / prepare / apply の 3 段へ分離する

- `SolidText3DComponent` から直接 `GlyphMeshBuilder.Build()` / `BuildPerCharacter()` / `OutlineMeshBuilder.Build()` を呼び切るのではなく、まず component state を immutable な `TextStateRequest` として切り出す。
- `prepare` 段では request から `PreparedDisplayResult` を生成する。ここには本体 mesh 用の頂点・index・normal 群、PerCharacter 用 glyph data、outline 用 mesh data、最終署名を含める。
- `apply` 段だけが Unity の `Mesh`, `MeshFilter`, `MeshRenderer`, child GameObject を更新する。
- sync path は `capture -> prepare -> apply` を同一呼び出し内で完遂し、deferred path は `capture` 後に `prepare` を外へ逃がす。

### 3. public API は additive な latest-only request に限定する

- 新しい high-frequency 用 API は `RequestRegenerateMesh()` とし、同期の `RegenerateMesh()` とは呼び分ける。
- 補助の公開面は最小限に留める。
  - `public void RequestRegenerateMesh();`
  - `public bool HasPendingRegeneration { get; }`
  - `public event Action<RegenerationFailureInfo> DeferredRegenerationFailed;`
- `SuppressAutoRegenerate` は既存互換のため当面残すが、新 feature の制御点としては使わない。

### 4. request ordering は version と latest slot で管理する

- 各 component は単調増加する `requestVersion` を持つ。
- deferred path では `inFlightRequest` を 1 件だけ維持し、追加要求は FIFO に積まず `pendingLatestRequest` を常に上書きする。
- `prepare` 完了時に ready result の version が `lastRequestedVersion` より古ければ apply せず破棄する。
- これにより、古い値の完成結果が後から表示を巻き戻すことを防ぐ。

### 5. prepared result cache は per-component / bounded / LRU にする

- cache key は `DisplayResultSignature` とし、少なくとも text、font source、font size、layout、outline、object mode など見た目に影響する入力を含める。
- cache scope は component-local に限定する。これにより multi-object 更新時の lock contention と相互 eviction を避ける。
- capacity は初版では internal 定数または非公開 serialize field で固定し、公開設定にはしない。YAGNI を優先する。
- cache hit 時は `prepare` を省略し、すぐ apply へ進める。same-signature current display の場合は apply も不要として return する。

### 6. 既存 builder は pure-data 境界を共有する

- 現在の `GlyphMeshBuilder.Build()` と `BuildPerCharacter()` は最終的に Unity `Mesh` を返すため、deferred path ではそのまま worker に載せにくい。
- そのため `GlyphMeshData` を中心に、Mesh 作成前の頂点・index・normal 集約処理を shared helper 化する。
- body / outline / per-character のいずれも同じ preparation core を通し、sync path と deferred path の visual parity を保証する。

### 7. failure policy は keep-last-good + observable error に統一する

- deferred prepare が失敗しても current visible mesh は維持し、空メッシュや中間状態へ切り替えない。
- failed request は再試行キューへ戻さず破棄し、`DeferredRegenerationFailed` イベントで request version / message を通知する。
- sync path の失敗挙動は現行 `RegenerateMesh()` の早期 return / warning と整合するように揃える。

## Validation Plan

### Focused Edit Mode Tests

- `SolidText3DComponentTests`: `RegenerateMesh()` が同期完了 API のまま動作し、same-signature skip、空文字列クリア、フォント欠落時の直前表示維持を壊していないこと
- `PreparedDisplayResultCacheTests`: cache hit、見た目変更時 miss、capacity 上限到達時の LRU eviction、evict 後も新規登録が継続すること
- `DeferredRegenerationTests`: pending request が latest 1 件へ集約されること、古い completed result が apply されないこと、failure 時に visible mesh が維持されること
- `PerformanceTests`: same-signature sync skip の GC zero 維持、deferred request submit の軽量性、cache hit path が cold build より短いこと

### Focused Play Mode / Runtime Tests

- `DeferredRegenerationRuntimeTests`: timer 相当の更新を 1 component / 複数 component に対して流し、最終表示が常に最新要求へ一致すること
- `DeferredRegenerationRuntimeTests`: 1 つの重い component 更新が他 component の apply まで一律停止させないこと
- `PerformanceRuntimeTests`: 既存 20 component / 30 frame ベンチを回帰基準として維持し、新 feature が clean-frame performance を悪化させないこと

### Manual / Profiler Validation

- Unity Profiler で `SolidText3DComponent.RegenerateMesh`, `GlyphMeshBuilder.Build`, `MeshExtruder.BuildGlyphMesh`, `OutlineContourBuilder.BuildProfiles`, `OutlineMeshBuilder.Build` の dirty frame を比較する
- high-frequency sample を 10 Hz 以上で 60 秒動かし、latest-only 反映・巻き戻りなし・失敗時 keep-last-good を目視確認する
- Unity 6 LTS（6000.x）で Windows player の手動ビルド確認を tasks に必須タスクとして入れる

## Complexity Tracking

憲法違反なし。複雑度増加は request state、prepared result、cache の 3 要素に限定される。より大きな scheduler、global cache、複数優先度キュー、public tuning surface は初版では採用しない。
