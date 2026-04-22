# Breaking Changes — v2.0.0

このドキュメントでは v1.0.0 から v2.0.0 への**破壊的変更**をまとめます。  
アップグレードの際は以下の手順に従って移行してください。

---

## 1. `MeshGenerationParams.FontPath` の削除

### 変更内容

`MeshGenerationParams` 構造体の `FontPath` (string) プロパティが**削除**されました。

### 移行方法

フォントバイトを直接 `FontData` (byte[]) に渡してください。

#### 変更前 (v1.x)

```csharp
var p = new MeshGenerationParams
{
    FontPath = "Assets/Fonts/MyFont.ttf",
    Text = "Hello",
    // ...
};
```

#### 変更後 (v2.0)

```csharp
byte[] fontBytes = File.ReadAllBytes("Assets/Fonts/MyFont.ttf");
// または TextAsset.bytes / UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(...).bytes

var p = new MeshGenerationParams
{
    FontData = fontBytes,
    Text = "Hello",
    // ...
};
```

---

## 2. `SolidText3DComponent.Font` (string) の削除

### 変更内容

`SolidText3DComponent` の `Font` プロパティ（フォントファイルパスを表す string）が**削除**されました。

### 移行方法

Inspector 上で **FontAsset** フィールドに TTF/OTF ファイルをドラッグ＆ドロップしてください。  
スクリプトからは `FontAsset` プロパティ (UnityEngine.Object) を使用します。

#### 変更前 (v1.x)

```csharp
GetComponent<SolidText3DComponent>().Font = "Assets/Fonts/MyFont.ttf";
```

#### 変更後 (v2.0)

```csharp
// Inspector でフォントアセットを設定するか、スクリプトで代入する
var fontTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Fonts/MyFont.bytes");
GetComponent<SolidText3DComponent>().FontAsset = fontTextAsset;
```

> **ヒント**: `.ttf`/`.otf` を Unity プロジェクトにインポートすると、  
> `FontAssetPostprocessor` が自動的に隣接する `.bytes` ファイルを生成します。  
> 生成された `.bytes` TextAsset を `FontAsset` フィールドに割り当ててください。

---

## 3. 新規 API の概要

v2.0.0 で追加された主要な API を以下に示します。

| 追加項目 | 種別 | 説明 |
| --- | --- | --- |
| `HorizontalAnchor` | enum | 水平方向アンカー（Left / Center / Right） |
| `VerticalAnchor` | enum | 垂直方向アンカー（Upper / Middle / Lower） |
| `DepthAnchor` | enum | 奥行き方向アンカー（Front / Center / Back） |
| `WritingMode` | enum | 書字方向（Horizontal / Vertical） |
| `ObjectMode` | enum | SingleObject / PerCharacter |
| `LayoutEngine` | class | レイアウト計算ユーティリティ |
| `CharacterObjectPool` | class | Per-Character モード用 GameObject プール |
| `SolidText3DComponent.SuppressAutoRegenerate` | property | デバウンス制御フラグ |
| `SolidText3DComponent.IsDirty` | property | ダーティ状態の読み取り |
| `SolidText3DComponent.RegenerateMesh()` | method | 外部からの即座な再生成トリガー |
| `GlyphMeshBuilder.BuildPerCharacter()` | method | Per-Character 用メッシュ一括生成 |
