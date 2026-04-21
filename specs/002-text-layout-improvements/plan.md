# Implementation Plan: Solid Text 3D — レイアウト・フォント・パフォーマンス改善

**Branch**: `002-text-layout-improvements` | **Date**: 2026-04-21 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/002-text-layout-improvements/spec.md`

## Summary

SolidText3D パッケージに 7 つの改善を加える：  
① 3 軸アンカー指定（垂直 / 水平 / 奥行き）、② 縦書きレイアウト対応、  
③ Per-Character 個別オブジェクトモード（オブジェクトプール）、  
④ フォント参照を文字列型 → Inspector の Object フィールドへ変更（破壊的変更・MAJOR バンプ）、  
⑤ .ttf/.otf → .bytes 自動変換（AssetPostprocessor）、  
⑥ Inspector 入力デバウンスによるレイテンシ解消、  
⑦ フレームごとの GC アロケーション削減（ダーティフラグ + 同一テキスト早期リターン）。

## Technical Context

**Language/Version**: C# (.NET Standard 2.1) / Unity 6 (6000.x LTS)  
**Primary Dependencies**: SixLabors.Fonts（フォント解析）、LibTessDotNet v1.1.15（三角分割）  
**Storage**: N/A（ランタイムはメモリ完結。Editor が .bytes キャッシュを AssetDatabase に書き込む）  
**Testing**: Unity Test Framework — Edit Mode テスト（純粋 C# ロジック）＋ Play Mode テスト（MonoBehaviour）  
**Target Platform**: Unity Editor（全 OS）＋ Unity ビルドターゲット全般（PC / Mobile / WebGL）  
**Project Type**: UPM パッケージ（Asset Store 配布）  
**Performance Goals**: ランタイム時フレームあたり GC Alloc = 0 バイト（テキスト未変更時）、毎フレーム更新シナリオで 60fps 維持  
**Constraints**: ランタイム asmdef は Editor Only アセンブリ参照禁止、.NET Standard 2.1 APIのみ使用  
**Scale/Scope**: 単一 UPM パッケージ、7 機能追加 / 変更、公開 API の MAJOR バンプ（1.x → 2.0.0）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

<!-- SolidText3D 憲法 v2.1.0 に基づくゲート -->

- [x] **I. UPM 構造**: 既存 `Editor/`・`Runtime/`・`Tests/` 構造を維持。新ファイルはすべて各ディレクトリに配置する設計。
- [x] **II. Editor/Runtime 分離**: `FontAssetPostprocessor` と Inspector デバウンスロジックは `Editor/` 配下のみ。ランタイムは `FontData (byte[])` 経由でフォントデータを受け取り、`UnityEditor` 名前空間不使用。
- [x] **III. テストファースト**: spec.md に 7 ユーザーストーリーそれぞれの Acceptance Scenarios を記載済み。Edit Mode テスト（アンカー計算・縦書きレイアウト・プール管理）および Play Mode テスト（入力デバウンス）の計画含む。
- [x] **IV. 後方互換性**: `public string Font` プロパティの削除は spec の Clarification で「即時破壊的変更」として明示的に承認済み。→ **MAJOR バンプ: 1.0.0 → 2.0.0** が必要。`[Obsolete]` ステップはユーザーの明示的指示により省略する。
- [x] **V. パフォーマンス**: `LateUpdate()` 内では `_isDirty` チェックのみ（GC Alloc ゼロ）。テキスト不変時の早期リターン追加。Per-Character プールは Destroy ではなく SetActive(false) で再利用。
- [x] **VI. Asset Store 準拠**: 新規サードパーティ依存なし。既存 SixLabors.Fonts・LibTessDotNet は `Third Party Notices.md` 記載済み。
- [x] **VII. シンプルさ**: アンカー / 書字方向 / モードは enum フィールドで直接制御。不必要なストラテジーパターン・ファクトリー等の抽象化なし。

**⚠️ MAJOR バンプ正当化**: 憲法 IV は原則として `[Obsolete]` 経由を要求するが、フォント参照フィールド（`string _font` → `UnityEngine.Object _fontAsset`）は型が根本的に異なるため `[Obsolete]` で段階移行が技術的に困難。かつ spec Clarification で開発者が破壊的変更を明示承認している。この違反を Complexity Tracking に記録する。

## Project Structure

### Documentation (this feature)

```text
specs/002-text-layout-improvements/
├── plan.md              # このファイル（/speckit.plan コマンド出力）
├── research.md          # Phase 0 出力
├── data-model.md        # Phase 1 出力
├── quickstart.md        # Phase 1 出力
├── contracts/           # Phase 1 出力
│   └── SolidText3DComponent-API.md
└── tasks.md             # Phase 2 出力（/speckit.tasks コマンド — このコマンドでは未作成）
```

### Source Code (repository root)

```text
Packages/com.masachuang.solidtext3d/
├── package.json                          ← version: 1.0.0 → 2.0.0 に更新
├── CHANGELOG.md                          ← 破壊的変更を記録
├── Runtime/
│   ├── SolidText3DComponent.cs           ← 修正: _font(string)削除・_fontAsset追加・アンカー・書字方向・モードフィールド追加
│   ├── MeshGenerationParams.cs           ← 修正: Anchor・WritingMode・WordWrap フィールド追加
│   ├── GlyphMeshBuilder.cs               ← 修正: アンカーオフセット・縦書きレイアウト呼び出し追加
│   ├── LayoutEngine.cs                   ← 新規: 横書き・縦書きレイアウト計算（ApplyLayout の置き換え）
│   ├── CharacterObjectPool.cs            ← 新規: Per-Character モード用オブジェクトプール
│   ├── GlyphContour.cs                   ← 修正: AdvanceHeight フィールド追加（縦書き用）
│   ├── GlyphContourBuilder.cs            ← 読み取り専用（変更なし想定）
│   ├── BezierSubdivider.cs               ← 変更なし
│   ├── MeshExtruder.cs                   ← 変更なし
│   ├── GlyphMeshData.cs                  ← 変更なし
│   └── Plugins/                          ← 変更なし
│       ├── SixLabors.Fonts.dll
│       └── LibTessDotNet.dll
├── Editor/
│   ├── SolidText3DInspector.cs           ← 修正: Object フィールド・デバウンス追加
│   └── FontAssetPostprocessor.cs         ← 新規: .ttf/.otf → .bytes 自動変換
└── Tests/
    ├── Editor/
    │   └── （既存テストファイル + 新規テスト追加）
    └── Runtime/
        └── （既存テストファイル + 新規テスト追加）
```

**Structure Decision**: 既存の単一パッケージ構造を継承。新ファイルはすべて既存の `Runtime/` または `Editor/` に配置する。テスト構造も既存 `Tests/` 配下に追加する形とする。

## Implementation Sequence

> 各ステップは前ステップへの依存を持つ。この順序で実装すること。

### ステップ 1 — 新規 Enum 型の追加（依存なし）

**対象ファイル（新規作成）**: `Runtime/TextAnchorEnums.cs`（または既存クラスに追記）  
**参照**: [data-model.md § 新規 Enum 型](data-model.md#新規-enum-型)  
**内容**: `HorizontalAnchor`, `VerticalAnchor`, `DepthAnchor`, `WritingMode`, `ObjectMode` の 5 enum を定義する。  
**テスト**: なし（値の定義のみ）

### ステップ 2 — `MeshGenerationParams` の更新（Enum 型に依存）

**対象ファイル（修正）**: `Runtime/MeshGenerationParams.cs`  
**参照**: [data-model.md § MeshGenerationParams（修正）](data-model.md#meshgenerationparams修正)、[contracts/SolidText3DComponent-API.md § MeshGenerationParams](contracts/SolidText3DComponent-API.md)  
**内容**:

- `FontPath (string)` フィールドを削除する
- `HorizontalAnchor`, `VerticalAnchor`, `DepthAnchor`, `WritingMode`, `MaxWidth`, `MaxHeight`, `VerticalColumnWidth`, `RotateAsciiInVertical` を追加する  
**注意**: `GlyphMeshBuilderTests.cs` が `FontPath` を使っているため、テストも同時に修正する（`FontPath` → `FontData = File.ReadAllBytes(...)` に書き換え）

### ステップ 3 — `GlyphContour` の更新（依存なし）

**対象ファイル（修正）**: `Runtime/GlyphContour.cs`  
**参照**: [data-model.md § GlyphContour（修正）](data-model.md#glyphcontour修正)  
**内容**: `AdvanceHeight (float)`, `CharIndex (int)`, `IsVisible (bool)` を追加する

### ステップ 4 — `LayoutEngine` の新規作成（ステップ 2・3 に依存）

**対象ファイル（新規作成）**: `Runtime/LayoutEngine.cs`  
**参照**: [data-model.md § LayoutEngine](data-model.md#layoutengine新規-runtime-クラス)、[research.md § R-005](research.md#r-005-テキスト-3-軸アンカー計算)、[research.md § R-006](research.md#r-006-縦書きレイアウト)  
**内容**:

- `ApplyHorizontalLayout()`: 横書き座標計算（既存の `GlyphMeshBuilder.ApplyLayout()` を移植＋折り返し対応）
- `ApplyVerticalLayout()`: 縦書き座標計算（上→下、列は右→左）
- `CalculateAnchorOffset()`: `Mesh.bounds` からアンカーオフセットを計算  
**アンカーオフセット計算式**: [research.md § R-005](research.md#r-005-テキスト-3-軸アンカー計算) を参照（水平: Left=0, Center=-width/2, Right=-width; 垂直: Upper=-height, Middle=-height/2, Lower=0; 奥行き: Front=0, Center=-depth/2, Back=-depth）  
**テスト（Edit Mode）**: `Tests/Editor/LayoutEngineTests.cs` を新規作成
- `ApplyHorizontalLayout_EmptyGlyphs_DoesNotThrow`
- `CalculateAnchorOffset_Center_ReturnsHalfExtents`
- `ApplyVerticalLayout_SingleChar_YIsNegative`

### ステップ 5 — `GlyphMeshBuilder` の更新（ステップ 2・3・4 に依存）

**対象ファイル（修正）**: `Runtime/GlyphMeshBuilder.cs`  
**参照**: [data-model.md § データフロー概要](data-model.md#データフロー概要)、[contracts/SolidText3DComponent-API.md § GlyphMeshBuilder](contracts/SolidText3DComponent-API.md)  
**内容**:

1. `ApplyLayout()` プライベートメソッドを削除し、`LayoutEngine.ApplyHorizontalLayout()` / `ApplyVerticalLayout()` 呼び出しに置き換える
2. `GetFontBytes()` から `FontPath` 参照を削除する（`FontData` のみ使用）
3. `Build()` 内で `MeshExtruder.Build()` 後に `LayoutEngine.CalculateAnchorOffset()` を呼び出し、全頂点にオフセットを加算する
4. `BuildPerCharacter()` メソッドを追加する（詳細: [data-model.md § BuildPerCharacter 設計](data-model.md#buildpercharacter-メソッド設計)）  
**テスト（Edit Mode）**: `GlyphMeshBuilderTests.cs` に追記

- `Build_WithCenterAnchor_BoundsSymmetric`
- `Build_VerticalMode_YDecreases`
- `BuildPerCharacter_ThreeChars_ReturnsThreeMeshes`

### ステップ 6 — `CharacterObjectPool` の新規作成（ステップ 5 に依存）

**対象ファイル（新規作成）**: `Runtime/CharacterObjectPool.cs`  
**参照**: [data-model.md § CharacterObjectPool](data-model.md#characterobjectpool新規-runtime-クラス)、[research.md § R-004](research.md#r-004-per-character-オブジェクトプール)  
**内容**:

- `Sync(List<GlyphContour> visibleGlyphs, List<Mesh> perCharMeshes)` で `GlyphMeshBuilder.BuildPerCharacter()` の結果と `GlyphContour` のオフセットを使って子 GameObject を同期する
- 子 GameObject には `MeshFilter` + `MeshRenderer` を付与する（初回のみ `AddComponent`）
- `IsVisible == false` のグリフはスキップする  
**テスト（Edit Mode）**: `Tests/Editor/CharacterObjectPoolTests.cs` を新規作成
- `Sync_MoreChars_CreatesNewChildren`
- `Sync_FewerChars_DeactivatesExcess`
- `Sync_SameCount_ReusesExistingChildren`

### ステップ 7 — `SolidText3DComponent` の更新（ステップ 1〜6 すべてに依存）

**対象ファイル（修正）**: `Runtime/SolidText3DComponent.cs`  
**参照**: [data-model.md § SolidText3DComponent（修正）](data-model.md#solidtext3dcomponent修正)、[research.md § R-007](research.md#r-007-同一テキスト早期リターンパフォーマンス)、[research.md § R-003](research.md#r-003-inspector-入力デバウンス)  
**内容**:

1. `[SerializeField] string _font` および `public string Font` プロパティを削除する
2. `_fontAsset (UnityEngine.Object)`, `_fontBytesCache (TextAsset, HideInInspector)` を追加する
3. アンカー・書字方向・モードの新フィールドとプロパティを追加する
4. `_lastParamHash (int)` を追加し、`RegenerateMesh()` 冒頭でハッシュ比較による早期リターンを実装する（[research.md § R-007](research.md#r-007-同一テキスト早期リターンパフォーマンス) 参照）
5. `_suppressAutoRegenerate (bool)` 内部フラグを追加し、デバウンス中は `LateUpdate` の自動再生成を抑制する（[research.md § R-003](research.md#r-003-inspector-入力デバウンス) 参照）
6. `ObjectMode` 変更時の `CharacterObjectPool` 切り替えロジックを実装する（[data-model.md § 状態遷移: ObjectMode 切り替え](data-model.md#状態遷移-objectmode-切り替え) 参照）
7. フォント Missing 時の動作（直前メッシュ維持・警告 1 回のみ）を実装する  
**テスト（Edit Mode）**: `SolidText3DComponentTests.cs` に追記

- `RegenerateMesh_SameParams_SkipsRegeneration`
- `ObjectMode_PerCharacter_CreatesChildObjects`
- `FontAsset_Missing_MaintainsPreviousMesh`

### ステップ 8 — `FontAssetPostprocessor` の新規作成（Editor）

**対象ファイル（新規作成）**: `Editor/FontAssetPostprocessor.cs`  
**参照**: [data-model.md § FontAssetPostprocessor](data-model.md#fontassetpostprocessor新規-editor-クラス)、[research.md § R-002](research.md#r-002-ttfotf-ファイルの自動-bytes-変換)  
**内容**:

1. `OnPostprocessAllAssets` で `.ttf`/`.otf` を検知する
2. `Assets/SolidText3DFonts/{assetGuid}.bytes` に一時ファイル経由でアトミック書き込みする
3. `AssetDatabase.ImportAsset()` で `.bytes` を登録する
4. シーン内の全 `SolidText3DComponent` を走査し、`_fontAsset` の GUID が一致するものに `_fontBytesCache` を自動設定してシリアライズする（`EditorUtility.SetDirty()` + `AssetDatabase.SaveAssets()`）  
**注意**: ステップ 4 の「シーン内走査」は `FindObjectsByType<SolidText3DComponent>()` では Prefab アセットを見つけられないため、開いているシーン内のみ対象とし、Prefab は次回 Inspector 表示時に自動設定される（許容範囲の制限として明記）  
**テスト（Edit Mode）**: `Tests/Editor/FontAssetPostprocessorTests.cs` を新規作成

- `OnPostprocess_TtfFile_CreatesBytesFile`
- `OnPostprocess_ExistingBytesFile_Skips`
- `OnPostprocess_IoError_LogsError`

### ステップ 9 — `SolidText3DInspector` の更新（ステップ 7・8 に依存）

**対象ファイル（修正）**: `Editor/SolidText3DInspector.cs`  
**参照**: [research.md § R-001](research.md#r-001-unity-inspector-での-object-フィールドによるフォント参照)、[research.md § R-003](research.md#r-003-inspector-入力デバウンス)  
**内容**:

1. `DrawDefaultInspector()` を廃止し、各フィールドを手動描画する（Object フィールドのフィルター設定のため）
2. `EditorGUILayout.ObjectField("Font Asset", ..., typeof(UnityEngine.Object), false)` でフォントフィールドを表示する
3. フォントが未アタッチの場合に `EditorGUILayout.HelpBox()` で警告を表示する
4. `EditorGUI.BeginChangeCheck()` / `EndChangeCheck()` + `EditorApplication.update` でデバウンスを実装する
5. テキストフィールドへのキー入力中は `_target.SuppressAutoRegenerate = true` を設定し、デバウンス後に `RegenerateMesh()` を呼び出して `SuppressAutoRegenerate = false` に戻す

### ステップ 10 — `package.json` と `CHANGELOG.md` の更新（最終ステップ）

**対象ファイル（修正）**: `Packages/com.masachuang.solidtext3d/package.json`、`CHANGELOG.md`  
**参照**: [contracts/SolidText3DComponent-API.md § CHANGELOG エントリ](contracts/SolidText3DComponent-API.md#changelog-エントリv200-用)  
**内容**:

- `package.json`: `"version": "1.0.0"` → `"version": "2.0.0"`
- `CHANGELOG.md`: contracts に記載の CHANGELOG エントリを追加する

---

## Complexity Tracking

| 違反 | 理由 | より単純な代替案を却下した理由 |
| ---- | ---- | ----------------------------- |
| 憲法 IV: `[Obsolete]` ステップ省略（`string Font` 完全削除） | `string` → `UnityEngine.Object` は型が根本的に異なるため、`[Obsolete]` 付きの旧 `string Font` プロパティを残しても自動移行不可。コンパイルエラーで強制移行するほうがユーザーに明確。spec Clarification で開発者が「即時破壊的変更」を明示承認済み。 | `[Obsolete]` 付きの旧プロパティを 1 MINOR 維持する案→ 型変換のスタブコードが複雑になり、かつユーザーが旧 API で実行できてしまうため混乱を招く。 |
