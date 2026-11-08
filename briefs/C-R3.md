# C 線 R3：故事卡、任務、提示、對話、逐句白話、注釋、題目

先讀 AGENTS.md（**R3 起可加簡短白話提示對話**，規則在鐵律）、docs/r3-diagnosis.md（四專家診斷）、docs/plan.md、sources/liuruan_tw.txt、sources/variants.md、docs/content/acts.md、glossary.md、compare_taohuayuan.md、Assets/_Project/Data/*.json（只讀）。只能寫 docs/content/r3/ 與 tools/check_r3.py。不開 Unity。交件報告 docs/content/r3/C-R3.md。

原則：
- 原文引用一律逐字照 liuruan_tw.txt（type="original"）。
- 白話（type="plain"）每句 ≤20 字，給沒讀過故事的國中生，不得新增原文沒有的情節；留宿段照 AGENTS.md 略過，只用「遂停半年」。
- 地名器物照原文：筒瓦屋（不是銅）、度山出一大溪（不是涉水）、百鳥鳴呼（不是啼鳴）。「洞中方七日，世上已千年」不是本篇原文，出現時要標明「後人俗語」。
- 遊戲裡**沒有告示板**了（R3b 會移除），第四幕改由二女與屋內事件推進。

## 要交的檔案（JSON 用 UTF-8，格式照寫，S 線會照這個讀）
1. `story_card.json`：開場故事卡 3–4 張（每張 ≤45 字，含人物、時代「漢明帝永平五年」、剡縣、入天台山取穀皮、迷路十三日糧盡；最後一張「你是劉晨，阮肇在你身旁」＋目標）。格式 `{"cards":[{"text":"…","orig":"可選，原文句"}]}`。
2. `objectives.json`：五幕每一步 `{goal, why, where, count}`（格式見 briefs/S-R3.md 第 3 點）。第四幕步驟改成：見溪邊二女 → 跟二女回家 → 屋內款待 → 群女來賀 → （遂停半年）→ 聽見百鳥鳴呼 → 求歸。第五幕：回到村口 → 找人問 → 見七世孫。
3. `hints.json`：每幕每步 30/60/90 秒三級提示，desktop 與 touch 各一版（touch 用「搖桿」「互動鈕」）。`{"acts":[{"act":1,"steps":[{"id":"bark","l1":{"desktop":"…","touch":"…"},"l2":{…},"l3":{…}}]}]}`。l3 是阮肇說的話（例如「跟我來，這邊！」）。
4. `dialogue.json`（格式見 S-R3 第 2 點）：
   - 每幕開頭 1–2 句劉阮白話對話（帶出目標）。
   - 第三幕撿到漂流物時的原文半句＋推論（「此知去人徑不遠」逐字）。
   - 第四幕：二女原文台詞全部（呼姓、「劉、阮二郎捉向所失流杯來」、「來何晚耶」、款待「敕云」、群女「賀汝婿來」、求歸時女子的原文回應），劉阮的白話反應（例如「她們怎麼認得我們？」對應「晨、肇既不識之」）、奏樂送行。
   - 第五幕：七世孫——原文是「問得七世孫，傳聞上世入山，迷不得歸」，七世孫台詞用白話轉述並附原文。
   每個 scene 給 trigger（例如 `act_start:1`、`interact:Act4_Women`、`interact:Act4_House`、`after:half_year`、`interact:Act5_Grandson`）。
5. `narration.json`（取代現行版）：每幕開頭先顯示**原文關鍵句**再接白話一句。格式 `{"acts":[{"act":1,"lines":[{"text":"…","type":"original"},{"text":"…","type":"plain"}]}]}`。
6. `sentences.json`：全文 7 段逐句切開，每句 orig＋plain＋terms（格式見 S-R3 第 6 點）。
7. `glossary_cards.json`：把 glossary.md 40 條轉成 `{term, reading, meaning}`；「要」讀ㄧㄠ；sentences 用到的 terms 都要在這裡。
8. `questions_r3.json`：補 4 題理解型三選一（一詞多義「去」、二女呼名暗示什麼、「忻怖交並」喜什麼怕什麼、「瓊實」前後呼應），格式沿用 clues.json 題目欄位；並寫明 clues.json:127 與 EpilogueCore 推論題重複，建議保留哪一題。
9. `epilogue_guide.json`：結局導讀 3–4 頁（半年 vs 七世、為什麼是七世孫、與〈桃花源記〉一句比較、俗語標註），每頁 ≤60 字。
10. `tools/check_r3.py`：檢查所有 type=original 的字串都在 liuruan_tw.txt 逐字找到（或在 variants.md）、plain ≤20 字（故事卡／導讀依各自上限）、禁用字（銅瓦、涉水、啼鳴、告示板）不出現、sentences 的 terms 都在 glossary_cards。實跑輸出貼報告。
