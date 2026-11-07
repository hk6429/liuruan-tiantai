# R2 離線資產核對

PASS：29 PNG＋2 FBX 複製完整、六格圖集 UV 合法；六支未調整的 S 線 R1 原始碼及 R1CoreTests 與 staging 相同。MinimapCam 僅修改 HUD 框定位／顯示／框材質疊加。

已目視五張 *_tilecheck：村落石板有規律重複；初始地形 tile 8m、村落 6m、步道每 4m 重複，仍須引擎機位驗收。

- `Assets/_Project/Art/R1/foliage/bamboo.png`：1024×1024，447592 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/foliage/fern.png`：1024×1024，862777 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/foliage/foliage_atlas.png`：2048×2048，2461974 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/foliage/hanging_vine.png`：1024×1024，484597 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/foliage/shrub.png`：1024×1024，735947 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/foliage/tall_grass.png`：1024×1024，596998 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/foliage/wildflowers.png`：1024×1024，746602 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/dirt_path.png`：1024×1024，1284605 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/dirt_path_n.png`：1024×1024，688387 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/grass.png`：1024×1024，1526879 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/grass_n.png`：1024×1024，942944 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/moss_rock.png`：1024×1024，1517076 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/moss_rock_n.png`：1024×1024，811502 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/riverbed_pebble.png`：1024×1024，1654659 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/riverbed_pebble_n.png`：1024×1024，1010180 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/village_stone.png`：1024×1024，1429181 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ground/village_stone_n.png`：1024×1024，818543 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/sky/sky_day.png`：2048×1024，1898638 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/sky/sky_return.png`：2048×1024，1583147 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/bark.png`：256×256，84128 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/minimap_frame.png`：256×256，38203 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/peach_empty.png`：256×256，53667 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/peach_full.png`：256×256，61066 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/peach_half.png`：256×256，62124 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/portrait_frame.png`：256×256，52252 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/portrait_ruanzhao.png`：256×256，100668 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/skill_bark.png`：256×256，96666 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/skill_call.png`：256×256，92556 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/R1/ui/skill_water.png`：256×256，87019 bytes；SHA-256 與 staging 相同。
- `Assets/_Project/Art/Characters/liuchen.fbx`：1601692 bytes；SHA-256 `fec9f06db9f7b0923f0e8aaf812009b27b57a81014da5002d9303618f96f8aa9`，與 staging 相同。
- `Assets/_Project/Art/Characters/ruanzhao.fbx`：1791836 bytes；SHA-256 `b3f1040b8dbc9cb85569bc3fa685e3f0431f75665e0ac4bc9e439d24b1e3d98e`，與 staging 相同。

## R2b 資產檢查（2026-11-07）

10 Mixamo FBX＋2 獨立 albedo PNG 以 SHA-256 與 staging 比對一致；S R2 六支 Runtime 與 R2CoreTests 逐位元組一致。舊 FBX／無副檔名抽取檔未刪除，新流程不引用。

- Assets/_Project/Art/Characters/liuchen/liuchen@run.fbx
- Assets/_Project/Art/Characters/liuchen/liuchen@idle.fbx
- Assets/_Project/Art/Characters/liuchen/liuchen@climb.fbx
- Assets/_Project/Art/Characters/liuchen/liuchen@walk.fbx
- Assets/_Project/Art/Characters/liuchen/liuchen@pickup.fbx
- Assets/_Project/Art/Characters/liuchen/liuchen@pickfruit.fbx
- Assets/_Project/Art/Characters/liuchen/liuchen_albedo.png
- Assets/_Project/Art/Characters/ruanzhao/ruanzhao@idle.fbx
- Assets/_Project/Art/Characters/ruanzhao/ruanzhao@run.fbx
- Assets/_Project/Art/Characters/ruanzhao/ruanzhao@walk.fbx
- Assets/_Project/Art/Characters/ruanzhao/ruanzhao@climb.fbx
- Assets/_Project/Art/Characters/ruanzhao/ruanzhao_albedo.png
