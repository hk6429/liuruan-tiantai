---
date: 2026-11-07
type: 交件報告
tags: [天台山流杯記, A線, R2, 概念圖, Dzine]
---

# A 線 R2：建物與道具概念圖

## 交件摘要

8 張正式 PNG、check.py 與 contact.png 已落盤；實測 8/8 PASS。所有專案寫入限 staging/art/R2/。未開 Unity、未推送 GitHub 或部署。

工具：Codex 內建 image_gen（ChatGPT 內建生圖路徑）；共 13 次單張生成，沒有使用 API、API Key 或其他生圖服務。生成工具預設原稿另存於工具管理的 generated_images；本輪已複製至 raw/ 留存。正式圖僅以 Pillow 等比縮至 1024×1024；沒有裁切、補邊、背景重繪或手動修飾。

已讀：AGENTS.md、docs/plan.md（含第五節）、staging/art/R1/prompts.md；已透過 references.jpg 檢視 concept/ 全部 8 張與 ref/ 全部 8 張。RTK.md 在專案根目錄不存在。以本輪指定的 R2/A-R2.md 為唯一交件報告位置，未寫 logs/ 或 Obsidian（本輪限定只能寫 R2）。

## 正式檔案與視覺核對

| 檔案 | 採用版本 | 人工核對 | SHA-256 |
|---|---|---|---|
| house_tongwa.png | v1 | 銅綠金邊圓筒瓦、朱紅柱、完整小屋 | `bcfb010bdb81dd746765d51cff87e0623a971ceb666493191362f74cfb9a25d4` |
| pavilion_luozhang.png | v3 | 四只銅鈴皆可辨識、深紅薄紗、3/4 俯視 | `d1fa629fc00bb88ae3322aa6e1246777ce5ec2692339223781503f24d2409d7b` |
| peach_tree.png | v1 | 粗短樹幹、大顆粉紅桃子 | `20640e0dd91b61d374adb73b5330bb85c4eeaaf172e09718dd11c5eab5d06f53` |
| chu_tree.png | v1 | 灰褐斑駁樹皮、剝開處與纖維清楚 | `489c2f21b85650d065c32d414059570d457d5fbc0e0a0eadda32f7b0cc2f1528` |
| drift_items.png | v2 | 蕪菁葉／漆耳杯／黑芝麻飯糝；三件並排且互不重疊 | `a3da99d17ad7baf5d958e704377e4d946fd3335ec32937698ab06b3a5a8c508a` |
| home_house.png | v1 | 灰冷民居、破瓦混茅草、歪木門 | `1aa1a8393b81633320820d30a29d03d7e48fbf60782a40d26ad25601fe383570` |
| vine_cliff.png | v2 | 天然石台、垂直岩段與可攀粗藤；無建物 | `6ac10476c08d9d9e68f89143397047b417b5053ca37e69c38c91b419406d675e` |
| village_board.png | v2 | 兩柱一匾、三張空白竹簡卡、3/4 俯視 | `1d1885c21fe67c5f5d154fb65d86554ed07b03fdf08c78ccb1e4fa6368a0a428` |

contact.png：四欄兩列 1280×696，帶檔名供核對，不是 Dzine 單件輸入；8 張正式素材內無文字。references.jpg 是開工前參考圖總覽。raw/ 保留 13 張生成原稿；manifest.json 與 revisions.json 記錄提示詞、原稿位置與版本；check-output.txt 是下列實測輸出。

## 驗收方法

check.py 使用 Pillow，執行方式：

```sh
UV_CACHE_DIR=staging/art/R2/.uv-cache uv run --no-project --with pillow python staging/art/R2/check.py
```

逐張驗證存在、PNG 格式、1024×1024、無透明背景。四角各取 32×32 區塊的 RGB 平均，corner_delta 是四角每個色版的最大差，須 < 8；平均背景每色版須距 #D9D9D9 不超過 12，且背景色版差 < 8。此批背景約 #D3D2D2，屬指定的淺灰範圍。以背景色最大色版差 > 18 分割主體，計算包圍盒四邊留白，皆須 ≥ 6%；深色接地陰影也納入保守計算。程式內有合格留白、碰邊、空白、錯尺寸與四角色差的 assert 自我檢查。

限制：色差分割不能取代人工視覺驗收；與背景近色的部位及細薄半透明羅帳可能被漏算，已搭配原圖和 contact.png 核對，未裁切主體。四角統計只驗證指定採樣區，不代表每一個背景像素都完全相同。

### 最終實測輸出

```text
PASS house_tongwa.png 1024x1024 corner_delta=1.06 bg=(211, 210, 210) margins_LTRB=(8.79, 16.21, 9.57, 16.99)%
PASS pavilion_luozhang.png 1024x1024 corner_delta=0.93 bg=(210, 210, 209) margins_LTRB=(11.82, 8.4, 13.67, 9.38)%
PASS peach_tree.png 1024x1024 corner_delta=0.69 bg=(212, 211, 211) margins_LTRB=(7.03, 11.04, 7.71, 11.43)%
PASS chu_tree.png 1024x1024 corner_delta=0.84 bg=(211, 210, 210) margins_LTRB=(12.3, 9.47, 16.21, 11.33)%
PASS drift_items.png 1024x1024 corner_delta=0.70 bg=(211, 211, 210) margins_LTRB=(14.65, 38.77, 16.5, 38.77)%
PASS home_house.png 1024x1024 corner_delta=0.65 bg=(211, 210, 210) margins_LTRB=(12.3, 16.89, 12.01, 14.65)%
PASS vine_cliff.png 1024x1024 corner_delta=0.60 bg=(211, 210, 210) margins_LTRB=(17.19, 16.8, 19.43, 12.99)%
PASS village_board.png 1024x1024 corner_delta=0.45 bg=(212, 211, 211) margins_LTRB=(11.04, 8.89, 11.52, 8.59)%
TOTAL 8/8 PASS
```

## 重生紀錄

- 第一輪 6/8 PASS。drift_items v1 左留白 5.66%；vine_cliff v1 上／下留白 5.27%／5.37%，另多出非需求屋頂，均重生為 v2。
- pavilion v1 只清楚看見三鈴，v2 補到四鈴但偏正面；v3 改為 3/4 俯視且四鈴可辨識。
- village_board v1 偏正面，v2 改為 3/4 俯視，保留三張空白卡。

## 每張實際提示詞

以下每段為完整送入內建生圖工具的文字，含未採用版本；raw 檔名標明版本。

### house_tongwa.png — v1（採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE small squat Han-dynasty immortal-village house, broad curved roof of clearly visible rounded cylindrical barrel tiles in reflective jade copper-green with fine gold tile edging, vermilion red columns, warm cream plaster walls, simple wooden doorway. Cute chunky proportions, single storey, no surrounding plants or furniture.
```

### pavilion_luozhang.png — v1（未採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE small square open Han Chinese pavilion on a minimal integral platform, four red timber posts, jade green simple tiled canopy, deep crimson translucent gauze curtains hanging on ALL FOUR SIDES, parted enough to see empty interior. One small bronze bell hanging at each of four curtain corners, exactly four bells visible through translucent gauze where necessary. Curtains ankle length, clean readable thin red fabric, no furniture or people.
```

### pavilion_luozhang.png — v2（未採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE small square open Han Chinese pavilion on a minimal integral platform, four red timber posts, jade green simple tiled canopy, deep crimson translucent gauze curtains hanging on ALL FOUR SIDES, parted enough to see empty interior. One small bronze bell hanging at each of four curtain corners, exactly four bells visible through translucent gauze where necessary. Curtains ankle length, clean readable thin red fabric, no furniture or people. CRITICAL all FOUR small bells must be separately visible in this single view: front-left, front-right, rear-left and rear-right. Suspend rear bells slightly lower on longer cords so both can be seen THROUGH the open middle below the canopy, no bell hidden by posts or curtains. Four bells total, one per corner. Curtains deep DARK crimson burgundy, not orange red. Higher three-quarter elevated view to see all four corners, generously framed.
```

### pavilion_luozhang.png — v3（採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE small square open Han Chinese pavilion on a minimal integral platform, four red timber posts, jade green simple tiled canopy, deep crimson translucent gauze curtains hanging on ALL FOUR SIDES, parted enough to see empty interior. One small bronze bell hanging at each of four curtain corners, exactly four bells visible through translucent gauze where necessary. Curtains ankle length, clean readable thin red fabric, no furniture or people. CRITICAL all FOUR small bells must be separately visible in this single view: front-left, front-right, rear-left and rear-right. Suspend rear bells slightly lower on longer cords so both can be seen THROUGH the open middle below the canopy, no bell hidden by posts or curtains. Four bells total, one per corner. Curtains deep DARK crimson burgundy, not orange red. Higher three-quarter elevated view to see all four corners, generously framed. OVERRIDE CAMERA: absolutely NOT frontal or symmetrical elevation. Camera on front-right diagonal at azimuth 40 degrees and elevation 30 degrees. See TWO adjacent pavilion faces equally with the nearest CORNER POST offset slightly left of center, roof ridge diagonal. Four bells separately visible: one at near corner, one left corner, one right corner, fourth hanging lower inside the rear corner visible in open gap left of near post. Keep the four red gauze walls and empty interior. At least 12% margin.
```

### peach_tree.png — v1（採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE peach tree, very short thick rounded brown trunk and compact wide crown of warm green leaves, abundant LARGE pink ripe peaches visibly hanging throughout the canopy, chunky handmade clay style. Exposed root ends integral to trunk, no soil disk or pot, no detached fruit or background.
```

### chu_tree.png — v1（採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE paper mulberry tree Broussonetia papyrifera, short thick grey-brown mottled bark trunk and compact warm green crown with visibly serrated broad heart-shaped and three-lobed mulberry leaves. A clearly readable broad strip of bark peeled back from the FRONT of lower trunk, pale cream fibrous inner bark exposed, curled brown bark flap still attached. No tools, pots or soil disk.
```

### drift_items.png — v1（未採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. EXCEPTION to single object: exactly THREE separate pickup objects in ONE horizontal row with LARGE equal empty gaps for later cutting, all aligned around vertical center, whole group centered. LEFT one large fresh GREEN TURNIP LEAF with broad crinkled leaf blade and thick pale green stalk, no turnip root. MIDDLE one Han-dynasty wooden lacquer EAR CUP, low shallow oval bowl with two broad winglike handles, red lacquer interior and dark brown black lacquer outside, visible wood rim. RIGHT one SMALL loose clump of recognizable white cooked rice grains sprinkled with BLACK sesame seeds, no bowl or plate. Each object isolated from other objects, no water, no ripple, no extra leaves or rice grains outside its clump. Each about 19% canvas width, gaps about 9% canvas width.
```

### drift_items.png — v2（採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. EXCEPTION to single object: exactly THREE separate pickup objects in ONE horizontal row with LARGE equal empty gaps for later cutting, all aligned around vertical center, whole group centered. LEFT one large fresh GREEN TURNIP LEAF with broad crinkled leaf blade and thick pale green stalk, no turnip root. MIDDLE one Han-dynasty wooden lacquer EAR CUP, low shallow oval bowl with two broad winglike handles, red lacquer interior and dark brown black lacquer outside, visible wood rim. RIGHT one SMALL loose clump of recognizable white cooked rice grains sprinkled with BLACK sesame seeds, no bowl or plate. Each object isolated from other objects, no water, no ripple, no extra leaves or rice grains outside its clump. Each about 19% canvas width, gaps about 9% canvas width. CRITICAL FRAMING: shrink the ENTIRE three-item group to fit ONLY between x=180 and x=844 in the 1024px canvas. Absolutely no subject outside this central 65% width. Keep LARGE equal 80px or more blank gaps between items, make ear cup smaller to match leaf width. Background flat grey, not gradient.
```

### home_house.png — v1（採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE old dilapidated small Han-dynasty ordinary village house, same cute squat proportions as the immortal village house but COLD desaturated grey blue palette. Rough grey plaster and weathered exposed wood, visibly patched roof MIXING ragged straw thatch with broken grey cylindrical tiles, one crooked slanted wooden door hanging off alignment. Modest single storey, tired ancient house, no background, no rubble scatter, no plants, no scenery.
```

### vine_cliff.png — v1（未採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE freestanding vertical rock cliff SEGMENT as a self-contained game module, not a landscape. Tall chunky grey warm stone with rounded facets, front covered by thick climbable twisting woody vines, prominent graspable vine loops and warm green leaves. Small clearly visible flat ledge/platform integral to TOP for standing after climbing. Complete bottom and top of rock visible, no surrounding terrain, no trees, no soil base, no people.
```

### vine_cliff.png — v2（採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching the rounded Tiantai mountain art style. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE freestanding vertical rock cliff SEGMENT as a self-contained game module, not a landscape. Tall chunky grey warm stone with rounded facets, front covered by thick climbable twisting woody vines, prominent graspable vine loops and warm green leaves. Small clearly visible flat ledge/platform integral to TOP for standing after climbing. Complete bottom and top of rock visible, no surrounding terrain, no trees, no soil base, no people. CRITICAL: this is ONLY NATURAL ROCK AND VINES, absolutely NO buildings, roof, gate, pillars, fences, red wood, railings or architecture. Top ledge is bare natural stone. Entire rock occupies middle 65% height with at least 17% empty grey above and below. Make rock wider and shorter, 1.4 height-to-width ratio.
```

### village_board.png — v1（未採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE wooden village quest noticeboard, exactly TWO thick upright timber posts supporting ONE horizontal wooden header plaque, exactly THREE BLANK hanging bamboo tablet cards side by side beneath header. Each card is a distinct small rectangular group of pale bamboo slips tied together, no writing or decorative marks. Cute chunky warm honey-brown carved wood, clean rounded edges, short integral timber feet. No roof, no surrounding props.
```

### village_board.png — v2（採用）

```text
Use case: stylized-concept. Generate ONE square 1024x1024 PNG game asset concept for Dzine image-to-3D. Cute chibi stylized handcrafted clay miniature, clean softly rounded shapes, saturated warm colors, matching Tiantai Chinese fantasy village with jade roofs and red timber. Orthographic-like three-quarter elevated view from front, camera 30 degrees downward. Center complete subject including all extremities inside central 76% of canvas, at least 12% blank margin on ALL four sides including shadow. Perfectly flat uniform solid light grey #D9D9D9 background edge to edge, no gradient, no horizon, no vignette, no scenery or ground patch, only a very small soft contact shadow beneath subject. No lettering, symbols, labels, watermark, people or unrelated props. ONE wooden village quest noticeboard, exactly TWO thick upright timber posts supporting ONE horizontal wooden header plaque, exactly THREE BLANK hanging bamboo tablet cards side by side beneath header. Each card is a distinct small rectangular group of pale bamboo slips tied together, no writing or decorative marks. Cute chunky warm honey-brown carved wood, clean rounded edges, short integral timber feet. No roof, no surrounding props. CRITICAL CAMERA: rotate the board 30 degrees around its vertical axis, viewed from FRONT-RIGHT AND ABOVE, showing the RIGHT SIDE faces of both posts, feet and thick header. Strong obvious three-quarter perspective with top beam receding diagonally, NOT a flat frontal symmetric elevation. The three blank cards are all readable and non-overlapping. Blank header, no carved symbols.
```

## 未執行與交接

未進行 Dzine 轉 3D、拆分漂流物、拓樸／背面／碰撞體驗收或 Unity 匯入。本輪交件是轉模用概念圖；薄紗、銅鈴、樹葉與細米粒能否正確生成仍需主線實際轉模確認。漂流物需先依三件間的空白間隔切成獨立輸入圖，再送單物件轉模。下一步由主線操作 Dzine。
