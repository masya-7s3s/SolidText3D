<!--
SYNC IMPACT REPORT
===================
バージョン変更: 1.0.1 → 2.0.0（MAJOR: Unity 最小対応バージョン引き上げ）
変更された原則: なし
追加セクション: なし
削除セクション: なし
変更セクション:
  - 技術的制約 > 対応プラットフォーム: モバイル（iOS/Android）を v1 スコープ外として明記（I2 修正）
  - 技術的制約 > Unity バージョンポリシー: 最小対応バージョンを Unity 6 LTS (6000.x) に引き上げ（I3 修正）
  - 開発ワークフロー > PR マージ前の必須ゲート §6: Unity 6 LTS のみに更新（I3 修正）
テンプレート更新状況:
  - plan-template.md: ✅ 整合確認済み（plan.md の Unity 6 記載と一致）
  - spec-template.md: ✅ 整合確認済み（spec.md の「Unity 6 以降」記載と一致）
  - tasks-template.md: ✅ 整合確認済み（tasks.md T037 の Unity 6 検証と一致）
延期事項:
  - なし
-->

# SolidText3D プロジェクト憲法

## コアの原則

### I. UPM パッケージ構造の遵守（最優先）

すべての機能は Unity Package Manager（UPM）標準に準拠したパッケージ構造で実装しなければならない。

- `Editor/`、`Runtime/`、`Tests/` ディレクトリを明確に分離し、それぞれに `asmdef` ファイルを配置する
- `package.json` には `name`（逆ドメイン形式）、`version`、`displayName`、`unity`（最小対応バージョン）、`description`、`keywords`、`author` を必ず記載する
- サンプルは `Samples~/` に配置し、パッケージのランタイムコードに直接含めない
- `Editor/` 配下のコードは `UNITY_EDITOR` 条件コンパイルシンボルを用いてビルドから除外し、ランタイムアセンブリには一切参照させない

**根拠**: Asset Store および UPM で配布されるアセットはこの構造を前提として動作する。逸脱はユーザーのプロジェクトを壊す可能性がある。

### II. エディタ拡張とランタイムの厳格な分離（非交渉的）

エディタ専用コードとランタイムコードは、物理的にも依存関係の上でも完全に分離されなければならない。

- ランタイム `asmdef` は `Editor` Only フラグを持つアセンブリを参照してはならない
- `UnityEditor` 名前空間への参照は `#if UNITY_EDITOR` ブロック内のみで許可する
- ランタイムで利用する ScriptableObject や MonoBehaviour は `Runtime/` に配置する
- エディタウィンドウ・カスタムインスペクター・Gizmos コードはすべて `Editor/` に配置する

**根拠**: ランタイムがエディタコードに依存するとビルド時にコンパイルエラーまたは実行時例外が発生し、ユーザーのプロジェクトに重大な影響を与える。

### III. テストファースト開発（非交渉的）

すべての機能は テストが先に作成され、失敗を確認してから実装を開始しなければならない。

- **Edit Mode テスト**: 純粋な C# ロジック・ScriptableObject・エディタ拡張の単体テストに使用する
- **Play Mode テスト**: MonoBehaviour、コルーチン、シーンを伴う統合テストに使用する
- テストアセンブリは `Tests/Editor/` および `Tests/Runtime/` に配置し、専用の `asmdef` を持つ
- 各 PR はテストの追加・修正なしに機能追加を受け入れない（README の更新、アセットのみの変更を除く）
- テストカバレッジの目標: ランタイムロジック 80%以上

**根拠**: Unity 特有のライフサイクル（Awake/Start/OnDestroy）は予期せぬ副作用を生みやすいため、テストなしの変更は品質を保証できない。

### IV. 後方互換性と API の安定性

公開 API は Semantic Versioning（SemVer）に従い、破壊的変更は MAJOR バージョンアップでのみ許可される。

- 公開 API（`public`/`protected` メンバー）は `[Obsolete]` 属性を付けて最低 1 つの MINOR バージョンにわたり維持した後に削除する
- 対応 Unity バージョンの変更（下限引き上げ）は MAJOR バージョンアップとして扱う
- `package.json` の `unity` フィールドに記載された最小バージョンでコンパイル・動作を保証する
- API の変更は `CHANGELOG.md` に `Added / Changed / Deprecated / Removed / Fixed / Security` カテゴリで記録する

**根拠**: ユーザーのプロジェクトで突然ビルドが壊れることを防ぐ。Asset Store の審査ポリシーにも準拠する。

### V. パフォーマンスとメモリ効率

ランタイムコードはフレームごとのヒープアロケーションをゼロまたは最小に保たなければならない。

- `Update()` / `LateUpdate()` / `FixedUpdate()` 内での `new`、LINQ、文字列連結、ボックス化は禁止する
- 再利用可能なオブジェクトには オブジェクトプール を使用する
- 大量データ処理には `Unity.Collections`（`NativeArray` 等）を積極的に利用し、Burst Compiler / Job System の採用を検討する
- `Profiler.BeginSample` / `EndSample` を利用してホットパスのプロファイルを実施し、結果を PR に添付する
- GC.Alloc が発生するパスは設計レビューで正当化しなければならない

**根拠**: モバイルおよびローエンド PC でのフレームドロップは製品の評判に直結する。

### VI. Asset Store ガイドライン・法的要件への準拠

アセットは Unity Asset Store の審査要件および法的ライセンス条件を満たさなければならない。

- サードパーティのアセット・フォント・テクスチャを含める場合はライセンスを `Third-Party Notices.md` に明記する
- Unity 公式パッケージ（`com.unity.*`）への依存は `package.json` の `dependencies` に正確に記載する
- 独自に定義したアセンブリ名は `com.companyname.packagename.*` の逆ドメイン形式に統一する
- Asset Store 提出前に Unity 公式の [Asset Store Submission Guidelines] チェックリストをすべて通過しなければならない

**根拠**: ライセンス違反や審査不通過は販売停止および法的リスクに繋がる。

### VII. シンプルさと説明可能なコード

実装は可能な限りシンプルに保ち、すべての非自明な設計判断にはコメントで理由を記録する。

- YAGNI 原則: 現時点で不要な抽象化・汎化・設定オプションを追加しない
- 公開 API には XML ドキュメントコメント（`<summary>`, `<param>`, `<returns>`, `<example>`）を記述する
- マジックナンバーは名前付き定数または `[SerializeField]` として宣言する
- 同一処理が 3 回以上現れた場合のみリファクタリングを検討する（Rule of Three）

**根拠**: Unity アセットはさまざまなスキルレベルのユーザーが読むコードであるため、可読性は機能と同等に重要である。

## 技術的制約と対応環境

### 対応プラットフォーム

- **エディタ**: Windows 10/11、macOS 12+、Ubuntu 20.04+
- **ランタイムターゲット（v1）**: PC（Windows/macOS/Linux）のみ。モバイル（iOS/Android）はオプション対応
- **グラフィックス API**: URP（Universal Render Pipeline）必須対応、Built-in RP はオプション対応

### Unity バージョンポリシー

- **最小対応バージョン**: Unity 6 LTS（6000.x）。`package.json` の `unity` フィールドは `"6000.0"` を設定する
- **推奨バージョン**: Unity 6 最新 LTS パッチ
- 新 LTS リリースから 3 ヶ月以内に動作確認を実施し、`package.json` の `unityRelease` を更新する
- Unity 2022.3 LTS 以前は非対応。対応バージョン下限の引き上げは MAJOR バージョンアップとして扱う（原則 IV 準拠）

### 依存関係ポリシー

- ランタイムの外部依存は最小限に抑える（ユーザーのプロジェクトへの衝突リスク軽減）
- `com.unity.*` パッケージへの依存は機能上の必要性を PR で説明する
- `TextMeshPro`（`com.unity.textmeshpro`）は現バージョンの中核依存として許可する
- 依存パッケージは `package.json` の `dependencies` に必ずバージョン範囲を明記する

## 開発ワークフローと品質ゲート

### ブランチ戦略

- `main`: リリース済みの安定コード（直接プッシュ禁止）
- `develop`: 統合ブランチ
- `feature/###-name`: 機能ブランチ（`speckit.git.feature` で作成）
- `hotfix/###-name`: 緊急修正ブランチ

### PR マージ前の必須ゲート

1. ✅ すべての Edit Mode / Play Mode テストがパスしていること
2. ✅ `Editor/` と `Runtime/` の分離が維持されていること
3. ✅ 公開 API の XML ドキュメントコメントが追加または更新されていること
4. ✅ `CHANGELOG.md` が更新されていること
5. ✅ 新規 GC アロケーションがある場合は設計上の正当化コメントがあること
6. ✅ Unity 6 LTS（6000.x）で手動ビルド確認済みであること

### リリースフロー

1. `develop` → `main` へのマージ時に `package.json` の `version` を SemVer で更新する
2. Git タグ `v{version}` を作成する
3. Asset Store 提出用の `.unitypackage` は CI でエクスポートする
4. UPM 向け `npm publish`（または OpenUPM 登録）は手動ゲートで実施する

## ガバナンス

### 憲法の優先順位

この憲法はプロジェクト内のすべての他の慣行・ガイド・口頭合意に優先する。矛盾が生じた場合は本憲法が正として扱われ、他文書を更新する。

### 改定手続き

個人プロジェクトのため、手続きは軽量に保つ。

1. `constitution.md` を直接編集し、`Version` を SemVer ルールに従いインクリメントする
2. `Last Amended` 日付を更新する
3. 影響を受けるテンプレートがあれば同時に更新する
4. コミットメッセージに `docs: amend constitution to vX.Y.Z (変更概要)` 形式を使用する

### バージョニングポリシー

- **MAJOR**: 既存の原則の削除・再定義、または対応 Unity バージョンの重大な変更
- **MINOR**: 新原則の追加、セクションの実質的な拡張
- **PATCH**: 文言の明確化、誤字修正、非意味論的な改善

**Version**: 2.0.0 | **Ratified**: 2026-04-17 | **Last Amended**: 2026-04-17
