# C 線 R3 交件報告

日期：2026-11-08。範圍：僅 `docs/content/r3/` 與 `tools/check_r3.py`。未操作 Unity、未改 Assets、未部署；工作區其他線的變更保留原樣。

## 交件清單

| 檔案 | 內容與數量 |
|---|---|
| story_card.json | 3 張開場卡，每張 ≤45 字；最後一張定位玩家與目標 |
| objectives.json | 五幕 18 步，皆含 id、goal、why、where、count |
| hints.json | 同序 18 步 × 3 級 × 2 裝置，共 108 條；每條 ≤20 字 |
| dialogue.json | 五幕共 19 個 scene；每幕起始兩句劉阮白話，含漂流物、二女全部原文台詞、款待、賀婿、半年、鳥聲、求歸、奏樂送行與七世孫 |
| narration.json | 五幕各一條原文關鍵句，接一條 ≤20 字白話；取代舊 sentences 陣列格式 |
| sentences.json | 七段共 72 個短分句；orig、plain、terms；含標點可完整重組底本 |
| glossary_cards.json | glossary.md 全部 40 條，保留原注釋；易誤讀詞另填注音，其餘 reading 為空字串；要讀ㄧㄠ |
| questions_r3.json | 4 題，id=7–10；沿用 clues 容器及題目欄位，三選一、0 起算答案、每題兩條錯答提示 |
| epilogue_guide.json | pages: [{text}]，4 頁，每頁 ≤60 字 |
| tools/check_r3.py | 標準函式庫檢查器，無新增套件；支援 --self-test |
| C-R3.md | 本報告與介接／驗證說明 |

## 版本與教學處理

- 底本以本輪實讀的 `sources/liuruan_tw.txt` 為準，逐句翻譯完整保留現行七段，分句含逗號與說話提示語，避免長句白話超過 20 字。
- 開場依指定寫「十三日」，同卡明示底本作「十餘日」、十三日見異文；其 orig 仍引底本。未把十三日冒充底本原字。
- 第三幕先引底本「此處去人徑不遠。」，另列指定「此知去人徑不遠」，type=original 並附 source、label，明標 W、L 異文。介面須顯示 label，避免兩版本被當成兩次人物發言。
- 第四幕依二女→回家→款待→群女→半年→鳥聲→求歸七步，不依賴舊互動設施。筒瓦屋、度山、百鳥鳴呼均照底本。
- 半年轉場只用旁白原文「遂停半年」。逐句第六段保留既有刪節符號；沒有還原、翻譯或出題涉及省略內容。
- 七世孫兩句均有界線：入山傳聞先由旁白引原文，角色用短白話轉述，並附 orig。原文未直接提供七世孫的引號台詞。
- 「忻怖交並」的喜怕原因在答案回饋明說是依款待、呼姓、賀婿的推論，並非原文明說心理原因。
- 結局不換算固定年數；俗語所在頁就地標「後人俗語」，不設 type=original。

## S／U 線介接契約

JSON 使用 UTF-8、不含 BOM。objectives、hints、dialogue、narration 根節點皆為 acts；sentences 為 paragraphs；glossary 為 terms。story_card 為 cards；epilogue_guide 為 pages；題目沿用 clues。

任務與提示共用的步驟 id：

| 幕 | 步驟順序 |
|---|---|
| 1 | bark（count=5） |
| 2 | climb、peach |
| 3 | leaf、cup、rice、infer、cross_mountain |
| 4 | women、follow、house、guests、half_year、birds、return |
| 5 | village、ask、grandson |

除 bark 外 count=1；它們是遊戲進度單位，不宣稱原文指定五棵樹或一顆桃子。方向只寫可識別地標，沒有虛構方位或座標。

提示對應 l1=30 秒、l2=60 秒、l3=90 秒，計時由 S 線處理。l3 全部由阮肇說；提示檔依指定格式不另加 speaker/type，兩裝置各自具備文字，觸控二級提示使用互動鈕，移動提示使用搖桿。閱讀事件的三級提示採一起閱讀／聽鳥聲，不要求玩家離開事件。

每幕開頭 trigger=act_start:1 至 act_start:5；其餘如下：

| scene id | trigger |
|---|---|
| find_leaf | interact:Act3_Leaf |
| find_cup | interact:Act3_Cup |
| find_rice | interact:Act3_Rice |
| infer_people | after:drift_all |
| meet_women | interact:Act4_Women |
| follow_home | interact:Act4_Home |
| house_meal | interact:Act4_House |
| guests_arrive | after:meal |
| half_year | after:guests |
| hear_birds | after:half_year |
| ask_return | interact:Act4_Return |
| village_changed | interact:Act5_Village |
| ask_villagers | interact:Act5_Villager |
| meet_grandson | interact:Act5_Grandson |

這些是待接線的事件契約，不代表場景已有同名物件。請由事件轉接對應現有物件；對話讀完推進下一步，半年與鳥聲不應要求玩家另外找互動物。follow_home 在門前、house_meal 在屋內款待階段，避免同一門前事件連續重播。所有 scene id 唯一。

## 結局重複題建議

已實讀 `Assets/_Project/Data/clues.json:127`（id=6，題幹在第132行）與 `Assets/_Project/Scripts/Systems/EpilogueCore.cs:26`，以及 staging 同版核心：兩者都考「山中半年、人間多代」的時間落差。

**建議保留 EpilogueCore 的 EndingQuestion，取消 clues id=6 的重複作答與計分。** 核心題要求從文中選對照證據，適合結局讀後推論。保留 id=6 的全文及 isEnding 結局用途，不能直接刪整筆，以免破壞結局觸發。由 S／U 線處理結局題路由，確保只問一次；C 線本輪沒有修改唯讀題庫或 C#。

## 實跑驗證

指令：`python3 tools/check_r3.py --self-test`

結束碼：0。最終內容修改後實跑輸出：

```text
PASS 9 JSON；原文 107 處（異文 1 處）；短文 278 處
PASS 五幕任務／提示對齊；七段逐字重組；40 詞卡；4 題三選一；禁用字與俗語標註
PASS self-test: 12 個錯誤樣本皆攔截；異文來源已核對
```

原文檢查同時涵蓋 type=original 的 text、orig 與 original 欄位；白話檢查包含 plain 欄位、type=plain、任務三欄與全部裝置提示。長度含標點計算。詞卡 meaning 保留既有教學注釋，未套用短台詞的 20 字限制；題幹、選項與回饋也屬題庫欄位而非短對話。

自測攔截：錯引、21 字台詞、46 字故事卡、61 字導讀、四種禁用字、缺詞卡、缺原文分句、任務提示 id 錯置、俗語未標。除此之外，檢查完整七段重組、五幕順序、三選一答案範圍、scene id 唯一與先原文再白話。

## 未做、風險與下一步

- 未開 Unity、未建置、未做桌機／手機遊玩驗收，亦未把內容複製進 Assets。下一步由 S／U 線讀檔、接事件與實測。
- 自動檢查證明字串、長度與基本資料關係；不能證明場景可達、計時正確、異文標籤顯示或結局只計分一次。
- 本輪限寫指定路徑，且 Obsidian Vault 不在可寫範圍，未另存 Vault；本報告留在授權交件目錄。
