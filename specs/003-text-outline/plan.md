# Implementation Plan: テキストアウトライン生成

**Branch**: `003-text-outline` | **Date**: 2026-04-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-text-outline/spec.md`

## Summary

グリフのポリゴン輪郭を外側へオフセットしたアウトライン形状を生成し、文字本体の子 GameObject（専用 MeshFilter + MeshRenderer 付き）として管理する機能を追加する。オフセット演算には新規依存ライブラリ **Clipper2**（純 C#）を採用し、`BezierSubdivider` で事前にポリゴン化したパスをフォント em 空間（整数化スケール係数 ×1000）で処理した後に `LibTessDotNet` で三角分割してメッシュを生成する。表示モードは「ドーナツ」と「裏面埋め」の 2 種類をサポートし、厚さ・マテリアル・オフセット量を文字本体と独立して設定できる。

## Technical Context

**Language/Version**: C# (.NET Standard 2.1) / Unity 6 (6000.x LTS)  
**Primary Dependencies**: SixLabors.Fonts 2.1.3（フォント解析）、LibTessDotNet 1.1.15（三角分割）、**Clipper2 1.4.x**（新規: ポリゴンオフセット + 穴除去）  
**Storage**: N/A（ランタイムはメモリ完結。Editor が .bytes キャッシュを AssetDatabase に書き込む）  
**Testing**: Unity Test Framework — Edit Mode + Play Mode  
**Target Platform**: Unity 6 Editor + Runtime（Windows 必須対応；macOS / Linux はオプション対応）  
**Project Type**: UPM パッケージ（ライブラリ）  
**Performance Goals**: フレームごとの GC アロケーションゼロ（メッシュ再生成はパラメータ変更時のみ）  
**Constraints**: `Update` / `LateUpdate` 内ノーアロケーション、Editor 専用コードは `Editor/` asmdef に分離、Clipper2 は em 空間（整数スケール ×1000）で動作  
**Scale/Scope**: 単一 UPM パッケージ、コンポーネント 1 インスタンスあたり 1〜n 文字

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

<!-- SolidText3D 憲法 v1.0.0 に基づくゲート -->

- [x] **I. UPM 構造**: 新規クラス（`OutlineSettings`, `OutlineContourBuilder`, `OutlineMeshBuilder`, `OutlineDisplayMode`）を `Runtime/` に配置。インスペクター UI 変更は既存 `Editor/SolidText3DInspector.cs` を修正。テストは `Tests/Editor/` および `Tests/Runtime/` に追加。
- [x] **II. Editor/Runtime 分離**: アウトライン管理ロジック（子 GO 生成・メッシュ再生成）はすべて `Runtime/SolidText3DComponent.cs` 内で完結。`UnityEditor` 参照は `Editor/SolidText3DInspector.cs` のみ。
- [x] **III. テストファースト**: Edit Mode テスト（`OutlineContourBuilderTests.cs`）でオフセット輪郭計算・穴除去ロジックを先に記述。Play Mode テスト（`OutlineChildGOTests.cs`）で子 GO 生成・破棄・マテリアル独立を確認。
- [x] **IV. 後方互換性**: 新規 `SerializeField` の追加のみ（既存 API への破壊的変更なし）。SemVer MINOR バンプ `2.0.0 → 2.1.0`。
- [x] **V. パフォーマンス**: ダーティフラグ + `LateUpdate` パターンを踏襲。アウトラインメッシュ再生成はパラメータ変更時のみ実行。`LateUpdate` 内では `_isDirty` チェックのみ（GC ゼロ）。
- [x] **VI. Asset Store 準拠**: Clipper2 を `Third Party Notices.md` に追記。`Clipper2.dll` を `Runtime/Plugins/` に配置し逆ドメイン形式 asmdef 参照を追加。
- [x] **VII. シンプルさ**: YAGNI 原則に従い最小限の新クラスのみ追加。負値オフセット（内側オフセット）・アニメーション API などの将来拡張は本フィーチャーのスコープ外。

## Project Structure

### Documentation (this feature)

```text
specs/003-text-outline/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Packages/com.masachuang.solidtext3d/
├── Runtime/
│   ├── OutlineDisplayMode.cs        # 新規: ドーナツ / 裏面埋めモード enum
│   ├── OutlineSettings.cs           # 新規: アウトライン全設定の [Serializable] クラス
│   ├── OutlineContourBuilder.cs     # 新規: Clipper2 によるオフセット輪郭生成 + 穴除去
│   ├── OutlineMeshBuilder.cs        # 新規: オフセット輪郭から 3D メッシュ生成
│   ├── SolidText3DComponent.cs      # 修正: OutlineSettings フィールド追加 + 子 GO 管理
│   └── Plugins/
│       ├── Clipper2.dll             # 新規依存: ポリゴンオフセット演算
│       └── (既存 SixLabors.Fonts.dll, LibTessDotNet.dll)
├── Editor/
│   └── SolidText3DInspector.cs      # 修正: アウトライン設定セクションを Inspector UI に追加
└── Tests/
    ├── Editor/
    │   └── OutlineContourBuilderTests.cs   # 新規: Edit Mode テスト
    └── Runtime/
        └── OutlineChildGOTests.cs          # 新規: Play Mode テスト
```

**Structure Decision**: 既存の単一プロジェクト UPM 構造を踏襲。アウトライン機能は `SolidText3DComponent` の拡張として実装し、子 GameObject の管理ロジックを同クラスに収める。新規クラスは機能ごとに独立したファイルとして `Runtime/` に追加する。

## Complexity Tracking

憲法違反なし。全ゲート通過。

---

## Implementation Sequence

テストファースト原則（憲法 III）に従い、以下の順序で実装する。  
各ステップの詳細設計は括弧内のファイル・セクションを参照すること。

### Step 1: 依存 DLL の追加と asmdef 更新 *(環境セットアップ)*

**何をするか**: Clipper2 DLL をプロジェクトに追加し、参照を設定する。

1. NuGet から `Clipper2` パッケージを取得し `Clipper2Lib.dll` を抽出する  
   → 取得方法・バージョン: [research.md § R-006](research.md#r-006-clipper2-の-dll-取得バージョン)
2. `Runtime/Plugins/Clipper2Lib.dll` に配置し、`.meta` を生成する
3. `Runtime/com.masachuang.solidtext3d.Runtime.asmdef` の `precompiledReferences` に `"Clipper2Lib.dll"` を追加する
4. `Third Party Notices.md` に Clipper2 のライセンスブロックを追記する  
   → 追記テキスト: [research.md § R-006](research.md#r-006-clipper2-の-dll-取得バージョン)

---

### Step 2: データ型の新規作成 *(新規ファイル 2 本)*

**何をするか**: enum と設定クラスを先に用意し、後続ステップから型参照できるようにする。

1. `Runtime/OutlineDisplayMode.cs` を作成する  
   → 型定義: [data-model.md § OutlineDisplayMode](data-model.md#outlinedisplaymode)
2. `Runtime/OutlineSettings.cs` を作成する  
   → 型定義・フィールド詳細: [data-model.md § OutlineSettings](data-model.md#outlinesettings)

---

### Step 3: Edit Mode テストの先行記述 *(テストファースト)*

**何をするか**: `OutlineContourBuilder` の実装前にテストを書き、失敗を確認する。

ファイル: `Tests/Editor/OutlineContourBuilderTests.cs`  
→ テスト対象の仕様: [data-model.md § OutlineContourBuilder 処理フロー](data-model.md#outlinecontourbuilder内部クラス)  
→ 期待動作の根拠: [research.md § R-001](research.md#r-001-clipper2-c-ポリゴンオフセット-api)、[research.md § R-003](research.md#r-003-穴除去アルゴリズムfr-002)

テストケース候補:

- 正方形グリフに正のオフセットを与えると、結果パスの面積が元より大きい
- 「O」字形（外輪郭 + 穴輪郭）に大きいオフセットを与えると、戻り値が外周パス 1 本のみ（穴パスが除去されている）
- `offsetAmountEm = 0` のとき、元グリフと同一形状（変化なし）が返る

---

### Step 4: `OutlineContourBuilder` の実装

**何をするか**: Clipper2 を使ったオフセット輪郭生成 + 穴除去ロジックを実装する。

ファイル: `Runtime/OutlineContourBuilder.cs`  
→ メソッドシグネチャ: [data-model.md § OutlineContourBuilder](data-model.md#outlinecontourbuilder内部クラス)  
→ Clipper2 API の使い方: [research.md § R-001](research.md#r-001-clipper2-c-ポリゴンオフセット-api)  
→ em 空間 ↔ Unity 単位の座標変換: [research.md § R-002](research.md#r-002-オフセット演算の座標空間と整数スケール係数)（`fontSizeScale = MeshGenerationParams.FontSize`）  
→ 穴除去の判定ロジック: [research.md § R-003](research.md#r-003-穴除去アルゴリズムfr-002)

---

### Step 5: `OutlineMeshBuilder` の実装

**何をするか**: オフセット輪郭リストからメッシュの各部位（表面・側面・裏面）を生成する。

ファイル: `Runtime/OutlineMeshBuilder.cs`  
→ シグネチャ・生成部位テーブル・Z 座標計算: [data-model.md § OutlineMeshBuilder](data-model.md#outlinemeshbuilder内部クラス)  
→ Z ファイティング防止の計算式: [research.md § R-005](research.md#r-005-z-ファイティング防止)  
→ 内部の呼び出しフロー: [data-model.md § OutlineContourBuilder → OutlineMeshBuilder パイプライン](data-model.md#outlinecontourbuilder--outlinemeshbuilder-パイプライン)

---

### Step 6: `SolidText3DComponent` の修正

**何をするか**: コンポーネントに子 GO 管理ロジック・公開プロパティ・`LateUpdate` フックを追加する。

ファイル: `Runtime/SolidText3DComponent.cs`  
→ 追加フィールド・メソッド・プロパティ一覧: [data-model.md § SolidText3DComponent（修正）](data-model.md#solidtext3dcomponent修正)  
→ 子 GO のライフサイクル（生成・破棄・再利用）: [research.md § R-004](research.md#r-004-子-gameobject-のライフサイクル管理)  
→ 公開 API の仕様（ドキュメントコメント文面含む）: [contracts/SolidText3DComponent-API.md](contracts/SolidText3DComponent-API.md)

---

### Step 7: Play Mode テストの記述

**何をするか**: 子 GO のライフサイクルとマテリアル独立性を Play Mode で確認するテストを書く。

ファイル: `Tests/Runtime/OutlineChildGOTests.cs`  
→ 期待ライフサイクル: [research.md § R-004](research.md#r-004-子-gameobject-のライフサイクル管理)

テストケース候補:

- `OutlineEnabled = true` 後、`transform.Find("__OutlineMesh__")` が非 null を返す
- `OutlineEnabled = false` 後、`"__OutlineMesh__"` の子 GO が消えている
- 子 GO の `MeshRenderer.sharedMaterial` が文字本体の `MeshRenderer.sharedMaterial` と異なるインスタンスである（`OutlineMaterial` 設定時）
- `OutlineEnabled` を `false → true` と切り替えた後、前回の `OutlineOffset` 値が維持されている

---

### Step 8: Inspector UI の修正

**何をするか**: `SolidText3DInspector.cs` にアウトライン設定セクションを追加する。

ファイル: `Editor/SolidText3DInspector.cs`  
→ 表示レイアウト: [contracts/SolidText3DComponent-API.md § Inspector UI レイアウト](contracts/SolidText3DComponent-API.md#inspector-ui-レイアウト参考)

---

### Step 9: バージョン更新と CHANGELOG 記録

**何をするか**: `package.json` のバージョンを上げ、`CHANGELOG.md` に変更を記録する。

→ バンプ種別とバージョン番号: [data-model.md § SemVer バンプ](data-model.md#semver-バンプ)（`2.0.0 → 2.1.0`）
