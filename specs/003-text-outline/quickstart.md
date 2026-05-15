# Quickstart: テキストアウトライン生成（再設計版）

**Branch**: `003-text-outline-alt` | **Date**: 2026-05-12

## 前提

- `SolidText3DComponent` が GameObject にアタッチされている
- フォントと文字列が設定済みである
- Unity 6 (6000.x LTS) 環境で package が import 済みである

## 1. Inspector から outline を有効化する

1. 対象 GameObject を選択する
2. `SolidText3DComponent` の `Outline` セクションを開く
3. `Enabled` をオンにする
4. `Offset Amount`、`Thickness`、`Display Mode`、`Material` を設定する

**期待結果**:

- 正面からは文字外周に沿ったリング状シルエットが表示される
- `Donut` と `BackFilled` を切り替えても正面シルエットは変わらない
- `BackFilled` では厚さ変更時も背面位置が固定される
- `Offset Amount = 0` にすると outline は表示されず、後で 0 より大きい値に戻すと同じ child GO に再生成される

## 2. コードから設定する

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class OutlineSample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent _text;
    [SerializeField] private Material _outlineMaterial;

    private void Start()
    {
        _text.OutlineEnabled = true;
        _text.OutlineOffset = 0.05f;
        _text.OutlineThickness = 0.1f;
        _text.OutlineMaterial = _outlineMaterial;
        _text.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
    }
}
```

## 3. mode ごとの見え方を確認する

### Donut

- 2D 断面は `offset(originalFilled) - originalFilled`
- 厚みは中央基準で前後へ均等に配分される
- 背面から見ると中央に文字本体が抜けて見える

### BackFilled

- front silhouette は Donut と同一
- 背面は `bodyBack - 0.0001f` に固定される
- 背面から見ると太字状に埋まって見える

## 4. 実装検証の最短手順

1. `OutlineContourBuilderTests` を実行し、ring profile と winding parity が通ることを確認する
2. `OutlineMeshBuilderTests` を実行し、front cap が存在することを確認する
3. `Offset Amount` を `0` にして outline が非表示になり、再び増やすと再表示されることを確認する
4. Unity Editor で正面表示と背面表示を目視確認する

## 5. 検証結果スナップショット

- 2026-05-13: Edit Mode / Play Mode テストは all green を確認
- outline の clean frame 回帰は `PerformanceTests.OutlineEnabled_CleanLateUpdate_AllocatesZeroBytes` で確認対象に追加
- profiler sample 追加箇所: `OutlineContourBuilder.BuildProfiles`, `OutlineMeshBuilder.Build`, `SolidText3DComponent.RegenerateMesh`, `SolidText3DComponent.UpdateOutlineMesh`
- 2026-05-13: Profiler 上で dirty frame marker の厳密採取は見送った。実用上のパフォーマンス問題が出ていないこと、outline clean frame 回帰テストを追加済みであることをもって performance polish を受け入れる

### Profiler 証跡の取得手順

1. Unity Editor で `Assets/Scenes/SampleScene.unity` を開く
2. Hierarchy 上で `SolidText3DComponent` を持つサンプルオブジェクトを 1 つ選ぶ
3. Inspector で `Outline` を有効化し、`Offset Amount = 0.05`, `Thickness = 0.1`, `Display Mode = Donut` を設定する
4. Window > Analysis > Profiler を開く
5. CPU Usage を選び、Deep Profile はオフのまま Record を開始する
6. まず 120 フレーム程度アイドル状態で流し、clean frame の `SolidText3DComponent.LateUpdate` がほぼ即 return していることを確認する
7. 次に `OutlineOffset` を `0.05 -> 0.08 -> 0.05` と変え、dirty frame で `SolidText3DComponent.RegenerateMesh`, `SolidText3DComponent.UpdateOutlineMesh`, `OutlineContourBuilder.BuildProfiles`, `OutlineMeshBuilder.Build` が 1 回ずつ現れることを確認する
8. CPU Usage の Hierarchy または Timeline で上記 marker のフレームを選び、スクリーンショットを保存する
9. 保存する証跡は最低 2 枚: clean frame 1 枚、dirty frame 1 枚

### T039 の確認手順

1. Test Runner で `MasaChuang.SolidText3D.Tests.Editor.PerformanceTests` を実行する
2. `OutlineEnabled_CleanLateUpdate_AllocatesZeroBytes` が green なら、clean frame の追加 GC.Alloc なしを確認済みとして扱う
3. その後、Profiler の clean frame でも `GC Alloc` 列が 0 B 付近であることを確認する
4. 上の 2 条件が満たせたら T039 を完了にしてよい

### 実施メモ

- 本 feature では `OutlineEnabled_CleanLateUpdate_AllocatesZeroBytes` の追加と Edit Mode / Play Mode green を完了済み
- dirty frame marker の厳密採取は環境差で安定しなかったため、ユーザー判断で実用上問題なしとして受け入れた

## 6. トラブルシューティング

| 症状 | 確認ポイント |
| --- | --- |
| 正面だけ outline が見えない | front face 専用ロジックではなく、`RingContoursEm` が body と同じ押し出しコアに渡っているか確認する |
| offset path は取れているのに面が出ない | `OutlineContourBuilder` の出力が raw path ではなく final ring profile になっているか確認する |
| `Offset Amount = 0` でも outline child が消えてほしくない | child GO のライフサイクルが `OutlineEnabled` のみで制御され、offset 0 では mesh クリアだけを行っているか確認する |
| Donut と BackFilled で正面形状がズレる | 両 mode が同じ `RingContoursEm` を共有しているか確認する |
| BackFilled の背面位置が動く | shell の Z 配置と rear cap の anchor が `bodyBack - 0.0001f` に固定されているか確認する |

## 7. 手動ビルド記録

### Windows player build 実施手順

1. Unity Editor で File > Build Profiles を開く
2. Platform を Windows に切り替え、Architecture は普段使っている配布設定に合わせる
3. Scene List に有効な scene が入っていることを確認する。現状の Build Settings では `Assets/Samples/Solid Text 3D/2.0.0/CJK Example/CJKExample.unity` が有効
4. Build を実行し、出力先を `Builds/Windows-OutlineValidation` などの新規フォルダにする
5. build 完了後、生成された exe を起動する
6. SampleScene で text 本体と outline が表示されることを確認する
7. 可能なら inspector 相当の設定済みオブジェクトで `Donut` と `BackFilled` の見え方を確認する
8. 目視確認後、このセクションの記録欄に日時、Unity バージョン、出力先、結果を追記する

補足:

- `-buildWindows64Player` の batchmode 実行はこの環境で成果物なし / return code 1 だったため、Windows build は Editor からの手動実行を正とする

### 記録欄

- Date:
- Unity:
- Scene:
- Build Output:
- Result:
- Notes:
