# U 線 R2b：Mixamo 角色動畫＋S 線 R2 系統＋道具自動放置

先讀 AGENTS.md、你的 logs/U-R2.md 與 briefs/U-R2.md、staging/scripts/INTEGRATION.md 與 staging/scripts/logs/S-R2.md。寫入範圍同 R2a（Assets/、ProjectSettings/、logs/U-R2*.md）。交件報告 logs/U-R2b.md。執行環境限制同 R2a（你只寫檔，CC 觸發編譯與 bridge 指令）。

## R2a 實跑時 CC 發現並已修的問題（不要改回去）
1. 天空貼圖被匯入成 Cube：R2Setup.ConfigureTextures 已加 `importer.textureShape = TextureImporterShape.Texture2D;`。
2. **人物材質失敗**：staging/models/game/*.fbx 內嵌貼圖檔名無副檔名，Unity 認不出 → R2Setup.Character 丟「材質未正確解出」。這一版**不要再用內嵌貼圖**。

## 要做的事
1. **角色改用 Mixamo 骨架**（CC 已綁好）：
   - 模型＋皮膚：`staging/models/mixamo/<key>/<key>@idle.fbx`（含 skin）；其他 `@walk/@run/@climb`（兩人都有），劉晨另有 `@pickup/@pickfruit`（無 skin，只有動畫）。walk/run/idle/pickup/pickfruit 是 in-place；climb 帶位移。
   - 貼圖改用獨立 PNG：`staging/models/game/<key>_albedo.png`（BaseMap）、`<key>_metalrough.png`（可不用；若用，Smoothness 低）。材質自己建 URP Lit，**不要依賴 FBX 內嵌材質**（用 importer.materialImportMode = None 或 remap 到自建材質）。
   - 複製到 Assets/_Project/Art/Characters/<key>/。全部 ModelImporter 設 **Humanoid**；動畫檔的 avatar 用同角色 idle 檔產生的 avatar（sourceAvatar）。idle/walk/run loopTime=true。
   - 建 AnimatorController：Speed 混合樹（idle 0 → walk 1.5 → run 4），climb 狀態（Bool Climbing），劉晨加 Trigger Pickup／PickFruit。PlayerController 與 CompanionFollow 每幀餵 Speed；ClimbZone 進出攀爬時設 Climbing；漂流物撿起播 Pickup、摘桃播 PickFruit。applyRootMotion=false。
   - 移除 R2a 的 R2CharacterMotion（程序化彈跳）在有 Animator 的角色上的作用，避免疊加。身高校準 1.6m、腳底對齊沿用。
2. **S 線 R2 三個系統**（staging/scripts，照 INTEGRATION.md 複製進 Assets，測試一起進）：
   - SeasonSwap：第四幕「遂停半年」旁白時播 PlayHalfYear（6 秒四季輪轉、最後春天鳥鳴事件 → 播一個鳥鳴音效，沒有音效就先用 Debug log＋HUD 小字「百鳥啼鳴」）；第五幕求歸後 SwapToHome（仙村根物件關、歸鄉村開、天空換 sky_return）。
   - EpilogueDirector：結局面板改用它的資料模型——全文、推論題「為什麼是七世孫」、與〈桃花源記〉比較表、成績卡。
   - CharacterBob：只掛在**沒有 Animator 的**人物或道具上（目前可能用不到，接好即可）。
3. **道具模型自動放置**：R2Setup.Install 檢查 `staging/models/props/<name>.fbx`＋`<name>_albedo.png`，存在才複製進 Assets/_Project/Art/Props/ 並放到場景，不存在就跳過並寫進報告（CC 正在用 Dzine 產生，可能比你先或後完成）。名稱與放置：
   - house_tongwa：仙村 3–4 棟（取代灰盒房子，Village/House_* 換外觀，保留碰撞與名稱）
   - pavilion_luozhang：仙村中央 1 座
   - peach_tree：Act2_PeachTree 位置 1 棵＋崖頂周邊 3 棵
   - chu_tree：Act1_Bark_1..5 五個位置（取代灰盒楮樹）
   - drift_items：三件漂流物各自的外觀（若是一個模型含三件，就整個縮小掛在每件漂流物上即可）
   - home_house：歸鄉村 3–4 棟
   - vine_cliff：Act2 攀爬崖面
   - village_board：Act4_Board
   每個道具依 bounds 縮放到合理尺寸（房 5m 高、亭 5m、桃樹 4m、楮樹 5m、告示板 2.2m、崖依 ClimbZone 高度），腳底貼地。材質 URP Lit＋albedo。
4. R2Validation 改走 R2b：所有路線仍要 PASS、console error=0；Captures/r2/ 重拍；另拍 r2_chars.png（兩人近景、看得到走路姿勢）；最後 WebGL **release** build（development=false），寫 logs/U-R2-build.md（data 大小、wasm／data 檔名、壓縮格式）。build 的 compression 用專案現設定（Brotli），確認 Build/WebGL/Build/ 產出 .unityweb 或 .br 檔，scripts/deploy.sh 需要 `*.unityweb`。
5. 語法層編譯檢查同 R2a。報告寫明 CC 要寫的 bridge 指令字串（請用 `r2-r2b-001`）。
