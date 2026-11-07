# S 線 R2 交件報告

日期：2026-11-07。里程碑：SeasonSwap、EpilogueDirector、CharacterBob 原始碼與 Core 驗證完成。

## 範圍與來源

只寫入 `staging/scripts/`；未操作 Unity／UnityMCP／batchmode、未修改 Assets、GameFlow、HudUI、其他線檔案、套件或 asmdef，未推送 GitHub／Vercel。

已讀 AGENTS.md、docs/plan.md、staging/scripts/INTEGRATION.md、logs/S-R1.md（本資料夾）、Assets/_Project/Data/{clues,compare,narration}.json，以及只讀 GameFlow.cs、HudUI.cs、QuizStats.cs、sources/liuruan_tw.txt、docs/content/acts.md。根目錄 RTK.md 不存在。舊計畫的「百鳥啼鳴」以現行底本「百鳥鳴呼」為準，全文保留 C 線刪節，不恢復留宿段。

## 做了什麼

- SeasonCore：0–1 進度的春／夏／秋／冬／春取樣、兩季與 Next 權重（另一權重為 1−Weight）、固定 6 秒演出；每局只啟動一次，完成停春後鳥鳴一次。完成／播放中重呼叫不重設，事件內重入不多發。歸鄉取消演出、阻止再播，換景狀態冪等。
- SeasonSwap：四組 Inspector Palette（樹冠／地面／霧／光）、Renderer 欄位與 MaterialPropertyBlock、兩村根物件切換、指定 sky_return 材質與灰冷主光；全以欄位連接，不以字串搜尋場景。缺欄位／相同或互含根物件／流程物件在村落內時拒絕換景。完成先套春色才對外鳥鳴事件，歸鄉後不再套季節。
- EpilogueCore：純 C# DTO 與唯讀結局快照，從唯一 isEnding original 產生七段全文；比較表逐欄複製，實際八列；三選一原文證據題，正解索引 1。選項的引文來自 sources/liuruan_tw.txt，解析的時間落差說法逐字引用 acts.md 既有說明，不新增情節、不換算固定年數。沒有沿用四選一中的虛構情節干擾選項。
- 成績卡：剝皮 0–5、唯一漂流物 0–3、六張非結局閱讀題的首答正確率、流程傳入的有效遊玩秒數。未傳閱讀題算未首答正確；拒絕重複／未知／結局題 id、非法數值。零題分母回 0，結局題不列入閱讀率。
- EpilogueDirector：Inspector TextAsset 由 JsonUtility 讀取，Present 成功才保存 Model 並 OnReady，失敗清除 Model、回 false 並 OnError；只提供資料與事件，不建 UI、不讀存檔、不依賴 GameFlow／QuizStats。
- CharacterBobCore／CharacterBob：水平速度控制彈跳、微擺、前傾，步頻隨速增加並封頂；靜止僅呼吸縮放，最多 ±1.5%。可 SetSpeed 外餵或量測父根物件 X/Z 位移；視覺模型與移動根分離，停用還原初始變形，重新啟用重設位移錨點。
- 更新 INTEGRATION.md：三個元件掛法、參數、事件與 U 線實際 GameFlow／HudUI 尚待接線入口。
- 沿用共用 EditMode 測試與 console runner，新增三組測試；runner 直接讀取 C 線現行資料，不複製第二份 fixture、不新增套件。

## 檔案清單（相對 staging/scripts/）

新增：

- Runtime/SeasonCore.cs
- Runtime/SeasonSwap.cs
- Runtime/EpilogueCore.cs
- Runtime/EpilogueDirector.cs
- Runtime/CharacterBobCore.cs
- Runtime/CharacterBob.cs
- Tests/EditMode/R2CoreTests.cs
- logs/core-tests-R2.txt
- logs/S-R2.md

修改：

- INTEGRATION.md
- Tests/Console/Runner.cs
- Tests/run_core_tests.sh

## 實際驗證

`staging/scripts/Tests/run_core_tests.sh`：**91 passed, 0 failed**（原 R0/R1 69＋R2 22）。使用 .NET SDK 10.0.402、C# 9、warnings as errors、零 NuGet 套件；執行與 EditMode 同一份方法，但不是 Unity NUnit runner。初次通過後，因 Core 參數錯誤欄位與包裝驗證有小幅調整再跑一次，以下是最終實際輸出。

R2 覆蓋：五個季節精確邊界、每個混色邊界連續、權重範圍、中點、進度夾值、精確／超量 6 秒、鳥鳴重入只一次、重複播放／換景、提前歸鄉取消；實際 JSON 全列與每欄一致、DTO 不影響快照、全文等於原文及保留省略、三選一答案與來源、首答率部分／零／全對、成績範圍與 id／資料錯誤；靜止無彈跳與呼吸上下限、步頻單調與封頂、參數影響、零時間、極大有限值無 NaN／Infinity、非法輸入。

其他檢查：

- `sh -n staging/scripts/Tests/run_core_tests.sh`：通過。
- `git diff --check -- staging/scripts`：通過。
- 新增／既有 C#、整合文件、runner 行尾空白掃描：通過。
- 所有 *Core.cs 無 UnityEngine／UnityEditor／GameFlow／HudUI 引用：通過。
- 三個新元件無 GameObject.Find／FindFirstObjectByType：通過。
- 兩個既有 asmdef JSON 可解析，未修改：通過。

### 完整原始測試輸出

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
RESULT: 91 passed, 0 failed
```

## 沒做到與未解風險

- **未開 Unity、未編譯 MonoBehaviour、未跑 Unity Test Runner、場景、WebGL、手機或音畫驗收。** Core 全綠不代表已匯入、已掛載或玩家流程可用。
- GameFlow 現有 ApplyReturn／Act4_Board／ReturnHome 與 HudUI.ShowEnding 均未改。U 線必須串接半年完成解鎖求歸、所有 Act5／讀檔換景入口及結局資料呈現；否則仍是舊流程。
- 四季混色要求 Renderer shader 實際讀取所填顏色屬性；Terrain、烘焙光照與反射探針不在此包裝的控制範圍。天空、材質、兩村根物件均待 U 線指定與視覺檢查。兩村必須是獨立根、流程管理物件在其外。
- SeasonCore 是每個元件生命週期的一次演出；重玩重建元件。停用／timeScale=0 暫停演出；若中途歸鄉則不補發鳥鳴，避免遲到事件重開流程。
- 三選一是「原文證據組合」型題；沒有改 C 線既有四選一。U 線只呈現其中一份結局題，需避免重複出題與計分。
- 閱讀率採首答正確／全部非結局閱讀題；不等於最終全部作答次數的正確比例。計時、存讀檔、bitmask 轉唯一數量及 QuizStats 轉 ReadingResult 由 U 線負責，S 線未宣稱已完成串接。
- CharacterBob 適用尚未綁骨的視覺 pivot，不提供行走方向、腳步貼地、轉身或骨架 IK。父根瞬移最多有一幀封頂走路姿勢；U 線可停用／重啟元件。Animator 進場後須協調寫入權限或停用此元件，模型 pivot／朝向待校準。
- 本輪明定只能寫 staging/scripts/，因此未寫 Obsidian Vault，也未宣稱已另存。

## 下一步（U 線）

依 INTEGRATION.md 選擇性複製 Runtime 與 Tests/EditMode（不複製 Console），先跑 Unity 編譯及 EditMode；接 GameFlow／HudUI、設定材質與村落欄位，再逐項驗證 6 秒混色／鳥鳴／換景／結局三選一與成績、玩家和阮肇速度動畫及停用還原。最後才做 WebGL／手機驗收。
