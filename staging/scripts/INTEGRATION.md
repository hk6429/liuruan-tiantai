# S 線 R0／R1 掛載說明

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

本機實際使用 .NET SDK 10.0.402，限定 C# 9，零 NuGet 套件，warnings 視為 errors。runner 在暫存目錄編譯所有 `*Core.cs` 與同一份 EditMode 測試，反射呼叫 Test/TestCase；這是 console 斷言執行，不是啟動 Unity 的 NUnit runner。需 .NET SDK 8 以上；本機已有 dotnet，未使用 mono/csc 備援。測試產物離開時清除。

U 線匯入後請在 Test Runner → EditMode 跑 `Liuruan.Tests`，再現場驗證：HUD 初始三格／歸零 Toast 與 60% 速度／暫停與補桃／NavMesh 跟隨／無 NavMesh 備援／卡住及超距瞬移／動畫 Speed／呼喚冷卻環。S 線未執行這些 Unity 驗證。


## R1：MinimapCam（右下小地圖）

1. 建立獨立 Camera，掛 `Liuruan.MinimapCam`，不要掛在主相機。指定 Player、Companion；Target 可空，流程改變目標時呼叫 `SetTarget(Transform)`，清除用 `SetTarget(null)`。
2. World Min 是探索範圍左下角的世界 X/Z；World Size 是 X/Z 寬深（預設 -128/-128、256/256，**U 線須依灰盒實際範圍調整**）。這是固定世界格網，不隨玩家平移，避免迷霧紀錄漂移。Reveal Radius Cells 預設 2，以格為單位，採包含邊界的圓形揭露；半徑 0 只揭露所在格。
3. View Half Size 預設 24m，Camera Height 100m；地圖固定世界 +Z 朝上，俯視正交、aspect=1，跟隨玩家位置，不跟玩家旋轉。Map Layers 選環境層，排除主角／夥伴模型或非地圖特效。請避免其他相機控制器同時寫入此 Camera。
4. Awake 建立 `Output`（256×256 RenderTexture）及 `FogMask`（32×32 RGBA Texture2D；黑色 alpha 255＝霧、0＝已探索），銷毀時釋放。本批以內建 OnGUI 在右下直接合成底圖、世界對齊迷霧與圖示層，不需要新增 UI 套件；白色箭頭表示玩家朝向、青色點表示阮肇、黃色點表示當前目標，圖示畫在霧上，離開視野時隱藏。
5. `Output` **只含攝影機底圖**，迷霧與圖示是 OnGUI 疊層；若 U 線改用 Canvas，須一起移植疊層，不可只把 Output 放進 RawImage 就當作完整地圖。不要同時顯示兩份底圖。FogMask 的列從世界 -Z 到 +Z（Texture2D 的底至頂），取樣須使用固定 World Min/Size，不能直接伸滿跟隨視窗。超出世界範圍在第一幕保持黑霧。
6. GameFlow 初始化完成（Awake 之後）先呼叫 `SetAct(1)`；換幕呼叫 `SetAct(2..5)`。第一幕自動隨玩家所在格揭露，第二幕起完全取消迷霧；重新回第一幕保留本局探索紀錄，重開局需重建元件。`OnFogChanged(bool enabled)` 僅在有霧／無霧切換時發送，初始狀態請由流程主動設定。
7. 最小版只記錄每幀玩家所在格的周圍；瞬移不揭露中間路徑。預設 256m 世界每格 8m，若想要較細探索，先縮小 World Size。未提供存檔或額外 UI 互動。

## R1：ClimbZone（第二幕藤蔓）

1. 在藤蔓互動區物件掛 `Liuruan.ClimbZone`，隨附 BoxCollider 由 Awake 設為 trigger，調整 trigger 覆蓋藤蔓底部。指定 Player（移動根物件）、Player Movement（該玩家的一般移動 MonoBehaviour）、Hunger（同一玩家 HungerSystem）、Plane Origin、Top Landing。一般移動元件不可指向 ClimbZone 自己。
2. Plane Origin 放在**玩家根位置對應的藤蔓底邊中心**，local +X 是橫移方向、世界 +Y 是上攀方向、local +Z 朝離開山壁方向；只能繞世界 Y 旋轉、不能傾斜或滾轉。Width/Height 是世界公尺，不使用 Transform scale 換算。Top Landing 放在平台上安全的玩家根落點，必須可容納膠囊，不要放進山壁。
3. 玩家 Collider 或其子物件 Collider 進入 trigger 後 `CanInteract` 才為 true，互動呼叫 `TryInteract()`，成功回 true；一般移動已被停用時拒絕掛附，避免多區搶同一玩家。元件記住一般移動與 CharacterController 的啟用狀態，暫停兩者以鎖定藤蔓平面；釋放、到頂完成及元件停用都還原原始狀態。玩家不能另有持續推動的 Rigidbody、NavMeshAgent 或 Root Motion，若有，U 線需配合 `OnMovementLockChanged` 暫停它們。
4. 狀態：Grounded → Attaching（投影到平面）→ 下一個非零 dt 更新進入 Climbing。WASD／方向鍵沿藤蔓移動，手機每幀呼叫 `SetMoveInput(Vector2)`，放開時傳 Vector2.zero。向量會正規化以免斜走加速；正常 1.2m/s，Hunger.Value <1 為 0.6m/s，恰為 1 仍為 1.2m/s。Hunger 未指定時視為飽食，因此 U 線必須接上。
5. 向上到頂：TopOut → 以 1.2m/s 從藤頂移至 Top Landing → Grounded → `OnTopOut()`。`OnStateChanged(TopOut)` 是翻越開始，**GameFlow 應以 OnTopOut 作為已站上平台的通知**。此為位置過渡，動畫由 U 線依狀態接線；TopOut 期間維持移動鎖定。
6. 向下到底：Release → 還原控制 → Grounded；停在底部不輸入不會立刻放開。取消／切幕可呼叫 `Release()`；它會安全地還原控制，重複呼叫不多發事件。停用區域也自動 Release。頂端／底端及翻越路徑沒有碰撞掃描，須由 U 線安排安全落點與無障礙路徑。
7. `OnPromptChanged(bool)` 提供進出區提示；UI 可以顯示簡短白話「攀藤」。包裝不建立台詞、不處理 GameFlow、不自行消耗 E 或觸控互動鍵。

## R1：DriftPickup／DriftCollection（第三幕溪流）

1. 在第三幕流程根物件掛一個 `Liuruan.DriftCollection`。溪中三個漂流物各掛 `Liuruan.DriftPickup`，三者的 Collection **都指定同一個收集器**；指定 Player，Id 依序為 1＝蕪菁葉、2＝杯、3＝胡麻飯糝。請勿用三個不同的收集器，否則不會集滿。
2. Waypoints 至少兩點，依上游到下游排列；路徑點必須是獨立靜態物件，不能放在漂流物的子階層。Start 時快取世界座標，使用開放 Catmull–Rom 曲線（端點重複），Cycle Seconds 預設 30 秒走完全程，抵達下游後回上游循環；不是下游反向游回上游。各漂流物可用不同路徑／週期避免重疊。
3. 曲線採均勻參數，因此速度不是固定 m/s，彎道可能略有超出控制點；依溪道寬度放路徑點並調長週期以維持緩慢漂流。本輪未新增弧長表、浮力、物理或網路套件。
4. 玩家距離採三維直線距離，<=2m 可拾取；`CanInteract` 供 UI／互動路由器即時讀取。`OnPromptChanged(int id, bool visible)` 在靠近／離開／停用時通知顯示或清除提示，可顯示「撿起」。只有真正成功才回傳 `TryInteract() == true`，遠距／重複拾取均回 false。
5. 成功時 Core 先把該 id 記入共用收集器：收集器發 `OnCollected(int id)`，第三個唯一 id 接著發 `OnAllCollected()`，再由拾取物發 `OnPickedUp(int id)`；包裝清除提示並停用整個漂流物 GameObject（Renderer/Collider 一起停用）。如果流程會在全收集時立刻切幕，優先訂閱收集器事件。每個 id 只計一次，三件全收集只發一次，即使存在重複 id 的物件也不會多算；重啟物件仍會自行隱藏。
6. GameFlow 用收集器 `Count` 同步 HUD，`OnCollected` 更新三件線索、`OnAllCollected` 開推論題；各 `OnPickedUp` 留給拾取特效。不靠 static 全域狀態，重新開始此幕若要清空收集，重建共用收集器及三個漂流物，不要只 SetActive(true)。

## R1：GameFlow／輸入接線契約（U 線負責）

Runtime 不引用 GameFlow、PlayerController、Interactable、ScrollUI 或 TouchInput，也不依賴它們所在的組件。沿用現有事件路由即可；不須修改這批 asmdef。

| 元件 | C# 事件 | 參數／用途 |
|---|---|---|
| MinimapCam | `event Action<bool> OnFogChanged` | true＝第一幕迷霧啟用，false＝取消 |
| ClimbZone | `event Action<ClimbState> OnStateChanged` | 狀態改變（含 Attaching、Climbing、TopOut、Release、Grounded） |
| ClimbZone | `event Action<bool> OnMovementLockChanged` | 攀爬鎖定／還原時，通知其他移動或動畫系統 |
| ClimbZone | `event Action<bool> OnPromptChanged` | 藤蔓互動提示顯示／清除 |
| ClimbZone | `event Action OnTopOut` | 翻越完成、安全落點到達後通知流程 |
| DriftPickup | `event Action<int, bool> OnPromptChanged` | 漂流物 id、顯示或清除 |
| DriftPickup | `event Action<int> OnPickedUp` | 單一拾取物成功後的 id |
| DriftCollection | `event Action<int> OnCollected` | 共用唯一收集通知，id=1/2/3 |
| DriftCollection | `event Action OnAllCollected` | 三個唯一 id 全收集，只發一次 |

事件都應在 U 線 OnEnable 訂閱、OnDisable 解除。C# 事件不是 Inspector UnityEvent；如需 Inspector 綁定，請由既有輸入／流程腳本轉接。

現有 PlayerController 會透過 `Interactable.onInteract` 處理 E 與手機按鈕，可在 U 線用有保存參考的 UnityAction 轉接，例如：

```csharp
// U 線已有對應的 Interactable、ClimbZone 參考。
UnityEngine.Events.UnityAction climbAction = () => { climbZone.TryInteract(); };
climbInteractable.onInteract.AddListener(climbAction);
// 拆除時使用同一個 climbAction RemoveListener。
```

漂流物同理轉接 `TryInteract()`；其 Interactable 設 radius=2、clueId=-1、toast 空值，prompt 用「撿起」。現有 Interactable 只看水平距離，**真正能拾取仍以 DriftPickup.CanInteract 的三維 2m 為準**；U 線的提示路由應先檢查此值，避免樓層上下誤顯示。藤蔓的 Interactable 則須同時檢查 ClimbZone.CanInteract，因一般 Nearest 並不檢查 trigger。一次互動只選一個可互動元件，避免同幀多次處理；請勿讓另一支腳本重複 ConsumeDown。

ClimbZone 攀爬期間停用 PlayerController（含其相機 LateUpdate），相機暫留原位；若要攀爬鏡頭跟隨，U 線由獨立鏡頭腳本處理，不能重新啟用一般移動來解決。

## R1 驗證與交接邊界

`Tests/run_core_tests.sh` 已納入新三組測試；R0 的 40 案例保留，新 29 案例，共 **69 passed, 0 failed**。同一份 R1CoreTests.cs 可由 Unity NUnit 執行。原始 console 輸出：`logs/core-tests-R1.txt`，交件報告：`logs/S-R1.md`。

本輪只完成純 C# 測試及靜態檢查；未開 Unity、未編譯 MonoBehaviour、未掛場景。U 線需驗證：256² RenderTexture／迷霧世界座標對齊與第二幕取消／箭頭北朝上；玩家多 Collider 進出與攀爬控制還原／手機搖桿／上下端安全落點；三物件循環與隱藏／提示路由／三個 id 與推論題事件。OnGUI 的疊層順序及手機安全區也需實機驗收。

## R2：SeasonSwap（半年演出／歸鄉換景）

1. 在不隨兩村停用的流程物件掛 `Liuruan.SeasonSwap`。Inspector 有 Spring／Summer／Autumn／Winter 四組 Palette，各含 Canopy、Ground、Fog、Light。Canopy Renderers／Ground Renderers 指定要變色的 Renderer（多材質 Renderer 會套用同一顏色），Sun 指定主方向光。預設 Color Property 是 URP Lit 的 `_BaseColor`；自訂 shader 若不使用此屬性，必須填實際顏色屬性名稱。以 MaterialPropertyBlock 改色，不修改 sharedMaterial 資產；不要讓別支動畫同時覆寫相同色彩欄位。
2. Immortal Village Root／Home Village Root 指定兩組獨立且互不包含的村落根物件，初始仙村開、歸鄉村關由場景設定；Sky Return 指定 A 線 `sky_return` 材質，Return Light 預設灰冷 (0.62, 0.7, 0.82)。不會用名稱尋找物件。換景前會檢查兩根物件、天空與光源，缺欄位時記錯誤並保留原景，補齊後可重呼叫。
3. 第四幕「遂停半年」開始時呼叫 `PlayHalfYear()`；6 秒依春→夏→秋→冬→春混色，完成先套用春色，再發 `OnBirdsSing()` 一次。用這個事件接 AudioSource.Play 與開放求歸／恢復輸入；旁白用 narration.json 現有句子，鳥鳴文字以底本「百鳥鳴呼」為準。本元件不管理音訊、玩家移動、飢餓或面板暫停，流程須在演出期間集中管理鎖定。
4. `PlayHalfYear()` 同一元件生命週期只生效一次，播放中／完成後重呼叫均不重設計時、不重發鳥鳴。`IsPlaying` 供流程查詢；Time.timeScale=0 或停用元件時演出暫停，重新啟用繼續，重玩重建元件。
5. 第五幕／讀檔恢復 Act5 呼叫 `SwapToHome()`，關仙村、開歸鄉村、改 RenderSettings.skybox、主光轉灰冷，接著發 `OnHomeSwapped()`；重呼叫無作用。中途換景會取消季節且不補發鳥鳴，後續 PlayHalfYear 也無作用，避免春色蓋掉歸鄉光色。只改天空材質，不更新烘焙光照／反射探針；U 線驗證實際天空與環境光協調。
6. Core：`SeasonCore.Sample(progress)` 有限值夾到 0–1；Current／Next 是混色兩端，Weight 是 Next 權重，Current 權重為 1−Weight。0/.25/.5/.75/1 對應春／夏／秋／冬／春、Weight=0。邊界時季節索引與權重會切換，但最後混色連續。`Tick(dt)` 拒絕負值及非有限值，完成時先停止再通知，事件內重入不重複播放。

## R2：EpilogueDirector（只提供資料與事件）

1. 在流程物件掛 `Liuruan.EpilogueDirector`，Clues Json 指定 C 線 clues.json，Compare Json 指定 compare.json。使用獨立的可序列化 DTO（EpilogueClueDocument／EpilogueCompareDocument），不改既有 ClueData、GameFlow、HudUI、QuizStats 或 asmdef；JSON 解析使用 Unity 內建 JsonUtility，Core 只接收純 C# DTO。
2. 結局時呼叫 `Present(int barkCount, int driftCount, ReadingResult[] readings, float totalSeconds)`。成功回 true 並保存 `Model`、發 `OnReady(EpilogueModel)`；資料錯誤回 false、清除 Model、記錯誤並發 `OnError(string)`，不建立任何 UI。每次 Present 都重新產生當下快照並通知，開關結局頁由既有 UI 管理。
3. Model.Paragraphs 從唯一 isEnding=true 的 original 依換行分段，保留文字與「……」省略標記，**不使用 endingText 當全文**。目前 7 段，與 sources/liuruan_tw.txt 相同；不補回留宿段。Model.Comparison 逐列保留 compare.json 的 Aspect／Liuruan／Taohua，目前 8 列；模型使用唯讀集合且不持有可變 DTO 列參考。
4. Model.Question 是本輪三選一的原文證據題「為什麼找到的是七世孫？」。選項只引用底本原文，正解索引 **1（0 起算）**＝「遂停半年」與「問得七世孫」；解析的時間落差說法引用 docs/content/acts.md「結局與資料介接」現有說明，不主張固定七百年。這是有別於 clues.json 結局四選一的呈現資料，不改 C 線題庫，也不自動寫 QuizStats。U 線應只呈現一份結局題，避免 HudUI／ScrollUI 重複出題與重複計分。
5. Score：BarkCount 為已剝皮棵數 0–5；DriftCount 為唯一漂流物數 0–3，**不是 GameFlow.State 的 bitmask 數值**。TotalSeconds 為流程傳入的有限非負秒數；流程自行累計本局有效遊玩時間及處理讀檔，包裝不讀 PlayerPrefs 或使用系統時間推算。
6. ReadingResult 提供每筆非結局題的 id 與 FirstTryCorrect，可在 U 線用 `new ReadingResult(id, QuizStats.FirstTryCorrect(id))` 建立。ReadingTotal 從 clues.json 的非結局卡自動計數（目前 6，含第 3 幕兩題）；沒傳入的題算未首答正確，ReadingCorrect 只計 true。ReadingAccuracy=ReadingCorrect/ReadingTotal，0–1；UI 乘 100 顯示百分比，0 題時回 0。不含結局 id=6／InferId=100，拒絕重複或未知 id；不得直接用 QuizStats.FirstTryCount()/ScoredCount()，因其分母可能包含結局／推論題。
7. `EpilogueCore.Build(...)` 可直接傳 DTO 作純 C# 使用，驗證結局唯一、線索 id 唯一、比較表欄位非空及成績範圍；無效輸入拋 ArgumentException（數值範圍為 ArgumentOutOfRangeException）。不含存檔、網路、UI 或全域狀態。

## R2：CharacterBob（綁骨前模型動畫）

1. 掛在模型的獨立子物件／視覺 pivot，不掛 CharacterController／NavMeshAgent 的移動根物件；Motion Root 指向其移動父物件（空欄預設直接父物件）。不可指向自己或自己的子物件。模型以 local +Z 朝前、+Y 朝上，U 線先校正 pivot 與縮放。本元件記住初始 localPosition／localRotation／localScale，停用時還原；不要和 Animator／Root Motion 同時寫同一個視覺 pivot。
2. 預設 Auto Measure=true，在 LateUpdate 量測 Motion Root 世界水平 X/Z 位移÷deltaTime，不把垂直攀爬算走路；OnEnable 重設位置錨點，避免停用期間移動造成假速度。瞬移會被 Max Speed 夾住，但仍可能有一幀走路姿勢，瞬移流程可停用／重啟元件。
3. 外部可每幀 `SetSpeed(float metersPerSecond)`，第一次呼叫就切到外部速度模式；放開須餵 0，速度不得為負／NaN／無限。亦可在 Inspector 取消 Auto Measure 後餵值。參數於 Awake 讀取，調整後重建元件或重新進入 Play Mode。
4. 可調 Max Speed（預設 8）、Base Frequency（1 Hz）、Frequency Per Speed（0.4 Hz/(m/s)）、Bounce（0.06m）、Sway（3°）、Lean（8°）、Breath（0–0.015）與 Breath Frequency（0.3 Hz）。步頻隨速度增加至 Max Speed 封頂；彈跳、左右微擺與前傾強度按速度比例。速度 0 時上下彈跳／左右微擺／前傾均為 0，只做最多 ±1.5% 呼吸縮放；不建立事件或移動玩家。Core：`Frequency(speed)`、`Tick(dt,speed)` 回傳 BobPose（Height／RollDegrees／LeanDegrees／Scale）。

## R2：GameFlow／HudUI 接線與驗收（U 線負責）

已只讀現有 GameFlow.cs／HudUI.cs：GameFlow 的 Act4_Board 目前直接讀卡，ReturnHome／Start／Act5_Return／DebugNextAct 仍呼叫既有 ApplyReturn；HudUI.ShowEnding 仍是文字面板。**本輪未修改這些入口，匯入元件不會自動完成串接。**

U 線需在適當的 Act4 半年節點呼叫 PlayHalfYear，OnBirdsSing 才允許求歸；把所有進入／恢復 Act5 的換景入口接到同一個 SeasonSwap，避免舊 ApplyReturn 同時改色；結局成功時組合唯一收集數、閱讀結果與時間，呼叫 Present，讓 OnReady 接現有結局 UI。SeasonSwap／EpilogueDirector 不引用 GameFlow 或 HudUI。

| 元件 | C# 事件 | 用途 |
|---|---|---|
| SeasonSwap | `event Action OnBirdsSing` | 6 秒春回春完成，只發一次；接鳥鳴、求歸開放 |
| SeasonSwap | `event Action OnHomeSwapped` | 成功換成歸鄉村，只發一次 |
| EpilogueDirector | `event Action<EpilogueModel> OnReady` | 每次 Present 成功，提供整頁資料快照 |
| EpilogueDirector | `event Action<string> OnError` | Present 資料錯誤，提供原因 |

這些是 C# event，訂閱者在 OnEnable 訂閱、OnDisable 解除；初始化與呼叫安排在元件 Awake 之後。CharacterBob 無事件。

`Tests/run_core_tests.sh` 本輪直接讀專案的 clues.json、compare.json、原文及 acts.md，不另複製 fixture；console 由 .NET 內建 System.Text.Json 解析欄位，Unity EditMode 分支使用 JsonUtility，原始資料唯讀。從任意 cwd 執行 runner 都會使用腳本推導的專案根目錄。實際結果 **91 passed, 0 failed**（原 69＋R2 22），完整輸出見 `logs/core-tests-R2.txt`。

尚待 U 線：Unity 編譯與 EditMode；四季 Renderer shader 實際混色、6 秒與鳥鳴順序／暫停／重入／提前歸鄉；兩村根物件及 sky_return 指派、讀檔換景；結局排版與三選一互動／首答率／時間來源；玩家與阮肇不同速度、靜止呼吸、模型 pivot、停用還原及手機效能。S 線未開 Unity／未做 WebGL 或實機驗收。
