# クイックスタートガイド: Solid Text 3D v2.0.0

**Branch**: `002-text-layout-improvements` | **Date**: 2026-04-21

---

## v1.x からの移行ガイド

### 1. フォント参照の変更（破壊的変更）

**旧方法（v1.x）**:

```csharp
component.Font = "Assets/Fonts/MyFont.bytes";  // コンパイルエラーになります
```

**新方法（v2.x）**:

```csharp
// Inspector でフォントをアタッチする（推奨）
// → コンポーネントの「Font Asset」フィールドに .ttf/.otf ファイルをドラッグ

// または、コードから設定する場合（Editor 専用）:
#if UNITY_EDITOR
component.FontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
    "Assets/Fonts/MyFont.ttf");
#endif
```

### 2. .bytes ファイルの手動管理は不要になりました

v2.0.0 では `.ttf` / `.otf` ファイルを Unity プロジェクトにインポートすると、  
自動的に `Assets/SolidText3DFonts/` フォルダに `.bytes` ファイルが生成されます。  
`replace_fonts_dll.bat` 等の手動変換スクリプトは不要です。

---

## 新機能の使い方

### テキストアンカーを設定する

Inspector で「Horizontal Anchor」「Vertical Anchor」「Depth Anchor」の  
3 つのドロップダウンを独立して設定します。

```csharp
// コードから設定する場合:
var st = GetComponent<SolidText3DComponent>();
st.HorizontalAnchor = HorizontalAnchor.Center;
st.VerticalAnchor   = VerticalAnchor.Middle;
st.DepthAnchor      = DepthAnchor.Center;
// → テキストの重心が GameObject の原点と一致する
```

**アンカーの意味**:

| Vertical Anchor | Y 軸の挙動 |
| -------------- | --------- |
| `Upper` | テキスト上端が原点（Y=0） |
| `Middle` | テキスト中央が原点 |
| `Lower` | テキスト下端が原点（デフォルト） |

| Horizontal Anchor | X 軸の挙動 |
| ---------------- | --------- |
| `Left` | テキスト左端が原点（デフォルト） |
| `Center` | テキスト中央が原点 |
| `Right` | テキスト右端が原点 |

| Depth Anchor | Z 軸の挙動 |
| ----------- | --------- |
| `Front` | 前面（Z=0）が原点（デフォルト） |
| `Center` | 中央が原点 |
| `Back` | 背面が原点 |

---

### 縦書きを使う

Inspector の「Writing Mode」を `Vertical` に設定します。

```csharp
var st = GetComponent<SolidText3DComponent>();
st.WritingMode = WritingMode.Vertical;
st.Text = "あいうえお\nかきくけこ";  // \n で次の列へ折り返し
```

**縦書きのオプション**:

| オプション | 説明 |
| --------- | ---- |
| `Max Height` | この高さを超えると自動的に次の列へ（0 = 折り返しなし） |
| `Vertical Column Width` | 各列の幅（0 = FontSize × 1.1f を自動使用） |
| `Rotate ASCII In Vertical` | true にすると ASCII 英数字を 90 度時計回りに回転 |

---

### Per-Character モードを使う（文字ごとのアニメーション）

```csharp
var st = GetComponent<SolidText3DComponent>();
st.ObjectMode = ObjectMode.PerCharacter;
st.Text = "ABC";

// 次の LateUpdate() の後、子 GameObject が 3 つ生成される
// アクセス方法:
for (int i = 0; i < transform.childCount; i++)
{
    var child = transform.GetChild(i);
    // 各文字を個別にアニメーション
    child.localPosition += Vector3.up * Time.deltaTime;
}
```

**Per-Character モードの特徴**:

- 折り返し区切り文字（スペース等）には子 GameObject が生成されない
- 文字数減少時: 子 GameObject は非アクティブ化（Destroy されない）して再利用される
- モードを `SingleObject` に戻すと子 GameObject は非アクティブ化され、単一 Mesh に切り替わる

---

### 自動折り返しを使う

```csharp
var st = GetComponent<SolidText3DComponent>();
// 横書きで幅 5 ユニットを超えたら折り返す
st.MaxWidth = 5f;
st.Text = "長いテキストが自動的に折り返されます。";
```

---

### 頻繁なテキスト更新（UI ユースケース）

v2.0.0 では同一パラメータのメッシュ再生成をスキップするため、  
毎フレーム同じ値を設定しても無駄な処理が発生しません。

```csharp
void Update()
{
    // 値が変わった場合のみメッシュを再生成する（変わらなければスキップ）
    scoreText.Text = $"Score: {score}";
}
```

---

### Inspector 編集時の入力レイテンシについて

v2.0.0 では Inspector のテキストフィールドに入力中はメッシュ再生成が行われません。  
入力完了（Enter キー押下、またはフィールドからフォーカスを外す）後に再生成されます。  
これにより、長いテキストを入力する際の遅延が解消されます。

---

## フォントセットアップの手順（v2.0.0）

1. `.ttf` / `.otf` フォントファイルを Unity の `Assets/` フォルダにドラッグ
2. Unity が自動的に `.bytes` ファイルを `Assets/SolidText3DFonts/` に生成する
3. SolidText3DComponent の「Font Asset」フィールドに、手順 1 でインポートした `.ttf` / `.otf` ファイルをドラッグ
4. 完了 — テキストが指定フォントで表示される

> **注意**: `Assets/SolidText3DFonts/` フォルダはバージョン管理に含めることを推奨します。  
> フォントファイルの GUID を元にファイル名が決定されるため、同じフォントは常に同じ `.bytes` ファイルに対応します。

---

## よくある質問

**Q: フォントが Missing になった場合はどうなりますか？**  
A: 直前のメッシュを維持し続け、コンソールに警告が 1 回出力されます。

**Q: フォントをアタッチしない場合はどうなりますか？**  
A: デフォルトフォント（NotoSansJP-Black）で表示されます。Inspector に警告が表示されます。

**Q: テキストが空文字列の場合はどうなりますか？**  
A: メッシュがクリアされ、何も表示されません（エラーは発生しません）。

**Q: 縦書きで英数字を縦中横にできますか？**  
A: 縦中横は将来バージョンで対応予定です。現在は「回転なし」または「90 度回転」から選択できます。
