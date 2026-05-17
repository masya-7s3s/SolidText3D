# Quickstart: Text Regeneration Performance

**Branch**: `004-text-regeneration-performance` | **Date**: 2026-05-17

## Goal

この feature 完了後は、既存の同期 `RegenerateMesh()` を維持したまま、高頻度更新では `RequestRegenerateMesh()` を使って latest-only な追従と prepared-result reuse を利用できる。

## 1. 既存 sync path を使う

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class SyncExample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent _text;

    public void ApplyImmediate(string value)
    {
        _text.Text = value;
        _text.RegenerateMesh();
    }
}
```

期待結果:

- 呼び出し復帰時点で表示が更新済みである
- same-display なら重い再生成をスキップする
- 空文字列なら古い文字を残さず消去する

## 2. 高頻度更新では deferred path を使う

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class TimerExample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent _text;

    private void Awake()
    {
        _text.DeferredRegenerationFailed += OnDeferredRegenerationFailed;
    }

    private void OnDestroy()
    {
        _text.DeferredRegenerationFailed -= OnDeferredRegenerationFailed;
    }

    public void UpdateTimer(int totalSeconds)
    {
        _text.Text = totalSeconds.ToString("D2");
        _text.RequestRegenerateMesh();
    }

    private static void OnDeferredRegenerationFailed(RegenerationFailureInfo info)
    {
        Debug.LogWarning($"Deferred regeneration failed: v={info.RequestVersion} msg={info.Message}");
    }
}
```

期待結果:

- request submit 自体は重くならない
- 進行中 1 件の間に追加 request が来たら待機 latest 1 件だけが残る
- 古い completed result が後から表示を巻き戻さない
- failure 時も直前の completed display は維持される

## 3. cache hit を確認する

1. 同じ text と見た目設定で 2 回以上切り替える
2. 1 回目は cold build、2 回目以降は prepared-result reuse が走ることを確認する
3. outline 設定や font size を変えた場合は miss になり、変更後の見た目で再生成されることを確認する

## 4. Edit Mode tests を実行する

Unity Test Runner または batchmode で以下を実行する。

- `MasaChuang.SolidText3D.Tests.Editor.SolidText3DComponentTests`
- `MasaChuang.SolidText3D.Tests.Editor.PreparedDisplayResultCacheTests`
- `MasaChuang.SolidText3D.Tests.Editor.DeferredRegenerationTests`
- `MasaChuang.SolidText3D.Tests.Editor.PerformanceTests`

確認ポイント:

- sync path の後方互換が崩れていない
- latest-only queue が成立している
- LRU eviction が正しく動く
- clean-frame の追加 GC.Alloc が 0 のまま維持される

## 5. Play Mode / Runtime validation を実行する

- `MasaChuang.SolidText3D.Tests.Runtime.DeferredRegenerationRuntimeTests`
- `MasaChuang.SolidText3D.Tests.Runtime.PerformanceRuntimeTests`

確認ポイント:

- 5 object 以上の同時更新でも最新値への追従が破綻しない
- 1 つの重い object が他 object の apply を一律停止させない
- baseline runtime performance を大きく悪化させない

## 6. Profiler で確認する

1. Unity Editor で sample scene を開く
2. 12 文字以内の timer 表示を 10 Hz で更新する driver を用意する
3. CPU Usage を記録し、`SolidText3DComponent.RegenerateMesh` と deferred apply 周辺を観測する
4. 同一表示へ戻るケースで cold build より短い apply になっていることを確認する
5. 複数 object 同時更新時に入力・アニメーション・他 object 更新が完全停止しないことを確認する

## 7. Windows build gate

1. Unity 6 LTS（6000.x）で Windows player build を実行する
2. sample scene または feature validation scene で timer / score 更新を確認する
3. 結果をこのファイル末尾または tasks 完了メモへ記録する

## Record

- Date:
- Unity:
- Validation Scene:
- Edit Mode Tests:
- Play Mode Tests:
- Build Output:
- Result:
- Notes:
