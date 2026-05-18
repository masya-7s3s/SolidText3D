# Data Model: Text Regeneration Performance

**Branch**: `004-text-regeneration-performance` | **Date**: 2026-05-17

## Public API Additions

### `RegenerationFailureInfo`

```csharp
namespace MasaChuang.SolidText3D
{
    /// <summary>
    /// deferred regeneration failure の通知ペイロード。
    /// </summary>
    public readonly struct RegenerationFailureInfo
    {
        public long RequestVersion { get; }
        public string RequestedText { get; }
        public string Message { get; }
    }
}
```

| Field | Type | Description |
| --- | --- | --- |
| `RequestVersion` | `long` | 失敗した request の単調増加 version |
| `RequestedText` | `string` | 失敗時点の要求テキスト。ログやデバッグで使う |
| `Message` | `string` | 呼び出し側へ返す簡潔な失敗理由 |

## Internal Entities

### `TextStateRequest`

```csharp
namespace MasaChuang.SolidText3D
{
    internal readonly struct TextStateRequest
    {
        public long Version { get; }
        public DisplayResultSignature Signature { get; }
        public string Text { get; }
        public byte[] FontData { get; }
        public MeshGenerationParams GenerationParams { get; }
        public bool OutlineEnabled { get; }
        public OutlineSettings Outline { get; }
        public ObjectMode ObjectMode { get; }
    }
}
```

| Field | Validation / Notes |
| --- | --- |
| `Version` | component ごとに単調増加。stale result 判定に使用 |
| `Signature` | request capture 時に確定。skip / cache / stale discard の共通 key |
| `Text` | `null` は空文字列へ正規化 |
| `FontData` | capture 時点で snapshot を取る。deferred prepare 中に asset 差し替えが起きても request 自体は不変 |
| `GenerationParams` | visual parity のため sync / deferred 共通で使用 |
| `OutlineEnabled`, `Outline` | outline mesh 準備と invalidation に使用 |
| `ObjectMode` | `SingleObject` / `PerCharacter` を明示し、prepared result の形を決める |

### `DisplayResultSignature`

```csharp
namespace MasaChuang.SolidText3D
{
    internal readonly struct DisplayResultSignature : System.IEquatable<DisplayResultSignature>
    {
        public int HashCode { get; }
        public int FontSourceId { get; }
        public ObjectMode ObjectMode { get; }
    }
}
```

**役割**:

- current display と request の同一性判定
- prepared-result cache key
- stale apply の簡易比較

**入力要素**:

- `Text`
- font source identity (`_fontAsset` / `_fontBytesCache`)
- `ExtrusionDepth`, `FontSize`, `LetterSpacing`, `LineSpacing`
- `HorizontalAnchor`, `VerticalAnchor`, `DepthAnchor`, `WritingMode`
- `MaxWidth`, `MaxHeight`, `RotateAsciiInVertical`
- `OutlineEnabled`, `OutlineOffset`, `OutlineThickness`, `OutlineDisplayMode`, `OutlineMaterial`
- `ObjectMode`

### `PreparedDisplayResult`

```csharp
namespace MasaChuang.SolidText3D
{
    internal sealed class PreparedDisplayResult
    {
        public long Version { get; set; }
        public DisplayResultSignature Signature { get; set; }
        public GlyphMeshData BodyMeshData { get; set; }
        public GlyphMeshData OutlineMeshData { get; set; }
        public System.Collections.Generic.List<GlyphMeshData> PerCharacterMeshData { get; set; }
        public bool ClearsDisplay { get; set; }
    }
}
```

| Field | Description |
| --- | --- |
| `Version` | result がどの request から作られたかを示す |
| `Signature` | apply 前の current-state 比較と cache 収納に使用 |
| `BodyMeshData` | `SingleObject` 本体表示用 |
| `OutlineMeshData` | outline enabled 時の child mesh 用 |
| `PerCharacterMeshData` | `PerCharacter` mode 用の glyph 別データ |
| `ClearsDisplay` | 空文字列要求や可視データなしで current display を消去することを明示 |

### `PreparedResultCacheEntry`

```csharp
namespace MasaChuang.SolidText3D
{
    internal sealed class PreparedResultCacheEntry
    {
        public DisplayResultSignature Signature { get; set; }
        public PreparedDisplayResult Result { get; set; }
        public long LastUsedTick { get; set; }
    }
}
```

**契約**:

- cache capacity を超えたら `LastUsedTick` が最小の entry から evict する
- evict 後も新規 entry 登録は継続する
- failure result は cache しない

### `DeferredRegenerationState`

```csharp
namespace MasaChuang.SolidText3D
{
    internal sealed class DeferredRegenerationState
    {
        public long LastRequestedVersion { get; set; }
        public long LastAppliedVersion { get; set; }
        public TextStateRequest? InFlightRequest { get; set; }
        public TextStateRequest? PendingLatestRequest { get; set; }
        public PreparedDisplayResult ReadyResult { get; set; }
        public DisplayResultSignature LastAppliedSignature { get; set; }
    }
}
```

| Field | Description |
| --- | --- |
| `LastRequestedVersion` | submit 済み request の最大 version |
| `LastAppliedVersion` | 実際に visible display へ反映済みの version |
| `InFlightRequest` | 現在 prepare 中の 1 件 |
| `PendingLatestRequest` | 待機 latest 1 件。新要求到着時は上書き |
| `ReadyResult` | main thread apply 待ちの prepared result |
| `LastAppliedSignature` | same-display skip と cache hit 判定の基準 |

## State Transitions

### Sync Path

```text
[Dirty]
  -> CaptureRequest
  -> SignatureCheck
  -> CacheHit ? ApplyCached : PrepareImmediately
  -> Apply
  -> [Clean]
```

### Deferred Path

```text
[Idle]
  -> RequestRegenerateMesh()
  -> [InFlight]
  -> (newer request) overwrite PendingLatestRequest
  -> Prepare completes
  -> version stale ? discard : ReadyResult
  -> Apply on main thread
  -> PendingLatestRequest exists ? move to InFlight : [Idle]
```

### Failure Path

```text
[InFlight]
  -> Prepare throws / returns invalid
  -> Keep current visible display
  -> Raise DeferredRegenerationFailed
  -> Drop failed request
  -> PendingLatestRequest exists ? start next : [Idle]
```

## Invariants

- `RegenerateMesh()` は呼び出し復帰時点で `LastAppliedVersion == LastRequestedVersion` を満たす
- deferred path では `LastAppliedVersion` は単調増加し、古い version が後から適用されない
- `PendingLatestRequest` は最大 1 件のみ
- `PreparedDisplayResultCache` の entry 数は capacity 以下
- current visible display と `LastAppliedSignature` は常に一致する

## Validation Rules

- `DisplayResultSignature` は見た目に影響する入力が 1 つでも変われば不一致になること
- `PreparedDisplayResult` は sync path 新規生成結果と視覚的に同一であること
- `ClearsDisplay == true` の result は古い文字を残さないこと
- `PerCharacterMeshData` を持つ result は `CharacterObjectPool` の reuse 契約を破らないこと
