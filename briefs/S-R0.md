# S 線 R0：HungerSystem＋CompanionFollow 原始碼與測試

工作目錄：本專案根目錄。先讀 AGENTS.md、docs/plan.md 第三、四、六節。只能寫 staging/scripts/。不准開 Unity。

## 任務
寫在 staging/scripts/Runtime/ 與 staging/scripts/Tests/EditMode/，namespace Liuruan。Unity 6000.3、C# 9 以內、不依賴第三方套件（可用 UnityEngine.AI）。

1. **HungerCore**（純 C#，無 MonoBehaviour，可單元測試）：
   - 5 格飢餓值（float 0–5），每 N 秒（預設 24）掉 0.5 格；可 Pause/Resume（面板開啟、演出時暫停）。
   - Eat(amount)、FillAll()（攀桃「飢止體充」用）；歸零時觸發 OnStarving 事件（**不死亡**，只讓移動速度降到 60% 並出 Toast 提示去找食物）。
   - 第一幕起始 3 格（「糧乏盡」）。
2. **HungerSystem**（MonoBehaviour）：包 HungerCore，提供 SpeedMultiplier 給 PlayerController 讀；事件給 HUD。
3. **CompanionCore**（純 C#）：給定玩家位置、阮肇位置，決定狀態 Follow / Idle / Catchup / Teleport：距離 <2.5m Idle、2.5–12m Follow、>12m Catchup（加速）、>30m 或卡住 3 秒 Teleport 到玩家身後 2m。呼喚技能 Call()：冷卻 8 秒，回傳是否成功與剩餘冷卻比例（給左下頭像冷卻環）。
4. **CompanionFollow**（MonoBehaviour）：用 NavMeshAgent 執行 CompanionCore 的決策；沒有 NavMesh 時退回直接 MoveTowards；暴露 Speed 給動畫。
5. EditMode 測試（NUnit）覆蓋 HungerCore 與 CompanionCore 的所有狀態轉換與邊界。另寫一支 staging/scripts/Tests/run_core_tests.sh：用 dotnet（若本機有）把兩個 Core 類別與測試編成 console 跑過；若沒有 dotnet，改寫一個不依賴 UnityEngine 的最小斷言 runner（用 mono／csc 或任何本機可用的 C# 編譯器），並在報告寫明實際用什麼跑的。Core 類別不得引用 UnityEngine（向量用自訂 struct 或 System.Numerics）。
6. staging/scripts/INTEGRATION.md：U 線之後要怎麼掛（放哪個資料夾、哪個 GameObject、PlayerController 要加哪一行讀 SpeedMultiplier）。

## 驗收條件
- run_core_tests.sh 實際跑過、全綠，把輸出貼進交件報告。
交件報告寫 logs/S-R0.md。
