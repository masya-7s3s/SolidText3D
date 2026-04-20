# 公開 API コントラクト: Solid Text 3D

**フィーチャーブランチ**: `001-solid-text-3d`  
**作成日**: 2026年4月17日  
**バージョン**: v1.0.0  
**参照仕様**: `specs/001-solid-text-3d/spec.md`（FR-012）

---

## 概要

このドキュメントは `SolidText3DComponent` が外部に公開する C# API の契約を定義する。  
本契約に対して Edit Mode テスト・統合テストが記述される。

---

## 名前空間

```csharp
namespace YourCompany.SolidText3D
{
    // SolidText3DComponent, GlyphMeshBuilder, etc.
}
```

> ⚠️ `YourCompany` 部分はリリース前に実際の会社名／個人識別子に変更すること。  
> 例: `com.masya.solidtext3d` → 名前空間 `Masya.SolidText3D`

---

## `SolidText3DComponent` (MonoBehaviour)

### プロパティ

#### `Text`

```csharp
/// <summary>
/// 表示するテキスト文字列。Unicode (CJK を含む) をサポートする。
/// 変更するとダーティフラグが立ち、次の LateUpdate でメッシュが再生成される。
/// </summary>
/// <remarks>
/// 空文字を設定した場合、メッシュは 0 ポリゴン状態になる（エラーは発生しない）。
/// </remarks>
public string Text { get; set; }
```

**契約**:

- `null` を設定した場合は空文字として扱う
- 取得時は常に非 null の文字列を返す

---

#### `Font`

```csharp
/// <summary>
/// 3D メッシュ生成に使用する Unity フォントアセット（TTF/OTF）。
/// 変更するとダーティフラグが立ち、次の LateUpdate でメッシュが再生成される。
/// </summary>
/// <remarks>
/// null に設定した場合はデフォルトフォントにフォールバックし、
/// Debug.LogWarning が出力される。
/// </remarks>
public UnityEngine.Font Font { get; set; }
```

---

#### `ExtrusionDepth`

```csharp
/// <summary>
/// 文字メッシュの押し出し深さ（ワールド空間単位）。0 以上の値を指定する。
/// 0 を指定するとフラットな 2D メッシュが生成される。
/// </summary>
/// <remarks>
/// 負の値を設定した場合は 0 にクランプされ、Debug.LogWarning が出力される。
/// </remarks>
public float ExtrusionDepth { get; set; }
```

**契約**:

- 取得時は常に `>= 0.0f` の値を返す
- `0.0f` を設定した場合、前面ポリゴンのみの 2D メッシュが生成される

---

#### `OutlineWidth`

```csharp
/// <summary>
/// 文字輪郭のアウトライン幅（ワールド空間単位）。0 以上の値を指定する。
/// 0 を指定するとアウトラインは生成されない。
/// </summary>
/// <remarks>
/// 負の値を設定した場合は 0 にクランプされ、Debug.LogWarning が出力される。
/// アウトラインの色・マテリアルは MeshRenderer で管理する。
/// </remarks>
public float OutlineWidth { get; set; }
```

**契約**:

- 取得時は常に `>= 0.0f` の値を返す
- `0.0f` の場合、アウトライン用ジオメトリは生成されない

---

### 使用例

#### C# スクリプトからの基本的な使用

```csharp
using YourCompany.SolidText3D;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    private SolidText3DComponent _solidText;
    private int _score;

    void Awake()
    {
        _solidText = GetComponent<SolidText3DComponent>();
    }

    // スコアを更新する（次の LateUpdate でメッシュが自動再生成される）
    public void SetScore(int score)
    {
        _score = score;
        _solidText.Text = $"Score: {score}";
    }
}
```

#### Inspector での設定（エディタ）

```text
GameObject
├── SolidText3DComponent
│   ├── Text: "立体文字"
│   ├── Font: [MyFont.ttf]
│   ├── Extrusion Depth: 0.1
│   └── Outline Width: 0.0
├── MeshFilter    ← SolidText3DComponent が自動追加
└── MeshRenderer  ← SolidText3DComponent が自動追加
```

---

## `GlyphMeshBuilder` (static class, 内部公開)

> ⚠️ このクラスはパッケージ内部で使用する高度な API。通常のユーザーは `SolidText3DComponent` を経由すること。

```csharp
/// <summary>
/// テキストパラメータから Unity Mesh を生成するサービスクラス。
/// </summary>
public static class GlyphMeshBuilder
{
    /// <summary>
    /// 指定パラメータで 3D テキストメッシュを生成して返す。
    /// </summary>
    /// <param name="p">メッシュ生成パラメータ</param>
    /// <returns>生成された Unity Mesh。失敗時は空の Mesh を返す（例外は発生しない）</returns>
    public static Mesh Build(MeshGenerationParams p);
}
```

---

## `MeshGenerationParams` (struct, 内部公開)

```csharp
/// <summary>
/// GlyphMeshBuilder.Build() に渡すメッシュ生成パラメータ。
/// </summary>
public readonly struct MeshGenerationParams
{
    public string Text { get; init; }
    public byte[] FontData { get; init; }     // フォントのバイナリデータ
    public float ExtrusionDepth { get; init; }
    public float OutlineWidth { get; init; }
    public float LetterSpacing { get; init; }
    public float LineSpacing { get; init; }
    public float BezierErrorThreshold { get; init; }
}
```

---

## エラー処理の契約

| シナリオ | 挙動 | ログレベル |
| --- | --- | --- |
| `Font == null` | デフォルトフォントで生成を継続 | `Debug.LogWarning` |
| フォントファイルが削除済み | デフォルトフォントで生成を継続 | `Debug.LogWarning` |
| グリフが収録されていない文字 | 空グリフ（送り幅のみ）としてスキップ | なし |
| `Text` が空または null | 0 ポリゴンの空メッシュを返す | なし |
| `ExtrusionDepth < 0` | 0 にクランプして生成継続 | `Debug.LogWarning` |
| `OutlineWidth < 0` | 0 にクランプして生成継続 | `Debug.LogWarning` |
| メッシュ生成中の予期しない例外 | 空メッシュを返す | `Debug.LogError` |

**方針**: いかなる場合もコンポーネントの例外でゲームオブジェクトが壊れることはない。

---

## 後方互換性の保証

- v1.x の範囲で `Text`, `Font`, `ExtrusionDepth`, `OutlineWidth` プロパティのシグネチャを維持する
- プロパティの削除・リネームは MAJOR バージョンアップ（v2.0）まで行わない
- 内部実装クラス（`GlyphMeshBuilder`, `MeshExtruder` 等）は `internal` または `public` でも breaking change なしに変更できる旨を `CHANGELOG.md` に記載する
