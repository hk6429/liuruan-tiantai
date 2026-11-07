# A 線 R2：建物與道具概念圖（給 Dzine 轉 3D）

先讀 AGENTS.md、docs/plan.md 第五節、staging/art/concept/ 的定調圖、staging/ref/*.jpg、你上一輪的 staging/art/R1/prompts.md。只能寫 staging/art/R2/。生圖一律用 codex 內建生圖（ChatGPT OAuth），不走 API。交件報告寫 staging/art/R2/A-R2.md（含每張的 prompt）。

Dzine 轉 3D 規格（很重要）：**純淺灰底（#D9D9D9 左右）、單一主體完整不裁切、置中、3/4 俯視角、無地面陰影以外的背景物、無文字**。風格照定調圖：Q 版風格化、飽和暖色、手作黏土感的乾淨造型，跟參考影片一樣可愛。尺寸 1024×1024 PNG。

要的 8 張（檔名照寫）：
1. `house_tongwa.png`：仙村「筒瓦屋」——漢代小屋，圓筒瓦屋頂（青綠帶金邊、會反光），朱紅柱，比例 Q 版。
2. `pavilion_luozhang.png`：亭子，四面垂「絳羅帳」（深紅薄紗），帳角各懸一只小銅鈴。
3. `peach_tree.png`：結滿大顆粉紅桃子的桃樹，樹幹粗短，Q 版。
4. `chu_tree.png`：楮樹（構樹），樹皮灰褐帶斑，可看出一片被剝開的樹皮。
5. `drift_items.png`：一張圖三件漂浮物並排（間隔大，方便切開）：蕪菁葉（大綠葉）、漢代木杯（漆耳杯）、一小撮胡麻飯糝（一團米飯粒帶黑芝麻）。
6. `home_house.png`：歸鄉村的漢代民居，破舊、灰冷色、茅草夾瓦、木門歪斜，與仙村成對比。
7. `vine_cliff.png`：一段垂直岩壁，上面爬滿可攀的粗藤蔓，頂端有一小平台（第二幕攀援藤葛）。
8. `village_board.png`：村中木製告示板（任務板），兩柱一橫匾、掛三張空白竹簡卡。

驗收：8 張都存在、1024²、背景色均勻（四角取樣差異 < 8）、主體不碰邊（四邊留白 ≥ 6%）——寫一支 staging/art/R2/check.py 實測並把輸出貼報告；不合格的重生。另做 contact sheet `staging/art/R2/contact.png`。
