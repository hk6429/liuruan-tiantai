# U-R3 部署報告（v0.3.0，2026-11-08）

## 做了什麼
1. 測試：tools/check_clues.py、check_r3.py、check_teacher.py 全 PASS；scripts/test_r0_delivery.py 原本 1 失敗，原因是 R0 禁名表仍列 EpilogueDirector.cs（R3 自寫的 38 行 Liuruan 版，非劉姥姥 343 行那支）＋Unity 把 productName 重新序列化成 \uXXXX 轉義。更新測試後 3/3 OK。
2. commit a22900e：R3b 未提交修正（ClimbZone、StreamFlow.cs、HUD/NPC、字型子集、Captures、logs）＋測試更新。push 前已掃 xlsx/docx/csv/名冊/實名：無。
3. 部署：`NETLIFY_SITE_ID=7238bb04-… scripts/deploy.sh 0.3.0` → Netlify + Cloudflare Pages（branch main）。
4. 驗證：`scripts/verify.sh` 兩站 26 項全 OK（.unityweb br md5、loader identity md5、scene/main.0、parts.json、index 引用、wasm 標頭）。
5. GitHub：使用者本輪指定要推，覆寫 AGENTS.md「不推 GitHub」；建立公開 repo https://github.com/hk6429/liuruan-tiantai（master）。

## 線上網址
- https://liuruan-tiantai.pages.dev（對外推薦，無 Netlify 浮標）
- https://liuruan-tiantai.netlify.app

## 沒做到
- 未用瀏覽器實開看操作說明卡、未真機測手機；只做了 curl md5 回讀。
- ProjectSettings 的 cloud projectName 仍是 liulaolao-garden（Unity Cloud 欄位，不影響遊戲），未改。
