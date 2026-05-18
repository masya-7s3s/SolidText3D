# アウトラインを設定する

このガイドでは、文字の外周に立体 outline を付ける方法を説明します。

## 先に知っておきたいこと

outline は本体メッシュに混ざるのではなく、__OutlineMesh__ という子オブジェクトで別管理されます。  
そのため、本体と別 Material を使いやすく、オンオフの挙動も分かりやすくなっています。

## 基本設定

Inspector の Outline セクションで次を設定します。

- Enabled
- Offset Amount
- Thickness
- Display Mode
- Material

最初は次の設定から始めると分かりやすいです。

- Enabled = On
- Offset Amount = 0.05
- Thickness = 0.1
- Display Mode = Donut

設定後は Regenerate Mesh で反映します。

## 各項目の意味

### Enabled

outline 機能そのもののオンオフです。  
Off にすると、outline 用の子オブジェクトは破棄されます。

### Offset Amount

文字の外側へどれだけ広げるかです。  
0 にすると、outline 子オブジェクトは残したままメッシュだけ空になります。

### Thickness

outline の奥行きです。  
文字本体の ExtrusionDepth とは独立しています。

### Display Mode

次の 2 種類があります。

- Donut: 文字の周囲をリング状に見せる構成
- BackFilled: 正面のシルエットを維持しつつ、背面側を埋める構成

正面から見た輪郭はどちらもほぼ同じで、主な違いは奥行き方向の見え方です。

### Material

outline 専用 Material です。  
設定しない場合は、本体 MeshRenderer の sharedMaterial を使います。

## スクリプトから設定する

```csharp
using MasaChuang.SolidText3D;
using UnityEngine;

public sealed class OutlinePreset : MonoBehaviour
{
    [SerializeField] private SolidText3DComponent text3D;
    [SerializeField] private Material outlineMaterial;

    private void Start()
    {
        text3D.OutlineEnabled = true;
        text3D.OutlineOffset = 0.05f;
        text3D.OutlineThickness = 0.1f;
        text3D.OutlineDisplayMode = OutlineDisplayMode.BackFilled;
        text3D.OutlineMaterial = outlineMaterial;
        text3D.RegenerateMesh();
    }
}
```

## Donut と BackFilled の使い分け

- Donut: 文字のまわりに均一な縁取り感を出したいとき
- BackFilled: 背面側をしっかり埋めて厚み感を強めたいとき

まずは Donut で形を確認し、必要なら BackFilled に切り替えると判断しやすいです。

## outline が見えないとき

### Enabled をオンにしているのに見えない

- Offset Amount が 0 より大きいか確認します
- Material が透明すぎないか確認します
- 本体と同じ色、同じ陰影で見分けづらくなっていないか確認します

### 一時的に消したいが設定値は残したい

Offset Amount を 0 にします。  
子オブジェクトは残るので、あとで値を戻せば再利用されます。

### 完全に無効化したい

Enabled を Off にします。  
この場合は outline 子オブジェクトごと削除されます。
