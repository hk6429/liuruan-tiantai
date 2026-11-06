# 天台山・流杯記（liuruan-tiantai）— Codex 接手說明

Unity 6000.3.27f1 URP WebGL 國文遊戲，題材《幽明錄》〈劉晨阮肇入天台〉。回覆一律繁體中文（台灣）。
計劃書：docs/plan.md（五幕：迷山／攀桃／流杯／仙村／歸鄉；四線 U/S/A/C）。
參考畫面：staging/ref/*.jpg（UE5 Q 版小勇者影片截圖，風格與 HUD 版面照它）。
骨架來源：/Volumes/hk6429/projects/liulaolao-garden（劉姥姥，只讀，絕不修改該專案）。

## 鐵律
- 只寫自己這條線被指定的資料夾，不碰別線的檔案。
- 只有 U 線可以操作 Unity（UnityMCP 或 batchmode），其他線不得開 Unity。
- 台詞與原文只用 sources/liuruan_tw.txt 逐字，不准自編情節或台詞；自撰限簡短白話旁白。
- 女子留宿段（「各就一帳宿」起至「遂停半年」前）整段略過，只用旁白「遂停半年」帶過。
- 不做戰鬥。不推 GitHub、不推 Vercel。生成產物不放 iCloud。
- 刪檔用 /bin/rm；Blender 只能 headless（-b --factory-startup）。
- 完成時在 logs/<線>-R<輪>.md 寫交件報告：做了什麼、檔案清單、怎麼驗證的、沒做到什麼。不得謊報。
