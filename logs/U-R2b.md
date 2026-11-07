# U 線 R2b 交件報告

日期：2026-11-07。狀態：檔案整合與離線驗證完成，待 CC 執行 Unity。**本輪新截圖 0／8、WebGL 建置 0 次，未宣稱引擎路線 PASS 或 console error=0。**

## CC 執行入口

先在目前開啟此專案的 Unity 執行 Assets → Refresh，等待編譯完成，確認 Console 無編譯錯誤。再將以下完整字串寫入 `Assets/_Project/Editor/R1Command.txt`：

```text
r2-r2b-001
```

本輪未寫入指令檔，避免未 Refresh 的舊 Bridge 提前執行。沿用 R1Bridge 的 r2 前綴、Play 中先退出、清空指令、空值重設去重機制；選單仍為 Tools/Liuruan/R2/Validate All。

執行順序：R2Setup.Install → EditMode → 延後 3 秒進 Play → 五幕路線 → 結局面板各頁／作答 → 退出 Play／還原測試存檔 → Captures/r2 八張 → release WebGL＋Scene Bundle。失敗即停止並寫 U-R2-failure.md。全程仍由 EditorApplication.update 推進，不要求 Unity 在前景。

## 本輪變更

### 人物

- 兩人共 **10 FBX＋2 albedo PNG** 已複製到 `Assets/_Project/Art/Characters/<key>/`，與 staging 逐位元組比對一致。
- 新的 `R2bSetup.Character` 由 Install 呼叫：全部 FBX 設 Human；idle 建 Avatar 並檢查 isValid／isHuman，其他動畫 CopyFromOther／sourceAvatar 指向同角色 idle。禁用 FBX 材質匯入，不再呼叫 ExtractTextures；材質為獨立 PNG 的 URP Lit，Smoothness=.12，不使用 metalrough。
- idle／walk／run 循環；climb 亦循環。控制器以 Speed 混合 idle=0、walk=1.5、run=4，有閾值斷言。Climbing 進出 Climb，劉晨另有 Pickup／PickFruit Trigger 與播完返回移動狀態的轉場。
- applyRootMotion=false；climb 位移抽取後捨棄，世界位置仍由 ClimbZone 管理。身高 1.6m／腳底對齊沿用 bounds 校準並斷言；模型放在獨立 R2Visual pivot，保留 CharacterController／原碰撞體。
- PlayerController 既有實際水平 Speed 綁定新 Animator；CompanionFollow 每幀餵 Speed 並對齊移動方向。ClimbZone 進出設 Climbing；漂流物成功撿起設 Pickup；摘桃設 PickFruit，等該動畫長度＋.15 秒後才離開桃樹／讀卡，期間鎖定移動與重複互動。
- 新人物不掛 R2CharacterMotion／CharacterBob；安裝清除角色上的這兩種彈跳元件，舊 R2CharacterMotion 額外略過有 Animator 的模型。S R2 CharacterBob 完整匯入作為無 Animator 外觀的可用元件，目前未掛載。

### S R2 流程

- 六支 Runtime 與 R2CoreTests 精確複製進既有 Systems／EditMode 組件，沒有複製 Console NUnit 替身或修改 asmdef。
- 第四幕告示板讀卡完成顯示「遂停半年」並啟動 PlayHalfYear，6 秒期間鎖定玩家／互動／求歸，飢餓暫停；鳥鳴事件保存 halfYearComplete，才開放求歸。讀檔至未完成的第四幕末段會重播；已完成則直接保留求歸權限。
- Assets／staging 未找到音訊檔，故依本輪要求使用 Debug log＋HUD 小字「百鳥啼鳴」5 秒，未假造已播放音效。原文／資料內仍保留 S 線底本「百鳥鳴呼」。
- SeasonSwap 指定仙村 Village、獨立 R2HomeVillage、sky_return 與主光；指定桃樹／樹冠與村落地面 Renderer。所有 ApplyReturn 入口（求歸／第五幕互動／讀檔／Debug）改走同一 SwapToHome。歸鄉村有四棟房子：原有兩棟＋兩棟複製灰盒，初始關閉。第四幕互動點所掛道具於歸鄉隱藏。
- 第五幕保留非結局的歸鄉閱讀題，略過舊結局四選一；接著用 EpilogueDirector.Present 的資料顯示全文各段、三選一「為什麼找到的是七世孫」、八列比較表與成績卡。上一頁／下一頁逐頁巡覽，選項按鈕顯示對錯與解析，記 QuizStats.InferId。全文保持 C 線刪節，不恢復留宿段。
- 成績以 bitmask 計唯一收集數、六張非結局卡的 FirstTryCorrect、保存於 Progress 的有效遊玩秒數組成；閱讀／演出／結局不累計時間。

### 道具

Install 每次檢查 `staging/models/props/<name>.fbx` 和 `<name>_albedo.png`，兩者都有才同步到 `Assets/_Project/Art/Props/<name>/`，建立 URP Lit＋albedo 並換外觀；缺任一檔即 SKIP，保留灰盒。安裝結果另寫 U-R2-props.md；保留名稱／碰撞／互動根，不加模型碰撞體。重跑替換既有 R2Prop，避免累加外觀。

| 名稱 | 安裝目標／高度 | 本輪來源快照 |
|---|---|---|
| house_tongwa | Village/House_0..3，5m；另兩棟灰盒外觀隱藏 | 缺 FBX／PNG，待 CC 產生 |
| pavilion_luozhang | 仙村中央 1 座，5m | 同上 |
| peach_tree | Act2_PeachTree＋崖頂 3 棵，4m | 同上 |
| chu_tree | Act1_Bark_1..5，5m | 同上 |
| drift_items | 三個漂流根各掛完整模型，.35m | 同上 |
| home_house | 歸鄉村 4 棟，5m | 同上 |
| vine_cliff | 攀爬根，讀 ClimbZone.height | 同上 |
| village_board | Act4_Board，2.2m | 同上 |

交件時 props 資料夾尚不存在；這是當下唯讀快照，真正採用哪些模型以 CC 觸發 Install 當刻的檔案與 U-R2-props.md 為準。

### 驗證／建置入口

- R2Validation 保留原五幕真實攀藤／追漂流物路線，新增 Humanoid／Root Motion、Climbing 還原、半年期間求歸禁止、換村／結局收集數斷言。結局逐段翻頁、三選一按鈕作答／解析、逐列比較表、六題首答成績均有面板斷言。
- 安裝期間的 console error 也納入失敗條件，EditMode 不接受失敗或跳過；Play 與 build 結尾仍檢查 error／exception／assert=0。
- R30Capture 的 r2_chars 以兩人的 walk clip 取 30% 時間姿勢後攝影，AnimationMode 結束即還原；其他六機位與 Play HUD 保留。八檔都檢查本輪時間戳、>1024 bytes，不採舊圖。
- release 設 development=false；壓縮沿用專案目前 Brotli＋decompressionFallback，未改 ProjectSettings。檢查本輪 .bundle-done、*.unityweb（scripts/deploy.sh 必要）、wasm／data 存在及非空 data 嚴格 <26,214,400 bytes。成功後 U-R2-build.md 記錄壓縮格式、wasm／data 實際檔名與 bytes。

## 真正執行的驗證

| 項目 | 結果／證據 |
|---|---|
| Unity 隨附 Roslyn＋Bee 參照 | Runtime、Assembly-CSharp、Editor、EditModeTests 四組件 exit 0，無診斷；logs/U-R2-compile.md |
| 匯入 Assets 的 Core／共用測試＋暫存 console runner | **91 passed，0 failed**；logs/U-R2-core-tests.md |
| 資產／S R2 複製完整性 | 10 FBX＋2 PNG SHA-256 相同；6 Runtime＋1 測試逐位元組相同；logs/U-R2-assets.md |
| diff 空白檢查 | git diff --check -- Assets ProjectSettings 通過 |

暫存檢查工具沿用 `/tmp/liuruan-r2-compile.py`／`/tmp/liuruan-r2-compile/`；Core runner 在 `/tmp/liuruan-r2b-tests/`，零 NuGet。這些是離線驗證，**不是 Unity Refresh、EditMode、Play、截圖或 WebGL 執行證據**。

## 檔案清單

新增（含對應 .meta）：

- Assets/_Project/Editor/R2bSetup.cs
- Assets/_Project/Scripts/Systems/{SeasonCore,SeasonSwap,EpilogueCore,EpilogueDirector,CharacterBobCore,CharacterBob}.cs
- Assets/_Project/Tests/EditMode/R2CoreTests.cs
- Assets/_Project/Art/Characters/liuchen/liuchen@{idle,walk,run,climb,pickup,pickfruit}.fbx 與 liuchen_albedo.png
- Assets/_Project/Art/Characters/ruanzhao/ruanzhao@{idle,walk,run,climb}.fbx 與 ruanzhao_albedo.png

修改：

- Assets/_Project/Editor/{R2Setup,R2Validation,R30Capture}.cs
- Assets/_Project/Scripts/{GameFlow,HudUI,PlayerController,R2CharacterMotion}.cs
- Assets/_Project/Scripts/Systems/{CompanionFollow,ClimbZone,DriftPickup}.cs
- logs/U-R2-{assets,compile,core-tests,build}.md；新增本報告 logs/U-R2b.md

本輪沒有修改 staging、來源原文、Data、Main.unity、字型或 ProjectSettings；工作區原有其他線修改保留。舊角色 FBX／無副檔名抽取檔保留但新流程不使用；天空 Texture2D 修正保留。

## 未完成／風險與下一步

CC 需以上述指令完成實際匯入／Avatar retarget、模型朝向與步態、climb 位移抽取、季節顏色、8 張新圖與包體驗收；AnimationMode 的 Humanoid walk 取樣也必須看實圖，不能只看檔案大小。道具尺寸與構圖仍須現場目視；source bounds 不保證不同生成模型的美術比例。沒有鳥鳴音效。

本輪未開 UnityMCP／batchmode／第二個 Editor，未部署／push，未測手機。Obsidian Vault 位於本輪允許範圍與沙盒可寫目錄之外，未另存，也未宣稱已儲存。
