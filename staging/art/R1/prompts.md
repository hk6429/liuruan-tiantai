# A 線 R1 提示詞與處理紀錄

工具：內建 image_gen，單張依序生成。參考已檢視 staging/ref 全部八張與 concept 主場景、歸鄉、阮肇造型。
所有圖像禁止文字、浮水印、影片原角色、劍盾與小象。
圖集採兩欄三列。影像後製僅依本輪明確指定執行：尺寸、貼圖無縫、Sobel 法線、透明與圖集組裝。

輸入圖像：peach_half / peach_empty 以本輪 ui/peach_full.png 為編修參考；portrait_ruanzhao 以 staging/art/concept/06_ruanzhao.png 為造型參考。其餘依已檢視的定調圖與下列提示詞新生成。
fern 首次透明稿有淡色光暈，第二次改純洋紅底重生；後列同名提示詞為採用版本。
全部共 24 次單張序列生圖，採用 23 張生成原稿；原稿留在 raw/。天空原稿 1774×887，最終等比放大到 2048×1024；其餘原稿 1254²，最終依規格縮小。
地面：重疊區最小差異路徑裁接，再做 4 px 邊界過渡，輸出 2×2 tilecheck。法線：亮度近似高度，Gaussian 1.2 px、循環取樣 Sobel、強度 4、+Y 編碼，再做 8 px 邊界過渡。
天空：96 px 左右過渡，上下 64 px 極點收斂；草叢：長草使用生成 alpha，其餘以洋紅底色差去背並縮邊 1 px；所有透明圖以 Lanczos 等比縮放並保留透明留白。
ui_contact.png 與 foliage_contact.png 為含英文檔名的檢查圖，不是遊戲素材；正式遊戲圖像皆無文字。

## sky/sky_day.png

```text
Use case: stylized-concept. Generate exactly ONE 2048x1024 PNG 2:1 full spherical EQUIRECTANGULAR 360x180 panorama for Unity skybox. Tiantai mountain distant rounded fantastic peaks beneath a sea of soft clouds, clear warm sunny day, golden sunlight, sky blue overhead and saturated warm green distant mountains. Hand-painted stylized chibi game environment matching the established cute rounded Tiantai concepts. Camera level, horizon absolutely horizontal at image midpoint. Full-sphere latitude longitude projection: zenith top and nadir bottom compressed appropriately, no perspective lens or fisheye. Left/right borders join continuously with matching clouds colors and mountain silhouettes. No foreground objects, people, buildings, text, watermark, original video hero, swords, shields or elephants.
```

## sky/sky_return.png

```text
Use case: stylized-concept. Exactly ONE 2048x1024 2:1 full spherical equirectangular latitude-longitude 360x180 panorama for a Unity skybox. Tiantai mountain distant rounded dramatic peaks and cloud sea, overcast grey cold early evening at return home, subdued slate blue grey clouds with a faint muted dusk band. Rounded hand-painted stylized chibi game environment, not photography, same terrain language as warm Tiantai concepts. Level horizontal horizon at image midpoint, no tilted horizon, no fisheye. Top zenith and bottom nadir in full-sphere projection, left and right edge clouds colors and mountain silhouettes must join seamlessly. Entirely distant atmosphere, no buildings no foreground trees no characters no text no watermark no swords shields elephants or original video hero.
```

## ground/grass.png

```text
Use case: stylized-concept. Exactly ONE square 1024x1024 albedo texture for a warm sunny chibi Tiantai mountain game, stylized HAND PAINTED broad readable brush shapes, saturated warm natural palette, not photographic. Orthographic top down material surface fills every pixel. Seamless tileable all four edges, consistent scale throughout, diffuse flat even lighting with NO directional cast shadows, no perspective, no horizon, no scene objects, no vignette, no text watermark characters swords shields or elephants. Material: dense short warm yellow-green meadow grass, small layered stylized blades forming a continuous ground carpet, subtly varied clumps and dark green recesses, no large flowers or rocks.
```

## ground/dirt_path.png

```text
Use case: stylized-concept. Exactly ONE square 1024x1024 albedo texture for a warm sunny chibi Tiantai mountain game, stylized HAND PAINTED broad readable brush shapes, saturated warm natural palette, not photographic. Orthographic top down material surface fills every pixel. Seamless tileable all four edges, consistent scale throughout, diffuse flat even lighting with NO directional cast shadows, no perspective, no horizon, no scene objects, no vignette, no text watermark characters swords shields or elephants. Material: warm ochre brown packed earthen footpath soil, soft stylized brush patches, sparse tiny rounded grains and worn earthy cracks, uniformly soil across entire image, no grass borders or literal winding path.
```

## ground/moss_rock.png

```text
Use case: stylized-concept. Exactly ONE square 1024x1024 albedo texture for a warm sunny chibi Tiantai mountain game, stylized HAND PAINTED broad readable brush shapes, saturated warm natural palette, not photographic. Orthographic top down material surface fills every pixel. Seamless tileable all four edges, consistent scale throughout, diffuse flat even lighting with NO directional cast shadows, no perspective, no horizon, no scene objects, no vignette, no text watermark characters swords shields or elephants. Material: weathered rounded warm-grey rock surface with irregular lush green moss islands, gentle softened cracks, hand-painted broad stone facets, no freestanding boulders.
```

## ground/riverbed_pebble.png

```text
Use case: stylized-concept. Exactly ONE square 1024x1024 albedo texture for a warm sunny chibi Tiantai mountain game, stylized HAND PAINTED broad readable brush shapes, saturated warm natural palette, not photographic. Orthographic top down material surface fills every pixel. Seamless tileable all four edges, consistent scale throughout, diffuse flat even lighting with NO directional cast shadows, no perspective, no horizon, no scene objects, no vignette, no text watermark characters swords shields or elephants. Material: closely packed small rounded riverbed pebbles, muted warm grey ochre jade and beige stones, gentle painted facets, shallow dark recesses, NO water or caustics.
```

## ground/village_stone.png

```text
Use case: stylized-concept. Exactly ONE square 1024x1024 albedo texture for a warm sunny chibi Tiantai mountain game, stylized HAND PAINTED broad readable brush shapes, saturated warm natural palette, not photographic. Orthographic top down material surface fills every pixel. Seamless tileable all four edges, consistent scale throughout, diffuse flat even lighting with NO directional cast shadows, no perspective, no horizon, no scene objects, no vignette, no text watermark characters swords shields or elephants. Material: ancient immortal village paving, large softly worn warm beige grey stone slabs in an irregular fitted tessellation, narrow moss-green joints, hand-painted chunky beveled edges, no decorative symbols.
```

## foliage/tall_grass.png

```text
Use case: stylized-concept. Generate exactly ONE isolated foliage billboard card PNG, square 1024x1024. TRUE transparent background alpha, no white or checkerboard image, no floor ground base pot or cast shadow. Front-facing plant cluster, entire silhouette visible with generous 8 percent transparent margins, plant grows from bottom center. Cute stylized hand-painted game art with chunky readable leaves matching warm sunny Tiantai chibi forest, saturated warm yellow-greens with gentle golden highlights, soft shading, not photo. No text watermark characters swords shields elephants. Subject: one tuft of long tapering arching grass blades, lush layered green blades, clean narrow silhouette tips and transparent gaps between blades.
```

## foliage/fern.png

```text
Use case: stylized-concept. Generate exactly ONE isolated foliage billboard card PNG, square 1024x1024. TRUE transparent background alpha, no white or checkerboard image, no floor ground base pot or cast shadow. Front-facing plant cluster, entire silhouette visible with generous 8 percent transparent margins, plant grows from bottom center. Cute stylized hand-painted game art with chunky readable leaves matching warm sunny Tiantai chibi forest, saturated warm yellow-greens with gentle golden highlights, soft shading, not photo. No text watermark characters swords shields elephants. Subject: one spreading fern clump, several arching stems with recognizably paired fern leaflets, soft yellow green new growth and deeper green mature fronds.
```

## foliage/fern.png

```text
Use case: stylized-concept. Generate exactly ONE isolated foliage billboard card PNG, square 1024x1024. Perfectly uniform flat solid MAGENTA #FF00FF background for chroma keying, completely hard clean plant silhouette, no halo no glow no fog, opaque plant leaves, no pink on subject, no floor ground base pot or cast shadow. Front-facing plant cluster, entire silhouette visible with generous 8 percent transparent margins, plant grows from bottom center. Cute stylized hand-painted game art with chunky readable leaves matching warm sunny Tiantai chibi forest, saturated warm yellow-greens with gentle golden highlights, soft shading, not photo. No text watermark characters swords shields elephants. Subject: one compact fully visible spreading fern clump with arching paired leaflets, transparent gaps to be keyed, every frond entirely within canvas, warm jade green and golden-green new growth, no haze between leaves.
```

## foliage/wildflowers.png

```text
Use case: stylized-concept. Generate exactly ONE isolated foliage billboard card PNG, square 1024x1024. Perfectly uniform flat solid MAGENTA #FF00FF background for chroma keying, completely hard clean plant silhouette, no halo no glow no fog, opaque plant leaves, no pink on subject, no floor ground base pot or cast shadow. Front-facing plant cluster, entire silhouette visible with generous 8 percent transparent margins, plant grows from bottom center. Cute stylized hand-painted game art with chunky readable leaves matching warm sunny Tiantai chibi forest, saturated warm yellow-greens with gentle golden highlights, soft shading, not photo. No text watermark characters swords shields elephants. Subject: one small clump of wild daisies, white petals with golden yellow centers on green leafy stems, several flowers at different heights, NO pink or purple petals.
```

## foliage/shrub.png

```text
Use case: stylized-concept. Generate exactly ONE isolated foliage billboard card PNG, square 1024x1024. Perfectly uniform flat solid MAGENTA #FF00FF background for chroma keying, completely hard clean plant silhouette, no halo no glow no fog, opaque plant leaves, no pink on subject, no floor ground base pot or cast shadow. Front-facing plant cluster, entire silhouette visible with generous 8 percent transparent margins, plant grows from bottom center. Cute stylized hand-painted game art with chunky readable leaves matching warm sunny Tiantai chibi forest, saturated warm yellow-greens with gentle golden highlights, soft shading, not photo. No text watermark characters swords shields elephants. Subject: one low broad rounded woodland shrub, compact small overlapping oval leaves, branching woody stems visible at base, no flowers or fruit, fully framed.
```

## foliage/bamboo.png

```text
Use case: stylized-concept. Generate exactly ONE isolated foliage billboard card PNG, square 1024x1024. Perfectly uniform flat solid MAGENTA #FF00FF background for chroma keying, completely hard clean plant silhouette, no halo no glow no fog, opaque plant leaves, no pink on subject, no floor ground base pot or cast shadow. Front-facing plant cluster, entire silhouette visible with generous 8 percent transparent margins, plant grows from bottom center. Cute stylized hand-painted game art with chunky readable leaves matching warm sunny Tiantai chibi forest, saturated warm yellow-greens with gentle golden highlights, soft shading, not photo. No text watermark characters swords shields elephants. Subject: one compact miniature bamboo thicket of three slender segmented bamboo canes with lanceolate leaf sprays, whole stems and tips visible, no clipping.
```

## foliage/hanging_vine.png

```text
Use case: stylized-concept. Generate exactly ONE isolated foliage billboard card PNG, square 1024x1024. Perfectly uniform flat solid MAGENTA #FF00FF background for chroma keying, completely hard clean plant silhouette, no halo no glow no fog, opaque plant leaves, no pink on subject, no floor ground base pot or cast shadow. Front-facing plant cluster, entire silhouette visible with generous 8 percent transparent margins, vines hang down from a short horizontal woody stem near top center. Cute stylized hand-painted game art with chunky readable leaves matching warm sunny Tiantai chibi forest, saturated warm yellow-greens with gentle golden highlights, soft shading, not photo. No text watermark characters swords shields elephants. Subject: one hanging vine curtain of five loosely separated trailing tendrils with small heart shaped warm green leaves, distinct lengths, entire top attachment and bottom tips visible with margins, suspended from above, NOT growing upward from the ground.
```

## ui/peach_full.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: one whole full ripe pink-and-golden peach as a hunger meter symbol, rounded heartlike peach silhouette with a central cleft, tiny brown stem and one bright green leaf, delicate thin warm golden contour, bright filled peach flesh color, no frame.
```

## ui/peach_half.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: Edit the supplied full peach HUD symbol into HALF hunger state. Preserve EXACT same entire peach silhouette, position, stem, leaf, scale and fine warm gold contour. LEFT HALF peach fruit remains ripe pink and golden filled; RIGHT HALF fruit becomes dark muted desaturated brown empty inset. Vertical division at middle, intact whole fruit shape, not a cut peach or bite. No fruit pit, no new props. Match reference precisely.
```

## ui/peach_empty.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: Edit the supplied full peach HUD symbol into EMPTY hunger state. Preserve EXACT same entire peach silhouette, position, stem, leaf, scale and thin warm golden contour. Entire fruit interior now dark muted desaturated brown inset, only gold edge visible as empty peach shaped meter. Whole intact outline, no bite or fruit pit, no new props. Match reference precisely.
```

## ui/bark.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: a small curled strip of PAPER MULBERRY TREE BARK, fibrous pale tan inner bark with rough brown outer bark edge, two short curled fibrous strips stacked, resource inventory icon, NOT grain husk or cereal, no frame.
```

## ui/skill_bark.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: one circular dark walnut wooden skill medallion with thin warm gold rim, center pictogram of a short upright paper mulberry branch with its tan fibrous bark peeling back in one broad curl, simple readable harvesting bark action, no axe no weapon no letters. Icon and wooden disc opaque, outside disc transparent.
```

## ui/skill_water.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: one circular dark walnut wooden skill medallion with thin warm gold rim matching the bark skill, center large clean turquoise blue water droplet above a small cupped tan hand, readable collect water action. Icon and wooden disc opaque, outside disc transparent, no text or glow.
```

## ui/skill_call.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: one circular dark walnut wooden skill medallion with thin warm gold rim matching other skills, center simple warm cream side profile human head calling with a cupped hand near mouth and three gold sound wave arcs pointing right. Clear call companion action pictogram, no speech words no megaphone, no actual full character. Opaque medallion, transparent outside.
```

## ui/portrait_ruanzhao.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: Input image is IDENTITY reference of Ruan Zhao, use EXACT same friendly young Chinese chibi face, huge brown eyes, dark brown parted hair and small topknot, broad woven straw conical hat, navy blue Han cross collar tunic and cream collar. Create close-up head and shoulders circular HUD portrait, head much larger than torso, face centered fully readable at 256px. No staff or basket needed in this close crop. Entire circular portrait with muted warm sage backdrop INSIDE circle, outside circle truly transparent, no wooden frame (separate asset). Do not redesign as another character, no red cap.
```

## ui/portrait_frame.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: one EMPTY perfectly circular dark walnut WOODEN RING portrait frame with thin beveled warm GOLD inner and outer rims, restrained tiny gold cornerless accents, about 12 percent ring thickness. Center hole must be genuinely TRANSPARENT alpha, outside ring also transparent. Nothing inside hole, no portrait, no wood disc, no backdrop. Full frame with margin. Straight front view, symmetrical, not tilted or perspective.
```

## ui/minimap_frame.png

```text
Use case: stylized-concept. Exactly ONE square game HUD icon, designed to be clear at 256x256. Cute chunky hand-painted sculpted chibi game style, warm golden edge accents and soft controlled shading matching warm wood and gold HUD of Tiantai game. Single centered subject fully visible with 10 percent margin. True transparent alpha background, no floor shadow, no halo or glow, no checkerboard. No text letters numbers watermark, no sword shield elephant or original video character. Subject: one EMPTY SQUARE walnut wooden minimap frame, thin warm gold inner and outer trim and small restrained gold corner brackets, exact front view with parallel horizontal and vertical edges, not perspective. Frame about 8 percent width. Large square center opening genuinely TRANSPARENT alpha, outside frame transparent also. Nothing inside the hole, no map no compass no letters no backing disc. Entire square frame visible with margin.
```
