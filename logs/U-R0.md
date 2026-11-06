# U 線 R0 交件報告

日期：2026-11-07。狀態：**骨架與灰盒產生器已交件；Unity batchmode 場景生成驗收待 CC 執行。**

## 完成內容

- 從指定來源唯讀複製 Packages、完整 ProjectSettings、URP／Renderer／Volume、Noto Serif TC 與 TMP 必要資源、WebGL jslib 與模板；複製資產保留原 `.meta` GUID。
- productName 改為「天台山・流杯記」，companyName 保留；Android、Standalone、iPhone applicationIdentifier 均改為 `tw.naicheng.liuruan`。命名空間、存檔鍵、橋接函式與模板改為 Liuruan。
- 共用腳本全部帶入；額外保留 TouchControls、SpeechBridge、Strings、AudioManager、ProximitySwap 等必要依賴。拆掉禁用玩法的呼叫，未複製 EggMinigame、LaughMatch、BanquetGate、Hotfix1、Performances、Quest、EpilogueDirector、PortraitReadingLayout。
- 清除前作專屬中文文案。保留通用線索閱讀、分頁、選擇題、計分、觸控、鏡頭碰撞與畫質機制。未掛 ClueManager，避免空題庫啟動錯誤；本輪不整合 C 線題庫或 S 線新系統。
- Editor 工具保留 FontSubset（含 RebuildNoDialog）、R30Lighting、R30Foliage、R30Capture、SceneBundles；選單改為 Tools/Liuruan。R30Capture 改成本作六個機位，Lighting 使用本作三區探針範圍。
- 補齊來源 URP 管線實際所在的 Assets/Settings。移除複製 Profile 內 9 個無來源的測試／自訂元件，PC Renderer 舊探針除錯引用與 Mobile 一致清空，避免攜入失效依賴。
- 六支指定部署腳本已移植；站名改為 liuruan-tiantai，NETLIFY_SITE_ID 無預設值且必填，移除 itch，附上原外部 skill 中的 patch_index.py／make_headers.py，避免依賴個人家目錄。

## 灰盒產生器

入口：`Assets/_Project/Editor/LiuruanGreybox.cs` 的全域 static 方法 `LiuruanGreybox.Build()`。

執行後預期產物：

- Boot：黑底 Camera＋BootLoader；Main：240 × 240 公尺 Terrain，中央平坦通路、兩側低丘、溪床。
- Forest（南）：蜿蜒北上小徑、圓柱樹幹＋球形樹冠、起點村口與兩間灰盒房屋。
- Gorge（中）：兩側長方塊山壁、藍色水面、靜態箭頭標示北往南水流、西岸通路、崖頂桃樹。
- Village（北）：廣場與方塊房身＋楔形屋頂、告示板灰盒。
- 16 個 Empty：Act1_Start、Act1_Bark_1～5、Act2_ClimbBase、Act2_PeachTree、Act3_Drift_1～3、Act3_Wade、Act4_Women、Act4_House、Act4_Board、Act5_Return。歸鄉與起點座標同為 `(0, 0, -108)`。
- Player：劉晨膠囊、CharacterController、PlayerController；第三人稱鏡頭距離 3.6 公尺、pivot 高度 1.2 公尺、視角 55°。參考已實際檢視 staging/ref/ref_14.jpg，尚未驗證實際畫面。
- Companion：膠囊、無行為腳本。暖色 Directional Light 仰角 35°；沿用 Volume Profile。
- EditorBuildSettings：Boot、Main。重建靜態 TMP 字集；保留字型 GUID。
- Build 內含場景自我檢查：三區各一、幕點數為 16、起終點重合、玩家與主攝影機存在。重跑更新相同資產；會覆寫這兩個灰盒場景，後續手動編輯須先另存。

## 檔案清單

完整逐檔清單（含 `.meta`、複製／改寫／新增分類）見 `docs/U-R0-files.md`，共 233 筆。本輪交付 README.md、.gitignore、離線檢查腳本及兩份驗證紀錄。

## 已執行的驗證

| 檢查 | 實際結果 |
|---|---|
| Unity 6000.3.27f1 隨附 Roslyn，Runtime 與 Editor 分開預編譯 | Runtime 19 支、Editor 6 支，皆 exit 0／0 error；Runtime 12 個 CS0649（序列化／JSON 欄位由引擎設定），Editor 0 warning |
| Runtime 的 UNITY_WEBGL 分支 | 已包含於上述預編譯；未啟動 Unity |
| Python 離線測試 | 3 tests，全部通過：搬移／GUID、HTML 補丁冪等、WASM 位元組保留與錯誤標頭拒絕 |
| Shell 與 Python 語法 | bash -n 兩支 Shell；所有 Python 腳本 compile() 通過 |
| `rg -ri liulaolao Assets scripts` | 0 筆，exit 1（無符合項目） |
| 序列化資產與 ProjectSettings GUID 依賴掃描 | 對照同版本來源 PackageCache：0 個未解析的非內建 GUID |
| 未設定 NETLIFY_SITE_ID | 腳本在任何上傳前停止；未呼叫 Netlify／Cloudflare CLI |
| 禁用內容與產物 | 未複製前作 Art／Scenes／Data、Library／Build／Deploy／備份；未修改來源專案，未推送或部署 |

預編譯參考同一 Unity 安裝與來源相同版本套件的參考組件，僅屬 C# 靜態驗證，**不代表新專案已完成 Package Manager 匯入或 Unity 場景生成**。原始輸出：`logs/U-R0-roslyn.log`、`logs/U-R0-checks.log`。

## 尚未執行與風險

- 依任務限制，本輪沒有啟動 Unity Editor 或 Unity batchmode。**Boot.unity／Main.unity 目前尚未生成**，沒有宣稱 Unity 驗收完成，也沒有產生假的 unity-R0.log。
- CC 下一步執行：

```sh
Unity -batchmode -quit -projectPath /Volumes/hk6429/projects/liuruan-tiantai -executeMethod LiuruanGreybox.Build -logFile logs/unity-R0.log
```

- 須由該次執行確認新專案實際編譯、套件匯入、場景儲存、Terrain／字型重建；之後再開場景確認鏡頭構圖、可走性、屋頂與水面。未做 Play、WebGL build 或真機測試。
- 本輪只是定位與灰盒，沒有攀爬、漂流、五幕觸發邏輯、跟隨、季節切換及新版 HUD。水流用靜態箭頭表示。
- R30Lighting 的天空 HDR 與舊 static 清單未帶入，缺件時保留現狀；R30Foliage 的風動 shader 與調色盤尚無本作資產，工具會回報缺件而不套用。本輪未複製禁用的前作美術來滿足它們。
- 未部署、未推 GitHub／Vercel／itch。NETLIFY_SITE_ID 留待 CC 設定。
- Obsidian Vault 位於目前允許寫入範圍之外，本輪未同步；報告已保存在本專案 logs/U-R0.md。
