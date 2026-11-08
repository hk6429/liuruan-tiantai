# S 線 R3 交件報告

日期：2026-11-08。里程碑：六個純 C# Core、六個薄 MonoBehaviour 包裝、測試樣本、C 線實際資料契約測試與整合文件完成。

## 範圍與讀取來源

本次只寫 `staging/scripts/`，未開 Unity／UnityMCP／batchmode，未修改 Assets、C 線內容或其他線檔案，未推送或部署，未派子代理。

已讀 AGENTS.md（含 R3 白話規則）、docs/r3-diagnosis.md、staging/scripts/INTEGRATION.md、staging/scripts/logs/S-R2.md，以及唯讀的 Assets/_Project/Scripts/{GameFlow,HudUI,ClueManager}.cs。另讀現有 Core／包裝／console runner／測試、briefs/C-R3.md、sources/liuruan_tw.txt 與 C 線五份新 JSON、C-R3.md。根目錄沒有 RTK.md，未假稱已讀。

本輪沿用 Ponytail 最小完整變更：保留既有 DTO＋JsonUtility 邊界與 NUnit 測試格式，無新增套件、UI、導航、鏡頭控制、物理或存檔系統。

## 完成內容與介面

| 系統 | 純 Core | 薄包裝 | 本次提供 |
|---|---|---|---|
| 分級提示 | StallHintCore | StallHintSystem | 30/60/90 秒、暫停保留計時、進度歸零、依幕／步驟／裝置取文；Level2 光柱訊號、Level3 帶路及鏡頭訊號 |
| 對話 | DialogueCore | DialogueDirector | Start/Next/IsActive/CurrentLine/OnSceneEnd；每局預設一次、明確重播、依 trigger 啟動；保留 type 與選填 source/label/orig |
| 任務 | ObjectiveCore | ObjectiveTracker | 動機＋目標 n/N 與方向兩行、計數夾上限、切步驟／幕與讀檔指定數量、更新事件 |
| 救援 | RescueCore | RescueSystem | 連續著地且非水中滿 2 秒記錄安全點，y 及 X/Z 矩形檢查，回傳安全點事件，不自行移動玩家 |
| 畫面外箭頭 | OffscreenArrowCore | OffscreenArrow | row-major VP 投影、含深度的畫面內判斷、四邊位置與角度、W<=0 背後翻轉、W=0／正後方有限值備援 |
| 逐句閱讀 | ReadingCore | ReadingDirector | 依段取句、原文／原文＋白話模式、詞卡查詢、缺詞去重列出、模式事件 |

共用 R3DataCore 做時間／必要欄位／id／幕次檢查；R3Json 只在 Unity 包裝使用 TextAsset／JsonUtility。四個資料元件有 Load 成功／失敗結果、OnError，載入錯誤時 Core=null；Reading 的缺詞是可讀且明確列出的警告。

提示事件先更新 Level 再通知；通知中發生 Progress／換步驟會停止舊輪剩餘提示。對話最後一句結束前先清除當前場景，結束事件可安全開啟下一段。未知 scene／step 不清除有效狀態。

任務 n/N 是目前步驟的項目數量。Rescue 的邊界本身及 y 恰等門檻不傳送；空中、水中、超界不儲存。箭頭使用 CPU 相機投影，角度右為 0°、上為 90°，按 viewport 比例修正；正後方預設向下。詳細契約、掛法、事件、資料路徑在 INTEGRATION.md 的 R3 區段。

## C 線實際資料接通

先建立五份獨立測試樣本；C 線交件後已讀取 `docs/content/r3/{hints,dialogue,objectives,sentences,glossary_cards}.json` 實跑三項契約測試：

- 五幕 18 步任務均能選取，三級雙裝置提示都取得非空文字，計數與 JSON count 一致。
- 所有對話 scene 均按順序讀完、各結束一次、預設不能再自動播；type 與來源／異文 label／orig 保留。
- 七段逐句原文可逐字重組現行底本；40 張詞卡皆可查，所有句子的 terms 均存在，MissingTerms 為空；「要」讀ㄧㄠ。

Core 不負責判定內容引文是否符合底本；原文與異文的內容審核由 C 線負責。此處的實際資料測試驗證載入、欄位保留及七段原文／詞卡關係，不宣稱已完成介面標註。

## 檔案清單

新增 Runtime：

- Runtime/StallHintCore.cs、Runtime/StallHintSystem.cs
- Runtime/DialogueCore.cs、Runtime/DialogueDirector.cs
- Runtime/ObjectiveCore.cs、Runtime/ObjectiveTracker.cs
- Runtime/RescueCore.cs、Runtime/RescueSystem.cs
- Runtime/OffscreenArrowCore.cs、Runtime/OffscreenArrow.cs
- Runtime/ReadingCore.cs、Runtime/ReadingDirector.cs
- Runtime/R3DataCore.cs、Runtime/R3Json.cs

新增驗證與報告：

- Tests/EditMode/R3CoreTests.cs（六組 Core 測試及 R3ContentCoreTests）
- Tests/Fixtures/R3/hints.json
- Tests/Fixtures/R3/dialogue.json
- Tests/Fixtures/R3/objectives.json
- Tests/Fixtures/R3/sentences.json
- Tests/Fixtures/R3/glossary_cards.json
- logs/core-tests-R3.txt
- logs/S-R3.md

修改：INTEGRATION.md、Tests/Console/Runner.cs、Tests/run_core_tests.sh。既有 Runtime、測試檔及兩份 asmdef 未修改。

## 實際驗證

指令：`staging/scripts/Tests/run_core_tests.sh`

結果：**130 passed, 0 failed，結束碼 0**。既有 91＋新固定樣本 36＋實際 C 線資料 3。使用 .NET SDK 10.0.402、C# 9、warnings as errors、零 NuGet；console 執行的是同一份 NUnit Test/TestCase 方法，並非 Unity NUnit runner。第一輪固定樣本 127 全綠後，補入 C 線實際資料測試與來源欄位，再跑下列最終 130 案例。

runner 會在 C 線五份 JSON 全數存在時加入內容測試；若尚未交齊，明確印 SKIP，不算成已通過案例。固定 fixture 測試仍會執行。console 專用 NUnit 屬性替身不可匯入 Unity。

其他檢查：

- `sh -n staging/scripts/Tests/run_core_tests.sh` 通過。
- `git diff --check -- staging/scripts` 通過。
- staging 的 C#／shell／Markdown／JSON 行尾空白檢查通過（包含新增未追蹤檔案）。
- 所有 *Core.cs 無 UnityEngine／UnityEditor 引用，通過。
- 兩份 asmdef JSON 可解析，未改動。

原始輸出含本機 `CSSM_ModuleLoad` 訊息，未隱藏；dotnet 仍正常編譯並以 0 結束，所有測試通過。

### 最終完整原始測試輸出

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
PASS SeasonCoreTests.EarlyReturnCancelsSeasonWithoutLateBirds()
PASS SeasonCoreTests.ExactBoundaries(0,Spring,Summer)
PASS SeasonCoreTests.ExactBoundaries(0.25,Summer,Autumn)
PASS SeasonCoreTests.ExactBoundaries(0.5,Autumn,Winter)
PASS SeasonCoreTests.ExactBoundaries(0.75,Winter,Spring)
PASS SeasonCoreTests.ExactBoundaries(1,Spring,Summer)
PASS SeasonCoreTests.ExactDurationAndRepeatedHomeCallsAreIdempotent()
PASS SeasonCoreTests.RejectsInvalidInputs()
PASS SeasonCoreTests.SixSecondsEndsInSpringAndBirdsOnlyOnceEvenWithReentry()
PASS SeasonCoreTests.WeightsAndBlendedValuesAreContinuousAcrossEveryBoundary()
PASS EpilogueCoreTests.EndingParagraphsAreFullSourceVerbatimAndRetainOmission()
PASS EpilogueCoreTests.RealJsonRowsMatchEveryFieldAndModelCopiesData()
PASS EpilogueCoreTests.RejectsDuplicateUnknownAndEndingScoreIds()
PASS EpilogueCoreTests.RejectsInvalidScoreInputs()
PASS EpilogueCoreTests.RejectsMissingAmbiguousOrBrokenData()
PASS EpilogueCoreTests.ScoresUseAllNonEndingCardsAndFirstTryOnly()
PASS EpilogueCoreTests.ThreeEvidenceOptionsAndAnswerIndexMatchSourceAndActs()
PASS CharacterBobCoreTests.FiniteForHugeInputsAndLongRunningPhase()
PASS CharacterBobCoreTests.FrequencyIsMonotonicAndCapsAtMaxSpeed()
PASS CharacterBobCoreTests.IdleHasNoBounceSwayOrLeanAndBreathStaysWithinOnePointFivePercent()
PASS CharacterBobCoreTests.MovementUsesConfigAndZeroTimeDoesNotAdvancePose()
PASS CharacterBobCoreTests.RejectsInvalidSpeedTimeAndSettings()
PASS StallHintCoreTests.DeviceSwitchSelectsCurrentTextAndNextEvent()
PASS StallHintCoreTests.ExactThresholdsEmitOnceWithActionSignals()
PASS StallHintCoreTests.LargeTickEmitsAllLevelsInOrder()
PASS StallHintCoreTests.PausedReadingAndDialoguePreserveRemainder()
PASS StallHintCoreTests.ProgressAndNewStepResetButSameStepDoesNot()
PASS StallHintCoreTests.ProgressInsideEventStopsOldTick()
PASS StallHintCoreTests.RejectsBadTimeThresholdsAndIncompleteData()
PASS DialogueCoreTests.EndHandlerCanStartNextSceneWithoutLosingIt()
PASS DialogueCoreTests.OrderedLinesTypesAndSingleEndEvent()
PASS DialogueCoreTests.RejectsDuplicateIdsInvalidTypesAndLongPlainLines()
PASS DialogueCoreTests.UnknownBusyOnceAndExplicitReplay()
PASS ObjectiveCoreTests.RejectsMissingDataAndNegativeCounts()
PASS ObjectiveCoreTests.StepAndActSwitchResetCountAndUnknownKeepsState()
PASS ObjectiveCoreTests.TwoLinesIncludeMotivationCountAndDirection()
PASS RescueCoreTests.AirAndWaterResetTimerAndLargeTickSamplesOnce()
PASS RescueCoreTests.FallAndRectangleEdges(-10,0,0,False)
PASS RescueCoreTests.FallAndRectangleEdges(10,0,0,False)
PASS RescueCoreTests.FallAndRectangleEdges(0,-10,20,False)
PASS RescueCoreTests.FallAndRectangleEdges(0,-10.01,0,True)
PASS RescueCoreTests.FallAndRectangleEdges(-10.01,0,0,True)
PASS RescueCoreTests.FallAndRectangleEdges(10.01,0,0,True)
PASS RescueCoreTests.FallAndRectangleEdges(0,0,-20.01,True)
PASS RescueCoreTests.FallAndRectangleEdges(0,0,20.01,True)
PASS RescueCoreTests.RejectsUnsafeInitialPointAndInvalidSettings()
PASS RescueCoreTests.SavesOnlyAfterTwoContinuousGroundedDrySeconds()
PASS OffscreenArrowCoreTests.BehindFlipsProjectedDirectionAndAxisHasFiniteFallback()
PASS OffscreenArrowCoreTests.DiagonalAspectAndTranslatedMatrix()
PASS OffscreenArrowCoreTests.FourEdges(2,0,0.95,0.5,0)
PASS OffscreenArrowCoreTests.FourEdges(-2,0,0.05,0.5,180)
PASS OffscreenArrowCoreTests.FourEdges(0,2,0.5,0.95,90)
PASS OffscreenArrowCoreTests.FourEdges(0,-2,0.5,0.05,-90)
PASS OffscreenArrowCoreTests.InsideBoundaryAndDepthClipping()
PASS OffscreenArrowCoreTests.RejectsInvalidMatrixInsetAndAspect()
PASS ReadingCoreTests.MissingTermsAreListedUniquelyAndReadingRemainsAvailable()
PASS ReadingCoreTests.ParagraphModeAndTermCardsRoundTrip()
PASS ReadingCoreTests.RejectsDuplicateOrMissingDataAndInvalidMode()
PASS R3ContentCoreTests.ActualContentObjectivesAndHintsMatchEveryStepAndDevice()
PASS R3ContentCoreTests.ActualDialoguePlaysAllScenesAndPreservesVariantAndSourceMetadata()
PASS R3ContentCoreTests.ActualReadingHasEveryTermAndReconstructsSevenSourceParagraphs()
RESULT: 130 passed, 0 failed
```

## 未做、風險與下一步

- 沒有啟動 Unity，MonoBehaviour 尚未以 Unity 編譯或掛場景；沒有桌機／手機實玩驗收。Core 全綠只證明離線行為，不代表新手引導已上線可用。
- 這六個包裝僅提供資料及事件。光柱加亮、同伴帶路、鏡頭轉向、救援移動、任務／對話／詞卡 UI，全部須由 U 線消費事件接線；不以訊號存在冒充玩家看到效果。
- 現行 GameFlow 的 Awake 比這批包裝早，U 線應在 Start 後操作 Core；HudUI 原任務區需容納兩行。第四幕仍需移除舊 Board 分支並對應七步，第三幕舊 Drift_1..3 要映射 C 線 trigger；本輪沒有修改 GameFlow、HudUI、ClueManager 或結局題路由。
- 救援點由 U 線選擇可容納膠囊的落點；Core 不做碰撞掃描，水中狀態必須外餵；若仍在矩形內但被牆卡住，本版不自動處理。每幕要重設安全起點，避免送回上一幕。
- 箭頭的後方分類針對透視相機，正交相機 W 固定 1；手機安全區、Canvas 座標轉換與相機更新順序待 U 線驗證。
- 對話自動播放記錄是單局記憶體，不跨瀏覽器重整保存。U 線須依存檔控制何時觸發開幕。重播、退出對話與輸入恢復須在真實流程驗證。
- 本輪只允許寫 staging/scripts/，且 Obsidian Vault 不在沙箱可寫範圍，因此沒有另存 Vault；本報告即為授權範圍內的交件紀錄。

下一步：U 線依 INTEGRATION.md 匯入 Runtime、複製五份 C 線資料至 Assets/_Project/Data/r3/、接上述事件，先做 Unity 編譯與 EditMode，再依驗收清單測桌機及觸控的新手完整流程。
