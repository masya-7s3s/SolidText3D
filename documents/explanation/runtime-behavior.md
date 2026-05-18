# 更新タイミングと内部動作

このページでは、SolidText3DComponent がいつメッシュを作り直すのか、同期更新と deferred 更新がどう違うのか、フォントやマテリアルをどう扱うのかを説明します。

## 設定変更だけでは表示は変わらない

SolidText3DComponent は、Text や FontSize などのプロパティを変更したときに dirty 状態になるだけです。  
その場で自動再生成は行いません。

通常の流れは次の通りです。

1. プロパティを変更する
2. dirty 状態になる
3. RegenerateMesh() か RequestRegenerateMesh() を呼ぶ

ここで重要なのは、LateUpdate も dirty を見て自動再生成するわけではないという点です。  
LateUpdate は deferred regeneration の完了処理だけを担当します。

SolidText3DComponent 自体は ExecuteAlways なので Edit Mode でも存在し続けますが、メッシュ更新のトリガーはあくまで明示呼び出しです。

## RegenerateMesh() は同期更新

RegenerateMesh() は、呼び出し中に次の処理を行います。

1. 現在の設定から request を作る
2. フォントを解決する
3. 前回と同一署名なら適用をスキップする
4. 必要なら PreparedDisplayResult を生成する
5. その場でメッシュへ適用する

そのため、呼び出しが返った時点で表示反映まで終わっています。  
初回表示、ボタン操作、確実に同期反映したい UI で使いやすい API です。

## RequestRegenerateMesh() は deferred 更新

RequestRegenerateMesh() は、高頻度更新向けの non-blocking API です。  
呼び出し時点では表示反映を保証せず、準備処理を裏側で進めます。

現在の実装では、次のように動きます。

- 進行中 request は 1 件だけ持つ
- 追加 request は latest-only で 1 件だけ待機させる
- ワーカースレッドで PrepareDisplayResult を組み立てる
- Main Thread の LateUpdate で完了 result を取り込み、表示へ適用する

これにより、タイマーやスコア更新のような用途で、古い request がいくつもたまるのを防ぎます。

## HasPendingRegeneration が表すもの

HasPendingRegeneration は、次のいずれかが残っている間 true です。

- 進行中の request
- latest-only で待機している request
- まだ適用されていない ready result

つまり「deferred path が完全に落ち着いたか」を見るための状態フラグです。

## deferred 失敗時は keep-last-good

deferred path でフォント取得や準備に失敗した場合、DeferredRegenerationFailed イベントが発火します。  
ただし、表示は失敗した新結果に切り替わらず、直前の正常表示を維持します。

この挙動は、ランタイム更新中に一時的な失敗が起きても画面が急に空になりにくいようにするためです。

## 後から設定を変えたとき、古い deferred request はどうなるか

プロパティ変更時には内部バージョンが進み、待機中の deferred result は無効化されます。  
そのため、古い結果があとから戻ってきても、最新状態を巻き戻しにくい構成になっています。

実際には dirty 化のタイミングで pending latest request と ready result をクリアし、次回の regenerate 呼び出しで新しい request version を採番します。

## フォントの扱い

フォント解決の優先順は次の通りです。

1. Editor では FontAsset から直接読めたフォントデータ
2. 内部で保持している .bytes キャッシュ
3. パッケージ内の NotoSansJP-Black

フォントが取得できない場合、同期 path では警告を出し、deferred path では failure event で通知します。  
どちらの場合も、直前の正常表示を維持する方向で動きます。

## マテリアルの扱い

起動時に本体 MeshRenderer を確認し、マテリアル未設定やエラーシェーダー状態なら URP/Lit または Standard を探して設定します。  
これは、URP 環境で紫色のデフォルト表示になる状況を避けるためです。

outline 側は、専用 Material が設定されていればそれを使い、なければ本体の sharedMaterial を使います。

## 空文字列の扱い

Text が空文字列になると、表示はクリアされます。

- 本体メッシュは空になる
- PerCharacter モードの子オブジェクトは破棄される
- outline 子オブジェクトも削除される

そのため、表示を消したいだけなら GameObject を無効化しなくても Text を空にすれば対応できます。

## 同じ内容を再適用したとき

空文字列以外では、前回適用済みの署名と同じ request は表示適用をスキップします。  
これは無駄な再生成を避けるための最適化です。
