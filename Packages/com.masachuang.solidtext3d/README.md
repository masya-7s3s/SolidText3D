# Solid Text 3D

TTF/OTF フォントのグリフから 3D ポリゴンメッシュを生成する Unity UPM パッケージです。CJK テキスト、ランタイム更新、Per-Character 配置に加えて、文字本体とは独立した outline を Donut / BackFilled の 2 モードで生成できます。

## インストール

1. Unity Package Manager を開く
2. 「+」→ 「Add package from disk...」を選ぶ
3. Packages/com.masachuang.solidtext3d/package.json を指定する

manifest.json に直接記述する場合の例:

```json
{
  "dependencies": {
    "com.masachuang.solidtext3d": "file:../Packages/com.masachuang.solidtext3d"
  }
}
```

## 基本使用

1. GameObject に SolidText3DComponent を追加する
2. Inspector の Text & Font で Text と Font Asset を設定する
3. Geometry で Extrusion Depth と Font Size を調整する
4. 必要なら Layout / Outline / Output を調整する
5. Mesh Update の Regenerate Mesh を押してメッシュを更新する

期待結果:

- Offset Amount を上げると文字外周に outline が生成される
- OutlineOffset が 0 のときは child は維持したまま outline mesh だけがクリアされる
- Donut と BackFilled は正面シルエットを共有し、背面構成だけが変わる
- 高頻度更新では `RequestRegenerateMesh()` を使うと latest-only に追従する

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class OutlineSample : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent _text;
    [SerializeField] private Material _outlineMaterial;

    private void Start()
    {
        _text.Text = "Solid Text 3D";
        _text.OutlineEnabled = true;
        _text.OutlineOffset = 0.05f;
        _text.OutlineThickness = 0.1f;
        _text.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
        _text.OutlineMaterial = _outlineMaterial;
      _text.RegenerateMesh();
    }
}
```

## Inspector 構成

- `Mesh Update`: dirty 状態の確認と `Regenerate Mesh`
- `Text & Font`: テキスト本文とフォント設定
- `Geometry`: 押し出し厚み、サイズ、文字間・行間
- `Layout`: 書字方向、アンカー、折り返し上限
- `Outline`: アウトラインの有効化と見た目調整
- `Output`: `SingleObject` / `PerCharacter` の切り替え

## 主要 API

### SolidText3DComponent

| プロパティ | 型 | 説明 |
| --------- | --- | ---- |
| `Text` | `string` | 表示テキスト |
| `FontAsset` | `UnityEngine.Object` | Inspector で割り当てるフォントアセット |
| `ExtrusionDepth` | `float` | 本体メッシュの押し出し深さ |
| `OutlineEnabled` | `bool` | outline child の生成・維持を切り替える |
| `OutlineOffset` | `float` | outline の外側オフセット量 |
| `OutlineThickness` | `float` | outline の厚み |
| `OutlineDisplayMode` | `OutlineDisplayMode` | `Donut` / `BackFilled` |
| `OutlineMaterial` | `Material` | null のとき本体 material を継承 |
| `LetterSpacing` | `float` | 文字間スペース |
| `LineSpacing` | `float` | 行間係数 |
| `FontSize` | `float` | em 高さを Unity 単位へ変換するスケール |
| `ObjectMode` | `ObjectMode` | `SingleObject` / `PerCharacter` |
| `IsDirty` | `bool` | 手動で再生成が必要かどうか |
| `HasPendingRegeneration` | `bool` | deferred regeneration の進行中/待機中 request があるかどうか |
| `RegenerateMesh()` | `void` | 即時再生成 |
| `RequestRegenerateMesh()` | `void` | 高頻度更新向け deferred 再生成 |
| `DeferredRegenerationFailed` | `event Action<RegenerationFailureInfo>` | deferred regeneration failure を keep-last-good で通知 |

### Regeneration API の使い分け

- `RegenerateMesh()` は同期 API で、呼び出し復帰時点で表示が更新済みです
- `RequestRegenerateMesh()` は non-blocking submit を目的とした deferred API で、進行中 1 件 + latest-only 待機 1 件に集約されます
- `HasPendingRegeneration` は deferred path の in-flight / pending / ready 状態が残る間 `true` です
- `DeferredRegenerationFailed` は失敗 request を通知しますが、直前の visible display は維持されます

### OutlineDisplayMode

| 値 | 説明 |
| --- | ---- |
| `Donut` | 厚みを前後に均等配分するリング状 outline |
| `BackFilled` | 正面シルエットを維持しつつ、固定背面を単一の filled cap で閉じる outline |

## カスタムフォント

1. TTF/OTF を Assets 配下へ配置する
2. 必要なら生成された .bytes TextAsset を FontAsset に割り当てる
3. ランタイムで直接与える場合は MeshGenerationParams.FontData に byte[] を設定する

## パフォーマンス

- 自動再生成を行わないため、通常フレームで追加 GC.Alloc を発生させない設計です
- `RequestRegenerateMesh()` の submit path は current thread での余分な allocation を避ける構成です
- performance validation 条件は `submit p95 <= 50ms`、`heavy/light および 5 object 同時更新の p90 <= 100ms`、`cache-hit median drift <= 10%` を基準にします
- profiler sample: GlyphMeshBuilder.Build, MeshExtruder.BuildGlyphMesh, OutlineContourBuilder.BuildProfiles, OutlineMeshBuilder.Build, SolidText3DComponent.RegenerateMesh, SolidText3DComponent.UpdateOutlineMesh
- 2026-05-13 時点で Edit Mode / Play Mode テストは green を確認済みです

## 既知の制限

- SixLabors.Fonts は Unity 6 / CoreCLR 前提です
- very complex glyph では dirty 時の再生成コストが増えます
- Windows player の最終手動ビルド記録は quickstart に追記運用です

## ライセンス

MIT License。詳細は LICENSE.md を参照してください。サードパーティライセンスは Third Party Notices.md を参照してください。
