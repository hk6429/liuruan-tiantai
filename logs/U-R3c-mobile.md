# U-R3c 手機版面修正＋部署報告（v0.3.2，2026-11-08）

## 問題（Captures/r3/r3_mobile.png、r3_act1.png 舊圖）
- 直式：回本幕起點／跳過本幕壓到任務欄；小地圖壓互動鈕；頭像壓搖桿；底部操作提示截斷。
- 桌機橫式：右上線索冊／字級／畫質列被技能鈕與任務欄蓋住。
- 故事卡／對話期間任務欄仍露出（舊 bug）。

## 做了什麼
1. `HudUI.cs`：新增 `Layout()`，每幀依「觸控／直式」三種模式重排 JourneyHUD（技能鈕、回本幕／跳過、任務欄、鳥群提示、操作列、互動提示、歸返鈕、小地圖、頭像／體力框）；CanvasScaler 參考解析度直式改 720×1280；任務欄 `!busy` 時才顯示。
2. `ScrollUI.cs`：直式頂列（線索冊／字級／畫質／朗讀／成績）錨點 y 0.775–0.85 → 0.73–0.79、Toast 0.65–0.76 → 0.58–0.69；觸控橫式整排右縮到 x ≥ 0.50（成績 0.50、線索冊 0.60、字級 0.70、畫質 0.81、朗讀 0.91）。
3. 新工具 `Assets/_Project/Editor/HudLayoutCapture.cs`（選單 Tools/Liuruan/R3/HUD Layout Capture）：進 Play、推進開場旁白到 HUD 不忙，連拍桌機橫式／觸控橫式／觸控直式三張。踩坑：EditorApplication.update 在本幀 Update 之前跑而 CaptureScreenshot 幀尾才拍，所以 ForceTouch／GameView 尺寸切換都要放到「拍完的下一階段」；離開 Play 前要等 PNG 落盤。
4. 附帶：`R3Validation.cs` 截圖 superSize 3（另一 session 留下，一併提交）；Unity 重新序列化的 controller／字型資產／Main.unity 一併提交以使 repo 與本次建置一致。

## 怎麼驗證的
- `Captures/r3/r3c_desk.png`（1920×1080 桌機）、`r3c_touch_land.png`（1920×1080 觸控）、`r3c_touch_port.png`（1080×1920 觸控直式）目視：三種版面均無元件重疊、底部提示完整。
- tools/check_clues.py、check_r3.py、check_teacher.py 全 PASS；scripts/test_r0_delivery.py 3/3 OK。
- 部署與回讀：見下。

## 部署
- 建置：`Tools/Liuruan/Build/WebGL + Scene Bundle`，loader e3cb3684 → c80fe815，.bundle-done 20:09 更新，console 0 error。
- `NETLIFY_SITE_ID=7238bb04-… scripts/deploy.sh 0.3.2` → Netlify（deploy 6af066f0）＋Cloudflare Pages（29ae4d1f，branch main）。
- `scripts/verify.sh`：兩站各 13 項全 OK（.unityweb br md5、loader identity md5、scene/main.0、parts.json、index 引用、wasm 標頭）。
- 線上：https://liuruan-tiantai.pages.dev（對外推薦）、https://liuruan-tiantai.netlify.app

## 沒做到
- 未用真機（iPhone／iPad／Android）實測，只有 Editor GameView 固定尺寸截圖；真機瀏覽器網址列、安全區（瀏海）未驗。
- 靜音／朗讀鈕在 Editor 不顯示（SpeechBridge 不支援），觸控橫式右端 0.91–0.99 的位置只靠座標推算。
