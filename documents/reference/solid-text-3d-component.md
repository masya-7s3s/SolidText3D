# SolidText3DComponent リファレンス

SolidText3DComponent は、GameObject に3Dテキストメッシュを生成する MonoBehaviour です。  
Edit Mode と Play Mode の両方で動作し、設定変更時は次のフレームで自動再生成されます。

## 必須コンポーネント

追加時に次のコンポーネントを利用します。

- MeshFilter
- MeshRenderer

## 主なプロパティ

### 基本表示

| 項目 | 型 | 初期値 | 説明 |
| --- | --- | --- | --- |
| Text | string | Hello, World! | 表示する文字列 |
| FontAsset | UnityEngine.Object | null | フォント参照。未設定時はデフォルトフォントを使用 |
| ExtrusionDepth | float | 0.25 | 文字本体の厚み |
| FontSize | float | 1 | em 高さをUnity単位へ変換するサイズ |

### アウトライン

| 項目 | 型 | 初期値 | 説明 |
| --- | --- | --- | --- |
| OutlineEnabled | bool | false | アウトライン機能の有効化 |
| OutlineOffset | float | 0.05 | 外側へのオフセット量 |
| OutlineThickness | float | 0.25 | アウトラインの奥行き |
| OutlineDisplayMode | OutlineDisplayMode | Donut | アウトラインの奥行き構成 |
| OutlineMaterial | Material | null | null の場合は本体マテリアルを使用 |
| OutlineWidth | float | 0.05 相当 | OutlineOffset の別名 |

### レイアウト

| 項目 | 型 | 初期値 | 説明 |
| --- | --- | --- | --- |
| LetterSpacing | float | 0 | 追加の文字間隔 |
| LineSpacing | float | 1 | 行間倍率。縦書きでは列幅倍率 |
| HorizontalAnchor | HorizontalAnchor | Left | 横方向の基準位置 |
| VerticalAnchor | VerticalAnchor | Lower | 縦方向の基準位置 |
| DepthAnchor | DepthAnchor | Front | 奥行き方向の基準位置 |
| WritingMode | WritingMode | Horizontal | 横書き / 縦書き |
| MaxWidth | float | 0 | 横書き用の幅設定。現行実装では自動折り返し未使用 |
| MaxHeight | float | 0 | 縦書きでの列折り返し高さ。0 は無制限 |
| RotateAsciiInVertical | bool | false | 縦書き時にASCIIを90度回転 |

### オブジェクト構成

| 項目 | 型 | 初期値 | 説明 |
| --- | --- | --- | --- |
| ObjectMode | ObjectMode | SingleObject | 全文1メッシュか、文字ごとに分離するか |
| IsDirty | bool | 読み取り専用 | 再生成が必要かどうか |

## 主なメソッド

| メソッド | 戻り値 | 説明 |
| --- | --- | --- |
| RegenerateMesh() | void | 即時にメッシュを再生成する |

## enum 一覧

### OutlineDisplayMode

| 値 | 説明 |
| --- | --- |
| Donut | リング状に前後へ厚みを配分する |
| BackFilled | 正面シルエットを維持しつつ、背面側を埋める |

### WritingMode

| 値 | 説明 |
| --- | --- |
| Horizontal | 横書き |
| Vertical | 縦書き。上から下、列は右から左 |

### ObjectMode

| 値 | 説明 |
| --- | --- |
| SingleObject | テキスト全体で1つのメッシュ |
| PerCharacter | 可視文字ごとに子GameObjectを作る |

### HorizontalAnchor

| 値 | 説明 |
| --- | --- |
| Left | 左端が原点 |
| Center | 中央が原点 |
| Right | 右端が原点 |

### VerticalAnchor

| 値 | 説明 |
| --- | --- |
| Upper | 上端が原点 |
| Middle | 中央が原点 |
| Lower | 下端が原点 |

### DepthAnchor

| 値 | 説明 |
| --- | --- |
| Front | 前面が原点 |
| Center | 奥行き中央が原点 |
| Back | 背面が原点 |

## 実際の挙動メモ

### Text を空文字列にした場合

本体メッシュはクリアされます。  
PerCharacter モードの子オブジェクトも破棄されます。

### FontAsset が読めない場合

警告を出し、直前のメッシュを維持します。

### 同じ設定値を再適用した場合

内部のパラメータハッシュが同じなら再生成をスキップします。

### OutlineMaterial を設定しない場合

アウトライン側は本体の sharedMaterial を使います。

### アウトライン用オブジェクト名

アウトラインは __OutlineMesh__ という名前の子オブジェクトとして管理されます。

### PerCharacter の子オブジェクト名

文字ごとの子オブジェクトは Char_0, Char_1, Char_2... という名前で生成されます。
