# Research: テキストアウトライン生成

**Branch**: `003-text-outline` | **Date**: 2026-04-23  
**Status**: 完了（全 NEEDS CLARIFICATION 解決済み）

---

## R-001: Clipper2 C# ポリゴンオフセット API

### 決定事項

**Clipper2**（Angus Johnson 作、MIT ライセンス）の C# 実装を使用してポリゴンオフセット演算を行う。

使用する主要 API:

```csharp
using Clipper2Lib;

// パスをオフセット演算
var co = new ClipperOffset();
co.AddPaths(inputPaths, JoinType.Round, EndType.Polygon);
var solution = new Paths64();
co.Execute(deltaScaled, solution);   // delta = offsetAmount * SCALE_FACTOR (e.g. ×1000)
```

- **`JoinType.Round`**: コーナーを円弧補間する。文字の角（セリフ等）が過剰に尖らず自然な輪郭になる
- **`JoinType.Miter`**: 尖った角をシャープに保つ。必要に応じて選択可能（`MiterLimit` で制御）
- **`EndType.Polygon`**: 閉じたポリゴン（グリフ輪郭）に適用する際の必須設定
- **穴除去**: 正のオフセット（外側拡張）ではオフセット量が大きくなると内部の穴（例: 「O」の穴）が外周パスと接触・融合する。Clipper2 は `FillRule.NonZero` / `EvenOdd` いずれでも最終出力に不正な穴を含まない

### 根拠

- **純 C#**: DLL を `Runtime/Plugins/` に配置するだけで Unity 6 で動作。P/Invoke 不要
- **MIT ライセンス**: Asset Store 配布・商用利用ともに制限なし
- **穴除去組み込み**: FR-002（不自然な内側の穴の自動削除）が Clipper2 のオフセット計算の副産物として自然に解決される
- **整数演算**: `Paths64`（64-bit 整数座標）を使用し、em 空間 ×1000 スケールで誤差なく演算可能

### 検討した代替案

| 案 | 却下理由 |
| --- | ------- |
| 手動ポリゴン膨張（法線方向移動） | コーナー処理・自己交差・穴除去を手動実装する必要があり複雑度が高い |
| Geometry3Sharp | 依存サイズが大きく、本機能に必要なオフセット機能のみを取り出せない |
| NativeArray + Burst での独自実装 | 実装コストが非常に高い。Clipper2 で十分な精度が得られる |

---

## R-002: オフセット演算の座標空間と整数スケール係数

### 決定事項

オフセット演算は**フォント em 空間で実施**し、最終段階で Unity ワールド単位に変換する。

```text
スケール係数: SCALE = 1000
em 空間座標 (float) → ×1000 → int64 座標 (Paths64) → Clipper2 演算 → ÷1000 → Unity 単位 (float)
```

具体的な変換:

```csharp
const long SCALE = 1000L;

// em 空間 → 整数
long x64 = (long)(emX * SCALE);
long y64 = (long)(emY * SCALE);

// Clipper2 delta（オフセット量を em 空間に換算）
// offsetAmount はインスペクターで Unity ワールド単位指定
// fontSizeScale = MeshGenerationParams.FontSize（1.0f のとき 1 em = 1 Unity unit）
// em 空間でのオフセット = offsetAmount / fontSizeScale
double deltaScaled = (offsetAmount / fontSizeScale) * SCALE;
```

### 根拠

- `Paths64` は 64-bit 整数座標を使用するため、float の丸め誤差を回避できる
- em 空間でのオフセット演算後に Unity 単位変換を行うことで、フォントサイズ変更時も再演算なしにスケーリングが可能（`BezierSubdivider` の既存パターンと統一）

### 検討した代替案

| 案 | 却下理由 |
| --- | ------- |
| Unity ワールド空間で直接演算 | フォントサイズ変更のたびに再演算が必要。`BezierSubdivider` の出力との変換が煩雑 |
| `PathsD`（double 浮動小数点）使用 | `Paths64` より演算精度が下がる可能性がある。`Paths64` に統一することで安定性を確保 |

---

## R-003: 穴除去アルゴリズム（FR-002）

### 決定事項

Clipper2 の `ClipperOffset.Execute()` が出力する `Paths64` の各パスについて、**ワインディング方向（CCW = 外周、CW = 穴）を確認し、穴パスのみを除外**することで「消えるべき空洞」を削除する。

```csharp
// Clipper2 の出力からパスを分類
foreach (var path in solution)
{
    double area = Clipper.Area(path);
    if (area > 0)      // CCW（外周パス）
        outerPaths.Add(path);
    // area < 0 = CW（穴）→ ドーナツ状輪郭では保持, アウトライン外周では破棄
}
```

ただし、オフセット量が大きく内部の穴が外周と融合した場合、Clipper2 は穴として存在しない単一外周パスを返すため、追加処理は不要。

### 根拠

- Clipper2 は EvenOdd ルールに従い、正のオフセット時に消えるべき穴（例: 「O」の穴がオフセット外周と接触した後）は自動的に除去される
- 穴の有無の判定は `Clipper.Area()` の正負で簡単に識別できる

---

## R-004: 子 GameObject のライフサイクル管理

### 決定事項

`SolidText3DComponent` の既存フィールドにアウトライン用子 GameObject 参照を追加し、以下のライフサイクルで管理する:

```text
初期化時 (Awake/OnEnable): _outline.Enabled なら子 GO を生成してメッシュ構築
再生成時 (_isDirty = true):
  Enabled=false かつ 子 GO が存在 → Destroy(子 GO), 参照を null クリア
  Enabled=true かつ 子 GO が null  → new GameObject + AddComponent → メッシュ構築
  Enabled=true かつ 子 GO が存在  → メッシュのみ再構築（GO は再利用）
OnDestroy: Destroy(子 GO)（Editor では DestroyImmediate）
```

`CreateOutlineChild()` の実装方針:

- `new GameObject("__OutlineMesh__")` を生成し `AddComponent<MeshFilter>()` + `AddComponent<MeshRenderer>()`
- `transform.SetParent(this.transform, false)` で親子付け
- `hideFlags` を設定してヒエラルキーを汚さない（`HideFlags.HideInHierarchy` は選択肢だが、デバッグのため通常表示が望ましい）

### 根拠

- `SetActive` による非表示ではなく物理的に GO を破棄することで、シーン内に不要な GO が残らない
- メッシュ再生成はパラメータ変更時のみ発生するため、生成コスト（`new GameObject`）は頻繁には発生しない
- GO 再利用（Enabled=true で既存 GO がある場合）によりメッシュ差し替えのみに留められる

### 検討した代替案

| 案 | 却下理由 |
| --- | ------- |
| `CharacterObjectPool` を拡張して流用 | アウトライン用 GO は文字数に関係なく常に 1 つ。プール不要 |
| ScriptableObject でアウトライン設定を分離 | 1 コンポーネント専用設定なので SerializeField インライン定義で十分 |

---

## R-005: Z ファイティング防止

### 決定事項

**裏面埋めモード**において、アウトラインと文字本体の面が同一 Z 位置に重なることを防ぐため、以下の計算式でアウトライン裏面 Z を決定する:

```text
outlne_back_z = -max(bodyExtrusionDepth, outlineThickness) - Z_FIGHT_EPSILON
Z_FIGHT_EPSILON = 0.0001f
```

- 文字本体の表面を `Z = 0` とする（両モード共通）
- アウトライン表面: `Z = 0`（文字本体表面と一致）
- アウトライン裏面（裏面埋めモード）: 上記計算式による値

### 根拠

- `Z_FIGHT_EPSILON = 0.0001f` は Unity ワールド単位で 0.1mm 相当。目視で差は認識できない
- `max()` 取得により「文字本体が厚い場合」「アウトラインが厚い場合」どちらにも対応

### 検討した代替案

| 案 | 却下理由 |
| --- | ------- |
| ユーザーが手動でオフセット設定 | UX が悪い。ユーザーが Zファイティングを認識・対処する必要がある |
| レンダーキューのオフセットで制御 | マテリアル依存で、ユーザー側でマテリアルを変更した場合に無効化される |

---

## R-006: Clipper2 の DLL 取得・バージョン

### 決定事項

Clipper2 C# v1.4.0（または最新安定版）の NuGet パッケージから `Clipper2Lib.dll` を取得し、`Runtime/Plugins/Clipper2Lib.dll` に配置する。

- NuGet パッケージ: `Clipper2` by Angus Johnson
- ライセンス: Boost Software License 1.0（MIT 互換）
- URL: <https://github.com/AngusJohnson/Clipper2>

`Third Party Notices.md` に以下を追記:

```text
## 3. Clipper2
バージョン: 1.4.x
用途: ポリゴンオフセット演算（アウトライン輪郭生成）
ライセンス: Boost Software License 1.0
URL: https://github.com/AngusJohnson/Clipper2
```

### 根拠

- Boost Software License 1.0 は MIT と同等の許可型ライセンスであり、商用 Asset Store 配布に問題なし
- NuGet の .NET Standard 2.0 向けバイナリは Unity 6 の .NET Standard 2.1 ランタイムと互換性がある
