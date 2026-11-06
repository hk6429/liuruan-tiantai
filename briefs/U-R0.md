# U 線 R0：開專案骨架＋灰盒三區（不開 Unity Editor，CC 會用 batchmode 驗編譯）

工作目錄 /Volumes/hk6429/projects/liuruan-tiantai。先讀 AGENTS.md、docs/plan.md 第三、四、六節。

## 任務
1. 從劉姥姥 /Volumes/hk6429/projects/liulaolao-garden 複製 Unity 骨架到本專案：
   - Packages/manifest.json、Packages/packages-lock.json、ProjectSettings/ 全部（productName 改「天台山・流杯記」，companyName 保留，bundle id 改 tw.naicheng.liuruan）。
   - Assets/_Project/Settings（URP asset／Renderer／Volume profile）、Assets/_Project/Fonts（TMP 靜態字型資產與原始 Noto Serif TC）、Assets/Plugins/WebGL 下的 jslib、Assets/WebGLTemplates（若有）。
   - 通用腳本（Assets/_Project/Scripts）：BootLoader、PlayerController、CamOcclusion、CharacterAnimDriver、Interactable、ClueManager、ClueMarker、QuizStats、QualityToggle、RenderGovernor、DistanceCull、InstancedGroup、FindGlow、ScrollUI 以及它們編譯所需的依賴（TouchControls、SpeechBridge 等）。**不要**複製劉姥姥專屬玩法（EggMinigame、LaughMatch、BanquetGate、Hotfix1*、Performances/、Quest/、EpilogueDirector、PortraitReadingLayout）；若通用腳本引用到它們，刪掉該引用或改寫成最小 stub。
   - Editor 工具：FontSubset、R30Lighting、R30Foliage、R30Capture、以及「Build/WebGL + Scene Bundle」選單所在的檔案。選單路徑 Tools/Liulaolao/ 一律改 Tools/Liuruan/。
   - scripts/deploy.sh、verify.sh、prepare_wasm_delivery.py、patch_mobile_entry.py、patch_loading_recovery.py、compact_webgl_data.py：站名改 liuruan-tiantai，Netlify site id 改成環境變數 NETLIFY_SITE_ID（先留空，CC 之後填），移除 itch 那一步。
   - 一律帶 .meta 一起複製；**不要**複製 Library/、Build/、Deploy/、_backup_*、Art/、Scenes/、Data/ 的劉姥姥內容。
2. namespace / 字串：Liulaolao → Liuruan；腳本內劉姥姥專屬中文字串刪掉。
3. 寫 Assets/_Project/Editor/LiuruanGreybox.cs，提供 static 方法 `LiuruanGreybox.Build()`（給 batchmode -executeMethod 用），產生：
   - Assets/_Project/Scenes/Boot.unity（照劉姥姥 Boot＋BootLoader 結構）與 Main.unity。
   - Main：一塊 Terrain 約 240×240m，分三區並用 Empty 標記：Forest（南，森林小徑蜿蜒北上）、Gorge（中，山壁＋溪澗，溪水由北往南流）、Village（北，平地村落）。灰盒用簡單 primitive：樹＝圓柱＋球、山壁＝拉長方塊、溪＝藍平面、房屋＝方塊＋楔形屋頂。
   - 五幕的觸發點 Empty：Act1_Start、Act1_Bark×5、Act2_ClimbBase、Act2_PeachTree、Act3_Drift×3、Act3_Wade、Act4_Women、Act4_House、Act4_Board、Act5_Return（Act5 與 Act1_Start 同一處村口，計劃書第三節）。
   - Player（劉晨膠囊＋PlayerController＋第三人稱攝影機，攝影機低而貼近，參考 staging/ref）、Companion（阮肇膠囊，先不掛腳本）。
   - Directional Light 暖色斜射（約 35° 仰角）。
   - EditorBuildSettings：Boot、Main。
4. docs/U-R0-files.md 列出複製與改寫的每支檔案。

## 驗收條件（CC 會親自跑）
- `Unity -batchmode -quit -projectPath /Volumes/hk6429/projects/liuruan-tiantai -executeMethod LiuruanGreybox.Build -logFile logs/unity-R0.log` 0 編譯錯誤、兩個場景存在。
- grep -ri liulaolao Assets scripts 只剩註解或無。
交件報告寫 logs/U-R0.md。
