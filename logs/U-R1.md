# U 線 R1 交件報告

**狀態：程式碼與 Editor 備援驗證入口已備妥；Unity 場景整合、Play、截圖及 WebGL 尚未完成，不能放行 R1。**

## 已落盤的變更

- 將 S 線 R0 HungerCore、HungerSystem、CompanionCore、CompanionFollow 及 Runtime asmdef 複製至 `Assets/_Project/Scripts/Systems/`；EditMode/CoreTests.cs 與測試 asmdef 複製至 `Assets/_Project/Tests/EditMode/`。沒有匯入 Console runner，沒有接入 S 線 R1 系統。複製時碰到 S 線同步新增的三支 R1 Core，已移出 Assets；備份保留於 `_backup_r1/`。
- PlayerController 每幀以基礎速度乘 HungerSystem.SpeedMultiplier，沒有累乘基礎欄位；ClueData 增加 act。
- GameFlow：五棵穀皮位元進度、攀援瞬移、摘桃補滿並回溪谷、三件漂流物及作答後才開涉水、女子→屋→告示板→求歸、返村灰冷材質與房屋切換、結局面板；PlayerPrefs 保存幕次、收集、村中步驟與位置；F9 限 Editor／Development build。
- HudUI：程式建立五格桃形飢餓值、穀皮計數、F/R/G 菱形技能格、觸控圓鈕、圓框頭像及呼喚冷卻比例、空白 RawImage 小地圖、互動提示、目標與求歸按鈕。沿用 ScrollUI 的搖桿、TouchButton、閱讀卡、分頁與問答；停用舊 HUD 以免重疊。圖示與頭像為純色占位。
- R1Setup.Install：在已開啟的 Editor 掛載 HungerSystem／CompanionFollow／NavMeshAgent，設定跟隨玩家、15 個互動點、崖頂落腳平台、灰冷材質、題庫、字型與 HUD；烘 NavMesh 後檢查非空並儲存 Main。**此入口尚未執行，Main.unity 目前仍是 R0。**
- R1Validation：真 Unity TestRunnerApi 執行 EditMode，通過後進 Play，逐一靠近並呼叫 15 個實際 Interactable、以 UI 按鈕翻頁與作答，按求歸，檢查五幕狀態、補桃、呼喚冷卻、結局與 console error；測試前備份並在正常或提前結束時還原 PlayerPrefs。此驗證程式尚未執行。
- R30Capture 的 r1 tag 輸出改至 Captures/r1；SceneBundles.BuildAll 依 development 設定帶入 BuildOptions.Development。
- R1Serve.py 提供僅綁 localhost 的 HTTP 伺服器，對 .unityweb／.br 回傳 Content-Encoding: br，WASM／JavaScript 正確 MIME。尚未啟動成功。
- 修改前已將既有腳本、Main 場景、字型與 ProjectSettings 備份至 `_backup_r1/`；未修改 staging、Data、來源專案或其他線的檔案，未推送或部署。

## 已執行的驗證與輸出

| 項目 | 實際結果 | 證據 |
|---|---|---|
| Unity 隨附 Roslyn、專案目前的 Bee 參照組件 | Runtime asmdef、Assembly-CSharp、Editor、EditModeTests 四個組件均 exit 0／0 error | logs/U-R1-compile.md |
| Assets 中的兩個 Core ＋已匯入 EditMode 測試，以暫存主控台 runner 執行 | **40 passed, 0 failed**；不是 Unity Test Runner | logs/U-R1-core-tests.md |
| HTTP 標頭離線單元測試 | 1 test／5 個檔名案例全過；包含 .wasm.unityweb、.js.unityweb、.data.unityweb、.wasm.br 與 index.html | `python3 Assets/_Project/Editor/R1ServeTests.py` |
| 本機 HTTP 綁定 | **失敗：PermissionError: [Errno 1] Operation not permitted** | 沙盒禁止 socket.bind，未宣稱瀏覽器可載入 |
| read_console(types:[error]) | 最近一次回傳 0 log entries | 只代表既有 Editor Console；新腳本尚未 refresh，不能當成新程式 Unity 編譯通過 |
| 題庫逐字來源及略過段落 | 七張 original 均為 sources/liuruan_tw.txt 的連續逐字片段；均無「各就一帳宿」 | 下方逐卡結果 |

未以 S 線後續新增的 69 項測試冒充本輪已匯入測試；本輪正式計數為 40。

## 工具阻礙與備援入口

MCP 的 execute_code(action:execute)、refresh_unity、run_tests 都回傳 `MCP tool call requires approval, but approval policy is never`；Unity 原生介面也回傳 `Computer Use was not approved to use Unity`。沒有另開第二個 Unity，沒有改走其他管道繞過這些拒絕。

已準備任務明確允許的 Editor 腳本備援。`Assets/_Project/Editor/R1Command.txt` 內排入一次性 `all-r1`；切回目前已開的本專案 Editor，執行 **Assets → Refresh** 後，R1Bridge 會清空該請求，呼叫 R1Validation.Begin。手動入口為 **Tools/Liuruan/R1/Validate All**。

順序為安裝／烘 NavMesh → 真 EditMode Test Runner → Play 五幕與 UI 作答 → 六機位截圖 → Development WebGL＋Scene Bundle。失敗會停止並寫 logs/U-R1-failure.md；成功各階段分別寫 install／tests／play／captures／build 紀錄。不要在相同請求仍執行時重按選單。

建置成功後，伺服器指令為：

```sh
python3 Assets/_Project/Editor/R1Serve.py
```

網址 `http://127.0.0.1:8765`。目前環境禁止本機綁定，仍需在允許綁定連接埠的終端機啟動，之後實際用瀏覽器確認 Boot、場景包及 HUD 載入。

## 截圖清單

**實際產出 0／6 張六機位截圖；沒有假截圖。** `Captures/r1/` 目前為空。執行後預期六機位：r1_a_start.png、r1_b_forest.png、r1_c_gorge.png、r1_d_peach.png、r1_e_village.png、r1_f_overview.png；Play 另外預備 r1_hud.png。

## data 大小

WebGL 的 Build/*.data.unityweb **不存在，無法量測或驗證 <26,214,400 bytes**。下列僅為 C 線內容 JSON，不是 WebGL data 包：

- `clues.json`：10101 bytes
- `compare.json`：2631 bytes
- `narration.json`：868 bytes

- id 0／act 1：原文為來源逐字連續片段=True
- id 1／act 2：原文為來源逐字連續片段=True
- id 2／act 3：原文為來源逐字連續片段=True
- id 3／act 3：原文為來源逐字連續片段=True
- id 4／act 4：原文為來源逐字連續片段=True
- id 5／act 5：原文為來源逐字連續片段=True
- id 6／act 5：原文為來源逐字連續片段=True

## 檔案清單

- `Assets/_Project/Scripts/GameFlow.cs`
- `Assets/_Project/Scripts/HudUI.cs`
- `Assets/_Project/Scripts/PlayerController.cs`
- `Assets/_Project/Scripts/ClueManager.cs`
- `Assets/_Project/Scripts/ScrollUI.cs`
- `Assets/_Project/Scripts/Systems/CompanionCore.cs`
- `Assets/_Project/Scripts/Systems/CompanionFollow.cs`
- `Assets/_Project/Scripts/Systems/HungerCore.cs`
- `Assets/_Project/Scripts/Systems/HungerSystem.cs`
- `Assets/_Project/Scripts/Systems/Liuruan.Runtime.asmdef`
- `Assets/_Project/Tests/EditMode/CoreTests.cs`
- `Assets/_Project/Tests/EditMode/Liuruan.EditModeTests.asmdef`
- `Assets/_Project/Editor/R1Bridge.cs`
- `Assets/_Project/Editor/R1Command.txt`
- `Assets/_Project/Editor/R1Serve.py`
- `Assets/_Project/Editor/R1ServeTests.py`
- `Assets/_Project/Editor/R1Setup.cs`
- `Assets/_Project/Editor/R1Validation.cs`
- `Assets/_Project/Editor/R30Capture.cs`
- `Assets/_Project/Editor/SceneBundles.cs`

新增資產及資料夾的 .meta 已產生；備份位於 `_backup_r1/`。報告與測試輸出為 logs/U-R1*.md。

## 未完成與風險／下一步

1. 必須先讓現有 Unity 匯入新腳本；目前沒有場景掛載、NavMesh 烘焙或引擎測試成功證據。
2. 必須完成真 EditMode 全綠、Play 每幕互動與 console 0 error，並檢視實際 HUD、導航與六機位截圖；手機版未驗證。
3. 必須取得本輪 .bundle-done、loader.js 與 WebGL data 大小；本機伺服器與瀏覽器實際載入仍待完成。
4. 場景整合與驗證入口雖經靜態編譯，但尚未在引擎跑過；若現場失敗，應依 failure 記錄修正，不得將本報告視為驗收成功。
5. 開工時 ProjectSettings.asset 已有 Unity 寫入的既存異動（含 projectName 保留前作值），本輪未直接更動或還原該既存異動。Install 將設定 runInBackground=true，已有備份。
6. Obsidian Vault 位於可寫範圍之外，未同步；本輪實體報告保留於本專案。
