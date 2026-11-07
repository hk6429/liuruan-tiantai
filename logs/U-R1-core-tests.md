# 已匯入 Core 測試（主控台，非 Unity Test Runner）

使用 Assets 中的兩個 Core 與同一份 EditMode/CoreTests.cs；測試屬性替身與反射 runner 取自 staging，僅在暫存目錄調整為兩種測試類別，沒有匯入 Assets。

```text
/Volumes/hk6429/projects/liuruan-tiantai

歡迎使用 .NET 10.0!
---------------------
SDK 版本: 10.0.402
CSSM_ModuleLoad(): One or more parameters passed to a function were not valid.

----------------
安裝 ASP.NET 核心 HTTPS 開發憑證。
若要信任憑證，請執行 'dotnet dev-certs https --trust'
了解 HTTPS: https://aka.ms/dotnet-https

----------------
編寫您的第一個應用程式: https://aka.ms/dotnet-hello-world
了解新功能: https://aka.ms/dotnet-whats-new
探索文件: https://aka.ms/dotnet-docs
在 GitHub 上報告問題和尋找來源: https://github.com/dotnet/core
使用 'dotnet --help' 查看可用的命令或瀏覽: https://aka.ms/dotnet-cli
--------------------------------------------------------------------------------------
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
