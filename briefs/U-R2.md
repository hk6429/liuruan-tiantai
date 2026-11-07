# U 線 R2a：整合 R1 系統＋美術＋人物模型

先讀 AGENTS.md、docs/plan.md、你上一輪報告 logs/U-R1.md、staging/scripts/INTEGRATION.md、staging/scripts/logs/S-R1.md、staging/art/R1/（A-R1 報告在 logs/A-R1.md）、staging/models/README.md。寫入範圍：Assets/、ProjectSettings/、logs/U-R2*.md。交件報告 logs/U-R2.md。

## 執行環境（重要，R1 的教訓）
- 你的沙盒連不到 UnityMCP，也不能開 batchmode（Unity Editor 正開著這個專案）。你只寫檔；**CC 主線會在 Unity 觸發編譯並寫指令檔**。
- 沿用 `Assets/_Project/Editor/R1Bridge.cs` 的檔案輪詢機制：擴充它，指令字串以 `r2` 開頭時呼叫 `R2Validation.Begin()`（保留 `all` → R1 的行為；保留「Play 中先退出 Play」那行）。
- **TestRunner 的 RunFinished 回呼裡不可直接 `EditorApplication.isPlaying = true`**（會撞 RestoreSceneSetup）；照 R1Validation 現在的 `startplay` 延後 3 秒寫法。
- Begin 一開始先把所有 SessionState phase 清掉，避免上一輪殘留 phase 在 domain reload 後接著跑。
- 不要依賴 Unity 在前景；所有流程靠 EditorApplication.update 推進即可。

## 要做的事
1. **S 線 R1 系統接進場景**（原始碼在 staging/scripts/Runtime，照 INTEGRATION.md 複製進 Assets/_Project/Scripts/Systems/，測試進 Tests/EditMode/）：
   - MinimapCam：填滿 HUD 右下角小地圖框（框用 staging/art/R1/ui/minimap_frame.png），第一幕迷霧、Act2 起取消。
   - ClimbZone：Act2_ClimbBase 改成真正的攀藤（玩家互動後貼在崖面往上，頂端翻上平台），取代 R1 的瞬移。
   - DriftPickup：Act3 三件漂流物沿溪流 waypoint 往下游漂，E 撿起；三件集滿才觸發推論題。
2. **A 線 R1 美術接進場景**（原檔在 staging/art/R1/，複製進 Assets/_Project/Art/R1/，不要搬動原檔）：
   - sky_day.png 做 Panoramic 天空盒；sky_return.png 留給第五幕（建材質備用）。
   - 地面五組貼圖（含 _n 法線）套到森林／步道／岩石／溪床／村落地面材質，tiling 合理（看 *_tilecheck.png）。
   - 草叢圖集 foliage_atlas.png＋atlas_layout.json 做成卡片植被，沿用 R30Foliage 的做法撒在森林與溪谷，每區數量控制在 WebGL 可負擔（總 draw call 用 GPU instancing）。
   - UI：peach_full/half/empty 換掉 HUD 飢餓值、bark 穀皮圖示、skill_bark/water/call 三技能格、portrait_ruanzhao＋portrait_frame 左下頭像。
3. **人物模型**：staging/models/game/liuchen.fbx（玩家劉晨）、ruanzhao.fbx（阮肇），1.6m 高、腳底原點、貼圖內嵌（複製到 Assets/_Project/Art/Characters/，ModelImporter 解出貼圖與材質，材質用 URP Lit）。取代膠囊體的外觀（保留原碰撞體／CharacterController）。尚未綁骨：先做一個很小的程序化走路彈跳（依水平速度上下彈、靜止時微呼吸），S 線 R2 會交正式的 CharacterBob，屆時再換。
4. **R2Validation**（Editor，選單 Tools/Liuruan/R2/Validate All）：R2Setup.Install → EditMode 測試全綠 → Play 五幕走查（沿用 R1 路線，攀爬改走 ClimbZone 真實流程、漂流物要真的追上移動中的物件再互動；結束檢查 console error=0）→ R30Capture 存 Captures/r2/（六機位＋HUD，另加一張人物近景 r2_chars.png）→ 執行選單 `Tools/Liuruan/Build/WebGL + Scene Bundle`，**development=false** → 寫 logs/U-R2-build.md（含 data 檔大小，必須 < 26,214,400 bytes）。每一階段寫 logs/U-R2-*.md。
5. 寫完自己用 `dotnet`/roslyn 或 mcs 做語法層編譯檢查（R1 你有 logs/U-R1-compile.md 的做法），結果貼報告。報告寫清楚：CC 要寫什麼指令字串到 R1Command.txt 才會觸發。
