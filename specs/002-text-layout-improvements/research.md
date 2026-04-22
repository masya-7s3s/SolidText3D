# Research: Solid Text 3D — レイアウト・フォント・パフォーマンス改善

**Branch**: `002-text-layout-improvements` | **Date**: 2026-04-21  
**Status**: 完了（全 NEEDS CLARIFICATION 解決済み）

---

## R-001: Unity Inspector での Object フィールドによるフォント参照

### 決定事項

`[SerializeField] private UnityEngine.Object _fontAsset;` を使用し、Inspector に Object Field として表示する。型フィルターはエディタ側（`SolidText3DInspector`）で `EditorGUILayout.ObjectField(..., typeof(UnityEngine.Object), false)` として指定し、.ttf/.otf 両方を受け付ける。

### 根拠

- `UnityEngine.Font`（Unity 内蔵フォント型）は Unity が独自インポートしたデータを保持しており、SixLabors.Fonts が必要とする生バイナリを直接取得できない。
- `UnityEngine.DefaultAsset`（.bytes ファイルの型）を直接参照させると「.bytes ファイルを手動作成する」旧フローと変わらない。
- `UnityEngine.Object` を参照し、エディタ側で `AssetDatabase.GetAssetPath()` → `File.ReadAllBytes()` で生データを取得するのが最もシンプル。
- ランタイムでは `[SerializeField, HideInInspector] private TextAsset _fontBytesCache;` に変換済みデータを格納し、`FontData = _fontBytesCache?.bytes` で渡す。

### 検討した代替案

| 案 | 却下理由 |
| --- | ------- |
| `UnityEngine.Font` 型で参照 | 生バイナリを取得する公式 API がない。`font.name` 等でファイルパスを再解決するハックが必要。 |
| `TextAsset` 型で参照（.bytes を直接選択） | ユーザーが引き続き .bytes 変換を手動実施する必要がある。 |
| `string` フォント名のまま維持 | 現状の問題（文字列マッチング・摩擦）が解消されない。 |

---

## R-002: .ttf/.otf ファイルの自動 .bytes 変換

### 決定事項

`AssetPostprocessor` を `Editor/FontAssetPostprocessor.cs` に実装する。  
変換フロー:

1. `OnPostprocessAllAssets` で `.ttf` / `.otf` の Import を検知。
2. `AssetDatabase.GetAssetPath()` でパス取得 → `File.ReadAllBytes()` で生データ読み込み。
3. 生成先: `Assets/SolidText3DFonts/{guid}.bytes`（隠しフォルダではなく専用フォルダに配置）。
4. 既存 .bytes が存在する場合はスキップ（ハッシュ比較不要、ファイル存在確認のみ）。
5. 生成した `.bytes` を `AssetDatabase.ImportAsset()` で登録し、`_fontBytesCache` フィールドに自動設定。
6. 一時ファイル → リネーム（アトミック書き込み）で中断耐性を確保。

### 根拠

- `AssetPostprocessor` は Unity 公式の Import パイプラインフックで、アセット追加・変更時に確実に呼び出される。
- 隠しフォルダ（`.`プレフィックス）は Unity の AssetDatabase に認識されないため、通常フォルダに配置する。
- 既存ファイルのハッシュ比較は処理コスト増に対してメリットが小さい（フォントが変わる場合は GUID が変わり別ファイルになる）。

### 検討した代替案

| 案 | 却下理由 |
| --- | ------- |
| `OnValidate` でその都度変換 | `OnValidate` は頻繁に呼ばれるため重い I/O に不適切。Play Mode 移行時等にも発火する。 |
| カスタム `AssetImporter` | 登録・実装コストが高い。フォントはすでに Unity 標準でインポートされるため二重処理になる。 |
| `EditorApplication.projectChanged` フック | `AssetPostprocessor` より遅延が大きく、どのアセットが変わったか判定が煩雑。 |

---

## R-003: Inspector 入力デバウンス

### 決定事項

`SolidText3DInspector` に `EditorApplication.update` フックとタイムスタンプを使ったデバウンスを実装する。  

- テキストフィールドの変更検知: `EditorGUI.BeginChangeCheck()` / `EndChangeCheck()`。
- フィールドが変更されたら `_lastChangeTime = EditorApplication.timeSinceStartup` を記録。
- `EditorApplication.update` コールバックで `timeSinceStartup - _lastChangeTime >= 0.5` 秒（設定可能）経過したら `RegenerateMesh()` を呼び出す。
- 既存の `OnValidate()` は `_isDirty = true` のままにし、デバウンス有効時は `SolidText3DComponent._suppressAutoRegenerate = true`（`_suppressAutoRegenerate` フィールド、[data-model.md § SolidText3DComponent（修正）](data-model.md#solidtext3dcomponent修正) 参照）で `LateUpdate` の自動再生成を一時抑制する。

### 根拠

- `EditorApplication.update` は Editor のメインスレッドで毎フレーム安全に呼び出せる。
- `EditorApplication.delayCall` は一発のみで繰り返し入力に対応できない。
- デバウンス時間 0.5 秒は TextMeshPro と同等の体感値。

### 検討した代替案

| 案 | 却下理由 |
| --- | ------- |
| `OnLostFocus` / `Enter` のみ | spec の Acceptance Scenario 1 ではキー入力中は再生成しないと明記。ただし Scenario 2 ではフォーカスアウト / Enter で即時再生成が必要。両方対応するにはデバウンス + イベントの組み合わせが必要。 |
| `DelayedTextField` | フィールドが Enter を押すまで Unity に内容を保持しない。CJK 入力の IME 確定との相性が悪い。 |

---

## R-004: Per-Character オブジェクトプール

### 決定事項

`CharacterObjectPool.cs` を `Runtime/` に実装する（Editor 依存なし）。  
設計:

- `List<GameObject>` で子 GameObject を管理（最大を超えた分は非アクティブ）。
- 文字数増加時: 末尾に `new GameObject()` を追加し `MeshFilter + MeshRenderer` を付与。
- 文字数減少時: 余剰 GameObject を `SetActive(false)`（Destroy しない）。
- テキスト更新時: プールの先頭から必要数だけ `SetActive(true)` + メッシュ差し替え。
- 折り返し区切り文字（スペース等）は可視文字リストに含めず、プール生成対象外とする。

### 根拠

- 憲法 V: `LateUpdate()` 内での `new GameObject()` を避けるため、事前プールが必要。
- spec Clarification: 文字数減少時は非アクティブ化（Destroy しない）と明記。
- プールサイズ上限は設けない（テキスト上限を設けないと整合性が取れない）。

---

## R-005: テキスト 3 軸アンカー計算

### 決定事項

`GlyphMeshBuilder.Build()` でメッシュ生成後、バウンディングボックスを計算してアンカーオフセットを頂点全体に加算する。  

- 水平: Left = そのまま（x=0 が左端）、Center = -width/2 、Right = -width  
- 垂直: Upper = -height（上端が原点）、Middle = -height/2、Lower = 0（下端が原点）  
- 奥行き: Front = 0、Center = -depth/2、Back = -depth  
- バウンディングボックスは `MeshExtruder.Build()` 後に `Mesh.bounds` から取得（再計算不要）。

### 根拠

- 全グリフを結合した最終メッシュに対してオフセットを適用するのが最も実装コストが低い。
- 個別グリフへのオフセット適用よりも、最終メッシュへの一括適用がシンプル。

---

## R-006: 縦書きレイアウト

### 決定事項

`LayoutEngine.cs` に横書き・縦書き共通の座標計算を集約する。  
縦書きレイアウト:

- 文字を上から下へ配置（Y 座標を行内で減算）。
- 複数列は右から左へ並ぶ（X 座標を列ごとに減算）。
- 列幅 = インスペクタで指定する固定値 `_verticalColumnWidth`（デフォルト = `_fontSize * 1.1f`）。
- 各文字は列幅内で水平中央揃え。
- ASCII 英数字の回転: `_rotateAsciiInVertical` フラグ（false = 回転なし、true = 90度時計回り）。
- SixLabors.Fonts の `GlyphMetrics.Height`（AdvanceHeight に相当）を縦方向の送り幅として使用。縦書き専用メトリクスが取得できない場合は `AdvanceWidth` を代用。

### 根拠

- SixLabors.Fonts の `TextOptions.LayoutMode` に `VerticalTopBottom` が存在するが、CJK グリフの縦書き専用字形（縦書き用 OpenType 機能）への対応が不完全。そのため、水平レイアウト結果をベースに `LayoutEngine` で座標を再計算するアプローチを採用。
- 列幅固定（文字幅依存なし）は spec Clarification で明記。

### 検討した代替案

| 案 | 却下理由 |
| --- | ------- |
| `TextOptions.LayoutMode = VerticalTopBottom` のみ使用 | CJK 縦書き字形の適用に実績がなく、挙動が不明確。独自計算の方が制御しやすい。 |

---

## R-007: 同一テキスト早期リターン（パフォーマンス）

### 決定事項

`SolidText3DComponent` に `_lastRenderedText` / `_lastRenderedHash` を保持し、`_isDirty` フラグがセットされても前回と同一内容の場合はメッシュ再生成をスキップする。  

- `string.GetHashCode()` で高速比較（衝突時は文字列比較でフォールバック）。
- ハッシュのみ比較する場合、フォントやサイズが変わっても同一テキストならスキップされてしまうため、ハッシュの対象は「すべての描画パラメータを連結した文字列」とする（`_fontSize.ToString()` 等の文字列連結は更新時のみ許容）。
- `SolidText3DComponent` の実装では `_lastParamHash (int)` という単一フィールドでハッシュを保持する（[data-model.md § SolidText3DComponent（修正）](data-model.md#solidtext3dcomponent修正) 参照）。

### 根拠

- 最も単純かつゼロオーバーヘッドに近い最適化。毎フレーム同一値が渡されるカウンター表示等で有効。
- 連結文字列の生成は `_isDirty = true` 時のみ発生するため、憲法 V への影響なし。
