# リサーチ結果: Solid Text 3D

**フィーチャーブランチ**: `001-solid-text-3d`  
**作成日**: 2026年4月17日  
**ステータス**: 完了（全 NEEDS CLARIFICATION 解決済み）

---

## 未解決事項のリスト → 解決結果

| # | 課題 | 解決状況 |
| --- | ------ | ---------- |
| 1 | C# フォントパーサーの選定 | ✅ SixLabors.Fonts に決定 |
| 2 | 三角形分割ライブラリの選定 | ✅ LibTessDotNet に決定 |
| 3 | Bezier 曲線の離散化戦略 | ✅ 適応分割（自前実装）に決定 |
| 4 | UPM パッケージ構造 | ✅ Unity 標準構造に確定 |
| 5 | 押し出しメッシュ生成アルゴリズム | ✅ 標準押し出し法に決定 |
| 6 | アウトライン幾何生成アルゴリズム | ✅ 法線方向オフセット法に決定 |
| 7 | DLL の管理方法（NuGet → Unity） | ✅ NuGet から手動 DLL 配置に決定 |

---

## 研究結果 1: C# フォントパーサー選定

### 決定: `SixLabors.Fonts`（MIT ライセンス）

**根拠**:

- `IGlyphRenderer` コールバック API（`MoveTo / LineTo / QuadraticBezierTo / CubicBezierTo / EndFigure`）が 3D メッシュ生成のグリフ輪郭取得に最適
- 完全純粋 C#（ネイティブ DLL 不要）→ UPM パッケージに同梱しやすい
- .NET Standard 2.0 対応（Unity 6 互換）
- アクティブメンテナンス中（2024 年継続更新）
- CJK／OpenType GSUB 対応
- MIT ライセンス → Asset Store 販売・配布に支障なし

**代替案として検討したもの**:

| ライブラリ | 除外理由 |
| --- | --- |
| SharpFont（FreeType バインディング） | ネイティブ DLL（freetype6.dll 等）が必須のため、UPM パッケージ配布が複雑化。2016 年以降メンテナンス終了 |
| Typography.OpenFont | NuGet 上で unlisted／非推奨状態。低レベル API で TrueType の暗黙的中間点再構築ロジックを自前実装する必要あり。メンテナンス低調 |
| SkiaSharp | 内部でネイティブ Skia を使用。サイズ過大・依存関係複雑 |

**使用コード（概要）**:

```csharp
// SixLabors.Fonts でのグリフ輪郭取得
var fontCollection = new FontCollection();
fontCollection.Add(fontPath);       // または Add(Stream)
FontFamily family = fontCollection.Get("FontName");
Font font = family.CreateFont(size);

var renderer = new GlyphContourBuilder();  // IGlyphRenderer 実装
TextRenderer.RenderTextTo(renderer, "立体文字", options);
// → renderer.Contours に各グリフの輪郭が格納される
```

**NuGet パッケージ**: `SixLabors.Fonts`（最新版）

---

## 研究結果 2: 三角形分割ライブラリ選定

### 決定: `LibTessDotNet` v1.1.15（SGI Free Software License B v2.0）

**根拠**:

- グリフの複数コンター（外輪郭 ＋ ホール）を `AddContour()` でそのまま渡せる → 追加ロジック不要
- **EvenOdd WindingRule** が TrueType/OpenType フォントのアウトライン規則と一致
- 複雑な CJK グリフ（齢・議など多数のホールを持つ文字）でも安定動作
- Unity3D 採用実績多数（GitHub タグ `unity3d`）
- SGI Free Software License B v2.0 は MIT 同等の許諾型ライセンス → 商用配布・Asset Store 販売に問題なし
- 完全純粋 C#

**代替案として検討したもの**:

| ライブラリ | 除外理由 |
| --- | --- |
| Triangle.NET | ライセンスが不明確（Jonathan Shewchuk 独自ライセンス）。著者が商用利用非推奨と明言。Asset Store 販売不可リスク |
| poly2tri-csharp | 同一座標点に対応していない → 複雑 CJK グリフでエラー発生リスクあり |

**使用コード（概要）**:

```csharp
var tess = new LibTessDotNet.Tess();

// 外側輪郭（反時計回り）
tess.AddContour(outerContour, ContourOrientation.CounterClockwise);

// 内側輪郭・ホール（時計回り）- 'O' や '口' の内側など
foreach (var hole in innerContours)
    tess.AddContour(hole, ContourOrientation.Clockwise);

// EvenOdd ルールで三角形を生成
tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
```

**NuGet パッケージ**: `LibTessDotNet` v1.1.15

---

## 研究結果 3: Bezier 曲線離散化戦略

### 決定: 適応分割（Adaptive Subdivision）自前実装

**根拠**:

- 10〜30 行で実装可能なシンプルなアルゴリズム
- 曲率の大きい部分は細かく、直線に近い部分は粗くサンプリング → 品質とパフォーマンスのバランス最適
- 外部ライブラリ不要

**アルゴリズム概要**（再帰的 DeCasteljau 法）:

```csharp
// Quadratic Bezier 離散化
static void SubdivideQuadratic(Vector2 p0, Vector2 p1, Vector2 p2,
                                float threshold, List<Vector2> output)
{
    // DeCasteljau: t=0.5 の中点
    Vector2 mid = 0.25f * p0 + 0.5f * p1 + 0.25f * p2;
    Vector2 chord = (p0 + p2) * 0.5f;
    if (Vector2.Distance(mid, chord) <= threshold)
    {
        output.Add(p2);   // 精度OK → 終点追加
        return;
    }
    // 分割
    Vector2 q0 = (p0 + p1) * 0.5f;
    Vector2 q1 = (p1 + p2) * 0.5f;
    Vector2 q2 = (q0 + q1) * 0.5f;
    SubdivideQuadratic(p0, q0, q2, threshold, output);
    SubdivideQuadratic(q2, q1, p2, threshold, output);
}
```

**推奨パラメータ**:

- `errorThreshold`: em 正規化後で `0.5f / unitsPerEm`（約 0.0005）
- 最大再帰深度: `8`

**グリフ別想定頂点数**:

| グリフタイプ | 離散化後の想定頂点数 |
| --- | --- |
| 単純ラテン文字（I, l） | 4〜10 |
| 標準ラテン文字（A, B） | 20〜60 |
| 単純 CJK（一, 二） | 20〜80 |
| 標準 CJK（日, 口, 人） | 80〜200 |
| 複雑 CJK（齢, 議, 憂） | 200〜600 |

---

## 研究結果 4: 押し出し（Extrusion）アルゴリズム

### 決定: 標準的なポリゴン押し出し法

**手順**:

1. **前面**: 三角形分割済み 2D ポリゴンを Z=0 に配置（法線: +Z 方向）
2. **背面**: 同じポリゴンを Z=−`extrusionDepth` に配置（法線: −Z 方向、頂点順序反転）
3. **側面**: 輪郭の各エッジを前面→背面に繋ぐ四角形（2 三角形）
   - 輪郭頂点インデックス `[i, i+1]` に対して、前後 2 頂点で帯状に接続
   - 法線: 各エッジの外向き法線（法線スムージングはエッジ単位で `normalize((v1-v0).Perp())`）
4. **法線計算**: 前面・背面は Z 軸方向。側面はエッジ方向から外積で計算

**`extrusionDepth = 0` の特殊ケース**: 前面ポリゴンのみを生成（フラットメッシュ）

---

## 研究結果 5: アウトライン幾何生成

### 決定: 法線方向オフセット法（Miter Joint）

**手順**:

1. 各輪郭の頂点に沿って、外向き法線方向に `outlineWidth` だけオフセットした外側輪郭を生成
2. 外側輪郭と元の輪郭の間を帯状の四角形メッシュで埋める
3. **コーナー処理**: Miter join（角の鋭さに上限を設けて spike を防止。閾値: `miterLimit = 4.0f`）

**`outlineWidth = 0` の特殊ケース**: アウトライン生成をスキップ

**注意**: アウトライン色・マテリアルは `MeshRenderer` の `SubMesh` 機能または別 `GameObject` で管理。本コンポーネントはマテリアルを管理しない（spec 準拠）

---

## 研究結果 6: UPM パッケージ構造

### 決定: Unity 標準 UPM パッケージ構造（`Packages/` ディレクトリ配置）

**確定ディレクトリ構造**:

```text
Packages/
  com.yourcompany.solidtext3d/        ← UPM パッケージルート
    package.json                       ← マニフェスト（必須）
    README.md
    CHANGELOG.md
    LICENSE.md
    Third Party Notices.md             ← SixLabors.Fonts, LibTessDotNet 記載
    │
    Runtime/                           ← ランタイムコード
    │   com.yourcompany.solidtext3d.Runtime.asmdef
    │   SolidText3DComponent.cs        ← MonoBehaviour メインコンポーネント
    │   GlyphMeshBuilder.cs            ← メッシュ生成サービス
    │   BezierSubdivider.cs            ← Bezier 離散化
    │   MeshExtruder.cs                ← 押し出し処理
    │   GlyphContourBuilder.cs         ← IGlyphRenderer 実装
    │   Plugins/                       ← 純粋 C# サードパーティ DLL
    │       SixLabors.Fonts.dll
    │       SixLabors.Fonts.dll.meta
    │       LibTessDotNet.dll
    │       LibTessDotNet.dll.meta
    │       （依存 DLL: SixLabors.Memory.dll 等）
    │
    Editor/                            ← エディタ専用コード
    │   com.yourcompany.solidtext3d.Editor.asmdef
    │   SolidText3DInspector.cs        ← カスタムインスペクター
    │
    Tests/
    │   Editor/
    │   │   com.yourcompany.solidtext3d.Tests.Editor.asmdef
    │   │   GlyphMeshBuilderTests.cs
    │   │   BezierSubdividerTests.cs
    │   └── Runtime/
    │       com.yourcompany.solidtext3d.Tests.Runtime.asmdef
    │       SolidText3DRuntimeTests.cs
    │
    Samples~/                          ← UPM サンプル（チルダで非自動インポート）
    │   BasicUsage/
    │   CJKExample/
    │
    Documentation~/
        index.md
```

**asmdef 設定のキーポイント**:

- `Runtime/asmdef`: `overrideReferences: true` ＋ `precompiledReferences` で DLL 明示
- `Editor/asmdef`: `includePlatforms: ["Editor"]` ＋ `Runtime` asmdef 参照
- テスト asmdef: `optionalUnityReferences: ["TestAssemblies"]`

---

## 研究結果 7: NuGet DLL の Unity への組み込み方法

### 決定: NuGet から手動 DLL 配置

**手順**（開発者向け一回限りの作業）:

1. NuGet から `.nupkg` をダウンロード（`.zip` として展開）
2. `lib/netstandard2.0/*.dll` を抽出
3. `Packages/com.yourcompany.solidtext3d/Runtime/Plugins/` に配置
4. Unity の `.meta` ファイルが自動生成されることを確認
5. DLL Inspector で「Any Platform」設定を確認

**管理推奨**:

- 開発環境では [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) を導入して依存関係を管理
- 配布パッケージには DLL を直接バンドル（エンドユーザーが NuGet を使う必要はない）

**SixLabors.Fonts の依存 DLL**（`netstandard2.0` ビルド）:

- `SixLabors.Fonts.dll`
- `SixLabors.ImageSharp.dll`（必要に応じて確認）

**LibTessDotNet の依存 DLL**:

- `LibTessDotNet.dll`（依存 DLL なし）

---

## サマリー: 確定技術スタック

| 役割 | 決定 | NuGet | ライセンス |
| --- | --- | --- | --- |
| フォントパーサー | SixLabors.Fonts | `SixLabors.Fonts` | MIT |
| 三角形分割 | LibTessDotNet | `LibTessDotNet` v1.1.15 | SGI Free B v2 |
| Bezier 離散化 | 自前実装（10〜30 行） | — | — |
| 押し出し生成 | 自前実装 | — | — |
| アウトライン生成 | 自前実装（Miter join） | — | — |
| 言語 | C# (.NET Standard 2.1) | — | — |
| フレームワーク | Unity 6（6000.x LTS） | — | — |
| テスト | Unity Test Framework | — | — |
