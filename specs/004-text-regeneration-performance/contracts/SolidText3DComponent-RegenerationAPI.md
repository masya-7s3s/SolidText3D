# Contract: SolidText3DComponent Regeneration API v2.2.0

**Branch**: `004-text-regeneration-performance` | **Date**: 2026-05-17  
**SemVer**: `2.1.0 -> 2.2.0` (MINOR: additive API only)

## Baseline Preservation

以下は既存契約として維持する。

```csharp
/// <summary>
/// メッシュを即時再生成する。
/// 呼び出し復帰時点で表示結果は反映済みである。
/// </summary>
public void RegenerateMesh();

/// <summary>
/// 手動で再生成が必要かどうかを示す。
/// </summary>
public bool IsDirty { get; }
```

- `RegenerateMesh()` は今後も同期 API のまま維持する
- 内部的に cache や shared preparation core を使ってよいが、呼び出し側の観測可能契約は変えない
- dirty 状態は自動再生成しない。明示 API 呼び出しまで保持する

## New Additive API

### `RequestRegenerateMesh`

```csharp
/// <summary>
/// 高頻度更新向けの deferred regeneration を要求する。
/// 呼び出し時点では表示反映を保証しない。
/// 進行中 1 件を除く待機要求は常に最新 1 件へ集約される。
/// </summary>
public void RequestRegenerateMesh();
```

**Contract**:

- 呼び出しは non-blocking を目標とする
- 進行中 request は 1 件のみ
- 待機 queue は FIFO ではなく latest-only
- 完成結果が current request より古い場合、表示へ適用してはならない
- submit path は current thread の余分な allocation を避ける

### `HasPendingRegeneration`

```csharp
/// <summary>
/// deferred regeneration の進行中または待機中 request があるかを返す。
/// </summary>
public bool HasPendingRegeneration { get; }
```

**Contract**:

- `InFlightRequest` または `PendingLatestRequest` が存在する間は `true`
- apply 完了または全 request 破棄後は `false`
- sync path のみ使用中は `false` のままでよい

### `DeferredRegenerationFailed`

```csharp
/// <summary>
/// deferred regeneration が失敗したときに通知する。
/// 失敗しても current visible display は維持される。
/// </summary>
public event System.Action<RegenerationFailureInfo> DeferredRegenerationFailed;
```

**Payload**:

```csharp
public readonly struct RegenerationFailureInfo
{
    public long RequestVersion { get; }
    public string RequestedText { get; }
    public string Message { get; }
}
```

**Contract**:

- failed request は表示へ適用されない
- current visible display は keep-last-good を維持する
- event は main thread で発火する
- event payload に内部例外型や stack trace 全文を必須公開しない

## Ordering Semantics

```text
Call order:
  Request A
  Request B
  Request C

Allowed processing:
  A in-flight, B overwritten by C, apply A (only if still current), then apply C

Forbidden:
  A complete after C request and roll back visible text to A
  A/B/C all queued and drained FIFO while latest value waits
```

## Cache Semantics

- prepared result cache は public API ではなく internal implementation detail とする
- ただし observable behavior として、同一 display signature の再要求は cold build より短時間で適用されることを目標にする
- cache 上限到達時は least-recently-used entry から破棄する
- cache-hit latency は長時間運用後も中央値の 10% 超劣化を避けることを目標にする

## Validation Thresholds

- deferred submit: 95 percentile で 50ms 以下
- heavy/light 同時更新: 90 percentile で 100ms 以下
- 5 object 同時更新: 90 percentile で 100ms 以下
- cache-hit latency drift: 長時間運用後の中央値が初期中央値 +10% 以内

## Failure / Empty State Semantics

- 空文字列要求は deferred path でも安全に表示を消去する
- フォント欠落や prepare failure では古い completed display を維持する
- sync path の warning / early return と deferred path の failure event は意味的に矛盾しないこと

## Compatibility Notes

- `SuppressAutoRegenerate` は互換のため残してよいが、本 contract では deferred API の主制御点とはみなさない
- 既存の `Text`, `FontAsset`, `Outline*`, `ObjectMode` などの public properties はそのまま利用する
- docs / README / CHANGELOG は `RequestRegenerateMesh()` 追加に合わせて更新する
