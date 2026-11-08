# S 線 R3：提示與對話核心系統

先讀 AGENTS.md（R3 白話對話規則）、docs/r3-diagnosis.md、staging/scripts/INTEGRATION.md、你上一輪的 staging/scripts/logs/S-R2.md。也讀 Assets/_Project/Scripts/GameFlow.cs、HudUI.cs、ClueManager.cs（只讀，不准改）。只能寫 staging/scripts/。不開 Unity。延續純 C# Core＋MonoBehaviour 包裝＋NUnit 測試（console runner 可離線跑）。交件報告 staging/scripts/logs/S-R3.md。

C 線同時在寫 docs/content/r3/*.json（格式見下），你的 loader 要照這些格式讀；C 還沒交件時先用你自己寫的測試樣本。

## 要做的事
1. **StallHintCore**（停滯分級提示）：
   - 輸入：每幀 deltaTime、是否在讀卷軸／對話（暫停計時）、進度事件（歸零）。
   - 門檻預設 30/60/90 秒，觸發 Level1（文字）、Level2（操作提示＋目標光柱加亮）、Level3（同伴帶路＋鏡頭轉向）各一次；進度事件後重置。
   - 提示文字由 hints.json 依幕與裝置（desktop/touch）取。
   - 測試：暫停時不計、各級只發一次、進度歸零、裝置切換取對文字。
2. **DialogueCore**（對話序列）：
   - 讀 dialogue.json：`{ "acts":[{"act":4,"scenes":[{"id":"meet_women","trigger":"interact:Act4_Women","lines":[{"speaker":"女子","text":"…","type":"original"}, …]}]}]}`。
   - API：Start(sceneId)、Next()、IsActive、CurrentLine、OnSceneEnd(sceneId)。每個 scene 預設只自動播一次，可重播。
   - 測試：順序、結束事件、未知 id 不爆、type 欄位保留。
3. **ObjectiveCore**（任務欄文字）：讀 objectives.json `{ "acts":[{"act":1,"steps":[{"id":"bark","goal":"剝楮樹皮","why":"糧食快吃完了","where":"沿小徑往北，找有光柱的樹","count":5}]}]}`，輸出「目標 n/N」＋「方向」兩行；測試計數與步驟切換。
4. **RescueCore**：記錄最後安全點（著地且非水中、每 2 秒更新），判斷是否要傳回（y < 門檻、離開邊界矩形）；測試。
5. **OffscreenArrowCore**：給相機 VP 矩陣與目標世界座標，回傳「是否在畫面內」與畫面邊緣箭頭的位置與角度（含目標在相機後方時翻轉）；測試四邊與背後情況。
6. **ReadingCore**（逐句原文＋白話＋注釋）：讀 sentences.json `{ "paragraphs":[{"p":1,"sentences":[{"orig":"…","plain":"…","terms":["穀皮"]}]}]}` 與 glossary_cards.json `{ "terms":[{"term":"穀皮","reading":"","meaning":"…"}]}`；API：取段落、切換顯示模式（原文／原文＋白話）、查詞卡；測試詞卡都查得到、每句 terms 都在 glossary 裡（缺的列出）。
7. 每個系統一個薄 MonoBehaviour 包裝（只發事件、提供資料，不建 UI）。更新 INTEGRATION.md 寫清楚掛法、事件、資料檔放 Assets/_Project/Data/r3/ 的路徑。run_core_tests.sh 加新測試，實跑全綠，輸出貼報告。
