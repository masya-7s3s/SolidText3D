# Implementation Plan: テキストアウトライン生成（面生成経路の再設計）

**Branch**: `003-text-outline` | **Date**: 2026-05-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-text-outline/spec.md`

## Summary

既存 plan の「アウトライン専用 front face 三角分割」をやめ、XY 平面上で最終 2D プロファイルを確定してから、文字本体で実績のある押し出しロジックへ寄せる。具体的には `OutlineContourBuilder` が Clipper2 で `offset(originalFilled) - originalFilled` のリング断面を生成し、`OutlineMeshBuilder` はその断面を `MeshExtruder` と同じ cap/side 生成コアで立体化する。これにより、現在の不具合である「オフセットは取得できるが front face が表示されない」問題を、front face 専用実装ではなく共有ロジック化で解消する。

## Technical Context

**Language/Version**: C# (.NET Standard 2.1) / Unity 6 (6000.x LTS)  
**Primary Dependencies**: SixLabors.Fonts 2.1.3、LibTessDotNet 1.1.15、Clipper2 1.4.x  
**Storage**: N/A（ランタイムはメモリ完結。Editor のみ `.bytes` キャッシュを AssetDatabase に保持）  
**Testing**: Unity Test Framework（Edit Mode / Play Mode）  
**Target Platform**: Unity 6 Editor + Runtime（Windows 必須、macOS / Linux は任意）  
**Project Type**: UPM パッケージ（ランタイムライブラリ + エディタ拡張）  
**Performance Goals**: 通常フレームの `LateUpdate()` は GC.Alloc 0、メッシュ再生成はダーティ時のみ  
**Constraints**: `LateUpdate()` 内で `new` / LINQ / 文字列連結禁止、Editor/Runtime 分離、outline front cap は文字本体と同じ winding 規約を必ず守る  
**Scale/Scope**: 単一パッケージ内の outline 機能再設計。主な変更対象は `Runtime/` 3〜5 ファイル、`Tests/` 2〜4 ファイル、feature docs 一式

## Constitution Check

*GATE: Phase 0 前に確認し、Phase 1 設計反映後に再確認する。*

- [x] **I. UPM 構造**: 実装は `Packages/com.masachuang.solidtext3d/Runtime/` に限定し、Inspector 変更は `Editor/`、検証は `Tests/Editor/` と `Tests/Runtime/` に配置する。
- [x] **I-a. asmdef 命名整合**: package 名 `com.masachuang.solidtext3d` に合わせて asmdef 名も `com.masachuang.solidtext3d.*` へ統一し、既存の `com.yourcompany.solidtext3d.*` は本 feature で rename する。
- [x] **II. Editor/Runtime 分離**: outline profile 生成・押し出し・子 GO 管理はすべて runtime に閉じる。`UnityEditor` 参照は Inspector と Editor 破棄分岐のみ既存パターンを踏襲する。
- [x] **III. テストファースト**: 既存 `MeshExtruderTests` のパターンを流用し、profile 差分・winding parity・front cap 表示・BackFilled 背面固定の RED テストを先に追加する。
- [x] **IV. 後方互換性**: 公開 API は既存 plan の `Outline*` 追加範囲に留め、今回の再設計は内部実装差し替えとして扱う。SemVer は引き続き MINOR（`2.0.0 → 2.1.0`）。
- [x] **V. パフォーマンス**: outline 用の 2D profile 計算と mesh 合成はダーティ時のみ実行し、通常フレームでは既存 `_isDirty` チェック以外の追加アロケーションを発生させない。
- [x] **VI. Asset Store 準拠**: 追加依存は既存 plan の Clipper2 のみで増加なし。`Third Party Notices.md` 更新方針も変更なし。
- [x] **VII. シンプルさ**: outline 専用の別 tessellator を維持せず、既存 `MeshExtruder` の cap/side 生成コアへ集約する。これが今回の再設計の中心であり、最小の複雑度で最大の再利用を得る。

**Post-Design Re-check**: Phase 1 の設計反映後も全ゲート通過。追加されたのは internal profile モデルと共有 helper 抽出のみで、原則違反は発生しない。

## Project Structure

### Documentation (this feature)

```text
specs/003-text-outline/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── SolidText3DComponent-API.md
└── tasks.md
```

### Source Code (repository root)

```text
Packages/com.masachuang.solidtext3d/
├── Runtime/
│   ├── MeshExtruder.cs
│   ├── SolidText3DComponent.cs
│   ├── OutlineDisplayMode.cs
│   ├── OutlineSettings.cs
│   ├── OutlineContourBuilder.cs
│   ├── OutlineMeshBuilder.cs
│   └── Plugins/
│       └── Clipper2Lib.dll
├── Editor/
│   └── SolidText3DInspector.cs
└── Tests/
    ├── Editor/
    │   ├── MeshExtruderTests.cs
    │   ├── OutlineContourBuilderTests.cs
    │   └── OutlineMeshBuilderTests.cs
    └── Runtime/
        └── OutlineChildGOTests.cs
```

**Structure Decision**: 既存 UPM 構造を維持する。outline の 2D profile 生成は `OutlineContourBuilder`、mode ごとの mesh 合成は `OutlineMeshBuilder`、実際の cap/side 生成は `MeshExtruder` 由来の共有内部ロジックへ寄せる。これにより front face の責務を一箇所に集約する。

## Phase 0: Research Output

Phase 0 では次を確定した。

- raw offset path をそのまま front tessellation に渡すのではなく、まず `offset(originalFilled) - originalFilled` の最終リング断面を Clipper2 の boolean 演算で確定する
- outline の 3D 化は `MeshExtruder` と同じ cap/side 生成規約を共有する
- Donut と BackFilled は front silhouette を共有し、差は Z 配置と rear cap 合成だけに限定する
- regression の主検査点を「front cap の存在」と「body と outline の winding parity」に置く

詳細は [research.md](research.md) を参照。

## Phase 1: Design Decisions

### 1. 2D profile を先に確定する

- `OutlineContourBuilder` は「外周だけの raw offset path」ではなく、押し出しにそのまま使える canonical な profile を返す
- 返却内容は最低でも以下の 3 系統を持つ
  - `OriginalFilledContoursEm`: 元文字本体の充填領域
  - `OffsetFilledContoursEm`: 外側へ拡張した充填領域
  - `RingContoursEm`: `OffsetFilled - OriginalFilled` の front silhouette 用リング断面
- すべての contour は `MeshExtruder` が前提とする winding 規約に正規化する

### 2. front/back/side の生成コアを共有する

- `OutlineMeshBuilder` は LibTessDotNet を直接使って独自に front cap を組み立てない
- `MeshExtruder` 内の proven な cap/side 生成ロジックを internal helper 化し、body / outline の両方が同じ index 順序と normal 生成を使う
- これにより、front face の可視性に関わる winding 逆転バグを body と outline で分岐させない

### 3. Donut / BackFilled の差分を 3D 合成に閉じ込める

- **Donut**: `RingContoursEm` を厚さ付きで押し出し、中央基準で前後へ均等配分する
- **BackFilled**: `RingContoursEm` を背面固定アンカーで押し出し、さらに `OriginalFilledContoursEm` を rear cap として背面に追加する
- これにより両モードの front silhouette は必ず一致し、差は rear appearance と厚み配分だけになる

### 4. 子 GameObject と公開 API は維持する

- `SolidText3DComponent` の outline 子 GO 管理、material fallback、トグルによる生成/破棄方針は既存 plan を踏襲する
- `OutlineEnabled` が true かつ `OutlineOffset = 0` の場合は、child GO のライフサイクルを `Enabled` にのみひも付けたまま、mesh をクリアして outline なし相当の表示にする
- ユーザー向けの設定項目は `OutlineEnabled` / `OutlineOffset` / `OutlineThickness` / `OutlineMaterial` / `OutlineDisplayMode` から増やさない
- `specs/003-text-outline/contracts/SolidText3DComponent-API.md` は公開 API の正本として扱い、Outline 系公開 API の追加と同じフェーズで更新する

## Validation Plan

### Focused Edit Mode Tests

- `OutlineContourBuilderTests`: offset boolean difference がリング断面を返すこと、代表グリフセット（英字: `O`, `B`, `8` / 日本語: `あ`, `回`, `囲`）で hole 吸収時に single outer profile へ収束すること、`Clipper.Area() < 0` を穴として除去すること、Unity 単位入力が内部 em 空間へ正しく変換されること、`OutlineOffset = 0` では空の `RingContoursEm` を返して outline 形状を生成しないこと、winding が body 規約に正規化されること、オフセット量を 2 倍にしたとき結果輪郭の外周長増加率誤差が ±15% 以内に収まること
- `OutlineMeshBuilderTests`: ring shell から front cap が生成されること、BackFilled の rear anchor が `bodyBack - Z_FIGHT_EPSILON` から ±0.001 Unity 単位以内に収まること、Donut の前後張り出し量の差が ±0.001 Unity 単位以内に収まること、Donut と BackFilled の正面シルエットの XY 投影対称差面積が各グリフ外接矩形面積の 0.5% 以下に収まること
- `MeshExtruderTests`: 既存 donut contour テストを回帰基準として維持し、outline 断面でも同じ cap/side コアを通ることを確認する

### Runtime / Manual Validation

- `OutlineChildGOTests`: child GO の生成・削除・material 独立・設定保持、`OutlineOffset = 0` で mesh をクリアして child GO を再利用する挙動
- Unity 手動確認: 正面から front face が見えること、Donut 背面で中央が抜けること、BackFilled 背面で中央が埋まること、厚さ変更時の anchor 挙動が spec に一致すること、Unity 6 LTS（6000.x）で手動ビルド確認が通ること

## Complexity Tracking

憲法違反なし。今回の複雑度増加は internal profile モデルと共有 helper 抽出のみに限定され、front face まわりの実装重複を削減するため、総複雑度は旧 plan より低い。
