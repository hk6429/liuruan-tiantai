#!/usr/bin/env bash
# 絕不部署 Vercel（2026-08-24 起全域暫停）。
#
# 用法（在專案根目錄）：
#   scripts/deploy.sh <版本號，例如 0.1.0>
#   scripts/deploy.sh --dry-run <版本號>   # 只做唯讀檢查並印出每一步指令，不寫檔、不上傳
#
# 首次部署前：
#   - Cloudflare：npx wrangler pages project create liuruan-tiantai --production-branch main
#   - .cf-branch 內容必須是 main（deploy-gate hook 會檢查）。
# 部署完一定要跑 scripts/verify.sh 回讀比對 md5。
set -euo pipefail

SITE=liuruan-tiantai
DRY=0
[ "${1:-}" = "--dry-run" ] && { DRY=1; shift; }
VERSION="${1:?請給版本號，例如 scripts/deploy.sh 0.1.0}"
# run：dry-run 時只印指令；正式模式印出後執行
run() { echo "+ $*"; [ "$DRY" = 1 ] || "$@"; }

cd "$(dirname "$0")/.."
: "${NETLIFY_SITE_ID:?請先 export NETLIFY_SITE_ID=<Site ID>}"
[ -f Build/WebGL/index.html ] || { echo "找不到 Build/WebGL/index.html，請先在 Unity 打包 WebGL"; exit 1; }
CF_BRANCH=$(tr -d ' \n' < .cf-branch)
[ "$CF_BRANCH" = "main" ] || { echo ".cf-branch 不是 main"; exit 1; }
# nameFilesAsHashes：檔名每次打包都不同，index.html 必須引用 Build/ 裡實際存在的 loader 與 .unityweb
for f in Build/WebGL/Build/*.loader.js Build/WebGL/Build/*.unityweb; do
  [ -e "$f" ] || { echo "Build/WebGL/Build 缺 loader 或 .unityweb"; exit 1; }
  grep -q "$(basename "$f")" Build/WebGL/index.html || { echo "index.html 沒引用 $(basename "$f")，Build 可能不完整"; exit 1; }
done
# R30：Main 場景改成 StreamingAssets/scene/main.N＋parts.json（Tools/Liuruan/Build/WebGL + Scene Bundle 產生）。
#      .bundle-done 在 BuildPlayer 與切塊全部成功後才寫，必須比 loader 新；只跑 BuildPlayer（manage_build）不算完整建置。
LOADER=$(ls Build/WebGL/Build/*.loader.js | head -1)
[ -f Build/WebGL/.bundle-done ] && [ Build/WebGL/.bundle-done -nt "$LOADER" ] \
  || { echo "缺 Build/WebGL/.bundle-done 或比 loader 舊：請用 Tools/Liuruan/Build/WebGL + Scene Bundle 重建"; exit 1; }
python3 - Build/WebGL/StreamingAssets/scene <<'PY_SCENE'
import json, os, sys
d = sys.argv[1]
p = json.load(open(os.path.join(d, "parts.json")))
names = {f"{p['name']}.{i}" for i in range(p["parts"])}
size = sum(os.path.getsize(os.path.join(d, n)) for n in names)
extra = set(os.listdir(d)) - names - {"parts.json", ".DS_Store"}
if size != p["bytes"] or extra or p["parts"] < 1:
    sys.exit(f"scene 切塊不一致：parts 總和 {size}／parts.json {p['bytes']}，多餘檔案 {sorted(extra)}")
print(f"scene bundle OK：{p['parts']} 塊，{p['bytes']} bytes")
PY_SCENE
[ "$DRY" = 1 ] && echo "【dry-run】只印指令，不寫檔、不上傳。版本 v${VERSION}，CF branch=${CF_BRANCH}"

# 1. 同步到部署資料夾（--delete 會清掉上一版的舊雜湊檔）
run rsync -a --delete --exclude '.DS_Store' --exclude '.bundle-done' Build/WebGL/ Deploy/site/

# R21：只處理部署副本；資產逐檔自檢後再更新 data 雜湊與 index。
run python3 scripts/compact_webgl_data.py Deploy/site

# 2. 手機殼補丁（冪等，標記 mobile-patch）：只改 Deploy/site 的副本，不動 Build/
run python3 scripts/patch_index.py Deploy/site/index.html
run python3 scripts/patch_mobile_entry.py Deploy/site/index.html
run python3 scripts/patch_loading_recovery.py Deploy/site/index.html

# 2.5 單檔大小閘門：Cloudflare Pages 單檔上限 25 MiB（26214400 bytes），Round10 原留 1 MiB 邊際；R18 data 26,114,902 介於 24～25 MiB，閘門改為貼齊 CF 上限
#     dry-run 時 Deploy/site 尚未同步，改檢查 Build/WebGL
LIMIT=$((25 * 1024 * 1024))
SRC=Deploy/site; [ "$DRY" = 1 ] && SRC=Build/WebGL
big=$(find "$SRC" -type f -size +"$((LIMIT - 1))"c)
if [ -n "$big" ]; then
  echo "有檔案達到或超過 25 MiB，Cloudflare Pages 會拒收："
  for f in $big; do echo "  $(stat -f %z "$f") bytes  $f"; done
  echo "請縮小資產或重建場景包"; exit 1
fi
find "$SRC/Build" "$SRC/StreamingAssets/scene" -type f -exec stat -f '%z %N' {} + | sort -rn | awk '{printf "  %6.2f MiB  %s\n", $1/1048576, $2}'

# 3. 產生 _headers（.unityweb 送 Content-Encoding: br、正確 Content-Type、immutable 快取）
run python3 scripts/make_headers.py Deploy/site
run python3 scripts/prepare_wasm_delivery.py Deploy/site

# 4. Netlify：由 CC 設定 NETLIFY_SITE_ID。
run netlify deploy --dir Deploy/site --prod --site "$NETLIFY_SITE_ID" --message "v$VERSION"

# 5. Cloudflare Pages（--branch 取自 .cf-branch＝main；沒帶只會出 Preview）
run npx wrangler pages deploy Deploy/site --project-name "$SITE" --branch "$CF_BRANCH" --commit-dirty=true

if [ "$DRY" = 1 ]; then echo "【dry-run】結束，未執行任何寫入或上傳。"; else echo "部署指令跑完。接著執行：scripts/verify.sh"; fi

