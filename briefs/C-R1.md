# C 線 R1：改底本為《太平御覽》引《幽明錄》＋題庫

先讀 AGENTS.md、docs/plan.md 第一、三節、你上一輪的 logs/C-R0.md 與 sources/*。可寫：sources/、docs/content/、Assets/_Project/Data/（只有這個 Unity 資料夾）。交件報告寫 logs/C-R1.md。

## 決定（主線已拍板）
- **底本改為《太平御覽》卷四十一引《幽明錄》**（你 R0 的 Y 本）。理由：遊戲情節（取穀皮、筒瓦屋、遂停半年、七世孫）全依《幽明錄》系統。太平廣記〈天台二女〉（出《神仙記》）改為對校本，在 variants.md 說明。
- 屋瓦用底本「筒瓦屋」。留宿段照舊整段〔略〕。

## 任務
1. 重做 sources/liuruan_tw.txt（御覽為底本、一段一行、全形標點、台灣教科書慣例）、variants.md、provenance.md；舊版另存 sources/_R0_guangji/。御覽電子本若有明顯誤字，可據他本校改，但每一處都要在 variants.md 記錄「底本作某，據某本改」。
2. 重做 docs/content/acts.md、glossary.md 對應新底本。
3. 新增 Assets/_Project/Data/clues.json：格式照 Assets/_Project/Scripts/ClueManager.cs 的 ClueList／ClueData（只讀程式，不准改）。每筆多一個 int 欄位 act（1–5）。內容：
   - 每幕 1 張原文卷軸（original 逐字取自 liuruan_tw.txt、plain 白話、question 選擇題 4 選項、answer、feedback、wrongHints 2 條）。
   - 第三幕另加推論題：三件漂流物（蕪菁葉、杯、胡麻飯糝）→「此知去人徑不遠」的推論。
   - 結局：isEnding=true，原文全文（留宿段以「……」省略並在 feedback 不提），推論題「為什麼回家已是七世？」。
   - 題目要有誘答力、選項長度相近，國中程度。
4. docs/content/compare_taohuayuan.md：〈劉晨阮肇〉與〈桃花源記〉比較表（入口、異境樣貌、居民、停留、離開方式、回去後、能否重返、作者寓意），每格引原文；〈桃花源記〉原文也要附來源網址。結局面板會用這張表，另出一份 Assets/_Project/Data/compare.json（rows: [{aspect, liuruan, taohua}]）。
5. Assets/_Project/Data/narration.json：五幕的簡短白話旁白（每幕 ≤3 句、每句 ≤25 字），**只能描述原文已有的事**，不加情節、不寫人物台詞。

驗收：clues.json 用 python json 解析通過；每個 original 字串都是 liuruan_tw.txt 的子字串（寫一支 tools/check_clues.py 自動檢查並貼輸出）。
