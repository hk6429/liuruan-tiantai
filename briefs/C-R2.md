# C 線 R2：教師手冊、學習單、上線文案

先讀 AGENTS.md、docs/plan.md、sources/liuruan_tw.txt、sources/variants.md、docs/content/*.md、Assets/_Project/Data/*.json（只讀）。只能寫 docs/teacher/、docs/release/。不開 Unity。原文引用一律逐字照 sources/liuruan_tw.txt；留宿段略過的處理照 AGENTS.md。交件報告寫 docs/teacher/C-R2.md。

1. `docs/teacher/guide.md` 教師手冊：教學目標（對應 108 課綱國-J 學習表現，代號用國-J 不是語-J，不確定的寫「待核對」不要編）、一節課 45 分鐘流程（前 5 導入／25 玩／15 討論）、五幕對照原文段落與提問、推論題與比較表的參考答案、差異化（學習落後／超前）、常見迷思（「仙女」字樣原文未見等）、底本與異文說明（御覽卷41引幽明錄）。
2. `docs/teacher/worksheet.md` 與 `docs/teacher/worksheet.html`（A4 直式可列印、繁中、黑白友善、無外部資源）：學習單——線索推論表、時間母題（仙界半年 vs 人間七世）思考、與〈桃花源記〉比較表空白版、延伸寫作一題。
3. `docs/release/site.md`：網站標題、一句話簡介（≤60 字）、og description、操作說明（電腦：WASD／滑鼠、E 互動、F/R/G 技能；手機：左搖桿＋互動鈕）、出處與授權聲明（原文為古籍公有領域；美術為 AI 生成）。
4. `docs/release/credits.md`：工具與素材來源（Unity、Dzine、codex 生圖、Noto Serif TC OFL 字型等）。
5. 寫 `tools/check_teacher.py`：檢查手冊與學習單中所有「」引號內原文都能在 liuruan_tw.txt 逐字找到（或在 variants.md 列出），輸出 PASS/FAIL 清單，實跑貼報告。
