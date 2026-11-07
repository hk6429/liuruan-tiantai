# A 線 R1 交件報告

日期：2026-11-07。狀態：美術檔案已交付並完成磁碟與離線影像檢查；尚未進入 Unity 驗收。

## 完成內容與檔案

所有美術檔案位於 `staging/art/R1/`。只寫本輪美術目錄與本報告，沒有修改其他線、沒有開 Unity、沒有推送遠端。

| 目錄 | 遊戲素材 | 尺寸與數量 |
|---|---|---|
| sky/ | sky_day.png、sky_return.png | RGB，2048×1024，共 2 張 |
| ground/ | grass.png、dirt_path.png、moss_rock.png、riverbed_pebble.png、village_stone.png | RGB albedo，1024²，共 5 張 |
| ground/ | 上述五種各自的 *_n.png | RGB，1024²，Sobel 近似法線，共 5 張 |
| foliage/ | tall_grass.png、fern.png、wildflowers.png、shrub.png、bamboo.png、hanging_vine.png | 真透明 RGBA，1024²，共 6 張 |
| foliage/ | foliage_atlas.png | 真透明 RGBA，2048²，兩欄三列，共 1 張 |
| ui/ | peach_full.png、peach_half.png、peach_empty.png、bark.png、skill_bark.png、skill_water.png、skill_call.png、portrait_ruanzhao.png、portrait_frame.png、minimap_frame.png | 真透明 RGBA，256²，共 10 張 |

正式遊戲素材合計 **29 張 PNG**。附加檢查圖共 9 張：ground/ 五張 `*_tilecheck.png`（2048²）、sky/ 兩張 `*_wrapcheck.png`（4096×1024）、foliage/foliage_contact.png、ui/ui_contact.png。檢查圖上的英文檔名僅供查對；正式遊戲素材無文字與浮水印，未出現影片原角色、劍盾、小象。

其他檔案：

- `prompts.md`：全部實際提示詞、採用與重生說明、後製方式與參考圖。
- `raw/sky/`、`raw/ground/`、`raw/foliage/`、`raw/ui/`：23 張採用原稿，供後製重跑；不是正式素材，不應整包匯入 Unity。
- `foliage/atlas_layout.json`：兩欄三列的像素矩形與 Unity 左下原點 UV。
- `process_assets.py`：本輪後製與 assert 驗證。依賴 Pillow、NumPy；本機套件快取已存在，沒有下載成功或新增專案依賴的宣稱。
- `validate_assets.json`：38 張正式圖與檢查圖的尺寸、模式、大小、SHA-256、接縫差值與透明比例。
- `.venv/`：本輪 uv 建立的 Python 環境；套件從本機既有唯讀快取載入。

## 風格與內容

已讀 AGENTS.md、docs/plan.md 第五節與 HUD 規格，檢視 staging/ref/ 八張、concept 主場景與阮肇造型。暖陽、飽和暖綠、圓潤風格化手繪；歸鄉天空使用灰冷傍晚色。穀皮為楮樹纖維樹皮，並非穀殼。阮肇頭像直接以 06_ruanzhao.png 參考生成，保留斗笠、深色髮型、大眼與深藍交領衣。

24 次內建 image_gen 呼叫均單張序列執行，每張生成後立刻複製到指定路徑並以 Pillow 開啟、檢查大小。蕨初稿透明光暈不採用，重生純洋紅底後去背；長草保留生成 alpha，其餘五種植物以色差與縮邊去背。草叢圖集順序由上而下、每列左至右：長草／蕨、野花／矮灌木、竹叢／藤蔓。

## 驗證證據

最終執行輸出：

```text
PASS: 38 PNG 檔案可解碼、尺寸與透明通道正確；地面雙向與天空水平邊界差值為 0。
```

實際執行指令（從專案根目錄）：

```sh
pwd
PYTHONPATH=/Users/naichengchen/.cache/uv/archive-v0/1kSD84CsePHI9Fuk:/Users/naichengchen/.cache/uv/archive-v0/7OT-65SDVFmw2fA1 staging/art/R1/.venv/bin/python staging/art/R1/process_assets.py
```

檢查包含：

- 38 張均從磁碟重新開啟、解碼、核對尺寸，且雜湊沒有重複。
- 五種 albedo 與五種 normal：左右／上下對應最外緣像素最大差值均為 0。
- 天空兩張：2048×1024，左右邊界逐像素一致，上下極點列各自為單色。
- 六張植物、圖集、十張 UI：同時具有 alpha=0 與 alpha=255，四邊透明；兩個框中心 alpha=0。
- 內建小型 self-check：平面 Sobel 結果為 (128,128,255)，去背保留綠色前景並清除洋紅背景。
- 目視檢查最終五張 2×2 地面拼貼、兩張天空 wrapcheck、草叢圖集與草叢／UI 深綠背景總覽。移除初版地面接縫的寬幅混色重影，改用最低差異路徑裁接；清除草叢洋紅色溢邊。

## 未做到、限制與下一步

- **未開 Unity，也未做材質實際球面映射、WebGL 或手機實測。** U 線需要驗收全景仰俯視畸變、接縫、植被 Alpha Clip 與遠距 mipmap 表現。
- 天空生成原稿實際為 **1774×887**，已等比例放大至 2048×1024；並非原生 2048×1024，也不是 HDR。投影、極點與接縫已離線處理，不能取代 Unity 球面驗收。
- 法線為從 albedo 亮度推導的近似值，含手繪明暗影響；U 線匯入需設為 Normal map、從較低強度開始試，不代表實測幾何。
- 地面最外緣像素相同不代表不存在重複紋樣；仙村大石板仍可見規律重複，實景可透過鋪設比例與裝飾遮蔽評估接受度。
- 草叢單張是 1024²；圖集每格寬 1024、高 682 或 683，保留 32 px 透明留白，UV 以 atlas_layout.json 的整數矩形為準。透明四周不代表已驗證所有濾波／壓縮條件。
- 本輪限寫指定美術目錄與交件報告，未另存 Obsidian。
