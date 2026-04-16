# データモデル: Solid Text 3D

**フィーチャーブランチ**: `001-solid-text-3d`  
**作成日**: 2026年4月17日  
**参照仕様**: `specs/001-solid-text-3d/spec.md`

---

## 概要

Solid Text 3D は Unity の UPM パッケージとして実装される。ランタイムおよびエディタ双方で使用するエンティティを以下に定義する。

---

## エンティティ一覧

```text
SolidText3DComponent (MonoBehaviour)
│
├── 使用 → GlyphMeshBuilder (static service)
│         │
│         ├── 使用 → GlyphContourBuilder (IGlyphRenderer 実装)
│         │          └── 使用 → SixLabors.Fonts (外部ライブラリ)
│         │
│         ├── 使用 → BezierSubdivider (static utility)
│         │
│         ├── 使用 → MeshExtruder (static utility)
│         │          └── 使用 → LibTessDotNet (外部ライブラリ)
│         │
│         └── 返す → Unity Mesh
│
└── アタッチ先 → GameObject (MeshFilter + MeshRenderer)
```

---

## エンティティ詳細

### 1. `SolidText3DComponent` (MonoBehaviour)

**場所**: `Runtime/SolidText3DComponent.cs`  
**責務**: メッシュ生成パラメータを保持し、ダーティフラグで再生成をトリガーする。

#### フィールド（シリアライズ対象）

| フィールド名 | 型 | デフォルト値 | 説明 | 対応要件 |
| --- | --- | --- | --- | --- |
| `_text` | `string` | `"Text"` | 表示テキスト（Unicode 対応） | FR-002, FR-008 |
| `_font` | `UnityEngine.Font` | `null` | 使用フォント（TTF/OTF） | FR-001, FR-009 |
| `_extrusionDepth` | `float` | `0.1f` | 押し出し深さ（0 以上） | FR-003 |
| `_outlineWidth` | `float` | `0.0f` | アウトライン幅（0 以上） | FR-004 |
| `_letterSpacing` | `float` | `0.0f` | 文字間隔（em 単位） | — |
| `_lineSpacing` | `float` | `1.2f` | 行間（em 倍率） | — |

#### 非シリアライズフィールド

| フィールド名 | 型 | 説明 |
| --- | --- | --- |
| `_isDirty` | `bool` | メッシュ再生成が必要かどうかのダーティフラグ |
| `_meshFilter` | `MeshFilter` | キャッシュされた MeshFilter 参照 |

#### プロパティ（公開 API）

| プロパティ名 | 型 | 説明 | 対応要件 |
| --- | --- | --- | --- |
| `Text` | `string` | テキストの get/set（set で dirty 立て） | FR-002, FR-007 |
| `Font` | `UnityEngine.Font` | フォントの get/set（set で dirty 立て） | FR-001 |
| `ExtrusionDepth` | `float` | 押し出し深さの get/set（set で dirty 立て） | FR-003 |
| `OutlineWidth` | `float` | アウトライン幅の get/set（set で dirty 立て） | FR-004 |

#### ライフサイクルメソッド

| メソッド | タイミング | 処理 |
| --- | --- | --- |
| `Awake()` | 初期化時 | `MeshFilter`・`MeshRenderer` コンポーネント取得または自動追加 |
| `OnValidate()` | Inspector 変更時（Editor のみ） | `_isDirty = true` をセット |
| `LateUpdate()` | 毎フレーム末 | `_isDirty` なら `RegenerateMesh()` を呼び出し、フラグをリセット |

#### バリデーション

- `_extrusionDepth < 0` → `0` にクランプ、`Debug.LogWarning` 出力
- `_outlineWidth < 0` → `0` にクランプ、`Debug.LogWarning` 出力
- `_font == null` → デフォルトフォントを使用、`Debug.LogWarning` 出力（FR-009）

#### ステートマシン（ダーティフラグ）

```text
[Clean] ←──────────────────── RegenerateMesh() 完了
   │
   │ テキスト/フォント/深さ/アウトライン幅 変更
   ▼
[Dirty]
   │
   │ LateUpdate() 実行
   ▼
RegenerateMesh() → GlyphMeshBuilder.Build() → MeshFilter.sharedMesh 更新
   │
   ▼
[Clean]
```

---

### 2. `MeshGenerationParams` (struct)

**場所**: `Runtime/MeshGenerationParams.cs`  
**責務**: メッシュ生成に必要なすべてのパラメータをまとめた値型。

#### フィールド

| フィールド名 | 型 | 説明 |
| --- | --- | --- |
| `Text` | `string` | 生成対象テキスト |
| `FontPath` | `string` | フォントファイルの絶対パス |
| `FontData` | `byte[]` | フォントバイナリデータ（FontPath と排他） |
| `ExtrusionDepth` | `float` | 押し出し深さ |
| `OutlineWidth` | `float` | アウトライン幅 |
| `LetterSpacing` | `float` | 文字間隔 |
| `LineSpacing` | `float` | 行間倍率 |
| `BezierErrorThreshold` | `float` | Bezier 離散化誤差（省略時: 0.0005f） |

---

### 3. `GlyphContour` (class)

**場所**: `Runtime/GlyphContour.cs`  
**責務**: 単一グリフの輪郭データを保持する。

#### フィールド

| フィールド名 | 型 | 説明 |
| --- | --- | --- |
| `Contours` | `List<List<Vector2>>` | コンターの配列（1 グリフに複数コンターがある場合に対応、例: 'O', '口'） |
| `AdvanceWidth` | `float` | グリフの送り幅（文字間隔計算に使用） |
| `Bounds` | `Rect` | グリフのバウンディングボックス |

---

### 4. `GlyphContourBuilder` (class, IGlyphRenderer)

**場所**: `Runtime/GlyphContourBuilder.cs`  
**責務**: `SixLabors.Fonts` の `IGlyphRenderer` インターフェースを実装し、フォントからグリフの輪郭データを収集する。

#### 入出力

- **入力**: `SixLabors.Fonts` のテキストレンダリングコールバック
- **出力**: `List<GlyphContour>`（テキスト全体のグリフ輪郭リスト）

#### コールバックメソッド

| メソッド | 処理 |
| --- | --- |
| `BeginFigure()` | 新しいコンターを開始 |
| `MoveTo(Vector2 point)` | 現在のコンターに始点を追加 |
| `LineTo(Vector2 point)` | 現在のコンターに線分終点を追加 |
| `QuadraticBezierTo(Vector2 c, Vector2 to)` | `BezierSubdivider` で離散化し追加 |
| `CubicBezierTo(Vector2 c1, Vector2 c2, Vector2 to)` | `BezierSubdivider` で離散化し追加 |
| `EndFigure()` | 現在のコンターを閉じてリストに格納 |
| `EndGlyph()` | グリフの `GlyphContour` を確定してリストに格納 |

---

### 5. `BezierSubdivider` (static class)

**場所**: `Runtime/BezierSubdivider.cs`  
**責務**: 二次・三次 Bezier 曲線を適応分割アルゴリズムで頂点列に変換する。

#### メソッド

| メソッド | シグネチャ | 説明 |
| --- | --- | --- |
| `SubdivideQuadratic` | `(Vector2 p0, Vector2 p1, Vector2 p2, float threshold, List<Vector2> output) : void` | 二次 Bezier を離散化 |
| `SubdivideCubic` | `(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float threshold, List<Vector2> output) : void` | 三次 Bezier を離散化 |

---

### 6. `MeshExtruder` (static class)

**場所**: `Runtime/MeshExtruder.cs`  
**責務**: グリフ輪郭の 2D ポリゴンを三角形分割し、押し出し処理で 3D メッシュを生成する。

#### メソッド

| メソッド | シグネチャ | 説明 |
| --- | --- | --- |
| `Build` | `(List<GlyphContour> contours, MeshGenerationParams p) : Mesh` | メッシュ全体を生成して返す |
| `BuildGlyphMesh` | `(GlyphContour glyph, float extrusionDepth, float outlineWidth) : GlyphMeshData` | 1 グリフのメッシュデータを生成（戻り値型 `GlyphMeshData` の定義は § 9 を参照） |

#### 内部処理フロー

1. `LibTessDotNet.Tess` で各グリフの前面（XY 平面 Z=0）を三角形分割
2. 背面頂点を Z=−`extrusionDepth` に複製（頂点順序反転で法線反転）
3. 各コンターのエッジを繋ぐ側面クワッドを生成
4. `outlineWidth > 0` の場合、外側オフセット輪郭を生成してアウトライン帯メッシュを追加
5. 全グリフのメッシュを結合して単一 `UnityEngine.Mesh` を返す

---

### 7. `GlyphMeshBuilder` (static class)

**場所**: `Runtime/GlyphMeshBuilder.cs`  
**責務**: `SolidText3DComponent` から呼ばれる最上位のメッシュ生成サービス。

#### メソッド

| メソッド | シグネチャ | 説明 |
| --- | --- | --- |
| `Build` | `(MeshGenerationParams p) : Mesh` | パラメータからメッシュを生成して返す |

#### 処理フロー

1. `SixLabors.Fonts.FontCollection` でフォントを読み込む
2. `GlyphContourBuilder` で全グリフの輪郭を収集
3. `BezierSubdivider` で曲線を離散化（`GlyphContourBuilder` 内で実行）
4. `MeshExtruder.Build()` で 3D メッシュを生成
5. フォント未指定時・グリフ欠損時のフォールバック処理

> **実装上の注意: フォントバイトの取得方法**  
> `SolidText3DComponent._font`（`UnityEngine.Font`）から生の TTF/OTF バイト列を取得する Unity 標準 API は存在しない。実装では以下の方針を採用する。
>
> - **エディタ実行時（OnValidate 経由・エディタメッシュ更新）**: `#if UNITY_EDITOR` ガード内で `UnityEditor.AssetDatabase.GetAssetPath(font)` によりパスを取得し、`System.IO.File.ReadAllBytes()` で読み込む。
> - **ランタイム（ビルド済みゲーム）**: `AssetDatabase` は使用不可。v1 ではランタイム実行時に `Debug.LogWarning` を出力して空メッシュを返す。ランタイムでフォントを使用したい場合の回避策は [quickstart.md「トラブルシューティング」](quickstart.md) に記載する。

---

### 8. `SolidText3DInspector` (Editor class)

**場所**: `Editor/SolidText3DInspector.cs`  
**責務**: `SolidText3DComponent` の Custom Inspector。パラメータ変更時に即座にプレビューを更新する。

#### 処理

- `[CustomEditor(typeof(SolidText3DComponent))]` 属性でカスタムインスペクターとして登録
- Inspector の入力変更 → `serializedObject.ApplyModifiedProperties()` → `OnValidate()` が自動呼び出し
- エディタ実行中は `EditorApplication.QueuePlayerLoopUpdate()` でビューを再描画

---

### 9. `GlyphMeshData` (struct)

**場所**: `Runtime/GlyphMeshData.cs`  
**責務**: 単一グリフの 3D メッシュ構成データ（頂点・三角形インデックス・法線）を保持する一時的な値型。`MeshExtruder.BuildGlyphMesh()` が返し、`MeshExtruder.Build()` 内で全グリフ分を結合して最終 `UnityEngine.Mesh` を生成する際に使用される。

#### フィールド

| フィールド名 | 型 | 説明 |
| --- | --- | --- |
| `Vertices` | `List<Vector3>` | グリフの頂点リスト（前面・背面・側面をすべて含む） |
| `Triangles` | `List<int>` | 三角形インデックスリスト（3 要素ごとに 1 三角形） |
| `Normals` | `List<Vector3>` | 各頂点の法線ベクトル |
| `Offset` | `Vector3` | 文字配置計算で決定したこのグリフの配置オフセット（他グリフとの結合時に加算） |

---

## バリデーションルール

| フィールド | ルール | 違反時の処理 |
| --- | --- | --- |
| `text` | null または空文字を許容 | 空文字時はメッシュを非表示（0 ポリゴン）にする（FR エラーなし） |
| `extrusionDepth` | `>= 0.0f` | `0.0f` にクランプして `Warning` ログ出力 |
| `outlineWidth` | `>= 0.0f` | `0.0f` にクランプして `Warning` ログ出力 |
| `font` | null 許容 | デフォルトフォントにフォールバックして `Warning` ログ出力 |
| グリフ未収録文字 | 処理継続 | 空グリフ（AdvanceWidth のみ）としてスキップ、ログなし |
| フォントファイル削除 | 処理継続 | デフォルトフォントにフォールバック、`Warning` ログ出力 |

---

## 依存関係マップ

```text
SolidText3DComponent
  └─ requires MeshFilter (GetComponent / AddComponent)
  └─ requires MeshRenderer (GetComponent / AddComponent)
  └─ calls GlyphMeshBuilder.Build()

GlyphMeshBuilder
  └─ uses SixLabors.Fonts (external DLL)
  └─ uses GlyphContourBuilder
  └─ uses MeshExtruder

GlyphContourBuilder
  └─ implements SixLabors.Fonts.IGlyphRenderer
  └─ uses BezierSubdivider

MeshExtruder
  └─ uses LibTessDotNet (external DLL)

SolidText3DInspector [EDITOR ONLY]
  └─ uses SolidText3DComponent
  └─ uses UnityEditor API (UNITY_EDITOR guard)
```

---

## パフォーマンスに関する設計メモ

- `LateUpdate` 内では `GlyphMeshBuilder.Build()` 呼び出し以外の処理（`new`, LINQ, 文字列連結）を行わない
- `GlyphMeshBuilder.Build()` はメッシュ生成時のみ呼び出し（ダーティフラグで制御）
- 生成済み `Mesh` オブジェクトは毎回 `new Mesh()` せず `sharedMesh` の中身を `Clear()` して再利用
- `mesh.indexFormat = IndexFormat.UInt32` を設定し、大量頂点でのインデックスオーバーフローを防止

---

## テスト対象エンティティ

| エンティティ | テスト種別 | 主要テストケース |
| --- | --- | --- |
| `BezierSubdivider` | Edit Mode（Unit） | Quadratic/Cubic 離散化の精度、閾値境界値 |
| `GlyphContourBuilder` | Edit Mode（Unit） | コールバック経由のコンター収集 |
| `MeshExtruder` | Edit Mode（Unit） | 三角形分割（ホールあり）、押し出し頂点数・法線 |
| `GlyphMeshBuilder` | Edit Mode（Integration） | CJK 文字・空文字・フォントなし時の挙動 |
| `SolidText3DComponent` | Play Mode | ダーティフラグ動作、`LateUpdate` での再生成 |
