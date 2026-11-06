# U 線 R0 檔案清單

骨架來源：`/Volumes/hk6429/projects/liulaolao-garden`（唯讀）。表中「同相對路徑」代表來源專案中的相同路徑；所有複製的 Unity 資產均攜帶原 `.meta`，重新命名保留 GUID。

## 改寫範圍

- 命名空間、瀏覽器橋接函式、PlayerPrefs 鍵與模板目錄改為 Liuruan；站名改為 liuruan-tiantai。
- ProjectSettings 保留 companyName，修改 productName、Android／Standalone／iPhone bundle id；清除前作場景與 Input Action 綁定。Build() 完成後設定 Boot、Main。
- URP 管線實際位於 Assets/Settings，因此連同全域設定與 Profile 帶入；另外保留 Assets/_Project/Settings 全部。DefaultVolumeProfile 移除 9 個無來源的測試／自訂元件；PC Renderer 舊版探針除錯引用清空，與 Mobile 設定一致。
- 保留共用閱讀、分頁、問答與計分。拆除宴席、配對、小遊戲、任務、劇情與直式閱讀專用元件依賴；未複製禁用玩法。CharacterAnimDriver 改為直接 SetSpeaking()，不綁前作事件。
- DistanceCull 的依賴 ProximitySwap 一起搬入，改以 PlayerController 找玩家。SpeechBridge 移除任務語音依賴。
- R30Capture 改為本作六個機位；R30Lighting 改用三區探針範圍，天空與 static 清單未提供時跳過；R30Foliage 尚無風動 shader 與材質時回報缺件而不修改場景。
- FontSubset 保留 RebuildNoDialog，掃描本作 Data JSON／場景／腳本，移除前作資料檔名稱。
- deploy.sh 必填 NETLIFY_SITE_ID，不自動建站，移除 itch 步驟；補上原本位於外部 skill 的兩支必要 helper，部署腳本可在本專案內找到依賴。
- Assets/TextMesh Pro 是字型與 UI 的必要資源，未帶入前作美術、場景、題庫、Library、Build、Deploy 或備份。

## 完整實體檔案（包含 .meta）

| 本專案路徑 | 處理 | 來源／用途 |
|---|---|---|
| `.gitignore` | 新增 | Unity 快取、建置與部署副本排除 |
| `Assets/Plugins.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Plugins/WebGL.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Plugins/WebGL/LiuruanQuest.jslib` | 複製後改寫 | Assets/Plugins/WebGL/LiulaolaoQuest.jslib |
| `Assets/Plugins/WebGL/LiuruanQuest.jslib.meta` | 原樣複製 | Assets/Plugins/WebGL/LiulaolaoQuest.jslib.meta |
| `Assets/Plugins/WebGL/LiuruanSpeech.jslib` | 複製後改寫 | Assets/Plugins/WebGL/LiulaolaoSpeech.jslib |
| `Assets/Plugins/WebGL/LiuruanSpeech.jslib.meta` | 原樣複製 | Assets/Plugins/WebGL/LiulaolaoSpeech.jslib.meta |
| `Assets/Settings.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/DefaultVolumeProfile.asset` | 複製後改寫 | 同相對路徑 |
| `Assets/Settings/DefaultVolumeProfile.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/Mobile_RPAsset.asset` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/Mobile_RPAsset.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/Mobile_Renderer.asset` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/Mobile_Renderer.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/PC_RPAsset.asset` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/PC_RPAsset.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/PC_Renderer.asset` | 複製後改寫 | 同相對路徑 |
| `Assets/Settings/PC_Renderer.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/SampleSceneProfile.asset` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/SampleSceneProfile.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` | 原樣複製 | 同相對路徑 |
| `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Fonts.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Fonts/LiberationSans.ttf.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/Fonts & Materials.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/LineBreaking Following Characters.txt` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/LineBreaking Following Characters.txt.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/LineBreaking Leading Characters.txt` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/LineBreaking Leading Characters.txt.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/Sprite Assets.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/Style Sheets.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/Style Sheets/Default Style Sheet.asset` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/Style Sheets/Default Style Sheet.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/TMP Settings.asset` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Resources/TMP Settings.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/SDFFunctions.hlsl` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/SDFFunctions.hlsl.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_Bitmap-Custom-Atlas.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_Bitmap-Custom-Atlas.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_Bitmap-Mobile.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_Bitmap-Mobile.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_Bitmap.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_Bitmap.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF Overlay.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF Overlay.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF SSD.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF SSD.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-HDRP LIT.shadergraph` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-HDRP LIT.shadergraph.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-HDRP UNLIT.shadergraph` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-HDRP UNLIT.shadergraph.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Masking.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Masking.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Overlay.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Overlay.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile SSD.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile SSD.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile-2-Pass.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile-2-Pass.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Surface-Mobile.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Surface-Mobile.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Surface.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-Surface.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-URP Lit.shadergraph` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-URP Lit.shadergraph.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-URP Unlit.shadergraph` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF-URP Unlit.shadergraph.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_SDF.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_Sprite.shader` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMP_Sprite.shader.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMPro.cginc` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMPro.cginc.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMPro_Mobile.cginc` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMPro_Mobile.cginc.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMPro_Surface.cginc` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Shaders/TMPro_Surface.cginc.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Sprites.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Sprites/EmojiOne.json` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Sprites/EmojiOne.json.meta` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Sprites/EmojiOne.png` | 原樣複製 | 同相對路徑 |
| `Assets/TextMesh Pro/Sprites/EmojiOne.png.meta` | 原樣複製 | 同相對路徑 |
| `Assets/WebGLTemplates.meta` | 原樣複製 | 同相對路徑 |
| `Assets/WebGLTemplates/Liuruan.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/MemoryProfiler.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/MemoryProfiler.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/MemoryProfiler.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/MemoryProfiler.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/favicon.ico` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/favicon.ico |
| `Assets/WebGLTemplates/Liuruan/TemplateData/favicon.ico.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/favicon.ico.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/fullscreen-button.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/fullscreen-button.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/fullscreen-button.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/fullscreen-button.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/progress-bar-empty-dark.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/progress-bar-empty-dark.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/progress-bar-empty-dark.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/progress-bar-empty-dark.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/progress-bar-empty-light.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/progress-bar-empty-light.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/progress-bar-empty-light.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/progress-bar-empty-light.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/progress-bar-full-dark.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/progress-bar-full-dark.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/progress-bar-full-dark.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/progress-bar-full-dark.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/progress-bar-full-light.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/progress-bar-full-light.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/progress-bar-full-light.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/progress-bar-full-light.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/style.css` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/style.css |
| `Assets/WebGLTemplates/Liuruan/TemplateData/style.css.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/style.css.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/unity-logo-dark.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/unity-logo-dark.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/unity-logo-dark.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/unity-logo-dark.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/unity-logo-light.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/unity-logo-light.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/unity-logo-light.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/unity-logo-light.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/unity-logo-title-footer.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/unity-logo-title-footer.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/unity-logo-title-footer.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/unity-logo-title-footer.png.meta |
| `Assets/WebGLTemplates/Liuruan/TemplateData/webmemd-icon.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/webmemd-icon.png |
| `Assets/WebGLTemplates/Liuruan/TemplateData/webmemd-icon.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/TemplateData/webmemd-icon.png.meta |
| `Assets/WebGLTemplates/Liuruan/index.html` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/index.html |
| `Assets/WebGLTemplates/Liuruan/index.html.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/index.html.meta |
| `Assets/WebGLTemplates/Liuruan/thumbnail.png` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/thumbnail.png |
| `Assets/WebGLTemplates/Liuruan/thumbnail.png.meta` | 原樣複製 | Assets/WebGLTemplates/Liulaolao/thumbnail.png.meta |
| `Assets/_Project.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Editor.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Editor/FontSubset.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Editor/FontSubset.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Editor/LiuruanGreybox.cs` | 新增 | 灰盒產生器，含 16 點／三區／玩家自我檢查 |
| `Assets/_Project/Editor/LiuruanGreybox.cs.meta` | 新增 | 新檔／資料夾的 Unity GUID |
| `Assets/_Project/Editor/R30Capture.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Editor/R30Capture.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Editor/R30Foliage.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Editor/R30Foliage.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Editor/R30Lighting.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Editor/R30Lighting.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Editor/SceneBundles.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Editor/SceneBundles.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Fonts.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Fonts/NotoSerifTC SDF.asset` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Fonts/NotoSerifTC SDF.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Fonts/NotoSerifTC Static SDF.asset` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Fonts/NotoSerifTC Static SDF.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Fonts/NotoSerifTC-Regular.otf` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Fonts/NotoSerifTC-Regular.otf.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/AudioManager.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/AudioManager.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/BootLoader.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/BootLoader.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/CamOcclusion.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/CamOcclusion.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/CharacterAnimDriver.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/CharacterAnimDriver.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/ClueManager.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/ClueManager.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/ClueMarker.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/ClueMarker.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/DistanceCull.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/DistanceCull.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/FindGlow.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/FindGlow.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/InstancedGroup.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/InstancedGroup.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/Interactable.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/Interactable.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/PlayerController.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/PlayerController.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/ProximitySwap.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/ProximitySwap.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/QualityToggle.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/QualityToggle.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/QuizStats.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/QuizStats.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/RenderGovernor.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/RenderGovernor.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/ScrollUI.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/ScrollUI.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/SpeechBridge.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/SpeechBridge.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/Strings.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/Strings.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Scripts/TouchControls.cs` | 複製後改寫 | 同相對路徑 |
| `Assets/_Project/Scripts/TouchControls.cs.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Settings.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Settings/MainVolumeProfile.asset` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Settings/MainVolumeProfile.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Settings/PostProcessData_NoGrainSMAA.asset` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Settings/PostProcessData_NoGrainSMAA.asset.meta` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Settings/R30_Lighting.lighting` | 原樣複製 | 同相對路徑 |
| `Assets/_Project/Settings/R30_Lighting.lighting.meta` | 原樣複製 | 同相對路徑 |
| `Packages/manifest.json` | 原樣複製 | 同相對路徑 |
| `Packages/packages-lock.json` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/AudioManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/ClusterInputManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/DynamicsManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/EditorBuildSettings.asset` | 複製後改寫 | 同相對路徑 |
| `ProjectSettings/EditorSettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/GraphicsSettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/InputManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/MemorySettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/MultiplayerManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/NavMeshAreas.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/PackageManagerSettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/Physics2DSettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/PresetManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/ProjectSettings.asset` | 複製後改寫 | 同相對路徑 |
| `ProjectSettings/ProjectVersion.txt` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/QualitySettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/SceneTemplateSettings.json` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/ShaderGraphSettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/TagManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/TimeManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/URPProjectSettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/UnityConnectSettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/VFXManager.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/VersionControlSettings.asset` | 原樣複製 | 同相對路徑 |
| `ProjectSettings/XRSettings.asset` | 原樣複製 | 同相對路徑 |
| `README.md` | 新增 | 專案操作與驗證說明 |
| `docs/U-R0-files.md` | 新增 | 本清單 |
| `logs/U-R0-checks.log` | 新增 | 離線檢查紀錄 |
| `logs/U-R0-roslyn.log` | 新增 | Runtime 與 Editor 分開預編譯紀錄 |
| `logs/U-R0.md` | 新增 | 交件報告 |
| `scripts/compact_webgl_data.py` | 原樣複製 | 同相對路徑 |
| `scripts/deploy.sh` | 複製後改寫 | 同相對路徑 |
| `scripts/make_headers.py` | 複製依賴 | deploy.sh 必要依賴，從既有 unity-webgl-edu-game/scripts 複製 |
| `scripts/patch_index.py` | 複製依賴 | deploy.sh 必要依賴，從既有 unity-webgl-edu-game/scripts 複製 |
| `scripts/patch_loading_recovery.py` | 複製後改寫 | 同相對路徑 |
| `scripts/patch_mobile_entry.py` | 複製後改寫 | 同相對路徑 |
| `scripts/prepare_wasm_delivery.py` | 原樣複製 | 同相對路徑 |
| `scripts/test_r0_delivery.py` | 新增 | 離線搬移與部署補丁測試 |
| `scripts/verify.sh` | 複製後改寫 | 同相對路徑 |

## CC 執行 Build() 後產生

以下尚未產生，不能列為目前已存在的場景或資產：

- `Assets/_Project/Scenes/Boot.unity`、`Main.unity`。
- `GreyboxTerrain.asset`、`GreyboxRoof.asset`、`GreyboxGround.terrainlayer`、`GreyboxGround.asset`（皆在 Scenes）。
- `Assets/_Project/Materials/Greybox/*.mat` 與 Unity 自動產生的 `.meta`。
- FontSubset 重建靜態 SDF 字集；既有字型 GUID 不變。
