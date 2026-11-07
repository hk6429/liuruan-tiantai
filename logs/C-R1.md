# C 線 R1 交件報告

日期：2026-11-07。狀態：底本整理、題庫與比較資料已落盤，聚焦檢查通過；Unity 匯入、畫面及五幕觸發尚待 U／S 線接入驗收。

## 做了什麼

- 讀取 AGENTS.md、plan 第一／三節、C-R0 報告、sources 全部正文與證據，並只讀 ClueManager、ScrollUI、QuizStats 的資料與使用方式。根目錄 RTK.md 不存在，未假稱讀過。未操作 Unity。
- 改用《太平御覽》卷四十一引《幽明錄》，七段一段一行；廣記〈天台二女〉（出《神仙記》）改作對校。R0 五份文件與四份 evidence 完整另存 sources/_R0_guangji/。
- 本輪重新瀏覽御覽、通用《幽明錄》電子輯文與〈桃花源記〉，來源網址與修訂號均記在 provenance。未以搜尋摘要替代古文。
- 原文四處字詞調整全部記在 variants：敕雲→敕云、交幷→交並、全异→全異、遂留半年→遂停半年。最後一項是主線用語決定，**不當作已證實誤字**。保留筒瓦、十餘日、胡麻糝、此處去人徑不遠、百鳥鳴呼、問得七世孫等 Y 字句，不混入長本細節。
- 留宿整段以〔略〕處理；現行正文與結局用「……」標省略，僅以「遂停半年」銜接。省去內容不翻譯、不出題，結局 feedback 也不提。
- 五幕各一張卷軸，加第三幕三物推論題與一張結局全文推論題，共7筆。每筆 act、4選項、0起算 answer、2條 wrongHints 齊全。國中程度，誘答分別針對不能／不肯、敘事順序、證據與斷言、思鄉動機、七世固定換算等誤讀。
- 建立八面向比較，逐格引文並區分文本明說與閱讀詮釋；Markdown 與 compare.json 文字一致。附〈桃花源記〉主文及來源，不假稱本篇寫了重返失敗，也不假稱漁人返鄉已過數代。
- 建立五幕旁白，每幕2–3句，每句含標點最多25字；只敘述正文已有事情，無人物新台詞。

## 檔案清單

| 檔案 | bytes |
|---|---:|
| `sources/liuruan_tw.txt` | 1345 |
| `sources/variants.md` | 4787 |
| `sources/provenance.md` | 4775 |
| `sources/evidence/taohuayuan.txt` | 1199 |
| `docs/content/acts.md` | 7017 |
| `docs/content/glossary.md` | 4064 |
| `docs/content/compare_taohuayuan.md` | 4546 |
| `Assets/_Project/Data/clues.json` | 10101 |
| `Assets/_Project/Data/compare.json` | 2631 |
| `Assets/_Project/Data/narration.json` | 868 |
| `tools/check_clues.py` | 6717 |
| `sources/_R0_guangji/` | 9份典藏檔案（五份文件、四份證據） |
| `logs/C-R1.md` | 本交件報告 |

tools/check_clues.py 與本 logs 單檔是任務明訂的寫入路徑例外；Unity 內只寫 Assets/_Project/Data/。未改其他線 C#、場景、設定、計畫或既有紀錄；未提交 Git、未推 GitHub、未部署。

正文 SHA-256：`6d9fdc4a98517340ba84a5fa787d7a8d935be670ddb3ce0382ceeab6369a3fe9`。

## 驗證與實際輸出

執行 `python3 tools/check_clues.py`，結束碼0：

```text
PASS 底本：7段；與御覽證據字序一致（4處校改＋指定刪節）
PASS clues.json：JSON解析、7題格式、act、4選項、答案索引、2提示、依賴
PASS original：7/7 嚴格連續子字串（不去空白、不去標點）
PASS 結局：唯一isEnding、刪節全文、1處……、未含留宿段
PASS compare.json：8列；每格原文引句相符；Markdown與JSON一致
PASS narration.json：5幕、每幕≤3句、每句含標點≤25字
PASS acts.md／glossary.md：原文與例句逐字相符
PASS 檢查器自檢：成功攔截不屬於正文的original
```

另以記憶體內反例測試確認6/6攔截：原文錯字、答案超界、重複選項、循環依賴、缺少提示、結局缺全文。檢查器另內建一個不改檔的錯誤 original 自檢，未僅測正常資料。

R0 典藏驗證：五份文件均與本輪起始 HEAD 逐位元組相同，四份證據與原 evidence 複本相同。`git diff --check` 對本輪指定檔案通過。JSON 驗證使用 Python 標準函式庫，未新增套件。

手動內容複核：四選項各有單一正解；三件線索共同支持附近有人，不能斷言已見女子；半年／七世為時間落差推論，未定出固定年數換算；每幕旁白均能回指本文；比較表每格都有引句，未記載處明言未交代。

## 沒做到什麼、介接風險與下一步

- 未核古刻影像與印刷頁碼；電子本「百鳥鳴呼」保留，校本品質仍以電子轉錄為界。御覽固定修訂連結開啟為 Cache miss，當前頁完整故事讀取成功，兩者未混稱。
- 現行 ClueData 無 act，JsonUtility 會忽略額外欄位；須由 S／U 線另行接上 act。未改 C# 來冒充完成介接。
- id 契約：0第一幕、1第二幕、2第三幕卷軸、3第三幕加題、4第四幕、5第五幕、6結局。id3 requires=[2]；結局由現行 ClueManager 等六張非結局卷軸全收齊後開放。**須確保第三幕有id3的觸發點**，否則結局會等不到它。
- 本輪不填頂層 infer，第三幕加題不會延到結局；結局自身即是七世推論題。明填 endingText 防止使用舊預設來源文。
- compare.json 契約為 rows:[{aspect,liuruan,taohua}]；narration.json 為 acts:[{act,sentences}]。兩者尚未接進結局面板或幕次系統。U／S 線須確認載入引用、字型字表、換行捲動與觸發；七段結局文字尤其需要實際檢查可讀性。
- 未執行 Unity、建置、瀏覽器或手機測試；資料檢查通過不等於遊戲可玩驗收。未改 plan 內仍舊的銅瓦、十三日、逆流與廣記底本敘述；本輪決定與內容契約已明列，主線須另同步各線。
- 未寫入 Obsidian／LLM 知識庫：Vault 不在目前可寫範圍，也超出本輪指定寫入邊界；不宣稱已存。交件報告及所有正式內容均留在本專案。

下一步：U／S 依上述 id 與資料契約接入，確認id3可取得、act可用、結局全文可捲動、比較表可讀，以及刪節內容未被 evidence／R0典藏誤載。
