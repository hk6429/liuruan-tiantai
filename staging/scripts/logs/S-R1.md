# S 線 R1 交件報告

日期：2026-11-07。寫入範圍僅 `staging/scripts/`；未開 Unity、未跑 batchmode、未修改 Assets／其他線、未推送。

## 變更與交件

- MinimapFogCore：固定世界範圍的 32×32 格網、圓形揭露半徑、世界座標轉格、最大邊界歸最後一格、範圍外不揭露；Act2–5 關霧，回 Act1 保留探索。
- MinimapCam：專用正交北向俯視相機跟隨玩家，建立／釋放 256² RenderTexture 與 32² Texture2D 遮罩；右下 OnGUI 疊加世界對齊迷霧、玩家朝向箭頭、阮肇點、目標點。不新增 UI 套件。
- ClimbCore：Grounded→Attaching→Climbing→TopOut／Release，正常 1.2m/s、飢餓值 <1 為 0.6m/s；斜走不加速、左右／上下限位。
- ClimbZone：trigger 內互動掛附、藤蔓平面鎖定、鍵盤／外部手機向量輸入、頂端自動移至平台落點、底端自動放開；暫停並還原一般移動及 CharacterController 原始狀態，停用時也還原。
- DriftCore／DriftCollectionCore：開放 Catmull–Rom 路徑、沿路徑下游循環、三維 2m 拾取、共用唯一 id 記錄、重複拾取拒絕、全收集只通知一次。
- DriftPickup／DriftCollection：獨立三個物件共享收集器、靠近／離開提示事件、拾取後隱藏整個物件，重新啟用仍不重生；以 C# event 通知流程，不引用 GameFlow。
- INTEGRATION.md：掛載、Inspector 欄位、事件名稱與參數、GameFlow 輸入輸出契約、既有 Interactable 轉接方式與 U 線驗收項目。
- 沿用 R0 WorldPosition、RequireFinite、Check 及同份 EditMode／console 測試模式；沒有新增套件、組件依賴或測試替身。

## 檔案清單

新增：

- `Runtime/MinimapFogCore.cs`
- `Runtime/MinimapCam.cs`
- `Runtime/ClimbCore.cs`
- `Runtime/ClimbZone.cs`
- `Runtime/DriftCore.cs`（含 DriftCollectionCore）
- `Runtime/DriftPickup.cs`
- `Runtime/DriftCollection.cs`
- `Tests/EditMode/R1CoreTests.cs`
- `logs/core-tests-R1.txt`
- `logs/S-R1.md`（本報告）

修改：

- `INTEGRATION.md`
- `Tests/run_core_tests.sh`
- `Tests/Console/Runner.cs`

以上皆相對於 `staging/scripts/`；R0 Core、包裝與原測試未改。

## 實際驗證

執行 `staging/scripts/Tests/run_core_tests.sh`，結果 **69 passed, 0 failed**：原 R0 40 案例＋本輪 29 案例。使用本機 .NET SDK 10.0.402、C# 9、warnings as errors、零 NuGet 套件。此 runner 執行與 EditMode 相同的測試方法，並非 Unity Test Runner。

新增測試涵蓋：

- 迷霧：非正方形世界座標、負世界起點、內部格線、精確最大邊界、四側範圍外、半徑 0／2 與圓形遮罩、角落裁切、重複揭露、Act2–5 取消／回 Act1 保留、非法輸入。
- 攀爬：完整到頂事件序列、到頂保持鎖定直到 Finish、到底自動 Release、底部靜止、<1／=1 飢餓邊界、斜走速度、側邊限位、附著座標夾值、重複釋放、非法輸入。
- 漂流：曲線起終點／中間 waypoint／彎道取樣、週期精確邊界與餘額、2m／2.001m 拾取、三維距離、拾取後停止且不再拾取／重生、重複 id、全收集只發一次、事件內重入重複拾取、路徑陣列防外部修改、非法輸入。

其他實際檢查：

- `sh -n staging/scripts/Tests/run_core_tests.sh`：通過。
- `git diff --check -- staging/scripts`：通過；另掃描新增 C#／整合文件／shell 無行尾空白。
- 五個 `*Core.cs` 均無 UnityEngine／UnityEditor 引用；全部 Runtime 無 GameFlow 引用。
- 兩個 asmdef 均可解析為 JSON，未修改 asmdef。
- 原始輸出含 macOS `CSSM_ModuleLoad` 訊息，未隱藏；編譯與測試仍成功。

### 實際完整測試輸出

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
PASS MinimapFogCoreTests.OutsideDoesNotRevealEdge(-16.001,0)
PASS MinimapFogCoreTests.OutsideDoesNotRevealEdge(16.001,0)
PASS MinimapFogCoreTests.OutsideDoesNotRevealEdge(0,-32.001)
PASS MinimapFogCoreTests.OutsideDoesNotRevealEdge(0,32.001)
PASS MinimapFogCoreTests.RadiusClipsAtCornerAndKeepsHistoryAcrossActs()
PASS MinimapFogCoreTests.RadiusIsCircularAndRepeatedRevealIsIdempotent()
PASS MinimapFogCoreTests.RejectsInvalidInputs()
PASS MinimapFogCoreTests.WorldCoordinates(-16,-32,0,0)
PASS MinimapFogCoreTests.WorldCoordinates(-15,-30,1,1)
PASS MinimapFogCoreTests.WorldCoordinates(0,0,16,16)
PASS MinimapFogCoreTests.WorldCoordinates(16,32,31,31)
PASS ClimbCoreTests.AttachClampsAndExplicitReleaseIsIdempotent()
PASS ClimbCoreTests.DescendingToBottomReleasesAutomatically()
PASS ClimbCoreTests.DiagonalSpeedAndSideBounds()
PASS ClimbCoreTests.FullTopOutSequenceAndEvents()
PASS ClimbCoreTests.HungerSpeedBoundary(0,0.6)
PASS ClimbCoreTests.HungerSpeedBoundary(0.999,0.6)
PASS ClimbCoreTests.HungerSpeedBoundary(1,1.2)
PASS ClimbCoreTests.HungerSpeedBoundary(5,1.2)
PASS ClimbCoreTests.RejectsInvalidInputs()
PASS ClimbCoreTests.StationaryAtBottomDoesNotRelease()
PASS DriftCoreTests.AllCollectedOnceEvenWithReentrantDuplicatePickup()
PASS DriftCoreTests.CollectedItemsStopAndCannotReappearOrNotifyAgain()
PASS DriftCoreTests.CurveSamplesEndpointsKnotsAndBend()
PASS DriftCoreTests.LoopsAtExactPeriodAndPreservesRemainder()
PASS DriftCoreTests.PickupRadiusBoundary(2,True)
PASS DriftCoreTests.PickupRadiusBoundary(2.001,False)
PASS DriftCoreTests.PickupUsesThreeDimensionsAndCopiesWaypoints()
PASS DriftCoreTests.RejectsInvalidInputs()
RESULT: 69 passed, 0 failed
```

## 未做到與已知風險

- 本輪遵守 S 線限制，**未開 Unity、未編譯 MonoBehaviour、未跑 Unity NUnit／場景／WebGL／手機**；Core 全綠不等於場景驗收完成。
- MinimapCam.Output 只有底圖，霧與圖示由 OnGUI 疊層；U 線若改成 RawImage 必須連疊層一起移植。實際灰盒世界範圍、渲染層、相機高度／裁切、UI 疊層與手機安全區尚待校準。
- 攀爬需接好既有 Interactable／輸入路由；包裝不自行消耗 E 或手機互動鍵，避免與現有 PlayerController 重複處理。Hunger 欄位必須指定，否則視為飽食。
- 攀爬需垂直藤蔓平面、已校準的玩家根高度與安全平台落點。鎖定期間停用 CharacterController，不做牆面／翻越碰撞掃描；TopOut 是位置過渡，未製作角色翻越動畫。若有額外 Rigidbody／Root Motion，U 線須透過鎖定事件暫停。
- 攀爬停用既有 PlayerController 也會停住其相機 LateUpdate；如需專用攀爬鏡頭由 U 線接獨立鏡頭腳本。
- 漂流採均勻參數曲線，彎道非等速且可能略超出 waypoint；預設每 30 秒走完整條路。未做弧長等速表、浮力或物理。
- 共用收集器屬本局狀態，不提供跨場景存檔。三件須共用同一收集器；重玩需重建收集器及物件。
- 根目錄 RTK.md 不存在；已讀 AGENTS.md、計畫書第三／四／六節、S-R0 報告與 INTEGRATION.md。
- Obsidian 位於本輪允許寫入範圍外，依「只能寫 staging/scripts/」未另存，未宣稱已儲存。

## 下一步（U 線）

依 INTEGRATION.md 匯入 Runtime 與 Tests/EditMode（不匯入 Tests/Console），跑 Unity EditMode。掛三組元件與共用收集器，接互動及 GameFlow 事件，校準世界範圍／藤蔓平面／平台落點／溪流路徑，再實測地圖迷霧與圖示、攀爬還原與頂底端、三物件拾取及推論題。
