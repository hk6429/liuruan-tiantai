# U 線 R3a：修卡關（P0）＋錯字＋解開說明＋「真人可達」驗收

先讀 AGENTS.md（R3 新增白話對話規則）、docs/r3-diagnosis.md（四專家診斷，本輪依據）、你的 logs/U-R2b.md、Assets/_Project/Scripts/GameFlow.cs、HudUI.cs、ClueManager.cs、PlayerController.cs、Editor/R1Setup.cs、R2Setup.cs、R2bSetup.cs、R2Validation.cs、R1Bridge.cs、LiuruanGreybox.cs。
寫入範圍：Assets/、ProjectSettings/、logs/U-R3*.md。交件報告 logs/U-R3a.md。
執行環境：你不能開 Unity（沙盒碰不到 UnityMCP／batchmode）。只寫檔＋離線語法檢查（沿用 R2 的 /tmp 編譯檢查做法）。CC 會按 Cmd+R 編譯、寫 bridge 指令觸發驗收。
S/A/C 三線同時在跑（寫 staging/、docs/content/r3/），本輪 R3a **不要依賴它們**；R3b 才整合。

## 上一輪 CC 發現並已修的問題（不要改回去）
- 天空 TextureImporter 明設 textureShape=Texture2D；人物用獨立 albedo PNG 自建 URP Lit，不用 FBX 內嵌貼圖。
- 測試回呼裡不可直接進 Play，要延後 3 秒由 update 進。

## 要做的事
1. **第四幕屋子走不到（必卡）**：Act4 屋子互動點從 House_0 中心移到門前可站的位置（診斷建議 (-13.5,0,75)，請依實際門向與碰撞體計算，站點要在碰撞體外 ≥1m），互動半徑 3.5→5。所有互動點都檢查一次「中心在碰撞體內」的同類問題並修正。
2. **結局永久卡住**：
   - 結局頁加「重新開始」按鈕（中文），清掉所有 liuruan_* PlayerPrefs（含 liuruan_r1_progress、ended 狀態）後重載場景。
   - 線索冊「重新開始」同樣清全部鍵（ClueManager.cs:232 附近）。
   - 重整頁面時若已結束，顯示結局頁＋「重新開始」，而不是無出口。
3. **崖頂摔落**：攀爬完成後攀援點不要關閉（可重爬）；且在 Act2 已爬過但玩家高度 < 10m、桃還沒摘時，HUD 提示並可重爬。
4. **掉出地圖**：地圖四周加隱形邊界牆（地形範圍內縮 3m）；玩家 y < -10 時傳回本幕安全點並 toast「迷路了，回到山徑上」。
5. **漂流物太難撿**：撿取判定改水平距離 3m；玩家 4m 內時物件暫停漂流；漂到下游重生時不要瞬移到玩家看得見的位置以外太遠（或改成在溪邊淺灘擱淺等玩家）。
6. **攀援觸發框**放大到 (5,4,6)。
7. **正式版防卡**：HUD 加兩顆小鈕「回本幕起點」「跳過本幕」（跳過前確認一次，不用 JS confirm，用遊戲內面板）；F9 跳幕維持只在開發版。
8. **解開說明**：GameFlow.cs:52 `DeferHelp` 的流程修好——開場第一次進遊戲顯示操作說明（R3b 會換成故事卡＋教學，這輪先讓現有說明出現）；新增常駐「?」鈕（H 鍵也可開）打開操作說明；JourneyHud 不要藏操作提示列。
9. **錯字**（只改字，不動結構）：
   - 「銅瓦屋」→「筒瓦屋」（GameFlow.cs:46 等，全專案 grep）
   - 「百鳥啼鳴」→「百鳥鳴呼」（HudUI.cs:65、GameFlow.cs:91 等）
   - 「涉水」→ 依原文「度山出一大溪」改成「越過山，來到大溪邊」（R1Setup.cs:41 等）
   - 遠處按 E 的提示改成通用文字「附近沒有可互動的東西，靠近發光處再試」
   - 第一幕飢餓提示（HungerSystem.cs:43）改：第一幕「糧食快吃完了，先剝完樹皮」，第二幕以後維持找食物。
10. **R3Validation（最重要）**：新增 Editor/R3Validation.cs，在 R1Bridge 的 Tick 加 `command.StartsWith("r3")` → R3Validation.Begin()，round 字串回 "R3"。流程沿用 R2Validation（install→EditMode tests→延後 3 秒 startplay→play→capture→build），但 play 階段要**真人可達檢查**：
    - **禁止瞬移到互動點**。每一幕開始時可以把玩家放到該幕起點，之後只能用 CharacterController.Move（會撞牆）朝目標走（可用簡單路點），每幀步長 ≤ 玩家跑速×deltaTime；走到互動半徑內才呼叫互動。
    - 每個互動點記錄：最近可達距離、是否在半徑內、耗時。任一點走不到 → 寫進 logs/U-R3-failure.md 並標 FAIL。
    - 另外測：結局後按「重新開始」能回第一幕；玩家丟到 y=-20 會被傳回；漂流物在玩家靠近時會停。
    - 產出 logs/U-R3-play.md（每點距離表）、Captures/r3/ 每幕截圖（含 Act4 門口）、console error=0。
    - 最後 WebGL release build（同 R2：development=false、Brotli、.unityweb），寫 logs/U-R3-build.md，完成寫 Build/WebGL/.bundle-done。
11. 字型：所有改到的中文字串都在 Scripts/*.cs 常值內；R3Validation 的 install 階段呼叫 FontSubset.RebuildNoDialog()。

## 驗收
- 離線語法檢查通過；報告寫明 CC 要寫的 bridge 指令：`r3-r3a-001`（寫進 Assets/_Project/Editor/R1Command.txt）。
- 報告列：每個修點的檔案:行號、怎麼驗、沒做到什麼。不得謊報。
