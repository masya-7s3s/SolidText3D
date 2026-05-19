# SolidText3DComponent リファレンス

SolidText3DComponent は、GameObject に 3D テキストメッシュを生成する MonoBehaviour です。  
設定を変更すると dirty 状態になり、表示の更新は明示的に RegenerateMesh() または RequestRegenerateMesh() を呼んだタイミングで行われます。

## できること

- TTF / OTF フォント由来の 3D 文字メッシュ生成
- 横書き / 縦書きの切り替え
- アンカー基準での位置合わせ
- 文字本体とは別オブジェクトの outline 生成
- 文字単位の子 GameObject 出力
- 高頻度更新向け deferred regeneration

## 必須コンポーネント

追加時に次のコンポーネントを利用します。

- MeshFilter
- MeshRenderer

どちらかが欠けていても、Awake 時に自動で補われます。

## プロパティ

### 基本表示

| 項目 | 型 | 初期値 | 説明 |
| --- | --- | --- | --- |
| Text | string | Hello, World! | 表示する文字列。null を代入すると空文字列として扱います。 |
| FontAsset | UnityEngine.Object | null | Inspector では通常 Font を割り当てるフォント参照です。未設定時はデフォルトフォントへフォールバックします。 |
| ExtrusionDepth | float | 0.25 | 文字本体の押し出し厚みです。 |
| FontSize | float | 1 | em 高さを Unity ワールド単位へ変換するサイズです。 |

### アウトライン

| 項目 | 型 | 初期値 | 説明 |
| --- | --- | --- | --- |
| OutlineEnabled | bool | false | outline を有効化します。false のとき outline 子オブジェクトは破棄されます。 |
| OutlineOffset | float | 0.05 | 文字外周へのオフセット量です。0 未満は 0 に丸められます。 |
| OutlineThickness | float | 1 | outline の奥行き比率です。1 は本体の ExtrusionDepth と同じ厚さを表します。0 未満は 0 に丸められます。 |
| OutlineDisplayMode | OutlineDisplayMode | Donut | outline の奥行き構成です。 |
| OutlineMaterial | Material | null | null の場合は本体 MeshRenderer の sharedMaterial を使います。 |
| OutlineWidth | float | 0.05 相当 | OutlineOffset の別名です。既存 API 互換のために残されています。 |

### レイアウト

| 項目 | 型 | 初期値 | 説明 |
| --- | --- | --- | --- |
| LetterSpacing | float | 0 | 追加の文字間隔です。 |
| LineSpacing | float | 1 | 横書きでは行間倍率、縦書きでは列幅倍率として使われます。 |
| HorizontalAnchor | HorizontalAnchor | Left | 横方向の基準位置です。 |
| VerticalAnchor | VerticalAnchor | Lower | 縦方向の基準位置です。 |
| DepthAnchor | DepthAnchor | Front | 奥行き方向の基準位置です。 |
| WritingMode | WritingMode | Horizontal | Horizontal / Vertical を切り替えます。 |
| MonospaceMode | bool | false | 全角を 1em、半角を 0.5em の固定セルで配置する等幅表示モードです。LetterSpacing はセルの外側へ追加されます。 |
| MaxWidth | float | 0 | 横書き用の幅設定として保持されますが、現行実装では自動折り返しに使われません。 |
| MaxHeight | float | 0 | 縦書きで列を折り返す高さです。0 は無制限です。 |
| RotateAsciiInVertical | bool | false | 縦書き時に印字可能 ASCII を 90 度回転します。 |

### 出力と状態

| 項目 | 型 | 初期値 | 説明 |
| --- | --- | --- | --- |
| ObjectMode | ObjectMode | SingleObject | テキスト全体を 1 メッシュにするか、文字ごとに分けるかを切り替えます。 |
| IsDirty | bool | 読み取り専用 | 最後の設定変更がまだ表示に反映されていないと true です。 |
| HasPendingRegeneration | bool | 読み取り専用 | deferred regeneration の in-flight / pending / ready 状態が残っている間 true です。 |
| SuppressAutoRegenerate | bool | false | Editor 連携用の公開フラグです。現行実装ではこの値だけで再生成挙動は変わりません。 |

## メソッド

| メソッド | 戻り値 | 説明 |
| --- | --- | --- |
| RegenerateMesh() | void | 同期的にメッシュを再生成します。呼び出し復帰時点で表示反映まで完了します。 |
| RequestRegenerateMesh() | void | 高頻度更新向けの deferred regeneration を要求します。呼び出し時点では表示反映を保証しません。 |

## イベント

| イベント | 型 | 説明 |
| --- | --- | --- |
| DeferredRegenerationFailed | Action of RegenerationFailureInfo | deferred regeneration 失敗時に通知します。失敗しても直前の表示は keep-last-good で維持されます。 |

## Regeneration API の使い分け

| API | 向いている場面 | 挙動 |
| --- | --- | --- |
| RegenerateMesh() | ボタン押下、初回表示、確実に即時反映したい更新 | 同期実行です。 |
| RequestRegenerateMesh() | タイマー、スコア、メーターなど高頻度更新 | latest-only で古い request を圧縮しながら非同期準備します。 |

## Inspector セクション

現行のカスタム Inspector は次の構成です。

- Mesh Update: dirty 状態の確認と手動再生成
- Text & Font: テキスト本文とフォント設定
- Geometry: 押し出し厚み、サイズ、文字間・行間
- Layout: 書字方向、Monospace Mode、アンカー、折り返し関連設定
- Outline: outline の有効化と見た目設定
- Output: SingleObject / PerCharacter の切り替え

## enum 一覧

### OutlineDisplayMode

| 値 | 説明 |
| --- | --- |
| Donut | リング断面を文字本体の中央面基準で前後へ配分します。 |
| BackFilled | 正面シルエットを保ったまま、背面側を埋める構成です。 |

### WritingMode

| 値 | 説明 |
| --- | --- |
| Horizontal | 横書きです。 |
| Vertical | 縦書きです。文字は上から下、列は右から左へ進みます。 |

### ObjectMode

| 値 | 説明 |
| --- | --- |
| SingleObject | テキスト全体を 1 つのメッシュで出力します。 |
| PerCharacter | 可視文字ごとに子 GameObject を作成します。 |

### HorizontalAnchor

| 値 | 説明 |
| --- | --- |
| Left | 左端を原点に合わせます。 |
| Center | 中央を原点に合わせます。 |
| Right | 右端を原点に合わせます。 |

### VerticalAnchor

| 値 | 説明 |
| --- | --- |
| Upper | 上端を原点に合わせます。 |
| Middle | 中央を原点に合わせます。 |
| Lower | 下端を原点に合わせます。 |

### DepthAnchor

| 値 | 説明 |
| --- | --- |
| Front | 前面を原点に合わせます。 |
| Center | 奥行き中央を原点に合わせます。 |
| Back | 背面を原点に合わせます。 |

## 実装準拠の挙動メモ

### 設定変更だけでは表示は変わらない

Text や FontSize などの変更は dirty にするだけです。  
表示へ反映するには RegenerateMesh() か RequestRegenerateMesh() が必要です。

### Text を空文字列にした場合

本体メッシュはクリアされます。  
PerCharacter モードの子オブジェクトも破棄され、outline 子オブジェクトも削除されます。

### 同じ内容を再適用した場合

空文字列以外では、前回と同じ署名であれば表示適用をスキップします。  
「呼んだ回数」ではなく「最終的な表示状態」が基準です。

### dirty 時の deferred invalidation

Text や Layout を変更して dirty になると、待機中の latest request と ready result は破棄されます。  
そのため、古い deferred 結果が後から適用される可能性を下げています。

### フォント解決の優先順位

- Editor では FontAsset から直接読めたデータ
- 内部で保持している .bytes キャッシュ
- パッケージ同梱の NotoSansJP-Black

ビルド済みプレイヤーでは、FontAsset を差し替えただけでは .bytes キャッシュを解決しません。

### outline の管理方法

outline は __OutlineMesh__ という名前の子オブジェクトで別管理されます。  
OutlineEnabled を false にすると子オブジェクトごと破棄されます。  
OutlineOffset が 0 の場合は、outline 子オブジェクトを残したままメッシュだけ空になります。

### PerCharacter モードの構成

可視文字ごとに Char_0, Char_1, Char_2... という名前の子オブジェクトを生成します。  
親側の MeshRenderer は無効化され、余った子オブジェクトは破棄ではなく非アクティブ化して再利用されます。
