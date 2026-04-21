# 繧ｿ繧ｹ繧ｯ: Solid Text 3D 窶・繝ｬ繧､繧｢繧ｦ繝医・繝輔か繝ｳ繝医・繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ謾ｹ蝟・

**Input**: `specs/002-text-layout-improvements/` 縺ｮ險ｭ險域枚譖ｸ  
**Branch**: `002-text-layout-improvements`  
**Date**: 2026-04-21  
**Prerequisites**: plan.md 笨・/ spec.md 笨・/ data-model.md 笨・/ contracts/ 笨・/ quickstart.md 笨・

---

## 繝輔か繝ｼ繝槭ャ繝・ `[ID] [P?] [Story?] 隱ｬ譏餐

- **[P]**: 荳ｦ蛻怜ｮ溯｡悟庄・育焚縺ｪ繧九ヵ繧｡繧､繝ｫ繝ｻ譛ｪ螳御ｺ・ち繧ｹ繧ｯ縺ｫ髱樔ｾ晏ｭ假ｼ・
- **[Story]**: 蟇ｾ蠢懊☆繧九Θ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ・・S1縲弑S7・・
- 蜷・ち繧ｹ繧ｯ縺ｮ隱ｬ譏弱↓縺ｯ蟇ｾ雎｡繝輔ぃ繧､繝ｫ縺ｮ豁｣遒ｺ縺ｪ繝代せ繧定ｨ倩ｼ・

---

## Phase 1: 繧ｻ繝・ヨ繧｢繝・・・亥・譛峨う繝ｳ繝輔Λ・・

**逶ｮ逧・*: 譌｢蟄倥・ UPM 繝代ャ繧ｱ繝ｼ繧ｸ讒矩繧貞燕謠舌→縺吶ｋ縺溘ａ縲∬ｿｽ蜉縺ｮ繝励Ο繧ｸ繧ｧ繧ｯ繝亥・譛溷喧縺ｯ荳崎ｦ√・
縺薙・讖溯・縺ｧ縺ｯ譁ｰ隕上ヵ繧｡繧､繝ｫ縺ｯ縺吶∋縺ｦ譌｢蟄倥・ `Runtime/`繝ｻ`Editor/`繝ｻ`Tests/` 縺ｫ驟咲ｽｮ縺吶ｋ縲・

> _・域眠隕上・繝ｭ繧ｸ繧ｧ繧ｯ繝医せ繧ｭ繝｣繝輔か繝ｼ繝ｫ繝峨↑縺・窶・Phase 2 縺ｮ蝓ｺ逶､繧ｿ繧ｹ繧ｯ縺ｸ逶ｴ謗･遘ｻ陦鯉ｼ雲

---

## Phase 2: 蝓ｺ逶､・亥・繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ縺ｮ蜑肴署譚｡莉ｶ・・

**逶ｮ逧・*: 蜈ｨ繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ縺御ｾ晏ｭ倥☆繧句梛螳夂ｾｩ繝ｻ繝・・繧ｿ繝｢繝・Ν螟画峩繧貞・陦悟ｮ溯｣・☆繧九・

**笞・・驥崎ｦ・*: 縺薙・繝輔ぉ繝ｼ繧ｺ縺悟ｮ御ｺ・☆繧九∪縺ｧ縲√＞縺九↑繧九Θ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ繧ら捩謇九〒縺阪↑縺・・

- [X] T001 `Packages/com.masachuang.solidtext3d/Runtime/TextAnchorEnums.cs` 繧呈眠隕丈ｽ懈・縺励～HorizontalAnchor`繝ｻ`VerticalAnchor`繝ｻ`DepthAnchor`繝ｻ`WritingMode`繝ｻ`ObjectMode` 縺ｮ 5 縺､縺ｮ enum 繧・`MasaChuang.SolidText3D` 蜷榊燕遨ｺ髢薙↓螳夂ｾｩ縺吶ｋ・・ata-model.md ﾂｧ 譁ｰ隕・Enum 蝙・蜿ら・・・
- [X] T002 `Packages/com.masachuang.solidtext3d/Runtime/MeshGenerationParams.cs` 繧呈峩譁ｰ縺吶ｋ: `FontPath (string)` 繝輔ぅ繝ｼ繝ｫ繝峨ｒ蜑企勁縺励～HorizontalAnchor`繝ｻ`VerticalAnchor`繝ｻ`DepthAnchor`繝ｻ`WritingMode`繝ｻ`MaxWidth`繝ｻ`MaxHeight`繝ｻ`VerticalColumnWidth`繝ｻ`RotateAsciiInVertical` 縺ｮ 8 繝輔ぅ繝ｼ繝ｫ繝峨ｒ霑ｽ蜉縺吶ｋ・・ata-model.md ﾂｧ MeshGenerationParams 蜿ら・・・
- [X] T003 [P] `Packages/com.masachuang.solidtext3d/Runtime/GlyphContour.cs` 繧呈峩譁ｰ縺吶ｋ: `AdvanceHeight (float)`繝ｻ`CharIndex (int)`繝ｻ`IsVisible (bool)` 縺ｮ 3 繝輔ぅ繝ｼ繝ｫ繝峨ｒ霑ｽ蜉縺吶ｋ・・ata-model.md ﾂｧ GlyphContour 蜿ら・・・
- [X] T004 `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` 繧剃ｿｮ豁｣縺吶ｋ: `FontPath` 蜿ら・繧偵☆縺ｹ縺ｦ `FontData = File.ReadAllBytes(...)` 縺ｫ鄂ｮ縺肴鋤縺医ゝ002 縺ｮ遐ｴ螢顔噪螟画峩縺ｫ蟇ｾ蠢懊＆縺帙ｋ

**繝√ぉ繝・け繝昴う繝ｳ繝・*: 蝓ｺ逶､螳御ｺ・窶・莉･髯阪・蜷・ヵ繧ｧ繝ｼ繧ｺ縺ｯ迢ｬ遶九＠縺ｦ逹謇句庄閭ｽ縲・

---

## Phase 3: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 1 窶・繝輔か繝ｳ繝医・繧､繝ｳ繧ｹ繝壹け繧ｿ繧｢繧ｿ繝・メ (P1) 識 MVP

**逶ｮ讓・*: 繧､繝ｳ繧ｹ繝壹け繧ｿ縺ｮ Object 繝輔ぅ繝ｼ繝ｫ繝峨↓ `.ttf`/`.otf` 繝輔ぃ繧､繝ｫ繧偵い繧ｿ繝・メ縺吶ｋ縺縺代〒繝輔か繝ｳ繝医ｒ謖・ｮ壹〒縺阪ｋ縲よ立 `string Font` API 繧貞ｮ悟・蜑企勁縺励｀AJOR 繝舌Φ繝暦ｼ・.0.0 竊・2.0.0・峨→縺吶ｋ縲・

**迢ｬ遶九ユ繧ｹ繝・*: 繝輔か繝ｳ繝医ヵ繧｣繝ｼ繝ｫ繝峨↓繧｢繧ｻ繝・ヨ繧偵い繧ｿ繝・メ縺吶ｋ縺ｨ 3D 繝・く繧ｹ繝医′縺昴・繝輔か繝ｳ繝医〒陦ｨ遉ｺ縺輔ｌ繧九ゅヵ繧ｩ繝ｳ繝・Missing 譎ゅ・逶ｴ蜑阪Γ繝・す繝･繧堤ｶｭ謖√＠縲∬ｭｦ蜻翫ｒ 1 蝗槭□縺大・蜉帙☆繧九％縺ｨ繧貞腰迢ｬ縺ｧ遒ｺ隱阪〒縺阪ｋ縲・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 1 縺ｮ繝・せ繝・

- [X] T005 [P] [US1] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` 縺ｫ莉･荳・4 繝・せ繝医ｒ霑ｽ蜉縺吶ｋ: `FontAsset_Missing_MaintainsPreviousMesh`・・R-016: 繝輔か繝ｳ繝・Missing 譎ゅ↓逶ｴ蜑阪Γ繝・す繝･邯ｭ謖√・LogWarning 1 蝗槭・縺ｿ・峨・`Text_Empty_ClearsMesh`・・R-015: 繝・く繧ｹ繝医′遨ｺ譁・ｭ怜・縺ｮ縺ｨ縺阪Γ繝・す繝･縺後け繝ｪ繧｢縺輔ｌ縲∬ｭｦ蜻翫↑縺励・PerCharacter 繝｢繝ｼ繝峨〒縺ｯ蜈ｨ蟄・GameObject 縺碁撼繧｢繧ｯ繝・ぅ繝門喧縺輔ｌ繧九％縺ｨ・峨・`FontAsset_Changed_AtRuntime_Regenerates`・・S1 Scenario 3: 繝輔か繝ｳ繝医い繧ｻ繝・ヨ縺悟ｮ溯｡梧凾縺ｫ螟画峩縺輔ｌ縺溷ｴ蜷医∵ｬ｡繝輔Ξ繝ｼ繝縺ｮ譖ｴ譁ｰ縺ｧ譁ｰ縺励＞繝輔か繝ｳ繝医〒蜀肴緒逕ｻ縺輔ｌ繧九％縺ｨ・峨・`FontAsset_NotSet_SkipsMeshGeneration`・・R-012: 繝輔か繝ｳ繝医′譛ｪ險ｭ螳壹・蝣ｴ蜷医↓ `RegenerateMesh()` 縺悟叉蠎ｧ縺ｫ繝ｪ繧ｿ繝ｼ繝ｳ縺励※繝｡繝・す繝･逕滓・繧定｡後ｏ縺ｪ縺・％縺ｨ繧偵Λ繝ｳ繧ｿ繧､繝蛛ｴ縺ｧ蜊倅ｽ捺､懆ｨｼ縺吶ｋ・・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 1 縺ｮ螳溯｣・

- [X] T006 [US1] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` 繧剃ｿｮ豁｣縺吶ｋ: `[SerializeField] string _font` 縺ｨ `public string Font` 繝励Ο繝代ユ繧｣繧貞ｮ悟・蜑企勁縺励～_fontAsset (UnityEngine.Object)`繝ｻ`_fontBytesCache (TextAsset, HideInInspector)`繝ｻ`_fontMissingWarningIssued (bool)` 繧定ｿｽ蜉縺吶ｋ縲ＡFontAsset` 繝励Ο繝代ユ繧｣・・et/set・峨ｒ螳溯｣・＠縲～RegenerateMesh()` 蜀・↓繝輔か繝ｳ繝域悴險ｭ螳夲ｼ・R-012: 繝｡繝・す繝･逕滓・繧ｹ繧ｭ繝・・・峨・**遨ｺ譁・ｭ怜・・・R-015: 繝｡繝・す繝･繧ｯ繝ｪ繧｢繝ｻPerCharacter 蜈ｨ蟄・GameObject 髱槭い繧ｯ繝・ぅ繝門喧繝ｻ隴ｦ蜻翫↑縺暦ｼ・*繝ｻMissing・・R-016: 逶ｴ蜑阪Γ繝・す繝･邯ｭ謖√・LogWarning 1 蝗橸ｼ峨・蜃ｦ逅・ｒ霑ｽ蜉縺吶ｋ縲よ眠隕剰ｿｽ蜉縺吶ｋ `public FontAsset` 繝励Ο繝代ユ繧｣縺ｫ XML 繝峨く繝･繝｡繝ｳ繝医さ繝｡繝ｳ繝茨ｼ・<summary>`, `<param>`, `<returns>`・峨ｒ莉倅ｸ弱☆繧具ｼ域・豕・VII・会ｼ・ata-model.md ﾂｧ SolidText3DComponent 蜿ら・・・
- [X] T007 [US1] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` 繧剃ｿｮ豁｣縺吶ｋ: `DrawDefaultInspector()` 繧貞ｻ・ｭ｢縺励※謇句虚謠冗判縺ｫ蛻・ｊ譖ｿ縺医～EditorGUILayout.ObjectField("Font Asset", ..., typeof(UnityEngine.Object), false)` 縺ｧ繝輔か繝ｳ繝医ヵ繧｣繝ｼ繝ｫ繝峨ｒ霑ｽ蜉縺励√ヵ繧ｩ繝ｳ繝域悴繧｢繧ｿ繝・メ譎ゅ↓ `EditorGUILayout.HelpBox()` 縺ｧ隴ｦ蜻翫ｒ陦ｨ遉ｺ縺吶ｋ・・esearch.md ﾂｧ R-001 蜿ら・・・

**繝√ぉ繝・け繝昴う繝ｳ繝・*: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 1 螳御ｺ・窶・繧､繝ｳ繧ｹ繝壹け繧ｿ Object 繝輔ぅ繝ｼ繝ｫ繝峨〒縺ｮ繝輔か繝ｳ繝域欠螳壹→ Missing 蜍穂ｽ懊ｒ蜊倡峡縺ｧ讀懆ｨｼ蜿ｯ閭ｽ縲・

---

## Phase 4: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 2 窶・繝輔か繝ｳ繝医ヵ繧｡繧､繝ｫ縺ｮ閾ｪ蜍・.bytes 螟画鋤 (P2)

**逶ｮ讓・*: `.ttf`/`.otf` 繝輔ぃ繧､繝ｫ繧偵・繝ｭ繧ｸ繧ｧ繧ｯ繝医↓繧､繝ｳ繝昴・繝医☆繧九□縺代〒 `.bytes` 繝輔ぃ繧､繝ｫ縺瑚・蜍慕函謌舌＆繧後ｋ縲ゅΘ繝ｼ繧ｶ繝ｼ縺ｯ謇句虚螟画鋤荳崎ｦ√・

**迢ｬ遶九ユ繧ｹ繝・*: `.ttf` 繝輔ぃ繧､繝ｫ繧偵う繝ｳ繝昴・繝医＠縺ｦ `.bytes` 繝輔ぃ繧､繝ｫ縺・`Assets/SolidText3DFonts/` 縺ｫ逕滓・縺輔ｌ繧九％縺ｨ繧貞腰迢ｬ縺ｧ遒ｺ隱阪〒縺阪ｋ縲・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 2 縺ｮ繝・せ繝・

- [X] T008 [P] [US2] `Packages/com.masachuang.solidtext3d/Tests/Editor/FontAssetPostprocessorTests.cs` 繧呈眠隕丈ｽ懈・縺励∽ｻ･荳・3 繝・せ繝医ｒ螳溯｣・☆繧・ `OnPostprocess_TtfFile_CreatesBytesFile`・・ttf 繧､繝ｳ繝昴・繝医〒 .bytes 逕滓・ 窶・**SC-004 讀懆ｨｼ**: 繝・せ繝医さ繝｡繝ｳ繝医↓繝峨Λ繝・げ・・ラ繝ｭ繝・・ 1 謫堺ｽ懊〒繧ｻ繝・ヨ繧｢繝・・縺悟ｮ御ｺ・☆繧九ヵ繝ｭ繝ｼ縺ｧ縺ゅｋ縺薙→繧呈・險倥☆繧九％縺ｨ・峨・`OnPostprocess_ExistingBytesFile_Skips`・域里蟄・.bytes 蜀榊､画鋤繧ｹ繧ｭ繝・・・峨・`OnPostprocess_IoError_LogsError`・・/O 繧ｨ繝ｩ繝ｼ譎ゅ・ LogError・・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 2 縺ｮ螳溯｣・

- [X] T009 [US2] `Packages/com.masachuang.solidtext3d/Editor/FontAssetPostprocessor.cs` 繧呈眠隕丈ｽ懈・縺吶ｋ: `AssetPostprocessor` 繧堤ｶ呎価縺励～OnPostprocessAllAssets` 縺ｧ `.ttf`/`.otf` 繧呈､懃衍縺吶ｋ縲ＡAssets/SolidText3DFonts/{assetGuid}.bytes` 縺ｸ荳譎ゅヵ繧｡繧､繝ｫ邨檎罰縺ｮ繧｢繝医Α繝・け譖ｸ縺崎ｾｼ縺ｿ・・R-017・峨ｒ螳溯｣・＠縲～AssetDatabase.ImportAsset()` 縺ｧ逋ｻ骭ｲ縺吶ｋ縲ゅす繝ｼ繝ｳ蜀・・ `SolidText3DComponent` 繧定ｵｰ譟ｻ縺励※ `_fontBytesCache` 繧定・蜍戊ｨｭ螳壹＠ `EditorUtility.SetDirty()` + `AssetDatabase.SaveAssets()` 縺ｧ繧ｷ繝ｪ繧｢繝ｩ繧､繧ｺ縺吶ｋ・・ata-model.md ﾂｧ FontAssetPostprocessor 蜿ら・縲〉esearch.md ﾂｧ R-002 蜿ら・・峨・*蛻ｶ髯・*: `FindObjectsByType<SolidText3DComponent>()` 縺ｫ繧医ｋ襍ｰ譟ｻ縺ｯ髢九＞縺ｦ縺・ｋ繧ｷ繝ｼ繝ｳ蜀・・縺ｿ蟇ｾ雎｡縲１refab 繧｢繧ｻ繝・ヨ縺ｯ蟇ｾ雎｡螟悶→縺ｪ繧翫∵ｬ｡蝗・Inspector 陦ｨ遉ｺ譎ゅ↓閾ｪ蜍戊ｨｭ螳壹＆繧後ｋ・・lan.md 繧ｹ繝・ャ繝・8 縺ｮ豕ｨ險伜盾辣ｧ・・

**繝√ぉ繝・け繝昴う繝ｳ繝・*: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 2 螳御ｺ・窶・.ttf 繧｢繧ｿ繝・メ縺九ｉ .bytes 閾ｪ蜍慕函謌舌∪縺ｧ縺ｮ豬√ｌ繧貞腰迢ｬ縺ｧ讀懆ｨｼ蜿ｯ閭ｽ縲・

---

## Phase 5: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 3 窶・繝・く繧ｹ繝磯・鄂ｮ縺ｮ繧｢繝ｳ繧ｫ繝ｼ謖・ｮ・(P3)

**逶ｮ讓・*: Horizontal / Vertical / Depth 縺ｮ 3 霆ｸ繧｢繝ｳ繧ｫ繝ｼ繧偵◎繧後◇繧檎峡遶九＠縺溘ヵ繧｣繝ｼ繝ｫ繝峨〒險ｭ螳壹＠縲√Γ繝・す繝･繧偵い繝ｳ繧ｫ繝ｼ菴咲ｽｮ縺ｫ蜷医ｏ縺帙※繧ｪ繝輔そ繝・ヨ縺ｧ縺阪ｋ縲・

**迢ｬ遶九ユ繧ｹ繝・*: 蜷・ｻｸ縺ｮ繧｢繝ｳ繧ｫ繝ｼ繧貞・繧頑崛縺医√Γ繝・す繝･縺ｮ bounds 縺悟次轤ｹ縺ｫ蟇ｾ縺励※譛溷ｾ・←縺翫ｊ縺ｫ繧ｪ繝輔そ繝・ヨ縺輔ｌ繧九％縺ｨ繧貞腰迢ｬ縺ｧ遒ｺ隱阪〒縺阪ｋ縲・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 3 縺ｮ繝・せ繝・

- [X] T010 [P] [US3] `Packages/com.masachuang.solidtext3d/Tests/Editor/LayoutEngineTests.cs` 繧呈眠隕丈ｽ懈・縺励∽ｻ･荳九・繝・せ繝医ｒ螳溯｣・☆繧・ `ApplyHorizontalLayout_EmptyGlyphs_DoesNotThrow`繝ｻ`CalculateAnchorOffset_Center_ReturnsHalfExtents`縲ゅ＆繧峨↓ `CalculateAnchorOffset_AllCombinations_BoundsMatchOrigin` 繝代Λ繝｡繝ｼ繧ｿ蛹悶ユ繧ｹ繝茨ｼ・[TestCase]` 遲会ｼ峨〒 `HorizontalAnchor`ﾃ・ ﾃ・`VerticalAnchor`ﾃ・ ﾃ・`DepthAnchor`ﾃ・ 縺ｮ蜈ｨ 27 邨・∩蜷医ｏ縺帙ｒ蛻玲嫌縺励∝推邨・∩蜷医ｏ縺帙〒繝｡繝・す繝･縺ｮ蟇ｾ蠢懊☆繧矩ｭ轤ｹ繝ｻ霎ｺ繝ｻ驥榊ｿ・′蜴溽せ縺ｫ荳閾ｴ縺吶ｋ縺薙→繧呈､懆ｨｼ縺吶ｋ・・C-005 蜈ｨ邨・∩蜷医ｏ縺帑ｿ晁ｨｼ・峨ょ刈縺医※莉･荳・2 繝・せ繝医ｒ霑ｽ蜉縺吶ｋ: `AnchorChange_SetsDirtyFlag`・・R-005: 繧｢繝ｳ繧ｫ繝ｼ繝励Ο繝代ユ繧｣縺ｮ setter 縺・`_isDirty = true` 繧定ｨｭ螳壹＠縲∵ｬ｡繝輔Ξ繝ｼ繝縺ｮ繝｡繝・す繝･蜀崎ｨ育ｮ励′逋ｺ蜍輔☆繧九％縺ｨ繧呈､懆ｨｼ縺吶ｋ・峨・`Layout_MaxWidthZero_NoWordWrap`・・R-014: `MaxWidth` 縺・0 縺ｮ蝣ｴ蜷医↓閾ｪ蜍墓釜繧願ｿ斐＠縺瑚｡後ｏ繧後★縲∵隼陦後さ繝ｼ繝峨・縺ｿ縺ｧ陦後′蛻ｶ蠕｡縺輔ｌ繧九％縺ｨ繧呈､懆ｨｼ縺吶ｋ・・
- [X] T011 [P] [US3] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` 縺ｫ `Build_WithCenterAnchor_BoundsSymmetric` 繝・せ繝医ｒ霑ｽ蜉縺吶ｋ

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 3 縺ｮ螳溯｣・

- [X] T012 [US3] `Packages/com.masachuang.solidtext3d/Runtime/LayoutEngine.cs` 繧呈眠隕丈ｽ懈・縺吶ｋ: `internal static class LayoutEngine` 縺ｫ `ApplyHorizontalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)` 縺ｨ `CalculateAnchorOffset(Bounds meshBounds, MeshGenerationParams p)` 繧貞ｮ溯｣・☆繧九ゅい繝ｳ繧ｫ繝ｼ繧ｪ繝輔そ繝・ヨ險育ｮ怜ｼ擾ｼ域ｰｴ蟷ｳ: Left=0, Center=-width/2, Right=-width; 蝙ら峩: Upper=-height, Middle=-height/2, Lower=0; 螂･陦後″: Front=0, Center=-depth/2, Back=-depth・峨ｒ驕ｩ逕ｨ縺吶ｋ縲よ里蟄・`GlyphMeshBuilder.ApplyLayout()` 縺ｮ繝ｭ繧ｸ繝・け繧堤ｧｻ讀阪＠縲～MaxWidth` 縺ｫ繧医ｋ閾ｪ蜍墓釜繧願ｿ斐＠縺ｫ蟇ｾ蠢懊☆繧九・*FR-013 謚倥ｊ霑斐＠邊貞ｺｦ縺ｮ險隱槫愛螳・*: 蜷・枚蟄励・ Unicode 繧ｳ繝ｼ繝峨・繧､繝ｳ繝医′荳玖ｨ倥＞縺壹ｌ縺九・遽・峇縺ｫ蜷ｫ縺ｾ繧後ｋ蝣ｴ蜷医・譁・ｭ怜腰菴阪〒謚倥ｊ霑斐＠縲√◎繧御ｻ･螟厄ｼ医Λ繝・Φ譁・ｭ励・ASCII 遲会ｼ峨・繧ｹ繝壹・繧ｹ繝ｻ蛹ｺ蛻・ｊ譁・ｭ励ｒ蝓ｺ貅悶→縺励◆蜊倩ｪ槫腰菴阪〒謚倥ｊ霑斐☆: `\u2E80`窶伝\u9FFF`・・JK邨ｱ蜷域ｼ｢蟄励・縺ｲ繧峨′縺ｪ繝ｻ繧ｫ繧ｿ繧ｫ繝顔ｭ会ｼ峨～\uAC00`窶伝\uD7AF`・医ワ繝ｳ繧ｰ繝ｫ髻ｳ遽・峨～\uFF00`窶伝\uFF60`・亥・隗定恭謨ｰ蟄励・險伜捷・会ｼ・esearch.md ﾂｧ R-005 蜿ら・・・
- [X] T013 [US3] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` 繧剃ｿｮ豁｣縺吶ｋ: `ApplyLayout()` 繝励Λ繧､繝吶・繝医Γ繧ｽ繝・ラ繧貞炎髯､縺励※ `LayoutEngine.ApplyHorizontalLayout()` 蜻ｼ縺ｳ蜃ｺ縺励↓鄂ｮ縺肴鋤縺医～GetFontBytes()` 縺九ｉ `FontPath` 蜿ら・繧貞炎髯､縺吶ｋ縲ＡBuild()` 蜀・〒 `MeshExtruder.Build()` 蠕後↓ `LayoutEngine.CalculateAnchorOffset()` 繧貞他縺ｳ蜃ｺ縺励※蜈ｨ鬆らせ縺ｫ繧ｪ繝輔そ繝・ヨ繧貞刈邂励☆繧具ｼ・ata-model.md ﾂｧ 繝・・繧ｿ繝輔Ο繝ｼ讎りｦ・蜿ら・・・
- [X] T014 [US3] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` 繧剃ｿｮ豁｣縺吶ｋ: `_horizontalAnchor`繝ｻ`_verticalAnchor`繝ｻ`_depthAnchor` 縺ｮ 3 繝輔ぅ繝ｼ繝ｫ繝峨→縺昴ｌ縺槭ｌ縺ｮ繝励Ο繝代ユ繧｣・・HorizontalAnchor`繝ｻ`VerticalAnchor`繝ｻ`DepthAnchor`・峨～_maxWidth`繝ｻ`_maxHeight` 繝輔ぅ繝ｼ繝ｫ繝峨→ `MaxWidth`繝ｻ`MaxHeight` 繝励Ο繝代ユ繧｣繧定ｿｽ蜉縺吶ｋ縲ゅ・繝ｭ繝代ユ繧｣ setter 縺ｧ `_isDirty = true` 繧定ｨｭ螳壹☆繧九よ眠隕剰ｿｽ蜉縺吶ｋ縺吶∋縺ｦ縺ｮ `public` 繝励Ο繝代ユ繧｣縺ｫ XML 繝峨く繝･繝｡繝ｳ繝医さ繝｡繝ｳ繝茨ｼ・<summary>`, `<param>`, `<returns>`・峨ｒ莉倅ｸ弱☆繧具ｼ域・豕・VII・・
- [X] T015 [US3] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` 縺ｫ `HorizontalAnchor`繝ｻ`VerticalAnchor`繝ｻ`DepthAnchor` 縺ｮ 3 縺､縺ｮ繝峨Ο繝・・繝繧ｦ繝ｳ繝輔ぅ繝ｼ繝ｫ繝峨→ `MaxWidth`繝ｻ`MaxHeight` 縺ｮ謨ｰ蛟､繝輔ぅ繝ｼ繝ｫ繝峨ｒ謇句虚謠冗判縺ｧ霑ｽ蜉縺吶ｋ・・R-004 蜿ら・・・

**繝√ぉ繝・け繝昴う繝ｳ繝・*: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 3 螳御ｺ・窶・3 霆ｸ繧｢繝ｳ繧ｫ繝ｼ險ｭ螳壹→繝｡繝・す繝･菴咲ｽｮ繧ｪ繝輔そ繝・ヨ繧貞腰迢ｬ縺ｧ讀懆ｨｼ蜿ｯ閭ｽ縲・

---

## Phase 6: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 4 窶・繧､繝ｳ繧ｹ繝壹け繧ｿ蜈･蜉帙Ξ繧､繝・Φ繧ｷ縺ｮ謾ｹ蝟・(P4)

**逶ｮ讓・*: 繝・く繧ｹ繝医ヵ繧｣繝ｼ繝ｫ繝牙・蜉帑ｸｭ縺ｯ繝｡繝・す繝･蜀咲函謌舌ｒ謚大宛縺励・nter 繧ｭ繝ｼ縺ｾ縺溘・繝輔か繝ｼ繧ｫ繧ｹ繧｢繧ｦ繝医・繧ｿ繧､繝溘Φ繧ｰ縺ｧ縺ｮ縺ｿ蜀咲函謌舌☆繧九・

**迢ｬ遶九ユ繧ｹ繝・*: 繝・く繧ｹ繝医ヵ繧｣繝ｼ繝ｫ繝峨↓髟ｷ縺・枚蟄怜・繧堤ｴ譌ｩ縺丞・蜉帙＠縲∝・蜉帑ｸｭ縺ｫ蜀咲函謌舌′襍ｰ繧峨↑縺・％縺ｨ繧堤｢ｺ隱阪〒縺阪ｋ縲・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 4 縺ｮ繝・せ繝・

- [X] T016 [P] [US4] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DInspectorTests.cs` 繧呈眠隕丈ｽ懈・縺ｾ縺溘・譖ｴ譁ｰ縺励～SuppressAutoRegenerate_WhileFocused_BlocksRegeneration` 繝・せ繝医ｒ螳溯｣・☆繧・ `SuppressAutoRegenerate` 繝輔Λ繧ｰ縺・`true` 縺ｮ髢薙・ `RegenerateMesh()` 縺悟他縺ｳ蜃ｺ縺輔ｌ縺壹√ヵ繧ｩ繝ｼ繧ｫ繧ｹ繧｢繧ｦ繝亥ｾ後↓蜻ｼ縺ｳ蜃ｺ縺輔ｌ繧九％縺ｨ繧呈､懆ｨｼ縺吶ｋ・・R-006縲ヾC-001 縺ｮ蝓ｺ遉取擅莉ｶ遒ｺ隱搾ｼ・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 4 縺ｮ螳溯｣・

- [X] T036 [US4] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` 縺ｫ `_suppressAutoRegenerate (bool)` 繝輔ぅ繝ｼ繝ｫ繝峨→ `SuppressAutoRegenerate` 繝励Ο繝代ユ繧｣・・et/set・峨ｒ霑ｽ蜉縺励～LateUpdate()` 蜀・〒 `_suppressAutoRegenerate` 縺・true 縺ｮ蝣ｴ蜷医・ `RegenerateMesh()` 繧貞他縺ｳ蜃ｺ縺輔↑縺・ｈ縺・宛蠕｡繧定ｿｽ蜉縺吶ｋ縲ＡSuppressAutoRegenerate` 繝励Ο繝代ユ繧｣縺ｫ XML 繝峨く繝･繝｡繝ｳ繝医さ繝｡繝ｳ繝茨ｼ・<summary>` 縺ｮ縺ｿ縲Ｃool 繝励Ο繝代ユ繧｣縺ｯ蠑墓焚繝ｻ謌ｻ繧雁､繧呈戟縺溘↑縺・◆繧・`<param>` / `<returns>` 縺ｯ逵∫払蜿ｯ・峨ｒ莉倅ｸ弱☆繧具ｼ域・豕・VII・会ｼ・R-006縲〉esearch.md ﾂｧ R-003 蜿ら・・・
- [X] T017 [US4] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` 縺ｮ繝・く繧ｹ繝医ヵ繧｣繝ｼ繝ｫ繝峨↓ `EditorGUI.BeginChangeCheck()` / `EndChangeCheck()` 繧堤畑縺・※蜈･蜉帛､牙喧繧呈､懃衍縺吶ｋ: 蜈･蜉帶､懃衍譎ゅ↓ `_target.SuppressAutoRegenerate = true` 繧定ｨｭ螳壹☆繧九ＡEditorApplication.update` 縺ｯ繝輔か繝ｼ繧ｫ繧ｹ迥ｶ諷九・繝昴・繝ｪ繝ｳ繧ｰ・・EditorGUIUtility.editingTextField` 縺ｮ逶｣隕厄ｼ峨↓縺ｮ縺ｿ菴ｿ逕ｨ縺励√ち繧､繝槭・繧ｫ繧ｦ繝ｳ繝医ム繧ｦ繝ｳ縺ｯ螳溯｣・＠縺ｪ縺・ゅヵ繧ｩ繝ｼ繧ｫ繧ｹ繧｢繧ｦ繝医∪縺溘・ Enter 繧ｭ繝ｼ遒ｺ螳壽凾縺ｮ縺ｿ `RegenerateMesh()` 繧貞他縺ｳ蜃ｺ縺励※ `SuppressAutoRegenerate = false` 縺ｫ謌ｻ縺呻ｼ・R-006: 譎る俣邨碁℃縺ｫ繧医ｋ閾ｪ蜍慕｢ｺ螳壹↑縺励〉esearch.md ﾂｧ R-003 蜿ら・・・

**繝√ぉ繝・け繝昴う繝ｳ繝・*: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 4 螳御ｺ・窶・蜈･蜉帙ョ繝舌え繝ｳ繧ｹ縺ｫ繧医ｋ繝ｬ繧､繝・Φ繧ｷ謾ｹ蝟・ｒ蜊倡峡縺ｧ遒ｺ隱榊庄閭ｽ・・C-001: 50ms 譛ｪ貅縺ｮ蜈･蜉幃≦蟒ｶ・峨・

---

## Phase 7: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 5 窶・鬆ｻ郢√↑譁・ｭ怜・譖ｴ譁ｰ縺ｮ繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ謾ｹ蝟・(P5)

**逶ｮ讓・*: 繝・く繧ｹ繝医′豈弱ヵ繝ｬ繝ｼ繝螟画峩縺輔ｌ繧・UI 繝ｦ繝ｼ繧ｹ繧ｱ繝ｼ繧ｹ縺ｧ GC Alloc 繧呈椛蛻ｶ縺励∝酔荳繝代Λ繝｡繝ｼ繧ｿ譎ゅ・繝｡繝・す繝･蜀咲函謌舌ｒ繧ｹ繧ｭ繝・・縺励※ 60fps 繧堤ｶｭ謖√☆繧九・

**迢ｬ遶九ユ繧ｹ繝・*: 豈弱ヵ繝ｬ繝ｼ繝繝・く繧ｹ繝医ｒ螟画峩縺吶ｋ繧ｷ繝翫Μ繧ｪ縺ｧ蜀咲函謌舌さ繧ｹ繝医ｒ險域ｸｬ縺励∝酔荳繝・く繧ｹ繝域凾縺ｫ繧ｹ繧ｭ繝・・縺檎匱蜍輔☆繧九％縺ｨ繧貞腰迢ｬ縺ｧ遒ｺ隱阪〒縺阪ｋ縲・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 5 縺ｮ繝・せ繝・

- [X] T018 [P] [US5] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` 縺ｫ莉･荳・2 繝・せ繝医ｒ霑ｽ蜉縺吶ｋ: `RegenerateMesh_SameParams_SkipsRegeneration`・亥酔荳繝代Λ繝｡繝ｼ繧ｿ繝上ャ繧ｷ繝･譎ゅ↓蜀咲函謌舌′繧ｹ繧ｭ繝・・縺輔ｌ繧九％縺ｨ・峨・`RegenerateMesh_SameParams_ZeroGCAlloc`・亥酔荳繝代Λ繝｡繝ｼ繧ｿ譎ゅ↓ `GC.GetTotalMemory(false)` 蜑榊ｾ後・蟾ｮ蛻・′繧ｼ繝ｭ縺ｧ縺ゅｋ縺薙→窶・SC-003 讀懆ｨｼ・・
- [ ] T037 [P] [US5] `Packages/com.masachuang.solidtext3d/Tests/Runtime/PerformanceTests.cs` 繧呈眠隕丈ｽ懈・縺励～TextUpdate_EveryFrame_Under2msFrameTime` Play Mode 繝・せ繝医ｒ螳溯｣・☆繧・ 豈弱ヵ繝ｬ繝ｼ繝繝・く繧ｹ繝医ｒ螟画峩縺吶ｋ繧ｷ繝翫Μ繧ｪ縺ｧ `Profiler.BeginSample` / `EndSample` 繧剃ｽｿ逕ｨ縺励※繝｡繝・す繝･蜀咲函謌仙・逅・・謇隕∵凾髢薙ｒ險域ｸｬ縺励・00 譁・ｭ嶺ｻ･荳九・讓呎ｺ也噪縺ｪ譁・ｭ玲焚縺ｧ 2ms 譛ｪ貅縺ｧ縺ゅｋ縺薙→繧呈､懆ｨｼ縺吶ｋ・・C-002 讀懆ｨｼ・・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 5 縺ｮ螳溯｣・

- [X] T019 [US5] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` 縺ｫ `_lastParamHash (int)` 繝輔ぅ繝ｼ繝ｫ繝峨ｒ霑ｽ蜉縺励～RegenerateMesh()` 縺ｮ蜀帝ｭ縺ｧ迴ｾ蝨ｨ繝代Λ繝｡繝ｼ繧ｿ縺ｮ繝上ャ繧ｷ繝･縺ｨ `_lastParamHash` 繧呈ｯ碑ｼ・＠縺ｦ蜷御ｸ縺ｮ蝣ｴ蜷医・譌ｩ譛溘Μ繧ｿ繝ｼ繝ｳ縺吶ｋ繝ｭ繧ｸ繝・け繧貞ｮ溯｣・☆繧九・*GC 蛻ｶ邏・*: 繝上ャ繧ｷ繝･險育ｮ励・ `Text.GetHashCode() ^ 蜷・enum.GetHashCode()` 縺ｮ XOR 邨仙粋縺ｮ縺ｿ繧剃ｽｿ逕ｨ縺励～new`繝ｻ LINQ繝ｻ譁・ｭ怜・騾｣邨舌・`ToString()` 遲峨・ GC 繧｢繝ｭ繧ｱ繝ｼ繧ｷ繝ｧ繝ｳ繧堤匱逕溘☆繧句・逅・ｒ `LateUpdate()` 蜀・〒荳蛻・ｽｿ逕ｨ縺励↑縺・％縺ｨ・域・豕・V・峨・*萓句､・*: 遨ｺ譁・ｭ怜・・・""`・峨・ FR-015 縺ｮ隕丞ｮ壹↓繧医ｊ蟶ｸ縺ｫ繝繝ｼ繝・ぅ謇ｱ縺・→縺励√ワ繝・す繝･荳閾ｴ縺ｧ繧よ掠譛溘Μ繧ｿ繝ｼ繝ｳ縺励↑縺・％縺ｨ・・R-007 縺ｮ縲檎ｩｺ譁・ｭ怜・縺ｯ蟶ｸ縺ｫ繝繝ｼ繝・ぅ縺ｨ縺励※謇ｱ縺・崎ｦ丞ｮ壹ｒ蠢・★螳医ｋ縺薙→・会ｼ・R-007縲‥ata-model.md ﾂｧ `_lastParamHash` 螻樊ｧ陦ｨ蜿ら・縲〉esearch.md ﾂｧ R-007 蜿ら・・・

**繝√ぉ繝・け繝昴う繝ｳ繝・*: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 5 螳御ｺ・窶・繝・く繧ｹ繝域悴螟画峩譎ゅ・ GC Alloc 繧ｼ繝ｭ縺翫ｈ縺ｳ繝輔Ξ繝ｼ繝繝ｬ繝ｼ繝育ｶｭ謖√ｒ蜊倡峡縺ｧ讀懆ｨｼ蜿ｯ閭ｽ縲・

---

## Phase 8: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 6 窶・譁・ｭ励＃縺ｨ縺ｮ蛟句挨繧ｪ繝悶ず繧ｧ繧ｯ繝亥喧 (P6)

**逶ｮ讓・*: `ObjectMode.PerCharacter` 譎ゅ↓蜷・枚蟄励↓蟇ｾ蠢懊☆繧句ｭ・GameObject 繧堤函謌舌＠繧ｪ繝悶ず繧ｧ繧ｯ繝医・繝ｼ繝ｫ縺ｧ邂｡逅・☆繧九ょ庄隕匁枚蟄励・縺ｿ縺悟ｯｾ雎｡縺ｧ縲∝・繧頑崛縺域凾縺ｯ繝励・繝ｫ繧呈ｭ｣縺励￥蛻ｶ蠕｡縺吶ｋ縲・

**迢ｬ遶九ユ繧ｹ繝・*: Per-Character 繝｢繝ｼ繝峨〒 "ABC" 繧定ｨｭ螳壹＠縲・ 縺､縺ｮ蟄・GameObject 縺檎函謌舌＆繧後ｋ縺薙→繧貞腰迢ｬ縺ｧ遒ｺ隱阪〒縺阪ｋ縲・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 6 縺ｮ繝・せ繝・

- [X] T020 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/CharacterObjectPoolTests.cs` 繧呈眠隕丈ｽ懈・縺励∽ｻ･荳・3 繝・せ繝医ｒ螳溯｣・☆繧・ `Sync_MoreChars_CreatesNewChildren`繝ｻ`Sync_FewerChars_DeactivatesExcess`繝ｻ`Sync_SameCount_ReusesExistingChildren`
- [X] T021 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` 縺ｫ `BuildPerCharacter_ThreeChars_ReturnsThreeMeshes` 繝・せ繝医ｒ霑ｽ蜉縺吶ｋ
- [X] T022 [P] [US6] `Packages/com.masachuang.solidtext3d/Tests/Editor/SolidText3DComponentTests.cs` 縺ｫ莉･荳・2 繝・せ繝医ｒ霑ｽ蜉縺吶ｋ: `ObjectMode_PerCharacter_CreatesChildObjects`繝ｻ`PerCharacter_IndependentMaterial_CanBeSet`・・C-006: Per-Character 繝｢繝ｼ繝峨〒蜷・ｭ・GameObject 縺ｫ迢ｬ遶九＠縺・`MeshRenderer.material` 繧定ｨｭ螳壹〒縺阪ｋ縺薙→繧呈､懆ｨｼ窶・US6 Scenario 4 蟇ｾ蠢懶ｼ・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 6 縺ｮ螳溯｣・

- [X] T023 [US6] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` 縺ｫ `BuildPerCharacter(MeshGenerationParams p)` 繝｡繧ｽ繝・ラ繧定ｿｽ蜉縺吶ｋ: `IsVisible == true` 縺ｮ繧ｰ繝ｪ繝輔・縺ｿ繧貞ｯｾ雎｡縺ｫ縲・ 譁・ｭ励★縺､ `GlyphContourBuilder` + `MeshExtruder` 縺ｧ蛟句挨 Mesh 繧堤函謌舌＠縺ｦ `List<Mesh>` 縺ｨ縺励※霑斐☆・・ata-model.md ﾂｧ BuildPerCharacter 險ｭ險・蜿ら・・・
- [X] T024 [US6] `Packages/com.masachuang.solidtext3d/Runtime/CharacterObjectPool.cs` 繧呈眠隕丈ｽ懈・縺吶ｋ: `internal sealed class CharacterObjectPool` 縺ｫ `Sync(List<GlyphContour> visibleGlyphs, List<Mesh> perCharMeshes)` 繧貞ｮ溯｣・☆繧九よ枚蟄玲焚蠅怜刈譎ゅ・縺ｿ譁ｰ隕・GameObject 繧堤函謌撰ｼ・MeshFilter` + `MeshRenderer` 繧・`AddComponent`・峨＠縲∵枚蟄玲焚貂帛ｰ第凾縺ｯ菴吝臆繧・`SetActive(false)` 縺ｧ髱槭い繧ｯ繝・ぅ繝門喧縺吶ｋ縲・*`visibleGlyphs` 縺檎ｩｺ縺ｮ蝣ｴ蜷茨ｼ育ｩｺ譁・ｭ怜・: FR-015・峨・繝励・繝ｫ蜀・・蜈ｨ GameObject 繧・`SetActive(false)` 縺ｫ縺吶ｋ縲・* Destroy 縺ｯ陦後ｏ縺ｪ縺・ｼ・R-009b縲：R-015縲〉esearch.md ﾂｧ R-004 蜿ら・・・
- [X] T025 [US6] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` 縺ｫ `_objectMode (ObjectMode)` 繝輔ぅ繝ｼ繝ｫ繝峨→ `ObjectMode` 繝励Ο繝代ユ繧｣繧定ｿｽ蜉縺励～CharacterObjectPool` 繧､繝ｳ繧ｹ繧ｿ繝ｳ繧ｹ繧剃ｿ晄戟縺吶ｋ `_characterPool` 繝輔ぅ繝ｼ繝ｫ繝峨ｒ霑ｽ蜉縺吶ｋ縲ＡObjectMode` 螟画峩譎ゅ・蛻・ｊ譖ｿ縺医Ο繧ｸ繝・け・・erCharacter 竊・SingleObject 蛻・ｊ譖ｿ縺域凾縺ｫ蟄・GameObject 繧貞・縺ｦ髱槭い繧ｯ繝・ぅ繝門喧縲ヾingleObject 竊・PerCharacter 蛻・ｊ譖ｿ縺域凾縺ｫ繝励・繝ｫ繧貞・譛溷喧・峨ｒ螳溯｣・☆繧九ＡObjectMode` 繝励Ο繝代ユ繧｣縺ｫ XML 繝峨く繝･繝｡繝ｳ繝医さ繝｡繝ｳ繝茨ｼ・<summary>`・峨ｒ莉倅ｸ弱☆繧具ｼ域・豕・VII・会ｼ・ata-model.md ﾂｧ 迥ｶ諷矩・遘ｻ: ObjectMode 蛻・ｊ譖ｿ縺・蜿ら・・・
- [X] T026 [US6] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` 縺ｫ `ObjectMode` 縺ｮ驕ｸ謚槭ラ繝ｭ繝・・繝繧ｦ繝ｳ繧呈焔蜍墓緒逕ｻ縺ｧ霑ｽ蜉縺吶ｋ・・R-008 蜿ら・・・

**繝√ぉ繝・け繝昴う繝ｳ繝・*: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 6 螳御ｺ・窶・Per-Character 繝｢繝ｼ繝峨・蟄・GameObject 逕滓・繝ｻ繝励・繝ｫ邂｡逅・・繝｢繝ｼ繝牙・繧頑崛縺医ｒ蜊倡峡縺ｧ讀懆ｨｼ蜿ｯ閭ｽ縲・

---

## Phase 9: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 7 窶・邵ｦ譖ｸ縺阪ユ繧ｭ繧ｹ繝医・蟇ｾ蠢・(P7)

**逶ｮ讓・*: `WritingMode.Vertical` 譎ゅ↓譁・ｭ励ｒ荳翫°繧我ｸ九・蛻励ｒ蜿ｳ縺九ｉ蟾ｦ縺ｫ荳ｦ縺ｹ繧狗ｸｦ譖ｸ縺阪Ξ繧､繧｢繧ｦ繝医ｒ螳溽樟縺吶ｋ縲ゅい繝ｳ繧ｫ繝ｼ謖・ｮ壹・Per-Character 繝｢繝ｼ繝峨→縺ｮ蜷梧凾菴ｿ逕ｨ縺ｫ蟇ｾ蠢懊☆繧九・

**迢ｬ遶九ユ繧ｹ繝・*: 邵ｦ譖ｸ縺阪Δ繝ｼ繝峨〒縲後≠縺・≧縺医♀縲阪ｒ險ｭ螳壹＠縲∵枚蟄励′邵ｦ縺ｫ荳ｦ縺ｶ縺薙→繧貞腰迢ｬ縺ｧ遒ｺ隱阪〒縺阪ｋ縲・

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 7 縺ｮ繝・せ繝・

- [X] T027 [P] [US7] `Packages/com.masachuang.solidtext3d/Tests/Editor/LayoutEngineTests.cs` 縺ｫ莉･荳・2 繝・せ繝医ｒ霑ｽ蜉縺吶ｋ: `ApplyVerticalLayout_SingleChar_YIsNegative`・育ｸｦ譖ｸ縺肴凾縺ｫ Y 蠎ｧ讓吶′雋譁ｹ蜷代↓騾ｲ繧縺薙→・峨・`ApplyVerticalLayout_MultipleJapaneseChars_PositionsDecreasing`・・C-007: 譌･譛ｬ隱櫁､・焚譁・ｭ励〒蜷・Y 蠎ｧ讓吶′蜑阪・譁・ｭ励ｈ繧雁ｰ上＆縺上↑繧翫∝・髢馴囈繝ｻ譛螟ｧ鬮倥＆縺ｫ繧医ｋ謚倥ｊ霑斐＠繧よｭ｣縺励￥蜍穂ｽ懊☆繧九％縺ｨ繧呈､懆ｨｼ窶・`[TestCase]` 繝代Λ繝｡繝ｼ繧ｿ蛹悶〒隍・焚繧ｱ繝ｼ繧ｹ繧貞・謖吶☆繧九％縺ｨ・・
- [X] T028 [P] [US7] `Packages/com.masachuang.solidtext3d/Tests/Editor/GlyphMeshBuilderTests.cs` 縺ｫ `Build_VerticalMode_YDecreases` 繝・せ繝医ｒ霑ｽ蜉縺吶ｋ

### 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 7 縺ｮ螳溯｣・

- [X] T029 [US7] `Packages/com.masachuang.solidtext3d/Runtime/LayoutEngine.cs` 縺ｫ `ApplyVerticalLayout(List<GlyphContour> glyphs, MeshGenerationParams p)` 繧貞ｮ溯｣・☆繧・ 蜷・枚蟄励ｒ荳翫°繧我ｸ具ｼ・ 縺ｯ貂帛ｰ第婿蜷托ｼ峨↓荳ｦ縺ｹ縲～MaxHeight` 繧定ｶ・∴縺溘ｉ谺｡縺ｮ蛻暦ｼ・ 縺ｯ蟾ｦ譁ｹ蜷托ｼ峨∈謚倥ｊ霑斐☆縲・*`MaxHeight` 縺・0 縺ｾ縺溘・譛ｪ險ｭ螳壹・蝣ｴ蜷医・閾ｪ蜍墓釜繧願ｿ斐＠繧定｡後ｏ縺壹∵隼陦後さ繝ｼ繝会ｼ・\n`・峨・縺ｿ縺ｧ蛻励・蛻・ｊ譖ｿ縺医ｒ蛻ｶ蠕｡縺吶ｋ・・R-014・峨・* 蜷・・蜀・・譁・ｭ励・ `VerticalColumnWidth`・・ 縺ｮ蝣ｴ蜷医・ `FontSize ﾃ・1.1f`・峨ｒ蝓ｺ貅悶↓豌ｴ蟷ｳ荳ｭ螟ｮ謠・∴縺ｨ縺吶ｋ縲ＡRotateAsciiInVertical` 縺・true 縺ｮ蝣ｴ蜷医・ ASCII 闍ｱ謨ｰ蟄励げ繝ｪ繝輔・蝗櫁ｻ｢繝輔Λ繧ｰ繧定ｨｭ螳壹☆繧九り､・焚陦鯉ｼ域隼陦後さ繝ｼ繝会ｼ峨・谺｡蛻励∈縺ｮ謚倥ｊ霑斐＠縺ｨ縺励※蜃ｦ逅・☆繧具ｼ・R-010縲：R-011縲：R-011b縲：R-011c縲：R-014縲〉esearch.md ﾂｧ R-006 蜿ら・・・
- [X] T030 [US7] `Packages/com.masachuang.solidtext3d/Runtime/GlyphMeshBuilder.cs` 繧剃ｿｮ豁｣縺吶ｋ: `Build()` / `BuildPerCharacter()` 蜀・〒 `MeshGenerationParams.WritingMode` 繧貞盾辣ｧ縺励～Vertical` 縺ｮ蝣ｴ蜷医・ `LayoutEngine.ApplyVerticalLayout()` 繧貞他縺ｳ蜃ｺ縺吶ｈ縺・・蟯舌ｒ霑ｽ蜉縺吶ｋ
- [X] T031 [US7] `Packages/com.masachuang.solidtext3d/Runtime/SolidText3DComponent.cs` 縺ｫ `_writingMode (WritingMode)` 繝輔ぅ繝ｼ繝ｫ繝峨→ `WritingMode` 繝励Ο繝代ユ繧｣縲～_verticalColumnWidth (float)` 繝輔ぅ繝ｼ繝ｫ繝峨→ `VerticalColumnWidth` 繝励Ο繝代ユ繧｣縲～_rotateAsciiInVertical (bool)` 繝輔ぅ繝ｼ繝ｫ繝峨→ `RotateAsciiInVertical` 繝励Ο繝代ユ繧｣繧定ｿｽ蜉縺吶ｋ縲ゅ・繝ｭ繝代ユ繧｣ setter 縺ｧ `_isDirty = true` 繧定ｨｭ螳壹☆繧九よ眠隕剰ｿｽ蜉縺吶ｋ縺吶∋縺ｦ縺ｮ `public` 繝励Ο繝代ユ繧｣・・WritingMode`, `VerticalColumnWidth`, `RotateAsciiInVertical`・峨↓ XML 繝峨く繝･繝｡繝ｳ繝医さ繝｡繝ｳ繝茨ｼ・<summary>`, `<param>`, `<returns>`・峨ｒ莉倅ｸ弱☆繧具ｼ域・豕・VII・・
- [X] T032 [US7] `Packages/com.masachuang.solidtext3d/Editor/SolidText3DInspector.cs` 縺ｫ `WritingMode` 繝峨Ο繝・・繝繧ｦ繝ｳ縲～VerticalColumnWidth` 謨ｰ蛟､繝輔ぅ繝ｼ繝ｫ繝峨～RotateAsciiInVertical` 繝医げ繝ｫ繧呈焔蜍墓緒逕ｻ縺ｧ霑ｽ蜉縺吶ｋ・・R-010縲：R-011b縲：R-011c 蜿ら・・・

**繝√ぉ繝・け繝昴う繝ｳ繝・*: 繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ 7 螳御ｺ・窶・邵ｦ譖ｸ縺阪Ξ繧､繧｢繧ｦ繝医∝・謚倥ｊ霑斐＠縲、SCII 蝗櫁ｻ｢繧ｪ繝励す繝ｧ繝ｳ縲√い繝ｳ繧ｫ繝ｼ縺ｨ縺ｮ邨・∩蜷医ｏ縺帙ｒ蜊倡峡縺ｧ讀懆ｨｼ蜿ｯ閭ｽ縲・

---

## Final Phase: 繝昴Μ繝・す繝･繝ｻ讓ｪ譁ｭ逧・未蠢・ｺ・

**逶ｮ逧・*: 繝舌・繧ｸ繝ｧ繝ｳ譖ｴ譁ｰ縲，HANGELOG 險倩ｼ峨√ヱ繝・こ繝ｼ繧ｸ譛邨よ紛蛯吶・

- [X] T033 [P] `Packages/com.masachuang.solidtext3d/package.json` 縺ｮ `"version"` 繧・`"1.0.0"` 縺九ｉ `"2.0.0"` 縺ｫ譖ｴ譁ｰ縺吶ｋ・育ｴ螢顔噪螟画峩: `string Font` 蜑企勁縲…ontracts/SolidText3DComponent-API.md 蜿ら・・・
- [X] T034 [P] `Packages/com.masachuang.solidtext3d/CHANGELOG.md` 縺ｫ v2.0.0 繧ｨ繝ｳ繝医Μ繧定ｿｽ險倥☆繧・ 遐ｴ螢顔噪螟画峩・・Font (string)` 蜑企勁繝ｻ`FontAsset (Object)` 霑ｽ蜉・峨∬ｿｽ蜉讖溯・・医い繝ｳ繧ｫ繝ｼ繝ｻ邵ｦ譖ｸ縺阪・Per-Character繝ｻ閾ｪ蜍・.bytes 螟画鋤繝ｻ繝・ヰ繧ｦ繝ｳ繧ｹ繝ｻ繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ謾ｹ蝟・ｼ峨ｒ險倩ｼ峨☆繧具ｼ・ontracts/SolidText3DComponent-API.md ﾂｧ CHANGELOG 繧ｨ繝ｳ繝医Μ 蜿ら・・・
- [X] T035 [P] `Packages/com.masachuang.solidtext3d/Documentation~/BREAKING_CHANGES.md` 繧呈眠隕丈ｽ懈・縺励∵ｳ募・ IV 萓句､匁ｭ｣蠖灘喧險倬鹸繧定ｨ倩ｼ峨☆繧・ `[SerializeField] string _font` 竊・`UnityEngine.Object _fontAsset` 縺ｯ蝙九′譬ｹ譛ｬ逧・↓逡ｰ縺ｪ繧九◆繧・`[Obsolete]` 縺ｫ繧医ｋ谿ｵ髫守ｧｻ陦後′謚陦鍋噪縺ｫ蝗ｰ髮｣縺ｧ縺ゅｋ逅・罰繧定ｩｳ霑ｰ縺吶ｋ縺ｨ縺ｨ繧ゅ↓縲《pec Clarification 縺ｫ縺ｦ髢狗匱閠・′遐ｴ螢顔噪螟画峩繧呈・遉ｺ謇ｿ隱阪＠縺滓葎繧定ｨ倬鹸縺吶ｋ縲・*縺薙・繧ｿ繧ｹ繧ｯ縺ｯ Final Phase 縺ｮ蠢・亥ｮ滓命莠矩・〒縺ゅｊ縲∝ｮ御ｺ・メ繧ｧ繝・け繝昴う繝ｳ繝医→縺励※謇ｱ縺・*・・PM 諷｣萓・ 繝峨く繝･繝｡繝ｳ繝医・ `Documentation~/` 驟堺ｸ九↓驟咲ｽｮ 窶・諞ｲ豕・I 貅匁侠・会ｼ・lan.md ﾂｧ Constitution Check 蜿ら・・・

---

## 萓晏ｭ倬未菫ゅげ繝ｩ繝包ｼ医Θ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ髢難ｼ・

```text
Phase 2 (蝓ｺ逶､: T001-T004)
笏・
笏懌楳笏笆ｺ Phase 3: US1 繝輔か繝ｳ繝・Inspector (T005-T007) 竊・MVP 蛟呵｣・
笏・       笏・
笏・       笏披楳笏笆ｺ Phase 4: US2 .bytes 閾ｪ蜍募､画鋤 (T008-T009)
笏・
笏懌楳笏笆ｺ Phase 5: US3 繧｢繝ｳ繧ｫ繝ｼ謖・ｮ・(T010-T015)
笏・       笏・
笏・       笏懌楳笏笆ｺ Phase 6: US4 蜈･蜉帙ョ繝舌え繝ｳ繧ｹ (T016, T036, T017)
笏・       笏・
笏・       笏懌楳笏笆ｺ Phase 7: US5 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ (T018, T037, T019)
笏・       笏・
笏・       笏懌楳笏笆ｺ Phase 8: US6 Per-Character (T020-T026)
笏・       笏・
笏・       笏披楳笏笆ｺ Phase 9: US7 邵ｦ譖ｸ縺・(T027-T032)
笏・
笏披楳笏笆ｺ Final Phase: 繝昴Μ繝・す繝･ (T033-T034) 竊・蜈ｨ繝輔ぉ繝ｼ繧ｺ螳御ｺ・ｾ・
```

**迢ｬ遶句ｮ溯｡悟庄閭ｽ縺ｪ繝輔ぉ繝ｼ繧ｺ**: Phase 3 (US1) 縺ｨ Phase 5 (US3) 縺ｯ Phase 2 螳御ｺ・ｾ後↓荳ｦ蛻礼捩謇句庄閭ｽ縲・
Phase 4繝ｻ5繝ｻ6繝ｻ7 (US4-US7) 縺ｯ縺昴ｌ縺槭ｌ Phase 5 (US3) 縺ｮ螳御ｺ・ｒ蜑肴署縺ｨ縺吶ｋ縲・

---

## 荳ｦ蛻怜ｮ溯｡御ｾ具ｼ亥推繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ蜀・ｼ・

### Phase 2・亥渕逶､・牙・縺ｮ荳ｦ蛻怜ｮ溯｡・

```text
T001 (TextAnchorEnums.cs 譁ｰ隕丈ｽ懈・)
    竊・
T002 (MeshGenerationParams.cs 譖ｴ譁ｰ) 笏・荳ｦ蛻怜ｮ溯｡悟庄
T003 (GlyphContour.cs 譖ｴ譁ｰ)         笏・
    竊難ｼ井ｸ｡譁ｹ螳御ｺ・ｾ鯉ｼ・
T004 (GlyphMeshBuilderTests.cs 菫ｮ豁｣)
```

### Phase 5・・S3・牙・縺ｮ荳ｦ蛻怜ｮ溯｡・

```text
T010 (LayoutEngineTests.cs 譁ｰ隕・ 笏・荳ｦ蛻怜ｮ溯｡悟庄
T011 (GlyphMeshBuilderTests 霑ｽ險・ 笏・
    竊難ｼ医ユ繧ｹ繝井ｽ懈・蠕鯉ｼ・
T012 (LayoutEngine.cs 譁ｰ隕丈ｽ懈・)
    竊・
T013 (GlyphMeshBuilder.cs 菫ｮ豁｣)
    竊・
T014 (SolidText3DComponent 繧｢繝ｳ繧ｫ繝ｼ繝輔ぅ繝ｼ繝ｫ繝芽ｿｽ蜉) 笏・荳ｦ蛻怜ｮ溯｡悟庄
T015 (SolidText3DInspector 繧｢繝ｳ繧ｫ繝ｼ UI 霑ｽ蜉)       笏・
```

### Phase 8・・S6・牙・縺ｮ荳ｦ蛻怜ｮ溯｡・

```text
T020 (CharacterObjectPoolTests 譁ｰ隕・ 笏・
T021 (GlyphMeshBuilderTests 霑ｽ險・    笏・荳ｦ蛻怜ｮ溯｡悟庄
T022 (SolidText3DComponentTests 霑ｽ險・ 笏・
    竊難ｼ医ユ繧ｹ繝井ｽ懈・蠕鯉ｼ・
T023 (GlyphMeshBuilder.BuildPerCharacter 霑ｽ蜉)
    竊・
T024 (CharacterObjectPool.cs 譁ｰ隕丈ｽ懈・) 笏・荳ｦ蛻怜ｮ溯｡悟庄
T025 (SolidText3DComponent ObjectMode霑ｽ蜉) 笏・
    竊・
T026 (SolidText3DInspector ObjectMode UI 霑ｽ蜉)
```

---

## 螳溯｣・姶逡･

### MVP 繧ｹ繧ｳ繝ｼ繝暦ｼ域耳螂ｨ譛蛻昴・繝・Μ繝舌Μ繝ｼ・・

**Phase 2 + Phase 3・・S1・・* 縺ｮ縺ｿ縺ｧ MVP 縺ｨ縺励※謌千ｫ九☆繧・

1. `TextAnchorEnums.cs` 縺ｮ螳夂ｾｩ・・001・・
2. `MeshGenerationParams` 縺ｮ遐ｴ螢顔噪螟画峩蟇ｾ蠢懶ｼ・002-T004・・
3. `SolidText3DComponent` 縺ｮ Font 繝輔ぅ繝ｼ繝ｫ繝牙､画峩・・005-T006・・
4. `SolidText3DInspector` 縺ｮ Object 繝輔ぅ繝ｼ繝ｫ繝芽ｿｽ蜉・・007・・

縺薙ｌ縺縺代〒縲梧枚蟄怜・蝙九ヵ繧ｩ繝ｳ繝亥錐 竊・Object 繝輔ぅ繝ｼ繝ｫ繝峨∈縺ｮ遘ｻ陦後阪→縺・≧譛螟ｧ縺ｮ UX 隱ｲ鬘後′隗｣豸医＆繧後ｋ縲・

### 繧､繝ｳ繧ｯ繝ｪ繝｡繝ｳ繧ｿ繝ｫ繝・Μ繝舌Μ繝ｼ鬆・ｺ・

| 繝ｪ繝ｪ繝ｼ繧ｹ蛟呵｣・| 蜷ｫ縺ｾ繧後ｋ繝輔ぉ繝ｼ繧ｺ | 謠蝉ｾ帑ｾ｡蛟､ |
| ----------- | --------------- | ------- |
| v2.0.0-alpha.1 | Phase 2 + 3 | 繝輔か繝ｳ繝・Inspector 繧｢繧ｿ繝・メ・育ｴ螢顔噪螟画峩・・|
| v2.0.0-alpha.2 | + Phase 4 | .bytes 閾ｪ蜍募､画鋤 |
| v2.0.0-beta.1 | + Phase 5 + 6 | 繧｢繝ｳ繧ｫ繝ｼ謖・ｮ・+ 繝・ヰ繧ｦ繝ｳ繧ｹ |
| v2.0.0-beta.2 | + Phase 7 + 8 | 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ + Per-Character |
| v2.0.0 | + Phase 9 + Final | 邵ｦ譖ｸ縺・+ CHANGELOG + 繝舌・繧ｸ繝ｧ繝ｳ譖ｴ譁ｰ |

---

## 繝輔か繝ｼ繝槭ャ繝域､懆ｨｼ

蜈ｨ繧ｿ繧ｹ繧ｯ縺御ｻ･荳九・蠖｢蠑上↓貅匁侠縺励※縺・ｋ縺薙→繧堤｢ｺ隱・

- 笨・縺吶∋縺ｦ縺ｮ繧ｿ繧ｹ繧ｯ縺・`- [ ]` 繝√ぉ繝・け繝懊ャ繧ｯ繧ｹ縺ｧ蟋九∪繧・
- 笨・蜈ｨ繧ｿ繧ｹ繧ｯ縺ｫ騾｣逡ｪ ID 縺後≠繧具ｼ・001・杁037縲∵立陬懆ｶｳ繧ｿ繧ｹ繧ｯ T016b 竊・T036繝ｻT018b 竊・T037 縺ｫ謾ｹ逡ｪ貂医∩・・
- 笨・荳ｦ蛻怜ｮ溯｡悟庄閭ｽ繧ｿ繧ｹ繧ｯ縺ｫ縺ｯ `[P]` 繝槭・繧ｫ繝ｼ縺後≠繧・
- 笨・繝ｦ繝ｼ繧ｶ繝ｼ繧ｹ繝医・繝ｪ繝ｼ繝輔ぉ繝ｼ繧ｺ縺ｮ繧ｿ繧ｹ繧ｯ縺ｫ縺ｯ `[US1]`縲彖[US7]` 繝ｩ繝吶Ν縺後≠繧・
- 笨・蜈ｨ繧ｿ繧ｹ繧ｯ縺ｫ豁｣遒ｺ縺ｪ繝輔ぃ繧､繝ｫ繝代せ縺悟性縺ｾ繧後ｋ
- 笨・繧ｻ繝・ヨ繧｢繝・・繝ｻ蝓ｺ逶､繝輔ぉ繝ｼ繧ｺ縺ｮ繧ｿ繧ｹ繧ｯ縺ｫ縺ｯ繧ｹ繝医・繝ｪ繝ｼ繝ｩ繝吶Ν縺後↑縺・
- 笨・譛邨ゅヵ繧ｧ繝ｼ繧ｺ縺ｮ繧ｿ繧ｹ繧ｯ縺ｫ縺ｯ繧ｹ繝医・繝ｪ繝ｼ繝ｩ繝吶Ν縺後↑縺・
