# U 線 R1：灰盒可玩版（五幕走得完＋HUD）

先讀 AGENTS.md、docs/plan.md 第三、四節、logs/U-R0.md、staging/scripts/INTEGRATION.md。
Unity Editor 已開本專案，UnityMCP 在 http://127.0.0.1:8080/mcp（你是唯一可操作 Unity 的線）。`execute_code` 一律 action:"execute"、C#6、物件寫 UnityEngine.Object；改 C# 後 refresh_unity → read_console(types:["error"]) 必須 0 error。若 MCP 連不上，改用 Editor 腳本＋在交件報告寫清楚，**不要**另開第二個 Unity。

可寫：Assets/（**除了 Assets/_Project/Data/，那是 C 線的**）、ProjectSettings/、Captures/r1/、logs/U-R1*.md。不碰 staging/ 以外的別線檔案（staging/scripts 只讀）。改檔前備份到 _backup_r1/。

## 任務
1. 整合 S 線 R0：staging/scripts/Runtime → Assets/_Project/Scripts/Systems/；Tests/EditMode → Assets/_Project/Tests/EditMode/（加 asmdef）。Player 掛 HungerSystem，PlayerController 讀 SpeedMultiplier；Companion 掛 CompanionFollow。Main 烘 NavMesh（com.unity.ai.navigation）。用 Test Runner（MCP run_tests 或 execute_code）跑 EditMode 測試，全綠。
2. HUD（新 HudUI.cs，程式建立，照 plan 第四節與 staging/ref 版面）：左上 5 格桃形飢餓值＋「穀皮 ×N」；右上 3 個菱形技能格（剝皮 F／取水 R／呼喚 G，手機改三顆圓鈕，沿用 TouchControls）；左下阮肇頭像圓框＋冷卻環（讀 CompanionCore.Call 冷卻比例）；右下小地圖外框（先放空白 RawImage，R2 接 S 線 MinimapCam）；中下互動提示「[E] 撿起」等。圖示先用純色占位。
3. GameFlow.cs：五幕狀態機，依 R0 的 Empty 觸發：
   - Act1：Act1_Bark_1～5 互動剝皮（穀皮+1），5 棵全剝 → 解鎖 Act2。飢餓值起始 3 格。
   - Act2：Act2_ClimbBase 互動 → 暫時直接移到崖頂（R2 接 ClimbZone）；Act2_PeachTree 互動 → FillAll。
   - Act3：Act3_Drift_1～3 三件漂流物（暫用 Interactable）→ 收齊後開推論題 → Act3_Wade 涉水通過。
   - Act4：Act4_Women → Act4_House → Act4_Board（顯示原文段落卡）→「求歸」選項。
   - Act5：回到 Act5_Return，Village／村口物件換成灰冷版本（暫：換材質＋關掉一半房屋），觸發結局面板。
   - 每幕要顯示的原文／題目從 Assets/_Project/Data/clues.json 讀（C 線本輪會寫，欄位照 ClueManager 的 ClueData，另有 act 欄位 1–5）。若檔案還不存在，顯示占位鍵「〔act1-原文〕」，**不准自己寫古文或台詞**。
   - 進度存 PlayerPrefs；Debug 鍵 F9 跳下一幕（只在 Editor／Development build）。
4. 驗證：Play 模式用程式把五幕走一遍（可用 Debug 跳幕但每幕至少真的觸發一次互動），console 0 error；R30Capture 六機位截圖存 Captures/r1/。
5. WebGL Development build：用選單 Tools/Liuruan/Build/WebGL + Scene Bundle（MCP 可能逾時，等 Build/WebGL/.bundle-done 或 loader.js 換新），本機起伺服器（.unityweb 加 Content-Encoding: br）確認能載入。

交件報告 logs/U-R1.md：做了什麼、測試輸出、截圖清單、data 檔大小、未完成項。
