# U 線 R3a 交件報告

日期：2026-11-08。里程碑：P0 防卡、操作說明、錯字與 R3 真人可達驗收程式已落盤，離線編譯完成。**本輪未開 Unity，Unity EditMode／Play 尚未執行；新截圖 0／6，WebGL 建置 0 次，未宣稱真人可達 PASS 或 console error=0。**

## CC 執行入口

1. 在目前開啟本專案的 Unity 按 Cmd+R，等編譯完成，先確認 Console 無編譯錯誤。
2. 將下列完整字串寫入 `Assets/_Project/Editor/R1Command.txt`：

```text
r3-r3a-001
```

本輪未寫入指令檔，避免舊 Bridge 在 Refresh 前誤觸發。入口：`R1Bridge.cs:20`、`:26`；round 為 R3，呼叫 `R3Validation.Begin()`。也可用既有 Editor 的選單 `Tools/Liuruan/R3/Validate All`。不開第二個 Editor，不用 batchmode。

流程：R3Setup.Install → EditMode tests → Test Runner 回呼僅排程 → update 等 3 秒才進 Play → 每幕起點＋CharacterController 實際碰撞步行 → 結局面板巡覽／重載／按鈕重開 → 檢查六張本輪 Play 截圖 → release WebGL＋Scene Bundle。失敗停止，寫 U-R3-failure.md，不放行建置。

## 變更、位置與驗證契約

以下路徑均以 `Assets/_Project/` 為基準。

|修點|檔案：行號|本輪修改／CC 驗法|
|---|---|---|
|第四幕屋子|Editor/R3Setup.cs:17|讀 House_0/Walls 實際 Collider.bounds，取朝廣場東側 `max.x + 1`、`center.z`，y=.2；灰盒牆 x=-21.5..-14.5，得 (-13.5,.2,75)，半徑 5。灰盒是實心方塊、無門洞，因此以東側作門前。安裝斷言離牆 ≥.999m；真人路線走到半徑內並拍門前照。|
|所有互動中心|Editor/R3Setup.cs:27|以腳底上 .9m 的站立中心對實體 Collider.ClosestPoint 檢查；排除地形、trigger、角色；埋入者移至最近水平邊界外 1m，最多八輪，無法解開就失敗。安裝輸出逐點前後座標；最終是否真人可達由 Play 決定。|
|結局重新開始|Scripts/HudUI.cs:95；Scripts/GameFlow.cs:278|結局頁三鈕：上一頁、下一頁、重新開始。共用 RestartJourney 清 PlayerPrefs 並重載本場景。重載 ended 存檔沿用 GameFlow.Start／HudUI.Start 顯示結局，現有出口可用。驗收先重載已結束場景，再實按 Restart，檢查回第一幕及 ended／存檔／測試鍵／成績清空。|
|線索冊重新開始|Scripts/ClueManager.cs:232；Scripts/ScrollUI.cs:635|兩個入口都改走 RestartJourney；線索冊不再依賴可能不存在的 ClueManager.I。|
|崖頂掉落／重爬|Scripts/GameFlow.cs:45、:176、:238；Scripts/Interactable.cs:43|Act2 攀援點一直開啟；已爬過且高度 <10 顯示重爬提示；地面不能隔空摘桃。一般互動加高度差 ≤3m，攀援／漂流物使用各自判定。驗收真正啟動 ClimbZone 攀至頂端、確認攀援點仍啟用；完整跌落後再爬仍待 CC 人工補驗。|
|掉出地圖|Editor/R3Setup.cs:49；Scripts/GameFlow.cs:133、:265|依 Terrain 範圍內縮 3m 建四個無 Renderer 的 BoxCollider，內側位於 ±117m（現有 240m 地形），高度 -10..70m。玩家 y<-10 釋放攀援鎖定、回本幕安全點、toast「迷路了，回到山徑上」，保存位置。驗收特別丟到 y=-20 後檢查自動返回。|
|漂流物|Scripts/Systems/DriftCore.cs:53、:56；Scripts/Systems/DriftPickup.cs:38；Editor/R2Setup.cs:77|核心拾取改水平距離 ≤3m，互動半徑同步 3；玩家水平 4m 內停止 Tick；到下游最後路點擱淺等待，不循環瞬移。Core 測 3／3.001m、忽略高度、終點不瞬移；Play 在 3.9m 內靜候 .5 秒檢查物件停止，再走近撿取。|
|攀援觸發框|Editor/R2Setup.cs:54|BoxCollider.size=(5,4,6)，維持 trigger／原攀援平面與頂端落點。|
|正式版救援鈕|Scripts/HudUI.cs:69；Scripts/GameFlow.cs:272、:287|「回本幕起點」「跳過本幕」；跳過前遊戲內確認／取消，ExternalUI 鎖一般操作，提醒未完成收集與答題不計分。第五幕跳過進入結局。F9 及 DebugNextAct 保留 UNITY_EDITOR／DEVELOPMENT_BUILD 條件。|
|操作說明|Scripts/GameFlow.cs:126；Scripts/ScrollUI.cs:181、:220、:618；Scripts/HudUI.cs:62、:67|保留延後旗標，但還原進度後明確呼叫 MaybeShowHelp；首次進入顯示現有說明。常駐 ? 與 H 可開啟；操作提示列不再因 JourneyHud 隱藏，JourneyHUD 另有底部移動／互動／說明提示。確認面板及結局鎖定時禁止重疊開說明。驗收確認首次 StartBtn 出現並按下。|
|錯字與遠距提示|Scripts/GameFlow.cs:47、:48、:93；Scripts/HudUI.cs:86；Editor/R1Setup.cs:41；Scripts/Strings.cs:9|指定字串改「筒瓦屋」「百鳥鳴呼」「越過山，來到大溪邊」及通用無互動提示。活躍 C# 搜尋指定舊字串為 0；未改來源原文及歷史報告。|
|第一幕飢餓提示|Scripts/GameFlow.cs:132；Scripts/Systems/HungerSystem.cs:39|GameFlow 依幕設定 HungerSystem.StarvingMessage；第一幕「糧食快吃完了，先剝完樹皮」，第二幕起沿用找食物。避免 Runtime 組件反向依賴 Assembly-CSharp 的 GameFlow。|
|字型|Editor/R3Setup.cs:64|Install 明確呼叫 FontSubset.RebuildNoDialog；本輪所有新介面中文字串均位於 Scripts/*.cs 常值，可被既有字集掃描收錄。實際字型重建須 CC 執行。|
|不依賴其他 R3 線|Editor/R2Setup.cs:13；Editor/R2bSetup.cs:15、:162|R3Setup 以 UseInstalledArt／try-finally 重用 Assets 已交付美術；不讀 staging 的本輪模型／新 Runtime／內容，缺 Assets 必要人物檔即失敗。保留 Texture2D、獨立 albedo PNG／URP Lit 設定。R2 既有入口仍維持預設素材來源。|

重新開始採 PlayerPrefs.DeleteAll：目前程式中的偏好鍵皆為 liuruan_；除旅程／題目外，也重設本遊戲朗讀、字級、畫質與說明已讀狀態。Unity 沒有跨平台列舉 PlayerPrefs 鍵的 API，故不用有限的題號清單冒充「全部」。不影響其他應用程式的 PlayerPrefs。

## R3Validation 檢查方式

`Editor/R3Validation.cs`（新檔，含 .meta）：

- `:55` Begin；`:85` 測試回呼；`:99` 延後 3 秒，絕不在測試回呼內進 Play。
- `:107`／`:125` 備份／還原目前程式已知的字串／整數偏好與 0..100 題紀錄，避免把驗收成績留給使用者。測試完成或失敗均還原。
- `:149` Tick：按 Time.frameCount 每實際遊戲幀最多移動一次。放置入口只有 **y=-20 專項測試**與 **每幕起點**兩處；沒有移到互動點的程式。
- `:307` Walk：地面使用現有 NavMesh 路點，崖頂沿平台走；每幀經 PlayerController.MoveDisplacement → CharacterController.Move，水平步長 ≤moveSpeed×SpeedMultiplier×deltaTime，另斷言不超過跑速×deltaTime。攀爬呼叫真正 ClimbZone、由其演出與鎖定流程登頂。
- `:303` 逐點距離表：15 個互動點記錄最近水平距離、半徑、半徑內、耗時與 PASS／FAIL。每點 90 秒、總體 900 秒上限；走不到在表與 failure 記 FAIL。呼叫互動前還須通過 Interactable.Nearest，對齊真人 E 鍵實際選中的物件；非漂流／攀援點同時檢查高度，不把地面隔空摘桃算通過；途中若觸發額外救回也 FAIL。
- `:333`／`:358`：完整結局面板逐頁讀回；重載已結束場景，確認重新開始按鈕可見；實按按鈕重開，檢查回第一幕、收集進度／ended／測試鍵／推論成績清除。
- `:246`：核對 `Captures/r3/r3_act1.png` 至 `r3_act5.png` 與 `r3_act4_door.png` 六張，皆須本輪時間戳且 >1024 bytes。取自 Play 路線，不採 R2 固定機位或舊圖。
- `:258`：沿用既有 WebGL＋Scene Bundle 選單，development=false；查 Brotli、.unityweb、非空 wasm／data、data <25MiB、本輪 .bundle-done；最後查全流程 console error／exception／assert=0 才 PASS。若本輪後續檢查失敗則移除本輪成功旗標，避免誤放行。
- `:404`：失敗即記錄並停止、還原測試前偏好；額外保留 error／exception／assert 的訊息與堆疊。

CC 執行後才會生成：U-R3-bridge.md、U-R3-install.md、U-R3-tests.md、U-R3-playstate.md、U-R3-play.md、U-R3-failure.md、U-R3-captures.md、U-R3-build.md、Captures/r3/ 及 Build/WebGL/.bundle-done。本輪沒有偽造這些執行結果。

## 已執行的離線驗證

|項目|結果與證據|
|---|---|
|Unity 隨附 Roslyn＋既有 Bee 參照|Runtime、Assembly-CSharp、Editor、EditModeTests 四組件 exit 0、無診斷；logs/U-R3-compile.md。執行 `/tmp/liuruan-r3-compile.py`，輸出 `/tmp/liuruan-r3-compile/`。沒有啟動 Unity。|
|Core console 測試|91 passed、0 failed；logs/U-R3-core-tests.md。重用零 NuGet console runner、換入本輪 Assets/Core／測試；不是 Unity Test Runner。|
|直接受影響的 DriftCore 契約|Tests/EditMode/R1CoreTests.cs:144、:151、:179：下游擱淺、不循環瞬移；3m 邊界；水平距離忽略高度；均包含在上述 91 項內。|
|離線搜尋／放置入口稽核|活躍 C# 三種指定舊字串 0；R3Validation 僅兩處 MovePlayer：掉落測試與 GameFlow.ActStart。|
|差異空白檢查|git diff --check -- Assets ProjectSettings 通過。|

## 檔案清單

新增：

- Assets/_Project/Editor/R3Setup.cs（含 .meta）
- Assets/_Project/Editor/R3Validation.cs（含 .meta）
- logs/U-R3-compile.md、logs/U-R3-core-tests.md、logs/U-R3a.md

修改：

- Assets/_Project/Editor/{R1Bridge,R1Setup,R2Setup,R2bSetup}.cs
- Assets/_Project/Scripts/{ClueManager,GameFlow,HudUI,Interactable,ScrollUI,Strings}.cs
- Assets/_Project/Scripts/Systems/{DriftCore,DriftPickup,HungerSystem}.cs
- Assets/_Project/Tests/EditMode/R1CoreTests.cs

本輪未修改 Main.unity、ProjectSettings、Data、來源原文、staging、其他線內容；場景／字型變更由 CC 觸發安裝後產生。工作區其他線並行變更保留。沒有推 GitHub、部署或啟動代理。

## 未完成／風險／下一步

- CC 必須實跑指令後查看距離表、六張新圖、Console 與 build 報告。離線編譯不能證明碰撞、NavMesh、UI 排版與 WebGL 行為；尤其門口「朝廣場東側」仍須目視確認。路線找路失敗會 FAIL，不會穿牆補過。
- 自動化保留重爬入口的斷言，但崖頂自然跌落再攀一次、邊界四面撞牆、正式版救援確認／取消、H 鍵與手機版面，仍需 CC 補人工驗收。R3a 沒有聲稱完成這些測試。
- 驗收程式備份涵蓋目前原始碼全部已知偏好；未知外掛若自行新增不在清單內的鍵，DeleteAll 測試也會清掉。請使用專案測試資料；日後新增偏好時同步更新驗收備份清單。
- 不整合 S/A/C R3 的故事卡、白話對話與素材，留 R3b；本輪沒有新增對話或改動原文敘事。
- Obsidian Vault 不在本輪寫入範圍或沙盒可寫目錄內，未另存，也未宣稱已儲存。RTK.md 在專案根目錄不存在；沒有假造其規則。
