# 天台山・流杯記

以《幽明錄》〈劉晨阮肇入天台〉為題材的 Unity URP 國文教育遊戲。

- Unity：6000.3.27f1，URP 17.3.0，目標平台 WebGL。
- R0：BootLoader 分包骨架、240 × 240 公尺灰盒地形、森林／溪谷／仙村、16 個五幕定位點與第三人稱玩家。
- 計畫與規範：[docs/plan.md](docs/plan.md)、[AGENTS.md](AGENTS.md)。

## 建立灰盒

由 CC 在本機執行（R0 交付時尚未啟動 Unity）：

```sh
Unity -batchmode -quit -projectPath /Volumes/hk6429/projects/liuruan-tiantai -executeMethod LiuruanGreybox.Build -logFile logs/unity-R0.log
```

`Unity` 須指向 Unity 6000.3.27f1 執行檔。成功後產生 `Assets/_Project/Scenes/Boot.unity`、`Main.unity` 及地形、材質、屋頂網格；Build Settings 設為 Boot、Main。重跑會更新這兩個灰盒場景，請先保存後續手動修改。

Editor 選單：`Tools/Liuruan/Build/Greybox`。WebGL 分包建置：`Tools/Liuruan/Build/WebGL + Scene Bundle`，或 batchmode 執行 `Liuruan.SceneBundles.BuildAll`。

## 檢查

```sh
python3 scripts/test_r0_delivery.py
bash -n scripts/deploy.sh scripts/verify.sh
```

移動：WASD／方向鍵；按住滑鼠右鍵轉視角。手機沿用虛擬搖桿、拖曳轉視角與互動鍵。R0 的五幕點位僅為 Empty，阮肇尚無跟隨腳本，題庫尚未綁入場景；五幕玩法留待後續輪次。

## 部署

先用完整場景包選單建置，再由 CC 設定 `NETLIFY_SITE_ID` 環境變數；未設定時部署腳本會直接停止。`scripts/deploy.sh <版本號>` 處理部署副本並部署至 Netlify 與 Cloudflare Pages，完成後執行 `scripts/verify.sh`。目前未部署，未推 GitHub、Vercel 或 itch。

來源骨架僅讀取 `liulaolao-garden`；複製檔案保留 `.meta` GUID。詳細清單：[docs/U-R0-files.md](docs/U-R0-files.md)；交件與驗證界線：[logs/U-R0.md](logs/U-R0.md)。
