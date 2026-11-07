# A 線 R1：天空、地面貼圖、草叢卡片、UI 圖示

先讀 AGENTS.md、docs/plan.md 第五節、看 staging/ref/ 與 staging/art/concept/（已定調，風格要一致：風格化 Q 版、暖陽、飽和暖綠）。只能寫 staging/art/。生圖一次一張、序列。每個檔案存好後確認真的在磁碟上。交件報告寫 logs/A-R1.md，prompt 記在 staging/art/R1/prompts.md。

1. staging/art/R1/sky/：2:1 等距柱狀全景天空 2 張（2048×1024 以上）：sky_day.png（晴朗暖陽、遠山雲海、天台山奇峰）、sky_return.png（陰天灰冷、傍晚）。地平線要水平、左右邊可無縫接。
2. staging/art/R1/ground/：無縫平鋪貼圖 1024²，各一張 albedo：grass、dirt_path、moss_rock、riverbed_pebble、village_stone（仙村石板）。風格化手繪感，不要寫實照片。用 Python（Pillow，若無則用其他本機工具）做左右上下無縫處理與檢查（把貼圖 2×2 拼起來存 *_tilecheck.png）。各自再用 Python 從 albedo 生一張近似 normal（Sobel），檔名 *_n.png。
3. staging/art/R1/foliage/：去背 PNG（真透明背景）草叢卡片 6 種（長草、蕨、野花、矮灌木、竹叢、藤蔓垂條），排成 2×3 圖集 foliage_atlas.png（2048×2048），另存單張。若生圖無法直接透明，用綠幕／純色底生，再用 Python 去背並檢查 alpha。
4. staging/art/R1/ui/：HUD 圖示，透明 PNG 256²：peach_full、peach_half、peach_empty、bark（穀皮）、skill_bark、skill_water、skill_call、portrait_ruanzhao（圓形頭像，依 06_ruanzhao.png 的造型）、portrait_frame（木框金邊圓環）、minimap_frame（方形木框）。風格參考 staging/ref 的 HUD（暖金邊、木質）。
5. 不要文字、不要浮水印；不得出現影片原角色、劍盾、小象。
