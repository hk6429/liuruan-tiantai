# U 線 R3b：新手引導、對話、原文結合整合

先讀 AGENTS.md、docs/r3-diagnosis.md、你的 logs/U-R3a.md（以及 CC 實跑後補的 logs/U-R3-*.md）、staging/scripts/INTEGRATION.md 與 staging/scripts/logs/S-R3.md、docs/content/r3/C-R3.md 與該資料夾所有 json、staging/art/R3/A-R3.md。
寫入範圍：Assets/、ProjectSettings/、logs/U-R3*.md。交件報告 logs/U-R3b.md。執行限制同 R3a（只寫檔＋離線檢查；CC 編譯、觸發）。

## R3a 實跑後 CC 發現並已修的問題（不要改回去）
- R3a 實跑 r3-r3a-001 全 PASS（logs/U-R3-play.md 15 點真人可達、掉落救回、重開、release build）。R3a 的修正全部保留。
- 第四幕告示板（Act4_Board）本輪要移除，R3Validation 的可達表同步拿掉它、加上新的二女／門口／屋內／七世孫點。

## 要做的事
1. **S 線 R3 系統**照 INTEGRATION.md 複製進 Assets（含測試）：StallHint、Dialogue、Objective、Rescue、OffscreenArrow、Reading。R3a 已做的邊界牆／掉落傳回若與 RescueCore 重疊，改用 RescueCore，行為不變。
2. **資料**：docs/content/r3/*.json 複製到 Assets/_Project/Data/r3/；narration.json 換成新版。用 TextAsset 或 Resources 讀，WebGL 可用。
3. **開場**：第一次進遊戲 → 故事卡（story_card.json＋staging/art/R3/story_*.png，點擊或空白鍵下一張，可跳過）→ 30 秒互動教學：
   - 鏡頭帶到第一棵楮樹光柱 → 「用 WASD／搖桿走過去」（走到才下一步）→ 「按 E／互動鈕 剝皮」→ 剝完「很好！還有 4 棵」→ 箭頭依序點出飽足度、任務欄、「?」鈕。
   - 教學只出現一次（PlayerPrefs），「?」說明面板裡可「重看故事」。
4. **任務欄**：常駐上方，ObjectiveCore 兩行（目標 n/N＋方向／為什麼）。字要夠大，手機可讀。
5. **方向指引**：
   - 目前目標掛 FindGlow 光柱（取代褐色球）；
   - 目標在畫面外時畫面邊緣箭頭（icon_arrow.png，OffscreenArrowCore）；
   - 小地圖目標點超出範圍時貼邊顯示（MinimapCam.cs:100）。
6. **停滯提示**：StallHintCore 接 hints.json；L1 toast、L2 toast＋光柱加亮、L3 阮肇台詞＋阮肇走向目標＋鏡頭短暫轉向（1.5 秒）。
7. **對話框**：畫面下方，左側頭像（staging/art/R3/portrait_*.png）、名字、台詞；type=original 用另一個字色並在下方小字顯示白話（若有）；點擊／E／空白鍵下一句。dialogue.json 的 trigger 全部接上：
   - `act_start:N` 每幕開頭（在 narration 之後）
   - 第三幕撿到漂流物
   - 第四幕：**移除告示板**（Act4_Board 及其流程）。改為 見二女（對話）→ 跟二女走到筒瓦屋門口 → 屋內款待（桌上出現胡麻飯、山羊脯的簡單道具或圖示＋對話）→ 群女賀（對話）→ 遂停半年（SeasonSwap）→ 百鳥鳴呼（對話）→ 「求歸」→ 奏樂送行（對話＋音效若無則 toast）
   - 第五幕：回村口 → 七世孫 NPC（Act5_Grandson，放在歸鄉村路口可走到的位置）對話 → 結局。
8. **NPC 模型**：CC 正在用 Dzine 做 `staging/models/npc/{woman_a,woman_b,grandson}.fbx`＋`_albedo.png`（靜態模型，無骨架）。R3Setup：檔案在就複製進 Assets/_Project/Art/NPC/，URP Lit＋albedo、身高女子 1.55m／七世孫 1.65m、腳底貼地、掛 CharacterBob（呼吸）、面向玩家（Y 軸 LookAt）；不在就用膠囊＋頭像 billboard 並寫進報告。二女取代第四幕那顆球。
   同理 `staging/models/props/peach_tree.fbx`、`chu_tree.fbx`、`drift_leaf.fbx`、`drift_rice.fbx` 若存在就換上（R2 規格：桃樹 4m、楮樹 5m、漂流物依現有大小；蕪菁葉、胡麻飯各用自己的模型，杯子維持）。
9. **原文閱讀**：卷軸改成逐句顯示（ReadingCore）：每句原文，點一下展開白話；有 terms 的詞加底線，點了跳詞卡（card_frame.png 底）。保留「全段白話」切換。
10. **題目**：questions_r3.json 4 題接到對應幕的卷軸題；結局重複的推論題照 C 線建議刪一題。
11. **結局**：全文之前先播 epilogue_guide.json 導讀頁，再全文、作答、成績卡、「重新開始」。
12. **UI 收尾**：
    - 載入畫面中文化（BootLoader.cs:117「載入中 xx%」）；
    - 手機技能鈕改中文（剝皮／取水／呼喚）；
    - 中下方空黑框沒內容時隱藏；
    - 「?」說明面板加 F/R/G 說明與手機版說明。
13. **字型**：新增的中文全在 Scripts/*.cs 常值或 Data/r3/*.json；FontSubset 要把 Data/r3/*.json 也掃進去（改 FontSubset.cs），install 階段 RebuildNoDialog()。
14. **R3Validation 擴充**（沿用 R3a 的真人可達規則，禁止瞬移到互動點）：
    - 走完五幕所有新觸發點（二女、門口、屋內、七世孫），每段對話都 Next 到結束；
    - 檢查故事卡／教學在首次出現、第二次不出現；
    - 停滯模擬 95 秒（不移動）→ L1/L2/L3 都觸發；
    - 檢查畫面外箭頭出現；
    - Captures/r3/ 加拍：故事卡、教學、任務欄、對話框（二女、七世孫）、詞卡、結局導讀、手機版面（1080×1920 解析度一張）；
    - console error=0；最後 WebGL release build，同 R3a。

## 驗收
離線語法檢查＋Core 測試全綠；報告寫明 bridge 指令 `r3-r3b-001`；逐項列檔案:行號、怎麼驗、沒做到什麼。不得謊報。
