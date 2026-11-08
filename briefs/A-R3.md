# A 線 R3：NPC 原圖（給 Dzine 轉 3D）、對話頭像、故事卡插畫、UI 圖示

先讀 AGENTS.md、docs/r3-diagnosis.md、docs/plan.md 第五節、staging/art/concept/ 定調圖、staging/ref/*.jpg、你上一輪 staging/art/R2/A-R2.md（prompt 寫法沿用）。也看 staging/models/game/liuchen_albedo.png、ruanzhao_albedo.png 了解現有主角配色。只能寫 staging/art/R3/。生圖一律用 codex 內建生圖（ChatGPT OAuth），不走 API，每張包逾時、序列跑。交件報告 staging/art/R3/A-R3.md（含每張 prompt）＋ contact.png 接觸表＋ check.py（檢查尺寸、檔案存在）。

## 1. 給 Dzine 轉 3D 的原圖（最重要，CC 會拿去送 Dzine）
規格：1024×1024 PNG、**純淺灰底 #D9D9D9、單一主體完整不裁切、置中、無框、無底板、無地面陰影、無文字**；人物正面、雙臂微張離開身體（A-pose）、站直、全身入鏡；Q 版風格化、手作黏土感，與主角同一畫風。
- `npc_woman_a.png`：仙境女子甲，漢代曲裾深衣，淡青與白，髮髻，溫和微笑。
- `npc_woman_b.png`：仙境女子乙，漢代深衣，淡粉與絳紅，髮髻，和甲是一對但可區分。
- `npc_grandson.png`：七世孫，漢魏村民裝束（粗布短衣、頭巾），中年、樸實。
- `peach_tree_v2.png`：結滿粉紅大桃的 Q 版桃樹，**整棵完整、樹根直接在灰底上，不要任何框、卡片、地台、草地圓盤**。
- `chu_tree_v2.png`：楮樹（構樹），灰褐斑駁樹皮，有一片被剝開，同上無框無地台。
- `drift_leaf.png`、`drift_rice.png`：漂流物各自單件——蕪菁葉（一片大綠葉）、胡麻飯糝（一小團米飯帶黑芝麻，盛在小葉片上）。

## 2. 對話頭像（UI 用）
512×512 PNG、透明背景、胸像、Q 版同畫風、表情自然：`portrait_liuchen.png`、`portrait_ruanzhao.png`（照主角配色）、`portrait_woman_a.png`、`portrait_woman_b.png`、`portrait_grandson.png`（與上面 NPC 原圖同人設）。

## 3. 開場故事卡插畫
1920×1080 PNG，水墨淡彩融合 Q 版：`story_1.png` 二人背簍入天台山（雲霧山林）、`story_2.png` 山中迷路、天色將暗、乾糧袋空、`story_3.png` 遠望崖上桃樹。不要文字。

## 4. UI 圖示
256×256 透明 PNG，古風簡潔、深色描邊在淺／深背景都看得清：`icon_help.png`（問號卷軸）、`icon_arrow.png`（指向右的箭頭，給畫面邊緣指引旋轉用）、`icon_restart.png`、`icon_skip.png`、`icon_home.png`（回本幕起點）。另 `card_frame.png` 600×360 九宮格可切的詞卡底框（米黃紙＋細邊）。
