# クイックスタートガイド: Solid Text 3D

**対象**: Unity 6（6000.x LTS）以降  
**対象プラットフォーム**: PC（Windows / macOS / Linux）  
**作成日**: 2026年4月17日

---

## 前提条件

- Unity 6（6000.x LTS）以降がインストールされていること
- Universal Render Pipeline（URP）が設定済みであること

---

## ステップ 1: パッケージのセットアップ（開発者向け）

> **注意**: このステップはパッケージの開発者向けです。Asset Store からインストールする場合は「ステップ 2」からはじめてください。

### 1-1. サードパーティ DLL の取得（一回限りの作業）

Solid Text 3D は以下の純粋 C# ライブラリに依存します。  
NuGet から DLL を取得し、パッケージの `Runtime/Plugins/` フォルダに配置します。

| ライブラリ | NuGet パッケージ | ライセンス |
| --- | --- | --- |
| SixLabors.Fonts | `SixLabors.Fonts` | MIT |
| LibTessDotNet | `LibTessDotNet` | SGI Free Software License B v2.0 |

**取得手順**:

1. [NuGet.org](https://www.nuget.org/) から各パッケージの `.nupkg` をダウンロード
2. `.nupkg` を `.zip` としてリネームし展開
3. `lib/netstandard2.0/*.dll` を抽出
4. `Packages/com.yourcompany.solidtext3d/Runtime/Plugins/` に配置

> **開発時の推奨**: [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) を Unity プロジェクトに導入すると、`packages.config` で依存関係を管理できます。

### 1-2. UPM パッケージの構造確認

```text
Packages/
  com.yourcompany.solidtext3d/
    package.json
    Runtime/
      SolidText3DComponent.cs
      GlyphMeshBuilder.cs
      ... (その他のランタイムファイル)
      Plugins/
        SixLabors.Fonts.dll
        LibTessDotNet.dll
    Editor/
      SolidText3DInspector.cs
    Tests/
      Editor/
        ...
      Runtime/
        ...
```

---

## ステップ 2: 基本的な使い方（エディタでの静的テキスト）

### 2-1. ゲームオブジェクトにコンポーネントを追加

1. Unity エディタで **Hierarchy** ウィンドウを開く
2. 任意のゲームオブジェクトを選択（または新規作成：`GameObject > Create Empty`）
3. **Inspector** ウィンドウで **「Add Component」** をクリック
4. 検索ボックスに「Solid Text 3D」と入力してコンポーネントを追加

> `MeshFilter` と `MeshRenderer` コンポーネントが自動的に追加されます。

### 2-2. パラメータを設定する

| フィールド | 説明 | 推奨値 |
| --- | --- | --- |
| **Text** | 表示するテキスト（日本語・英語・CJK 対応） | `"Hello World"` |
| **Font** | TTF/OTF フォントアセット（null の場合はデフォルトフォント） | プロジェクトのフォントをドラッグ |
| **Extrusion Depth** | 文字の奥行き（ワールド単位） | `0.1` |
| **Outline Width** | アウトライン幅（0 でアウトラインなし） | `0.0` |

**Inspector での変更はシーンビューにリアルタイムで反映されます。**

---

## ステップ 3: フォントのインポート

1. TTF または OTF フォントファイルを **Project ウィンドウ** の `Assets/Fonts/` フォルダにドラッグアンドドロップ
2. インポートされたフォントアセットを `SolidText3DComponent` の **Font** フィールドにドラッグアンドドロップ

```text
Assets/
  Fonts/
    MyCustomFont.ttf    ← ここにフォントを配置
```

> **CJK フォント推奨例**: Noto Sans JP / Noto Serif JP（Google Fonts より無料取得可能）

---

## ステップ 4: スクリプトからの動的テキスト変更

```csharp
using YourCompany.SolidText3D;
using UnityEngine;

public class DynamicTextExample : MonoBehaviour
{
    // Inspector で割り当て
    [SerializeField] private SolidText3DComponent _solidText;

    void Start()
    {
        // テキストを変更 → 次の LateUpdate でメッシュが自動再生成
        _solidText.Text = "ゲーム開始！";
        _solidText.ExtrusionDepth = 0.2f;
    }

    // 毎秒スコアを更新する例
    int _score = 0;
    void Update()
    {
        _score++;
        _solidText.Text = $"スコア: {_score}";
        // → 同フレーム内の複数変更は LateUpdate で 1 回のメッシュ再生成に集約される
    }
}
```

---

## ステップ 5: マテリアルの設定

Solid Text 3D はマテリアルを管理しません。`MeshRenderer` コンポーネントを使って通常通りマテリアルを設定してください。

```text
GameObject
├── SolidText3DComponent
├── MeshFilter
└── MeshRenderer
      └── Materials
            ├── Element 0: [本体用マテリアル]    ← 文字の面
            └── Element 1: [アウトライン用マテリアル] ← OutlineWidth > 0 の場合に使用
```

---

## 手動実行が必要な作業（Unity エディタ上での確認）

以下の作業は Unity エディタ上での確認が必要です。

### A. DLL の Platform 設定確認

`Runtime/Plugins/` 内の各 DLL について、Unity エディタで以下を確認してください：

1. `SixLabors.Fonts.dll` を Project ウィンドウで選択
2. Inspector で「Plugin Import Settings」を確認
   - **Any Platform**: ✅ チェックあり
   - **Include Platforms**: `Any Platform` または `Editor, Standalone` にチェック
3. `LibTessDotNet.dll` も同様に確認

### B. asmdef の参照設定確認

`Runtime/com.yourcompany.solidtext3d.Runtime.asmdef` を選択し：

- `Override References`: ✅ チェックあり
- `Precompiled References` に `SixLabors.Fonts.dll` と `LibTessDotNet.dll` が含まれているか確認

### C. テストの実行確認

Unity メニュー → **Window > General > Test Runner** から：

- **Edit Mode** タブ: `GlyphMeshBuilderTests`, `BezierSubdividerTests` を実行
- **Play Mode** タブ: `SolidText3DRuntimeTests` を実行

---

## トラブルシューティング

### Q. シーンビューにメッシュが表示されない

1. `SolidText3DComponent` の **Text** フィールドが空でないか確認
2. `MeshRenderer` に **Material** が設定されているか確認
3. `ExtrusionDepth` が `0` の場合、平面メッシュになります（Z 軸方向から見ると見えません）

### Q. `Warning: Font is null, falling back to default font` が出る

- `SolidText3DComponent` の **Font** フィールドにフォントアセットを割り当ててください
- または、空のままにすれば内蔵デフォルトフォントが使用されます

### Q. CJK 文字が表示されない

- CJK グリフを含む TTF/OTF フォントファイルが必要です
- Windows 標準の「游ゴシック」「メイリオ」や Google Fonts の「Noto Sans JP」を使用してください

### Q. `Could not find assembly 'SixLabors.Fonts.dll'` エラーが出る

- `Runtime/Plugins/` に DLL が配置されているか確認
- asmdef の `Precompiled References` に DLL 名が正確に記載されているか確認
- Unity エディタを再起動してみてください

---

## 制限事項（v1.0）

- モバイル（iOS/Android）は非対応（v2.0 で対応予定）
- 文字アニメーション（各文字の個別トランスフォーム変化）は非対応
- SDF（Signed Distance Field）フォントは非対応（ポリゴンメッシュ方式）
- ランタイムのメッシュ生成はメインスレッドで実行（マルチスレッド化は v2.0 以降）
