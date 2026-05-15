# アウトラインを設定する

このガイドでは、文字の外周に立体アウトラインを付ける方法を説明します。

## 基本設定

Inspector の Outline セクションで次を設定します。

- Enabled
- Offset Amount
- Thickness
- Display Mode
- Material

まずは次の設定から始めると分かりやすいです。

- Enabled = On
- Offset Amount = 0.05
- Thickness = 0.1
- Display Mode = Donut

値を変えたあとは、Mesh Update セクションの Regenerate Mesh で反映します。

## 各項目の意味

### Enabled

アウトライン機能そのもののオン・オフです。  
Off にすると、アウトライン用の子オブジェクトは破棄されます。

### Offset Amount

文字の外側へどれだけ広げるかです。  
0 にすると、アウトライン用の子オブジェクト自体は残したまま、メッシュだけが空になります。

### Thickness

アウトラインの奥行きです。  
文字本体の厚みとは別に設定できます。

### Display Mode

次の2種類があります。

- Donut: 本体の周囲をリング状に囲む見え方
- BackFilled: 正面のシルエットは保ちつつ、背面側を埋めた見え方

見た目の差は主に奥行き方向の構成に出ます。  
正面から見た輪郭は、どちらも同じ外形を共有します。

### Material

アウトライン専用のマテリアルです。  
設定しない場合は、本体の MeshRenderer の sharedMaterial を使います。

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

## アウトラインが見えないとき

### Enabled をオンにしているのに見えない

- Offset Amount が 0 より大きいか確認します。
- Material が透明すぎないか確認します。
- 文字本体と同じマテリアルを使っていないか確認します。

### いったん消したいが設定値は残したい

Offset Amount を 0 にします。  
この場合、アウトライン用子オブジェクトは残るので、後で値を戻すと再利用されます。

### 完全に無効化したい

Enabled を Off にします。  
この場合、アウトライン用子オブジェクトは削除されます。
