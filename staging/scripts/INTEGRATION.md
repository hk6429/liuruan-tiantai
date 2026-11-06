# S 線 R0 掛載說明

本批提供原始碼；S 線未開 Unity、未寫 Assets、未改 PlayerController。

## 匯入位置

由 U 線複製（保留隨附 asmdef）：

- `Runtime/` → `Assets/_Project/Scripts/Liuruan/Runtime/`
- `Tests/EditMode/` → `Assets/_Project/Tests/EditMode/Liuruan/`

不要把 `Tests/Console/` 複製進 Assets，該處只含 console 測試用的 NUnit 屬性替身，匯入會與 Unity NUnit 衝突。不要把整個 staging/scripts 複製進 Assets。
Runtime 無第三方套件。EditMode 需專案已有 Unity Test Framework（NUnit）；本批不新增或修改 Unity 套件。

## 劉晨：HungerSystem

1. 在玩家劉晨的 GameObject 加入 `Liuruan.HungerSystem`，第一幕 `Initial Value = 3`、`Interval Seconds = 24`。
2. PlayerController 快取同物件元件：`hunger = GetComponent<Liuruan.HungerSystem>();`，並新增欄位 `private Liuruan.HungerSystem hunger;`。
3. 在既有速度計算完成後、乘上移動向量之前，加這一行（`moveSpeed` 換成實際的每幀區域速度變數）：

```csharp
moveSpeed *= hunger != null ? hunger.SpeedMultiplier : 1f;
```

每幀由基礎速度重算 `moveSpeed`，不可反覆修改 Inspector 的基礎速度欄位，否則會累乘。此專案尚無 PlayerController 原始碼可核對，因此這是插入契約，不是已套用的 diff。

4. HUD 啟用時先讀 `Value` 畫 5 格桃形圖示，再訂閱 `OnChanged(float)`；元件停用時解除訂閱。Inspector 也有 `On Changed` UnityEvent 可接 UI。
5. `OnStarving` / Inspector `On Starving` 只在正值跨到 0 時通知一次；沒有死亡、扣血或結局處理。
6. Toast UI 接 `OnToast(string)` 或 Inspector `On Toast`，顯示「肚子餓了，去找些食物吧。」；包裝層只發訊息，不建立 UI。
7. 面板／演出開始呼叫 `Pause()`，全部結束後 `Resume()`。這是可重複呼叫的布林暫停；若多面板重疊，請由既有面板管理者彙整後再 Resume。暫停保留未滿 24 秒的餘額。
8. 食物呼叫 `Eat(amount)`，攀桃完成呼叫 `FillAll()`。補食不重設仍在進行的計時；從飢餓恢復則重新計時。暫停期間仍可補食。重新開始第一幕時重新建立玩家物件，Core 沒有跨場景存檔。

Core API：`HungerCore(initialValue = 3, intervalSeconds = 24)`、`Tick(deltaSeconds)`、`Value`、`SpeedMultiplier`、`IsPaused`、`Pause/Resume/Eat/FillAll`、`OnChanged/OnStarving`。輸入 NaN、無限值、負時間或負食物量會拋出 ArgumentOutOfRangeException；初始飢餓值夾在 0–5。

## 阮肇：CompanionFollow

1. 在阮肇的移動根物件加入 `Liuruan.CompanionFollow`，`Player` 指向劉晨。
2. 可加 NavMeshAgent 並烘焙可行走範圍；建議基速 3、加速倍率 1.6。Agent 啟用且在 NavMesh 上時採 NavMesh 路徑；否則直接 MoveTowards。無 Agent 也能跑。
3. 根物件的位移由此元件負責，動畫關閉 Root Motion；`Animator` 的 Speed 參數每幀讀 `CompanionFollow.Speed`（實際世界位移 m/s）。瞬移當幀為 0。NavMeshAgent 的停止距離由元件設成 2.4m，使它確實進入 `<2.5m` 的 Idle 範圍。
4. 技能按鈕呼叫 `Call()`，回傳 true 才播放成功回饋；false 代表冷卻中，不延長冷卻。呼喚使阮肇維持 Catchup，直到 `<2.5m` 待機或觸發瞬移。
5. 左下頭像環每幀讀 `CooldownRatio`：1＝剛呼喚、0＝可呼喚；8 秒後歸零。若 UI 要顯示完成比例，讀 `1f - CooldownRatio`。Time.timeScale = 0 時跟隨與冷卻一起停止；未指定 Player 時不更新 Core。
6. 狀態距離採三維直線距離：`<2.5` Idle、`[2.5,12]` Follow、`(12,30]` Catchup、`>30` Teleport；呼喚中的 Catchup 優先於 Follow。
7. 非 Idle 狀態先記錄位置，此後連續 3 秒未離開進度錨點 0.05m 即視為卡住；累積移動達 0.05m、Idle 或瞬移會重設計時。Core 回傳的瞬移目標是玩家水平朝向後方 2m，零水平朝向則用世界 -Z。
8. Unity 包裝會在該目標附近 2m 取 NavMesh 落點（因此實際落點可能偏離恰好 2m），再 Warp；無有效落點或 Warp 失敗時直接設定位置。多層地形／山壁需要 U 線現場驗證安全落點。MoveTowards 不做避障與地面貼合，僅供沒有 NavMesh 的灰盒備援。

純 Core 使用 `WorldPosition(X,Y,Z)`，不引用 UnityEngine。`CompanionCore.Tick(dt, player, companion, playerForward)` 回傳狀態，並更新 `Destination`；`Call()`、`CooldownRatio` 提供技能契約。

## 驗證

在專案根目錄執行：

```sh
staging/scripts/Tests/run_core_tests.sh
```

本機實際使用 .NET SDK 10.0.402，限定 C# 9，零 NuGet 套件，warnings 視為 errors。runner 在暫存目錄編譯兩個 Core 與同一份 EditMode 測試，反射呼叫 Test/TestCase；這是 console 斷言執行，不是啟動 Unity 的 NUnit runner。需 .NET SDK 8 以上；本機已有 dotnet，未使用 mono/csc 備援。測試產物離開時清除。

U 線匯入後請在 Test Runner → EditMode 跑 `Liuruan.Tests`，再現場驗證：HUD 初始三格／歸零 Toast 與 60% 速度／暫停與補桃／NavMesh 跟隨／無 NavMesh 備援／卡住及超距瞬移／動畫 Speed／呼喚冷卻環。S 線未執行這些 Unity 驗證。
