---
date: 2026-11-08
type: 交件報告
tags: [天台山流杯記, A線, R3, 生圖, Dzine]
---

# A 線 R3：NPC、對話頭像、故事卡與 UI 圖示

## 交件與驗證

21 張正式 PNG 已落盤，check.py 實測 **21/21 PASS**；21 個 SHA-256 全數不同。正式交付及輔助檔案都在 staging/art/R3/。未操作 Unity、Dzine、GitHub 或部署。

工具：Codex 內建 image_gen，沿用 ChatGPT 內建生圖路徑，沒有使用 API、API Key 或其他生圖服務。26 次單張呼叫全部序列執行，每次包 180 秒逾時保護，本輪沒有逾時。raw/ 留存全部 26 張原稿（含 4 張灰底初版及 story_1 初版），manifest.json 記錄完整提示詞、參考圖及工具來源路徑。工具另有其管理的 generated_images 原稿；專案正式圖均已複製落盤。

已讀 AGENTS.md、docs/r3-diagnosis.md、docs/plan.md 第五節、staging/art/R2/A-R2.md 及 sources/liuruan_tw.txt。references.jpg 彙整並已檢視 concept/ 全部 8 張、ref/ 全部 8 張，以及指定兩張主角 albedo。劉晨採褐色衣與米色頭巾，阮肇採深藍衣與草帽。專案根目錄沒有 RTK.md。

本輪指定只能寫 R3，因此以本報告交接，未另寫 logs/ 或 Obsidian。

| 類別 | 正式檔案 | 尺寸／背景 |
|---|---|---|
| NPC | npc_woman_a.png、npc_woman_b.png、npc_grandson.png | 1024×1024，純 #D9D9D9 |
| 樹木 | peach_tree_v2.png、chu_tree_v2.png | 1024×1024，純 #D9D9D9 |
| 漂流物 | drift_leaf.png、drift_rice.png | 1024×1024，純 #D9D9D9 |
| 頭像 | portrait_liuchen.png、portrait_ruanzhao.png、portrait_woman_a.png、portrait_woman_b.png、portrait_grandson.png | 512×512，透明 alpha |
| 故事卡 | story_1.png、story_2.png、story_3.png | 1920×1080，不透明，無文字 |
| UI | icon_help.png、icon_arrow.png、icon_restart.png、icon_skip.png、icon_home.png | 256×256，透明 alpha |
| 詞卡框 | card_frame.png | 600×360，米黃紙、細邊、外圍透明 |

## 輸出方式與人工核對

第一批 NPC 與桃樹的灰底原稿有色差，女子乙原稿也不是正方形。因此再用內建生圖做背景移除，保留人設與完整主體；其餘轉模圖直接以內建生圖生成透明原稿。export.py 只做透明留白整理、等比縮放及 PNG alpha 合成：轉模圖主體置中、最大邊約佔畫布 84%，透明區合成精確 #D9D9D9；頭像與 UI 主體最大邊約佔 90%，保留透明度。未用程式畫替代素材、改人物或重繪背景。故事卡等比輸出，不裁切內容。

- 三位 NPC 正面站立、雙臂離開身體、手與鞋完整；女子甲青白、高髻玉簪，女子乙粉紅絳紅、盤髻紅簪；七世孫為中年頭巾村民。
- 桃樹完整大桃與根部，楮樹有斑駁樹皮、剝開缺口與淺色纖維；沒有框、地台、草地圓盤或地面陰影。
- 蕪菁葉與胡麻飯糝各一件，飯糝有黑芝麻並盛在小葉片上。
- 五張頭像逐張引用對應人設圖；contact.png 已人工檢視淺、深背景下的透明輪廓，未見明顯雜色殘影。
- 故事依序為二人背簍入山、暮色迷路與空糧袋、隔澗遠望崖桃。story_1 初版背簍多出採摘花草，已用內建生圖移除，採用空背簍版本。沒有文字、戰鬥或留宿畫面。
- 五個圖示都有深褐描邊及淡色外緣；右箭頭水平向右，可由主線程式旋轉。
- card_frame 九宮格建議 left/right/top/bottom 均為 **60 px**，Unity Sprite Mesh Type 可用 Full Rect、Image Type 用 Sliced。nine_slice_preview.png 模擬 900×360 與 600×500 拉伸，已目視檢查並斷言左上角像素保持不變。這是離線預覽，未在 Unity 驗收。

## 檢查方式

```sh
UV_CACHE_DIR=staging/art/R3/.uv-cache uv run --no-project --with pillow python staging/art/R3/check.py
```

check.py 檢查 21 個指定檔名存在、PNG 格式、精確尺寸；透明素材須同時有透明與不透明像素，主體不得碰邊。7 張轉模圖以灰底差值取得主體包圍盒，四邊至少 7% 留白；整個外圍 7% 區域每個像素必須等於 RGB(217,217,217)。內含錯尺寸與假透明的反例自我檢查。

check-output.txt 保存實測結果；checks.json 保存尺寸與 SHA-256；contact.png 為 1400×1470 四欄六列接觸表，帶檔名及淺／深檢視背景。check.py 重跑會更新這三個驗收檔，其中 check-output.txt 由執行時轉存 stdout。

程式不能判斷服制、植物辨識、表情、單一主體或敘事正確性，這些項目另以原稿及接觸表目視核對。

## 每張完整實際提示詞

### npc_woman_a.png

```text
Use case: stylized-concept. Generate ONE 1024x1024 PNG Dzine image-to-3D character source. Cute chibi handcrafted clay, rounded clean shapes, 1:2.5 head-to-body proportions matching the Tiantai protagonists. ONE adult immortal Chinese woman A, gentle smile, black hair in a neat high Han bun with small pale jade pin, pale cyan and white Han-dynasty curved-hem shenyi robe, simple pale teal sash, flat cloth shoes. Front view, standing straight, arms slightly spread away from torso in A-pose, hands unobscured, full body including hair and feet fully visible. Entire subject centered inside central 78% canvas, generous blank margin on all sides. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge. NO ground shadow, no ambient contact shadow, no ground, base, pedestal, frame, card, border, scenery, lettering or watermark. Soft even neutral lighting, opaque clay materials, no transparent gauze. One single complete character only.
```

後續編輯提示詞（採用編輯後版本）：

```text
Use case: background-extraction. Edit the supplied image only by removing ALL grey background to genuine transparent alpha. Preserve the EXACT same character/tree, pose, proportions, face, costume, colors, full silhouette and details unchanged. Remove all ground/contact/cast shadows. Do not add anything. One complete isolated subject, no frame base or ground. Generous transparent padding, square canvas. This will be composited onto pure #D9D9D9 for Dzine.
```

### npc_woman_b.png

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset source for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, rounded clean shapes, warm colors matching Tiantai protagonists. Center ONE complete subject inside central 76% of canvas with generous empty margins on ALL sides, every extremity visible. Perfectly flat uniform solid light grey EXACT #D9D9D9 background edge to edge, absolutely NO gradient, texture, vignette, horizon or ground shadow. No frame, border, card, base, pedestal, soil disk, grass patch, ground, lettering, labels, watermark, scenery or unrelated props. Soft neutral even lighting. ONE adult immortal Chinese woman B, front view, standing straight full body, arms spread slightly from torso A-pose with visible hands. Han-dynasty shenyi robe pale pink with deep crimson crossover collar, sash and underskirt edge; black hair in paired coiled bun with tiny red pin, friendly natural smile. Same design family as pale cyan woman A but distinguishable hair and colors. 1:2.5 head body, cloth shoes. No accessories in hands.
```

後續編輯提示詞（採用編輯後版本）：

```text
Use case: background-extraction. Edit the supplied image only by removing ALL grey background to genuine transparent alpha. Preserve the EXACT same character/tree, pose, proportions, face, costume, colors, full silhouette and details unchanged. Remove all ground/contact/cast shadows. Do not add anything. One complete isolated subject, no frame base or ground. Generous transparent padding, square canvas. This will be composited onto pure #D9D9D9 for Dzine.
```

### npc_grandson.png

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset source for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, rounded clean shapes, warm colors matching Tiantai protagonists. Center ONE complete subject inside central 76% of canvas with generous empty margins on ALL sides, every extremity visible. Perfectly flat uniform solid light grey EXACT #D9D9D9 background edge to edge, absolutely NO gradient, texture, vignette, horizon or ground shadow. No frame, border, card, base, pedestal, soil disk, grass patch, ground, lettering, labels, watermark, scenery or unrelated props. Soft neutral even lighting. ONE middle-aged Han-Wei Chinese male villager, seven-generation descendant, sturdy modest friendly face, short dark moustache, dark brown headscarf, coarse warm brown short cross-collar tunic, beige trousers, cloth belt and cloth shoes. Front view standing straight, arms slightly away from body in A-pose, both hands visible, full body, chibi 1:2.5 head body. No tools or props.
```

後續編輯提示詞（採用編輯後版本）：

```text
Use case: background-extraction. Edit the supplied image only by removing ALL grey background to genuine transparent alpha. Preserve the EXACT same character/tree, pose, proportions, face, costume, colors, full silhouette and details unchanged. Remove all ground/contact/cast shadows. Do not add anything. One complete isolated subject, no frame base or ground. Generous transparent padding, square canvas. This will be composited onto pure #D9D9D9 for Dzine.
```

### peach_tree_v2.png

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset source for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, rounded clean shapes, warm colors matching Tiantai protagonists. Center ONE complete subject inside central 76% of canvas with generous empty margins on ALL sides, every extremity visible. Perfectly flat uniform solid light grey EXACT #D9D9D9 background edge to edge, absolutely NO gradient, texture, vignette, horizon or ground shadow. No frame, border, card, base, pedestal, soil disk, grass patch, ground, lettering, labels, watermark, scenery or unrelated props. Soft neutral even lighting. ONE complete cute peach TREE, short thick warm brown trunk, warm green compact rounded canopy filled with many large PINK ripe peaches. All crown, branch tips and exposed root ends fully visible. Roots end directly against grey background, zero ground shadow, NO FRAME, CARD, PLATFORM, DISK, pot or grass. Slight elevated three-quarter view.
```

後續編輯提示詞（採用編輯後版本）：

```text
Use case: background-extraction. Edit the supplied image only by removing ALL grey background to genuine transparent alpha. Preserve the EXACT same character/tree, pose, proportions, face, costume, colors, full silhouette and details unchanged. Remove all ground/contact/cast shadows. Do not add anything. One complete isolated subject, no frame base or ground. Generous transparent padding, square canvas. This will be composited onto pure #D9D9D9 for Dzine.
```

### chu_tree_v2.png

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset source for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, rounded clean shapes, warm colors matching Tiantai protagonists. Center ONE complete subject inside central 76% of canvas with generous empty margins on ALL sides, every extremity visible. Genuine transparent alpha background, absolutely no background color, checkerboard, horizon or ground shadow. Final export will be composited onto solid #D9D9D9. No frame, border, card, base, pedestal, soil disk, grass patch, ground, lettering, labels, watermark, scenery or unrelated props. Soft neutral even lighting. ONE complete paper mulberry TREE Broussonetia papyrifera, broad serrated heart-shaped and three-lobed green leaves, compact crown, short thick mottled grey-brown bark. A broad bark strip peeled back on FRONT lower trunk, revealing cream fibrous inner bark, curled bark flap attached. All branches crown roots complete. Roots directly against grey, NO frame card ground platform disk or shadow. Slight elevated three-quarter view.
```

### drift_leaf.png

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset source for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, rounded clean shapes, warm colors matching Tiantai protagonists. Center ONE complete subject inside central 76% of canvas with generous empty margins on ALL sides, every extremity visible. Genuine transparent alpha background, absolutely no background color, checkerboard, horizon or ground shadow. Final export will be composited onto solid #D9D9D9. No frame, border, card, base, pedestal, soil disk, grass patch, ground, lettering, labels, watermark, scenery or unrelated props. Soft neutral even lighting. ONE single large fresh green TURNIP LEAF, broad crinkled leaf blade with gently lobed margins and thick pale green stalk, readable raised veins, no turnip root, no water, no additional leaves. Diagonal face-up three-quarter view, whole blade and stalk.
```

### drift_rice.png

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset source for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, rounded clean shapes, warm colors matching Tiantai protagonists. Center ONE complete subject inside central 76% of canvas with generous empty margins on ALL sides, every extremity visible. Genuine transparent alpha background, absolutely no background color, checkerboard, horizon or ground shadow. Final export will be composited onto solid #D9D9D9. No frame, border, card, base, pedestal, soil disk, grass patch, ground, lettering, labels, watermark, scenery or unrelated props. Soft neutral even lighting. ONE small clump of cooked white RICE sprinkled with clearly BLACK SESAME seeds resting on ONE small green leaf underneath. Single connected pickup object. Individual rounded rice grains readable, tiny leaf just large enough to hold rice. Elevated three-quarter view. No bowl plate water additional clumps or scattered grains.
```

### portrait_liuchen.png

參考圖：staging/art/concept/05_liuchen.png。

```text
Use case: stylized-concept. Generate ONE 512x512 PNG dialogue portrait for a Chinese Tiantai chibi game. Genuine transparent alpha background, no backdrop, checkerboard, circle, frame, text, watermark or cast shadow. Cute handcrafted clay with softly rounded shapes, expressive large brown eyes, warm natural skin, simple readable silhouette. Center chest-up bust, entire hair/headwear and both shoulders inside canvas, natural relaxed expression. Liu Chen, young Han Chinese man, short tousled dark hair with small topknot, beige cloth headband, warm BROWN cross-collar coarse tunic with cream inner collar and rope strap from wicker backpack, gentle earnest smile. Match the supplied protagonist reference colors and face.
```

### portrait_ruanzhao.png

參考圖：staging/art/concept/06_ruanzhao.png。

```text
Use case: stylized-concept. Generate ONE 512x512 PNG dialogue portrait for a Chinese Tiantai chibi game. Genuine transparent alpha background, no backdrop, checkerboard, circle, frame, text, watermark or cast shadow. Cute handcrafted clay with softly rounded shapes, expressive large brown eyes, warm natural skin, simple readable silhouette. Center chest-up bust, entire hair/headwear and both shoulders inside canvas, natural relaxed expression. Ruan Zhao, young Han Chinese man, black hair under wide woven STRAW HAT with small topknot, DARK NAVY BLUE cross-collar tunic with cream inner collar and rope backpack strap, friendly confident smile. Match supplied protagonist reference.
```

### portrait_woman_a.png

參考圖：staging/art/R3/raw/npc_woman_a.png。

```text
Use case: stylized-concept. Generate ONE 512x512 PNG dialogue portrait for a Chinese Tiantai chibi game. Genuine transparent alpha background, no backdrop, checkerboard, circle, frame, text, watermark or cast shadow. Cute handcrafted clay with softly rounded shapes, expressive large brown eyes, warm natural skin, simple readable silhouette. Center chest-up bust, entire hair/headwear and both shoulders inside canvas, natural relaxed expression. Same woman A as supplied NPC reference: pale cyan and white Han curved-hem robe, teal sash, neat high black bun and pale jade hairpin, gentle smile. Preserve face hair colors and costume.
```

### portrait_woman_b.png

參考圖：staging/art/R3/raw/npc_woman_b.png。

```text
Use case: stylized-concept. Generate ONE 512x512 PNG dialogue portrait for a Chinese Tiantai chibi game. Genuine transparent alpha background, no backdrop, checkerboard, circle, frame, text, watermark or cast shadow. Cute handcrafted clay with softly rounded shapes, expressive large brown eyes, warm natural skin, simple readable silhouette. Center chest-up bust, entire hair/headwear and both shoulders inside canvas, natural relaxed expression. Same woman B as supplied NPC reference: pale pink Han shenyi, deep crimson collar, black coiled bun and red hairpin, friendly smile. Preserve face hairstyle costume.
```

### portrait_grandson.png

參考圖：staging/art/R3/raw/npc_grandson.png。

```text
Use case: stylized-concept. Generate ONE 512x512 PNG dialogue portrait for a Chinese Tiantai chibi game. Genuine transparent alpha background, no backdrop, checkerboard, circle, frame, text, watermark or cast shadow. Cute handcrafted clay with softly rounded shapes, expressive large brown eyes, warm natural skin, simple readable silhouette. Center chest-up bust, entire hair/headwear and both shoulders inside canvas, natural relaxed expression. Same middle-aged villager as supplied NPC reference: brown headscarf, coarse brown tunic and beige collar, short moustache, modest friendly face. Preserve face hair costume.
```

### story_1.png

```text
Use case: illustration-story. ONE landscape 1920x1080 PNG opening story card, no text frame labels or watermark. Chinese pale ink wash and delicate watercolor landscape blended with cute handcrafted clay chibi characters, softly rounded, warm muted greens, atmospheric mist and elegant ink mountain contours. Exactly TWO young Han men entering Tiantai mountain forest together carrying wicker BACK BASKETS. Liu Chen wears brown tunic and beige headband; Ruan Zhao dark navy tunic and straw hat, same as supplied references. Rear three-quarter view, full bodies on winding trail in lower center, immense cloudy wooded mountains ahead, peaceful daylight, readable composition. No modern objects or other people.
```

後續編輯提示詞（採用編輯後版本）：

```text
Use case: precise-object-edit. Edit this opening story illustration. Remove ONLY the cut flowers, leafy sprigs and harvested plants sticking out of BOTH travelers' wicker back baskets. Leave both baskets EMPTY with natural dark empty basket interiors. Keep the same two travelers, clothing, faces, poses, full-body composition, mountains, forest, atmospheric mist, trail, pale ink watercolor and clay style, lighting, landscape 16:9 framing unchanged. Do not add anything. No text.
```

### story_2.png

```text
Use case: illustration-story. ONE landscape 1920x1080 PNG opening story card, no text frame labels or watermark. Chinese pale ink wash and delicate watercolor forest blended with cute handcrafted clay chibi characters, muted dusk blue-green and warm skin tones. Exactly TWO young Han travelers LOST in mountain forest as daylight fades: Liu Chen brown tunic beige headband with wicker back basket, Ruan Zhao dark navy tunic wide straw hat with wicker back basket. Liu Chen holds open a visibly EMPTY cloth dry-food BAG upside down, Ruan Zhao looks uncertainly at diverging overgrown trails. Tired hungry worried but child-friendly expressions, full bodies, no food in bag or nearby, no extra people or modern objects.
```

### story_3.png

```text
Use case: illustration-story. ONE landscape 1920x1080 PNG opening story card, no text frame labels or watermark. Chinese pale ink wash and delicate watercolor mountain landscape blended with cute handcrafted clay chibi characters, misty jade greens warm pink peach accents. Exactly TWO young Han travelers viewed from behind in lower foreground: Liu Chen brown tunic beige headband wicker back basket and Ruan Zhao dark navy tunic straw hat wicker back basket. They look and point up toward ONE clearly readable tree laden with LARGE PINK PEACHES on distant high inaccessible CLIFF TOP across deep rocky ravine, vines down cliff. No bridge stairs path to summit or other people. Clear visual story: hungry travelers have spotted fruit high above.
```

### icon_help.png

```text
Use case: stylized-concept. ONE 256x256 PNG game UI icon, actual transparent alpha background. Ancient Chinese simple handcrafted clay-inspired icon matching Tiantai chibi game. Bold clean silhouette with DARK BROWN outline PLUS fine pale cream outer keyline so visible on light AND dark backgrounds. Center entirely inside 80% canvas, generous padding. No button background tile, frame, drop shadow, watermark or letters. ONE small ancient cream parchment scroll bearing a SINGLE large dark brown QUESTION MARK as its symbol; rolled ends, question mark centered, instantly readable.
```

### icon_arrow.png

```text
Use case: stylized-concept. ONE 256x256 PNG game UI icon, actual transparent alpha background. Ancient Chinese simple handcrafted clay-inspired icon matching Tiantai chibi game. Bold clean silhouette with DARK BROWN outline PLUS fine pale cream outer keyline so visible on light AND dark backgrounds. Center entirely inside 80% canvas, generous padding. No button background tile, frame, drop shadow, watermark or letters. ONE bold thick RIGHT POINTING ARROW, simple solid warm gold fill, dark brown contour, thin cream outer keyline, no scroll or other objects. Perfectly horizontal rightward axis for runtime rotation.
```

### icon_restart.png

```text
Use case: stylized-concept. ONE 256x256 PNG game UI icon, actual transparent alpha background. Ancient Chinese simple handcrafted clay-inspired icon matching Tiantai chibi game. Bold clean silhouette with DARK BROWN outline PLUS fine pale cream outer keyline so visible on light AND dark backgrounds. Center entirely inside 80% canvas, generous padding. No button background tile, frame, drop shadow, watermark or letters. ONE clockwise circular arrow wrapping around empty center, warm antique gold, thick dark brown contour and thin cream outer keyline. Instantly means restart; no lettering.
```

### icon_skip.png

```text
Use case: stylized-concept. ONE 256x256 PNG game UI icon, actual transparent alpha background. Ancient Chinese simple handcrafted clay-inspired icon matching Tiantai chibi game. Bold clean silhouette with DARK BROWN outline PLUS fine pale cream outer keyline so visible on light AND dark backgrounds. Center entirely inside 80% canvas, generous padding. No button background tile, frame, drop shadow, watermark or letters. ONE skip-forward symbol: TWO right-pointing triangles followed by ONE vertical stop bar, warm antique gold, thick dark brown contours and thin cream outer keyline, very simple.
```

### icon_home.png

```text
Use case: stylized-concept. ONE 256x256 PNG game UI icon, actual transparent alpha background. Ancient Chinese simple handcrafted clay-inspired icon matching Tiantai chibi game. Bold clean silhouette with DARK BROWN outline PLUS fine pale cream outer keyline so visible on light AND dark backgrounds. Center entirely inside 80% canvas, generous padding. No button background tile, frame, drop shadow, watermark or letters. ONE small Han Chinese cottage with simple curved roof and doorway, integrated small return arrow pointing toward doorway, warm gold and cream, dark brown contour and cream outer keyline. Means return to this act's starting point.
```

### card_frame.png

```text
Use case: stylized-concept. Generate ONE 600x360 landscape PNG NINE-SLICE UI vocabulary card background for an ancient Chinese chibi game. Front view perfectly rectangular flat warm BEIGE PAPER panel with very subtle paper grain and a THIN continuous dark warm brown double border; simple softly rounded identical corners. Genuine transparent alpha outside the rectangle. Large EMPTY uniformly pale beige central writing area. Border strictly parallel to image edges, same width on all four sides, no perspective. Corner decoration minimal and confined to corners; all edge middle sections straight and uniform so nine-slicing stretches cleanly. No text symbols scroll rolls tassels folds pictures shadows or watermark. Entire panel visible with small transparent outer padding. Target Unity Sprite border 60px left right top bottom at 600x360 export.
```

## 未執行、風險與下一步

未進行 Dzine 轉模、背面／拓樸／綁骨／碰撞驗證或 Unity 匯入。袖口、髮簪、葉片與米粒的 3D 重建效果仍待實際轉模確認；A-pose 與完整正面圖不能保證自動綁骨成功。圖示已做離線淺／深背景目視檢查，尚未在實際 HUD 尺寸與手機上驗收。

下一步由 CC 使用 R3 根目錄下 7 張灰底正式圖逐件送 Dzine；raw/ 不是正式轉模輸入。U 線匯入其餘 UI 素材，設定詞卡九宮格，驗收實際 HUD。

