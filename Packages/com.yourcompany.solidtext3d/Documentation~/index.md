# Solid Text 3D — ドキュメント

## 概要

Solid Text 3D は TTF/OTF フォントのグリフから 3D ポリゴンメッシュを生成する Unity UPM パッケージです。
SixLabors.Fonts でグリフ輪郭を抽出し、LibTessDotNet で三角分割して、Unity の `Mesh` オブジェクトを生成します。

## API リファレンス

### SolidText3DComponent

`MonoBehaviour` として GameObject に追加して使用します。

| プロパティ/メソッド | 型 | 説明 |
| ------------------ | --- | ---- |
| `Text` | `string` | 表示テキスト。変更するとダーティフラグが立ち、次の LateUpdate でメッシュが再生成される |
| `Font` | `string` | フォントファイルのパス。省略時は Noto Sans JP を使用 |
| `ExtrusionDepth` | `float` | 押し出し深さ（Z 軸方向）。デフォルト: 0.1 |
| `OutlineWidth` | `float` | アウトライン幅（将来の拡張用）。デフォルト: 0 |
| `LetterSpacing` | `float` | 追加の文字間スペース。デフォルト: 0 |
| `LineSpacing` | `float` | 行間係数。デフォルト: 1.2（フォントサイズの 1.2 倍） |
| `IsDirty` | `bool` | ダーティフラグ（読み取り専用） |
| `RegenerateMesh()` | `void` | 即時メッシュ再生成（LateUpdate を待たずに実行） |

### MeshGenerationParams

メッシュ生成パラメータをまとめた struct です。

| フィールド | 型 | 説明 |
| ---------- | --- | ---- |
| `Text` | `string` | 表示テキスト |
| `FontPath` | `string` | フォントファイルパス |
| `FontData` | `byte[]` | フォントバイト配列（FontPath より優先される） |
| `ExtrusionDepth` | `float` | 押し出し深さ |
| `OutlineWidth` | `float` | アウトライン幅 |
| `LetterSpacing` | `float` | 文字間スペース |
| `LineSpacing` | `float` | 行間係数 |
| `BezierErrorThreshold` | `float` | ベジェ曲線の適応分割誤差閾値（デフォルト: 0.0005） |

### GlyphMeshBuilder（静的クラス）

```csharp
public static Mesh Build(MeshGenerationParams p);
```

パラメータに基づいてテキストの 3D メッシュを生成します。

### MeshExtruder（静的クラス）

```csharp
public static Mesh Build(List<GlyphContour> glyphs, MeshGenerationParams p);
public static GlyphMeshData BuildGlyphMesh(GlyphContour glyph, float extrusionDepth, float outlineWidth);
```

グリフ輪郭データから 3D メッシュを生成します。

## エディタでの使用ガイド

1. **GameObject を作成**: Hierarchy で右クリック → Create Empty
2. **コンポーネントを追加**: Inspector → Add Component → "Solid Text 3D Component"
3. **テキストを設定**: Inspector の `Text` フィールドにテキストを入力
4. **フォントを設定**（省略可）: `Font` フィールドにフォントファイルのパスを入力
5. **パラメータを調整**: `Extrusion Depth` / `Letter Spacing` / `Line Spacing` を調整

## ランタイムでの使用ガイド

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public class MyScript : MonoBehaviour
{
    private SolidText3DComponent _text3D;

    void Start()
    {
        _text3D = GetComponent<SolidText3DComponent>();
        _text3D.Text = "Hello, World!";
    }

    void Update()
    {
        // テキストを動的に変更（次の LateUpdate で自動再生成）
        if (Input.GetKeyDown(KeyCode.Space))
            _text3D.Text = "Changed!";
    }
}
```

## CJK 文字の使用

Noto Sans JP フォント（デフォルト埋め込み）は日本語・中国語・韓国語をサポートしています。

```csharp
_text3D.Text = "立体文字";  // 日本語
_text3D.Text = "汉字";     // 中国語
_text3D.Text = "한글";     // 韓国語
_text3D.Text = "Hello 世界"; // 混在テキスト
```

## トラブルシューティング

### メッシュが表示されない

- `SolidText3DComponent` に `MeshRenderer` と `MeshFilter` が自動追加されていることを確認してください
- `MeshRenderer` にマテリアルが設定されていない場合は、Inspector で任意のマテリアルを設定してください
- Console ウィンドウで警告・エラーを確認してください

### フォントが読み込まれない

- エディタ実行時: フォントファイルのパスが正しいことを確認してください
- ランタイムビルド時: `Resources/Fonts/NotoSansJP-Regular.ttf` が含まれていることを確認してください
- フォントファイルが存在しない場合、`Debug.LogWarning` が出力され空メッシュが返ります

### ランタイムでのフォントバイト制限

ランタイムビルドでは `Resources.Load<TextAsset>` でフォントを読み込みます。
カスタムフォントを使用する場合は `MeshGenerationParams.FontData` にバイト配列を直接渡してください。

### パフォーマンスについて

- テキスト変更はダーティフラグ経由で次フレームに処理されます（`LateUpdate` 内）
- `LateUpdate` 内でのアロケーションはゼロです（メッシュ再生成時のみアロケーション発生）
- 大量の 3D テキストオブジェクトがある場合は、変更しないオブジェクトの更新を避けるために `IsDirty` チェックが有効です

## 既知の制限事項

- SixLabors.Fonts は net6.0 ビルドのみ対応（Unity 6 / CoreCLR 環境が必要）
- アウトライン幅機能は現バージョンでは未実装（将来のバージョンで実装予定）
- 非常に複雑なグリフ（多数のベジェ曲線）では処理時間が増加する可能性があります
