# 天台山・流杯記｜製作名錄與素材來源

本表區分專案現有素材與規劃工具，不把規劃名單當作全部已匯入成品的證明。C 線本輪未開 Unity、未製作或重新匯入美術。

| 項目 | 工具／來源 | 用途與本輪可確認範圍 |
|---|---|---|
| 遊戲引擎 | [Unity](https://unity.com/) 6000.3.27f1、URP、WebGL | 版本依專案 AGENTS.md；建置與部署由 U 線負責 |
| 程式與內容協作 | [OpenAI Codex](https://openai.com/codex/) | 專案開發、內容整理與檢查工具；人工校對文本與教學推論 |
| AI 概念美術 | Codex 生圖（專案規劃標示 codex-image2） | 提示詞紀錄位於 `staging/art/concept/prompts.md`；不以此推定每個成品的生成模型或完成狀態 |
| 圖轉 3D | [Dzine](https://www.dzine.ai/) Normal | 計畫中的概念圖轉模型流程；各件生成及實際匯入狀態待 A／U 線素材清冊確認 |
| 模型整理 | [Blender](https://www.blender.org/) | 計畫中的模型瘦身流程，專案要求 headless；本輪未執行 |
| 角色綁骨 | [Mixamo](https://www.mixamo.com/) | 計畫中的自動綁骨流程；是否採用及動作來源待 A／U 線確認 |
| 主要中文字型 | [Noto Serif TC／Noto Serif CJK](https://github.com/notofonts/noto-cjk/tree/main/Serif) | 現有 `Assets/_Project/Fonts/NotoSerifTC-Regular.otf` 與 SDF 字型資產；採 SIL Open Font License 1.1 |
| 其他現有字型 | Liberation Sans／TextMesh Pro | `Assets/TextMesh Pro/Fonts/` 有字型授權檔；是否隨建置輸出待 U 線確認，若有輸出應保留其原授權 |
| 列印學習單 | 裝置既有中文字型、HTML／CSS | 使用系統字型，不內嵌或下載字型、圖片、樣式與腳本，無外部資源依賴 |
| 音效與音樂 | 待確認 | 本輪未收到逐件來源與授權清冊，不冒列音樂製作者或可用授權 |

## 文本與校勘

- 原文：古籍公有領域文本，[《太平御覽》卷四十一，地部六〈天台山〉引《幽明錄》](https://zh.wikisource.org/wiki/太平御覽/0041#天台山)。本專案引用 `sources/liuruan_tw.txt` 的七段校改刪節整理本，詳見 `sources/variants.md`。
- 對校：《太平廣記》卷六十一〈天台二女〉篇末標出《神仙記》，不是本專案所稱《幽明錄》的底本；其他電子輯文與《藝文類聚》見異文表。電子轉錄未核原刻，不冒稱影印定本。
- 比較閱讀：陶淵明〈桃花源記〉，古籍公有領域原文；專案所據[維基文庫固定修訂](https://zh.wikisource.org/w/index.php?title=桃花源記&oldid=2620894)及整理說明見 `docs/content/compare_taohuayuan.md`。
- 留宿段整段省略，僅以「遂停半年」轉場；停字為已揭露的採用異文，不宣稱御覽原字就是停。

## 字型授權附件

Noto Serif CJK 官方[授權全文](https://raw.githubusercontent.com/notofonts/noto-cjk/main/Serif/LICENSE)為 SIL OFL 1.1，可依授權條件使用與嵌入；隨字型再散布須保留著作權聲明及授權，修改版本另須遵守保留字型名稱等條件。本表不是授權全文的替代品。

本輪未在 `_Project/Fonts/` 找到對應 Noto 授權附件。上線封裝前，請 U 線依實際採用的字型版本附齊著作權聲明與 OFL 全文，並核對子集化／SDF 輸出的處理；C 線不修改 Assets。官方上游授權查核日：2026-11-07。

## 美術與整包作品的使用界線

本作美術為 AI 生成並經製作整理。生成與轉換工具的使用條件須依實際帳號方案及取得紀錄確認；列名工具不等於工具方背書，也不等於取得任意再散布授權。原文的公有領域狀態與字型 OFL 授權分別適用；程式、美術及教材的整體釋出授權本輪未指定。發布端應以逐件素材清冊補齊最終製作名錄。
