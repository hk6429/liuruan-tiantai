# U 線 R2a 交件報告

日期：2026-11-07。

**狀態：R2a 整合檔案與 CC 執行入口已交付；四個組件離線編譯、69 項純 Core 測試通過。尚未在 Unity 執行安裝／引擎測試／Play／截圖／WebGL，不可當作 R2 引擎驗收完成。**

## CC 觸發方式

1. 先在目前已開啟此專案的 Unity 執行 Assets → Refresh，等新腳本編譯結束，確認 Console 無編譯錯誤。
2. 把下列**完整文字內容**寫入 `Assets/_Project/Editor/R1Command.txt`：

```text
r2-r2a-001
```

R1Bridge 讀到任何 `r2` 前綴都會清空指令檔並呼叫 `R2Validation.Begin()`。本輪未預先寫入命令，避免舊版 Bridge 在新腳本匯入前吃掉命令。也可使用選單 **Tools/Liuruan/R2/Validate All**。保留 `all` → R1Validation 與 Play 中先退出 Play 的原有行為；空指令會重設去重狀態，可重新下同一命令。

流程完全由 EditorApplication.update 推進，不須 Unity 在前景；Play 會 QueuePlayerLoopUpdate。Begin 清除 R1／R2 phase、重設步驟，並還原未完成 R2 的測試存檔備份。TestRunner.RunFinished 僅排 `startplay`，由 update 延後至少 3 秒才進 Play，沒有在回呼直接進 Play。

## 已寫好的整合

- **S R1**：七支 Runtime 檔與 R1CoreTests 匯入既有 Systems／EditMode 組件，沒有匯入 Console NUnit 替身，也沒有提前納入 S R2 未交件系統。
- **小地圖**：獨立正交 256² RenderTexture，世界範圍 -120～120；保留固定世界座標迷霧與玩家／夥伴／目標圖示。MinimapCam 改讀 HUD RectTransform 的實際螢幕座標，內縮 8% 後填圖並疊 minimap_frame.png；刪除原來空白 RawImage，避免雙份底圖。Act1 有霧、Act2 起取消；閱讀／結局時跟 HUD 一起隱藏。
- **攀藤**：Act2_ClimbBase 設在西崖東側，18m 高、1.5m 寬；kinematic trigger＋平面原點＋平台安全落點。GameFlow 互動呼叫 TryInteract，只有 OnTopOut 才記為已攀上；鍵盤由 ClimbZone 讀取、手機搖桿由流程轉入。攀爬鎖定期間有獨立相機跟隨；一般控制器掛附當幀停止位移，避免對已停用的 CharacterController 呼叫 Move。
- **漂流**：三個物件 Id 1／2／3 共用 DriftCollection，每個四個獨立 waypoint，由北往南漂流後回上游，週期 43／48／53 秒。E／技能／觸控沿既有 Interactable 路由；Nearest 先檢查 ClimbZone／DriftPickup.CanInteract，避免水平距離假提示。成功拾取才更新位元與隱藏，三件全收集才開 Act3 推論題。存檔恢復會重建共用收集紀錄，恢復時不重複觸發推論。
- **美術**：只複製正式 29 PNG＋atlas_layout.json，未搬動 staging，未匯入 raw／tilecheck／contact。安裝建立兩個 Panoramic 天空材質，白天天空套用場景，返鄉時換灰冷天空。五套地面含低強度法線，Terrain 分區／步道／山崖／村落地面各套材質；地形 tile 8m、村落 6m、步道每 4m 重複。已目視五張 tilecheck，村落石板規律重複仍需實景評估。
- **植被**：依 atlas_layout.json 六格 UV 建六個共用交叉卡片 mesh。森林 240＋溪谷 160 卡片，另六段藤蔓；共同雙面 Alpha Clip URP Lit 材質，enableInstancing＋既有 InstancedGroup，草叢不投影、不做 static batching。R30Foliage 的舊風動 shader／調色盤在此專案不存在，因此沿用既有材質 instancing 模式，沒有宣稱已接風動。實際 draw call 尚未量測。
- **HUD**：三種桃子狀態、穀皮、三技能、阮肇頭像與框，均由安裝綁定 A R1 Sprite。保留呼喚冷卻環與現有按鈕事件。
- **人物**：兩個 FBX 複製到 Art/Characters；安裝呼叫 ModelImporter.ExtractTextures、抽取材質並 AddRemap 為 URP Lit，貼圖匯入上限 1024。以 Renderer bounds 校準 1.6m 身高、腳底對齊根原點，有斷言；只停用膠囊 Renderer，保留玩家 CharacterController；R1Setup 移除「刪除夥伴 Collider」的舊行為，既有夥伴膠囊若存在則保留並把中心移到腳底上方。未綁骨，以獨立外觀子物件做 3.5cm 走路彈跳、0.6cm 呼吸，後續由 S R2 CharacterBob 取代。

**上述場景／材質／抽取動作是 R2Setup 將在 CC 觸發時執行的行為；本輪未直接執行，也未手改 Main.unity YAML。**

## 驗證入口的實際判定

R2Setup.Install → Unity EditMode（Liuruan.EditModeTests，失敗或跳過即停止）→ 沿 R1 五幕路線走查。攀藤會等待實際攀至平台並檢查控制還原，不使用 R1 的攀援瞬移；漂流物從 6m 外開始，先觀察移動至少一秒，再透過 CharacterController.Move 逐段追上移動物件後互動，確認三件前不觸發推論、第三件後可作答。其他 R1 段落沿用靠近互動點、翻頁、正確作答與求歸按鈕。

Play 結束檢查五幕進度與期間 error／exception／assert=0。退出 Play 後 R30Capture 寫 `Captures/r2/` 六機位＋人物近景；Play 另存 HUD。八張都必須存在、大小 >1024 bytes 且為本次 Begin 後的新檔，舊截圖不算通過。

預期檔名：r2_a_start.png、r2_b_forest.png、r2_c_gorge.png、r2_d_peach.png、r2_e_village.png、r2_f_overview.png、r2_hud.png、r2_chars.png。

之後設定 **development=false**，執行既有 `Tools/Liuruan/Build/WebGL + Scene Bundle`。只有新 `.bundle-done`、非空 data 檔、每檔嚴格 **<26,214,400 bytes**、全流程捕捉 error=0 才標完成；data 實際檔名／bytes 寫 U-R2-build.md。測試前備份進度與作答紀錄，正常、失敗或手動停 Play 都還原。

## 已真正執行的離線檢查

| 項目 | 本輪結果 | 證據 |
|---|---|---|
| Unity 隨附 Roslyn，專案 Bee 引擎／套件參照 | Runtime、Assembly-CSharp、Editor、EditModeTests 皆 exit 0，0 error／0 warning | U-R2-compile.md |
| Assets 中五支 Core、兩份共用 EditMode 測試，console 反射 runner | **69 passed, 0 failed**；.NET SDK 10.0.402、C# 9、warnings as errors、零 NuGet | U-R2-core-tests.md |
| 複製完整性 | 29 PNG＋2 FBX 與 staging 逐位元組一致，圖集六格 UV 合法 | U-R2-assets.md |
| diff 格式 | git diff --check（限 Assets／ProjectSettings／U-R2 報告）通過 | 本輪終端輸出 |

Roslyn 檢查程式與回應檔在 /tmp/liuruan-r2-compile.py、/tmp/liuruan-r2-compile/；離線 Core runner 在 /tmp/liuruan-r2-tests/，無需網路。它們是暫存驗證工具，不是 Unity 產物。

## 階段紀錄與未完成項目

- U-R2-install.md／tests.md／play.md／captures.md／build.md 目前明確寫「待 CC 執行」，相應 Editor 階段成功後才覆寫結果。中途失敗寫 U-R2-failure.md；Bridge 收命令寫 U-R2-bridge.md。
- **本輪新截圖 0／8；本輪 WebGL 建置 0 次；本輪 data bytes 未量測。** 既有 R1 Build 不算 R2 證據。
- 尚未驗證 ModelImporter 在實際 FBX 上的貼圖抽取與 remap、天空球面、材質壓縮／法線、地圖疊層、上下端碰撞、NavMesh、手機觸控、GPU instancing draw call、瀏覽器載入與包體；若抽取／資產／流程不符會停止，須以 CC 執行結果處理。
- 未開 UnityMCP／batchmode／第二個 Editor；未部署、未 push；未改其他線目錄、Data 或 ProjectSettings。根目錄 RTK.md 不存在。Obsidian 位於指定寫入範圍外，未另存。

## 檔案清單

下列檔案及新增資產／資料夾對應 .meta；新 metadata 先保留 GUID，Texture／Model importer 的實際設定由 Unity Refresh／R2Setup 補齊。

- `Assets/_Project/Editor/R1Bridge.cs`
- `Assets/_Project/Editor/R1Setup.cs`
- `Assets/_Project/Editor/R30Capture.cs`
- `Assets/_Project/Editor/R2Setup.cs`
- `Assets/_Project/Editor/R2Validation.cs`
- `Assets/_Project/Scripts/GameFlow.cs`
- `Assets/_Project/Scripts/HudUI.cs`
- `Assets/_Project/Scripts/Interactable.cs`
- `Assets/_Project/Scripts/PlayerController.cs`
- `Assets/_Project/Scripts/R2CharacterMotion.cs`
- `Assets/_Project/Scripts/Systems/ClimbCore.cs`
- `Assets/_Project/Scripts/Systems/ClimbZone.cs`
- `Assets/_Project/Scripts/Systems/DriftCollection.cs`
- `Assets/_Project/Scripts/Systems/DriftCore.cs`
- `Assets/_Project/Scripts/Systems/DriftPickup.cs`
- `Assets/_Project/Scripts/Systems/MinimapCam.cs`
- `Assets/_Project/Scripts/Systems/MinimapFogCore.cs`
- `Assets/_Project/Tests/EditMode/R1CoreTests.cs`
- `Assets/_Project/Art/Characters/liuchen.fbx`
- `Assets/_Project/Art/Characters/ruanzhao.fbx`
- `Assets/_Project/Art/R1/foliage/atlas_layout.json`
- `Assets/_Project/Art/R1/foliage/bamboo.png`
- `Assets/_Project/Art/R1/foliage/fern.png`
- `Assets/_Project/Art/R1/foliage/foliage_atlas.png`
- `Assets/_Project/Art/R1/foliage/hanging_vine.png`
- `Assets/_Project/Art/R1/foliage/shrub.png`
- `Assets/_Project/Art/R1/foliage/tall_grass.png`
- `Assets/_Project/Art/R1/foliage/wildflowers.png`
- `Assets/_Project/Art/R1/ground/dirt_path.png`
- `Assets/_Project/Art/R1/ground/dirt_path_n.png`
- `Assets/_Project/Art/R1/ground/grass.png`
- `Assets/_Project/Art/R1/ground/grass_n.png`
- `Assets/_Project/Art/R1/ground/moss_rock.png`
- `Assets/_Project/Art/R1/ground/moss_rock_n.png`
- `Assets/_Project/Art/R1/ground/riverbed_pebble.png`
- `Assets/_Project/Art/R1/ground/riverbed_pebble_n.png`
- `Assets/_Project/Art/R1/ground/village_stone.png`
- `Assets/_Project/Art/R1/ground/village_stone_n.png`
- `Assets/_Project/Art/R1/sky/sky_day.png`
- `Assets/_Project/Art/R1/sky/sky_return.png`
- `Assets/_Project/Art/R1/ui/bark.png`
- `Assets/_Project/Art/R1/ui/minimap_frame.png`
- `Assets/_Project/Art/R1/ui/peach_empty.png`
- `Assets/_Project/Art/R1/ui/peach_full.png`
- `Assets/_Project/Art/R1/ui/peach_half.png`
- `Assets/_Project/Art/R1/ui/portrait_frame.png`
- `Assets/_Project/Art/R1/ui/portrait_ruanzhao.png`
- `Assets/_Project/Art/R1/ui/skill_bark.png`
- `Assets/_Project/Art/R1/ui/skill_call.png`
- `Assets/_Project/Art/R1/ui/skill_water.png`

交件報告及離線／階段紀錄：`logs/U-R2*.md`。
