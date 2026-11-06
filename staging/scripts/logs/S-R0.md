# S 線 R0 交件報告

日期：2026-11-07。範圍：僅 `staging/scripts/`；未開 Unity、未操作 batchmode、未修改別線、未推送。

## 變更

- HungerCore：預設 3/5 格、每 24 秒減 0.5、保留計時餘額的 Pause/Resume、Eat/FillAll、飢餓跨零事件與 0.6 移速倍率，無死亡機制。
- HungerSystem：MonoBehaviour 包裝、HUD C# 與 Inspector 事件、飢餓 Toast 訊息事件。
- CompanionCore：純 C# WorldPosition、距離四狀態、3 秒卡住判斷、玩家後方 2m 目標、8 秒呼喚冷卻與比例。呼喚成功後加速靠近，集合或瞬移後解除。
- CompanionFollow：NavMeshAgent 跟隨，無有效 NavMesh 時 MoveTowards 備援；暴露實際位移 Speed、Call、CooldownRatio、State。
- 共用 EditMode/console 測試、離線 console runner、兩個 asmdef、U 線掛載說明。

## 檔案

- `staging/scripts/INTEGRATION.md`
- `staging/scripts/Runtime/CompanionCore.cs`
- `staging/scripts/Runtime/CompanionFollow.cs`
- `staging/scripts/Runtime/HungerCore.cs`
- `staging/scripts/Runtime/HungerSystem.cs`
- `staging/scripts/Runtime/Liuruan.Runtime.asmdef`
- `staging/scripts/Tests/Console/Runner.cs`
- `staging/scripts/Tests/EditMode/CoreTests.cs`
- `staging/scripts/Tests/EditMode/Liuruan.EditModeTests.asmdef`
- `staging/scripts/Tests/run_core_tests.sh`
- `staging/scripts/logs/core-tests.txt`
- `staging/scripts/logs/S-R0.md`（本報告）

## 實際驗證

- 執行 `staging/scripts/Tests/run_core_tests.sh`，exit code 0。
- .NET SDK 10.0.402、C# 9.0、warnings as errors；零 NuGet 套件。使用最小 NUnit 屬性替身及反射 runner 執行同一份 EditMode 測試方法，並非 Unity Test Runner。
- 40 個測試案例全數通過，包含四狀態全部 16 種距離轉換組合、2.5/12/30m 邊界、3 秒卡住與進度重設、呼喚及 8 秒邊界、飢餓／暫停／補食／事件／輸入驗證。
- `sh -n` 檢查 shell 語法通過；asmdef JSON 解析通過；確認兩個 Core 沒有 UnityEngine/UnityEditor 引用。
- 原始輸出有 macOS `CSSM_ModuleLoad` 訊息；未隱藏，編譯與全部測試仍成功。

### 最終完整輸出

```text
/Volumes/hk6429/projects/liuruan-tiantai
Compiler: .NET SDK 10.0.402; C# 9.0; no NuGet packages; shared EditMode test methods
CSSM_ModuleLoad(): One or more parameters passed to a function were not valid.
PASS HungerCoreTests.ClampsInitialValue(-1,0)
PASS HungerCoreTests.ClampsInitialValue(0,0)
PASS HungerCoreTests.ClampsInitialValue(5,5)
PASS HungerCoreTests.ClampsInitialValue(6,5)
PASS HungerCoreTests.DropsAtExactIntervalAndPreservesRemainder()
PASS HungerCoreTests.EatClampFillAndChangeEvents()
PASS HungerCoreTests.FoodWorksWhilePausedAndDoesNotResetActiveClock()
PASS HungerCoreTests.LargeTickClampsAndRecoveryGetsFreshInterval()
PASS HungerCoreTests.PauseResumeIsIdempotentAndKeepsRemainder()
PASS HungerCoreTests.RejectsInvalidInputs()
PASS HungerCoreTests.StartsWithThreeAndNormalSpeed()
PASS HungerCoreTests.StarvesOncePerCrossingAndRecoversWithoutDying()
PASS HungerCoreTests.SupportsCustomInterval()
PASS HungerCoreTests.ZeroTickDoesNothing()
PASS CompanionCoreTests.AllSixteenDistanceDrivenTransitions(0)
PASS CompanionCoreTests.AllSixteenDistanceDrivenTransitions(1)
PASS CompanionCoreTests.AllSixteenDistanceDrivenTransitions(2)
PASS CompanionCoreTests.AllSixteenDistanceDrivenTransitions(3)
PASS CompanionCoreTests.CallAcceleratesUntilReunion()
PASS CompanionCoreTests.CallCooldownAndExactReadyBoundary()
PASS CompanionCoreTests.CatchupCanAlsoGetStuck()
PASS CompanionCoreTests.DistanceBoundaries(0,Idle)
PASS CompanionCoreTests.DistanceBoundaries(2.499,Idle)
PASS CompanionCoreTests.DistanceBoundaries(2.5,Follow)
PASS CompanionCoreTests.DistanceBoundaries(12,Follow)
PASS CompanionCoreTests.DistanceBoundaries(12.001,Catchup)
PASS CompanionCoreTests.DistanceBoundaries(30,Catchup)
PASS CompanionCoreTests.DistanceBoundaries(30.001,Teleport)
PASS CompanionCoreTests.ExactStuckBoundary()
PASS CompanionCoreTests.FiveCentimetresResetsStuckTimer()
PASS CompanionCoreTests.IdleNeverCountsAsStuck()
PASS CompanionCoreTests.MovesThroughEveryStateAndBack()
PASS CompanionCoreTests.ProgressAndIdleResetStuckTimer()
PASS CompanionCoreTests.RejectsInvalidTimeAndCoordinates()
PASS CompanionCoreTests.SlowProgressAccumulatesAgainstAnchor()
PASS CompanionCoreTests.StuckAtExactlyThreeSecondsTeleportsThenResets()
PASS CompanionCoreTests.TeleportClearsCallButPreservesCooldown()
PASS CompanionCoreTests.TeleportIsTwoMetresBehindNormalizedHorizontalForward()
PASS CompanionCoreTests.UsesThreeDimensionalDistance()
PASS CompanionCoreTests.ZeroForwardUsesWorldBack()
RESULT: 40 passed, 0 failed
```

## 未做到與風險

- 遵守 S 線限制，未在 Unity 6000.3 編譯或跑真正 NUnit EditMode；MonoBehaviour、HUD、PlayerController 接線與場景行為均待 U 線驗證。本報告不代表 Unity 驗收完成。
- 無 NavMesh 備援不避障或貼地；瞬移優先取目標附近 2m NavMesh 落點，多層地形安全性待 U 線現場驗證。
- 實際 PlayerController 尚未提供；INTEGRATION.md 給掛載位置與讀倍率的一行程式，但沒有修改或假稱核對過該控制器。
- 本機已有 dotnet，未使用或驗證 mono/csc 備援。console runner 需 .NET SDK 8 以上。
- `RTK.md` 在本專案根目錄不存在；已讀實際 AGENTS.md 與 docs/plan.md 第三、四、六節。
- 任務同時要求只能寫 staging/scripts/ 與報告放 logs/S-R0.md；已提出唯一例外確認，尚未收到答覆，因此本報告暫放 staging/scripts/logs/S-R0.md，未寫根目錄 logs/。
- Obsidian Vault 超出本輪允許寫入範圍，未儲存，也未宣稱已儲存。

## 下一步

U 線依 INTEGRATION.md 匯入指定兩個資料夾，跑 Unity EditMode，再驗證 HUD/Toast/移速、NavMesh/灰盒備援、卡住瞬移、動畫 Speed 與呼喚冷卻環。取得報告路徑例外授權後再搬到根目錄 logs/S-R0.md。
