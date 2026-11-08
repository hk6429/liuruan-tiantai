# U 線 R3b 交件報告

日期：2026-11-08。**本輪完成檔案整合及離線檢查；未開 Unity／UnityMCP／batchmode，未觸發指令，新增實跑截圖 0／15、WebGL build 0 次。未宣稱碰撞步行、手機、Console 或 release build 已通過。**

## CC 入口與放行順序

先在現有 Unity 重新整理／等待編譯，再將下列字串寫入 `Assets/_Project/Editor/R1Command.txt`：

```text
r3-r3b-001
```

既有 R1Bridge 的 r3 入口會呼叫本輪 R3Validation.Begin → R3Setup.Install（含 R3bSetup.Integrate）→ EditMode → 延後進 Play → 新手／停滯／18 點實走／結局 → 本輪 15 圖檢查 → release WebGL＋Scene Bundle。任何失敗寫 U-R3-failure.md 並停止放行；不另開 Editor。R3a 的歷史 PASS 不當成本輪 PASS。

## 逐項變更與驗法

|需求|檔案：行號|交付內容／驗法|
|---|---|---|
|1 系統／測試|`Assets/_Project/Scripts/Systems/StallHintSystem.cs:6`；`Assets/_Project/Tests/EditMode/R3CoreTests.cs:35`|匯入 S 線新增 14 個 Runtime 原始檔及 R3 測試、5 fixtures；沿用既有 asmdef。沒有複製舊 DriftCore 覆蓋 R3a。Core 測試 132／0。|
|2 資料|`Assets/_Project/Editor/R3bSetup.cs:20`|9 JSON 已逐位元組複製到 Data/r3，Data/narration.json 同步新版；安裝器綁定 TextAsset，執行時不使用檔案系統。|
|3 故事／教學|`Assets/_Project/Scripts/R3Journey.cs:73`；`Assets/_Project/Scripts/R3Journey.cs:214`|故事卡圖文、點擊／空白／跳過；鏡頭指楮樹，走到互動半徑才進剝皮步驟；首棵完成回饋與三處介面提示。故事／教學各有 PlayerPrefs；? 可重看故事。互動流程約 30 秒，依玩家實際走到及剝皮才前進，不強制 30 秒跳過。|
|4 任務|`Assets/_Project/Scripts/R3Journey.cs:143`；`Assets/_Project/Scripts/HudUI.cs:44`|ObjectiveCore 同步五幕 18 步；上方常駐兩行動機／目標 n/N 與方向。橫版 750×88、直版 680×110、24–26 字級。排版仍須 CC 截圖確認。|
|5 指引|`Assets/_Project/Editor/R3bSetup.cs:82`；`Assets/_Project/Scripts/R3Journey.cs:204`；`Assets/_Project/Scripts/Systems/MinimapCam.cs:100`|舊 R1Marker 球隱藏，目標改 FindGlow 直立光柱；OffscreenArrow 在相機更新後畫圖、套 pixelRect／safeArea，素材朝右直接使用 AngleDegrees。小地圖目標貼邊。|
|6 停滯|`Assets/_Project/Scripts/R3Journey.cs:171`；`Assets/_Project/Scripts/Systems/CompanionFollow.cs:12`|30／60／90 秒提示；L2 光柱材質／燈光加亮；L3 阮肇短句 toast、NavMesh 帶路 8 秒、鏡頭指向 1.5 秒。閱讀、對話、教學、季節／結局暫停計時。|
|7 對話／流程|`Assets/_Project/Scripts/R3Journey.cs:109`；`Assets/_Project/Scripts/GameFlow.cs:212`；`Assets/_Project/Scripts/GameFlow.cs:232`|底部頭像、姓名、台詞；原文金色，白話對照小字，異文 label 可見；點擊／E／空白 Next。19 trigger 全接。第四幕二女→門口→屋內款待→群女→閱讀→遂停半年／SeasonSwap→鳥聲→求歸／送行；第五幕村口→詢問→七世孫→閱讀／結局。缺奏樂音效用 toast，不假稱有音檔。|
|7 場景站點|`Assets/_Project/Editor/R3bSetup.cs:45`；`Assets/_Project/Editor/R3bSetup.cs:46`|移除 Act4_Board 及 VillageBoard；門前保留 R3a 東側安全點，另將 House_0 的實心牆改成三面牆＋2.8m 東門洞及屋內站點；保留灰盒屋頂，加桌面、胡麻飯／山羊脯道具及標籤，重烘 NavMesh。此處新增碰撞，必須 CC 實走確認。|
|8 模型|`Assets/_Project/Editor/R3bSetup.cs:114`；`Assets/_Project/Editor/R3bSetup.cs:132`；`Assets/_Project/Editor/R3bSetup.cs:147`|CC Install 當下再查 staging/models。FBX＋albedo 齊才匯入，URP Lit，女子 1.55m／七世孫 1.65m、腳底與水平置中斷言、CharacterBob 呼吸、Y 軸面向玩家。缺件採膠囊＋portrait billboard；桃／楮 4／5m、葉／飯 .35m，杯維持既有。|
|9 閱讀|`Assets/_Project/Scripts/Systems/ReadingCore.cs:52`；`Assets/_Project/Scripts/ScrollUI.cs:173`；`Assets/_Project/Scripts/ScrollUI.cs:404`|每句原文可點展開白話；有 terms 的詞加底線並開九宮格詞卡。最長詞優先，避免「復／無復相識」巢狀 link。保留全段白話開／關與翻頁。新增純 Core 測試覆蓋句連結、詞連結、重疊詞、白話展開與非法段落。|
|10 題目|`Assets/_Project/Scripts/GameFlow.cs:73`；`Assets/_Project/Scripts/Systems/EpilogueDirector.cs:9`；`Assets/_Project/Tests/EditMode/R3CoreTests.cs:273`|questions_r3 id=7–10 合入對應幕卷軸，結局成績合併 10 道閱讀題。原有 id=6 留作全文資料，不走作答；結局只用 EpilogueCore 的推論題。既有 QuizStats.InferId 已是 100，本輪未修改題號。|
|11 結局|`Assets/_Project/Scripts/HudUI.cs:159`；`Assets/_Project/Scripts/HudUI.cs:173`|先 4 頁導讀，再全文／證據推論題／比較表／成績卡與重新開始。沿用 R3a DeleteAll 重開出口，不重複問舊 id=6。|
|12 介面|`Assets/_Project/Scripts/BootLoader.cs:30`；`Assets/_Project/Scripts/HudUI.cs:144`；`Assets/_Project/Scripts/ScrollUI.cs:646`|載入百分比、解壓縮／進入故事及重試按鈕中文化；Boot 安裝綁 NotoSerifTC 字型。手機技能已有剝皮／取水／呼喚，本輪保留。空互動提示背景隱藏；? 增 F/R/G、觸控操作及重看故事。|
|13 字型|`Assets/_Project/Editor/FontSubset.cs:118`；`Assets/_Project/Editor/R3Setup.cs:67`|既有字型收集器原本已遞迴掃 Data；本輪明示包含 r3，不增加第二套掃描器。新增遊戲文案在 Scripts／JSON；安裝末尾重建，實際字型產物留 CC 生成。|
|14 驗收|`Assets/_Project/Editor/R3Validation.cs:42`；`Assets/_Project/Editor/R3Validation.cs:260`；`Assets/_Project/Editor/R3Validation.cs:501`|18 個站點全以每幕起點出發及 CharacterController 步行／真正攀爬接近；只另有 y=-20 救援專項放置。每段對話 Next 完成、19 scene 結束計數；首次／重載引導、95 秒三級提示、箭頭、詞卡、導讀、15 張本輪截圖、手機 1080×1920 實際 PNG 尺寸與觸控介面。最後 console=0 才 release build。|
|救援／存檔|`Assets/_Project/Scripts/R3Journey.cs:157`；`Assets/_Project/Scripts/GameFlow.cs:63`|保留四面邊界牆；掉落改由 RescueCore 管理，取地形範圍 ±117 與 y<-10，兩秒乾燥著地更新安全點、切幕重設起點。水域用實際水面 Renderer.bounds（灰盒刻意沒有水面 Collider），不以 grounded 代替。舊第四幕存檔未完成半年者回二女步驟，已完成者解鎖求歸；不清除收集及既有成績。|

## 已執行的離線檢查

- **四組件編譯 exit 0，無診斷**：Unity 隨附 Roslyn 與本專案既有 Bee 參照，未啟動 Unity。涵蓋 Liuruan.Runtime、Assembly-CSharp、Assembly-CSharp-Editor、Liuruan.EditModeTests。證據：`logs/U-R3b-compile.md`；可重跑 `/tmp/liuruan-r3b-compile.py`，輸出在 `/tmp/liuruan-r3b-compile/`。
- **Core 132 passed、0 failed**：保留 R3a 91，S 線 R3 39，另加句／詞連結及合併 10 題成績兩測試。測試讀本輪 Assets 的 Core、fixtures 與 r3 資料；原始逐案輸出及指令：`logs/U-R3b-core-tests.md`。console NUnit 替身只在 /tmp，沒有編進 Assets。
- `python3 tools/check_r3.py --self-test`：9 JSON、107 處原文／異文、278 處短文、72 句逐字重組、40 詞卡、4 題與 12 個錯誤樣本攔截 PASS。
- `python3 Assets/_Project/Editor/R3OfflineChecks.py`：9 JSON／新版 narration、5 fixtures、10 PNG 與交付來源逐位元組相同；PNG 尺寸、字型掃描、路由與驗收靜態契約 PASS。證據：`logs/U-R3b-offline.md`。這是檔案及接線檢查，不是實跑證明。
- R3a 的 `DriftCore.cs`、`DriftPickup.cs`、`R1CoreTests.cs` 與本輪開始的 `/tmp/liuruan-r3b-before` 快照逐位元組相同。保留 3m 水平拾取、4m 暫停、下游擱淺及攀援不關閉等修正。
- 本輪 Scripts／Editor／Tests／Data 的 `git diff --check` 通過。全 Assets 的檢查另發現本輪開始前已存在的兩份人物 controller 尾端空白，未擅自修改。

## 檔案清單

新增：

- Scripts/R3Journey.cs、R3Npc.cs、R3Billboard.cs。
- Editor/R3bSetup.cs、R3OfflineChecks.py。
- Scripts/Systems/{StallHintCore,StallHintSystem,DialogueCore,DialogueDirector,ObjectiveCore,ObjectiveTracker,RescueCore,RescueSystem,OffscreenArrowCore,OffscreenArrow,ReadingCore,ReadingDirector,R3DataCore,R3Json}.cs。
- Tests/EditMode/R3CoreTests.cs；Tests/Fixtures/R3/{dialogue,glossary_cards,hints,objectives,sentences}.json。
- Data/r3/ 的全部 9 JSON；Art/R3/ 的 story_1–3、portrait_liuchen／ruanzhao／woman_a／woman_b／grandson、icon_arrow、card_frame，共 10 PNG。
- 新檔／新資料夾附 .meta，保留全部既有 GUID。上列檔案相對 Assets/_Project/。
- logs/U-R3b.md、U-R3b-compile.md、U-R3b-core-tests.md、U-R3b-offline.md、U-R3b-handoff.md。

修改：

- Editor/{R3Setup,R3Validation,R2bSetup,FontSubset}.cs；R2bSetup 僅補舊告示板缺席時的安全檢查，避免第二次 Install 空參考。
- Scripts/{GameFlow,HudUI,ScrollUI,FindGlow,BootLoader,TouchControls}.cs。
- Scripts/Systems/{CompanionFollow,MinimapCam,EpilogueDirector}.cs。
- Data/narration.json。

本輪不寫 Main.unity、Boot.unity 或 ProjectSettings 的最終場景狀態；安裝器由 CC 執行後才會生成場景／材質／字型變更及 U-R3b-assets.md。沒有推 GitHub、部署或派子代理。

## 沒做到／風險與下一步

1. **CC 實跑仍為必要驗收**：離線 C# 成功不等於 Unity 匯入／原生 API／場景接線成功。請先看 U-R3-tests.md，再看 18 點距離表、19 對話完成、首次／再次、95 秒與 15 張新圖，最後看 release build。本輪不覆寫或借用 R3a 的遊玩／截圖／build 成功作為 R3b 證據。
2. 截至本輪檔案檢查，`staging/models/npc/` 尚不存在，四件指定新 props FBX 也未到。**未宣稱新模型已安裝**。CC 觸發時會再檢查，有件即匯入；缺件採膠囊＋頭像／既有道具，寫入實際生成的 U-R3b-assets.md。
3. 新屋內門洞、二女導航、手機文字大小、詞卡觸控及新增光柱的觀感，需要看實際畫面。屋內採可走入的灰盒牆及原屋頂，沒有假稱已完成精緻室內美術；新模型正面朝向也須目視校正。
4. 手機截圖以 Editor GameView 內部反射 API 切固定尺寸，並驗證實際 Screen 及 PNG 像素；若 Unity 版本 API 不符會明確 FAIL，不能改成拉伸圖片冒充。這是 Editor 觸控版面驗收，仍不等於真機 WebGL 測試。Boot 字型及圖片增加建置體積，仍由既有 25MiB 門檻實際檢查。
5. 原始對話的逐句小字對照採 sentences 的已有白話；異文沒有對應白話時保留 label／原字，不自行補寫原文。奏樂與鳥聲缺音效的部分使用文字提示。
6. 新手教學只在完成後存已讀；中途關閉會再出現。明確「重新開始」沿用 R3a 清全部偏好，因此會再次呈現故事與教學。舊第四幕未播完者回二女步驟，避免告示板移除後卡關。
7. Obsidian Vault 不在本輪寫入範圍或沙盒可寫目錄，未另存，也沒有宣稱已儲存。根目錄 RTK.md 不存在。依長任務規則另附 U-R3b-handoff.md；沒有假稱可自行重設模型上下文。
