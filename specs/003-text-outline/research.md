# Research: テキストアウトライン生成（面生成経路の再設計）

**Branch**: `003-text-outline-alt` | **Date**: 2026-05-12  
**Status**: 完了

## R-001: raw offset path ではなく最終 2D profile を先に確定する

**Decision**: Clipper2 の役割を「外周オフセット」だけで終わらせず、outline が実際に表示すべき 2D 断面そのものを確定する。`OutlineContourBuilder` は少なくとも `OriginalFilledContoursEm`、`OffsetFilledContoursEm`、`RingContoursEm = OffsetFilled - OriginalFilled` を返す。

**Rationale**:

- front face を「offset path と元文字 path のその場合成」に頼ると、tessellation 前提と winding 前提が outline 専用実装へ分散する
- 2D 断面を boolean 演算で確定してから押し出せば、3D 側は body と同じ処理に寄せられる
- hole が大きなオフセットで自然に消えるケースも、2D boolean の段階で正しく収束させやすい

**Alternatives considered**:

- offset 外周だけを返して front cap 側で元文字を穴として再解釈する案: front face 不具合の再発点になるため却下
- 手動で外周法線を膨張させる案: 自己交差と hole 吸収の扱いが不安定なため却下

## R-002: outline の 3D 化は文字本体と同じ押し出しコアへ寄せる

**Decision**: `OutlineMeshBuilder` は独自 tessellator を持たず、`MeshExtruder` の cap/side 生成ロジックを共有して outline を立体化する。実装形態は `MeshExtruder` 内 helper の internal 抽出を第一候補とする。

**Rationale**:

- 現在 repo に存在する実績ある立体化経路は [Packages/com.masachuang.solidtext3d/Runtime/MeshExtruder.cs](Packages/com.masachuang.solidtext3d/Runtime/MeshExtruder.cs) だけであり、front/back/side の winding がここで揃っている
- outline だけ別の front cap 生成を持つと、今起きている「front face が表示されない」不具合が再発しやすい
- helper 抽出なら body の既存テスト資産をそのまま回帰基準に使える

**Alternatives considered**:

- `OutlineMeshBuilder` が LibTessDotNet を直接呼ぶ専用実装: 現在の故障点であり却下
- `MeshExtruder.BuildGlyphMesh()` を完全ブラックボックス再利用する案: BackFilled の rear cap と任意 Z anchor 制御がやや不自然になるため helper 抽出案を優先

## R-003: winding は body と同じ規約に正規化し、回帰検査は parity で持つ

**Decision**: `OutlineContourBuilder` の出力 contour は、outer / hole の向きを `MeshExtruder` が受け取る body contour と同じ規約へ必ず正規化する。検査は絶対的な符号固定よりも「body の front cap と outline の front cap が同じ規約を満たすか」で行う。

**Rationale**:

- repo memory にある既知の失敗例では、body front cap と outline front cap の signed area の符号が逆転していた
- 問題は「どちらの符号が正しいか」よりも「body と outline が同じ規約か」にある
- parity ベースの回帰検査なら座標系差異に左右されにくい

**Alternatives considered**:

- Clipper2 の返却順だけを信用する案: ライブラリ出力と renderer 側前提のねじれを検知しにくいため却下
- outline 側だけ三角形 index を都度反転調整する案: 症状に対する対症療法で、根本の規約不一致を隠すだけなので却下

## R-004: BackFilled は donut shell に rear infill を追加して実現する

**Decision**: 両モードとも front silhouette には `RingContoursEm` を使う。Donut はそれを中央基準で押し出す。BackFilled は同じ ring shell を背面固定アンカーで押し出し、`OriginalFilledContoursEm` を rear infill cap として背面に追加する。

**Rationale**:

- 両モードで front silhouette を共有できる
- BackFilled の差分を rear appearance のみへ閉じ込められる
- shell と rear cap を分けると、front side の確実性を保ったまま背面仕様だけを追加できる

**Alternatives considered**:

- BackFilled 専用の完全別 mesh 生成案: front bug を別経路に複製するため却下
- 3D boolean でくぼみを直接切る案: 実装コストが高すぎるため却下

## R-005: Z 配置ルールは mode ごとに固定する

**Decision**:

- Donut: 厚みは中央基準で前後へ均等配分する
- BackFilled: 背面位置は `bodyBack - Z_FIGHT_EPSILON` に固定し、厚み増分は前方へだけ反映する
- `Thickness == 0` のときはどちらの mode でも front cap のみを生成し、rear cap / side face は追加しない

**Rationale**:

- spec の FR-008 / FR-009 / SC-004 と一致する
- anchor ルールが mode ごとに明確で、front silhouette と分離できる

**Alternatives considered**:

- 両 mode とも front Z を固定する案: BackFilled の「背面固定」要件と衝突するため却下

## R-006: child GameObject と依存ポリシーは変更しない

**Decision**: outline 用 child GameObject、material fallback、Clipper2 依存、`Third Party Notices.md` 更新方針は既存 plan を維持する。

**Rationale**:

- 現在の不具合は child 管理ではなく面生成責務の分離にある
- 問題箇所以外の設計を動かさない方が実装と検証が明快になる

**Alternatives considered**:

- outline を別コンポーネントへ分離する案: API と Inspector の複雑化だけが増えるため却下

## Validation Strategy

**Decision**: 失敗の主戦場を Edit Mode の 2D/3D 回帰テストで囲う。

**最低限追加する検査**:

- `OutlineContourBuilderTests`: boolean difference がリング断面を返すこと
- `OutlineContourBuilderTests`: hole 吸収時に single outer profile へ収束すること
- `OutlineContourBuilderTests`: body contour と outline contour の winding parity が一致すること
- `OutlineMeshBuilderTests`: outline front cap が存在し、front silhouette が mode 間で一致すること
- `OutlineMeshBuilderTests`: BackFilled の rear anchor が固定されること

**Rationale**:

- 現在の不具合は compile error ではなく表示バグなので、最短の回帰防止策は geometry テスト
- [Packages/com.masachuang.solidtext3d/Tests/Editor/MeshExtruderTests.cs](Packages/com.masachuang.solidtext3d/Tests/Editor/MeshExtruderTests.cs) に既に donut contour の検査パターンがあり、これを outline 側へ横展開できる
