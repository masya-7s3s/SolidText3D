# Solid Text 3D

TTF/OTF フォントのグリフから 3D ポリゴンメッシュを生成する Unity UPM パッケージです。

## インストール手順

1. Unity Package Manager を開く（Window > Package Manager）
2. 「+」ボタン → 「Add package from disk...」を選択
3. `Packages/com.MasaChuang.SolidText3D/package.json` を選択

または `Packages/manifest.json` に直接記述：

```json
{
  "dependencies": {
    "com.MasaChuang.SolidText3D": "file:../Packages/com.MasaChuang.SolidText3D"
  }
}
```

## 基本的な使い方

1. GameObject に `SolidText3DComponent` を AddComponent する
2. Inspector で以下のパラメータを設定する：
   - **Text**: 表示するテキスト
   - **Font**: フォントファイルのパス（省略時は Noto Sans JP を使用）
   - **Extrusion Depth**: 押し出し深さ（Z 軸方向）
   - **Outline Width**: アウトライン幅
   - **Letter Spacing**: 文字間スペース
   - **Line Spacing**: 行間スペース

スクリプトからも変更できます：

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    private SolidText3DComponent _text3D;

    void Start()
    {
        _text3D = GetComponent<SolidText3DComponent>();
    }

    public void SetScore(int score)
    {
        _text3D.Text = $"Score: {score}";
    }
}
```

## 主要 API

### SolidText3DComponent

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `Text` | `string` | 表示テキスト |
| `Font` | `string` | フォントファイルパス |
| `ExtrusionDepth` | `float` | 押し出し深さ |
| `OutlineWidth` | `float` | アウトライン幅 |
| `LetterSpacing` | `float` | 文字間スペース |
| `LineSpacing` | `float` | 行間スペース（デフォルト 1.2） |
| `IsDirty` | `bool` | ダーティフラグ（読み取り専用） |
| `RegenerateMesh()` | `void` | 即時メッシュ再生成 |

### GlyphMeshBuilder

```csharp
// 直接メッシュを生成する場合
var p = new MeshGenerationParams
{
    Text = "Hello",
    FontPath = "path/to/font.ttf",
    ExtrusionDepth = 0.1f,
    BezierErrorThreshold = 0.0005f,
    LetterSpacing = 0f,
    LineSpacing = 1.2f
};
Mesh mesh = GlyphMeshBuilder.Build(p);
```

## カスタムフォントの割り当て方

1. TTF/OTF フォントファイルを `Assets/` 以下の任意の場所に配置する
2. `SolidText3DComponent` の `Font` フィールドにフォントファイルのパスを入力する
3. Inspector で変更すると自動的にメッシュが再生成される

## 既知の制限事項

- SixLabors.Fonts は net6.0 ビルドのみ対応（Unity 6 / CoreCLR 環境が必要）
- ランタイムビルドでは `Resources/Fonts/NotoSansJP-Regular.ttf` が埋め込まれている必要がある
- アウトライン幅機能は将来のバージョンで実装予定

## ライセンス

MIT License — 詳細は `LICENSE.md` を参照してください。  
サードパーティライセンスは `Third Party Notices.md` を参照してください。

