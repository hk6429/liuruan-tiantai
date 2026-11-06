#!/usr/bin/env bash
# 部署後回讀驗證：線上檔案 md5 必須等於 Deploy/site 本機檔。只做唯讀 curl，不改任何檔案。
#
# 用法（在專案根目錄）：
#   scripts/verify.sh [網址 ...]
# 不給網址時預設驗兩站：https://liuruan-tiantai.netlify.app  https://liuruan-tiantai.pages.dev
# Cloudflare 專案名被佔用時網域會帶後綴（例如 liuruan-tiantai-abc.pages.dev），請自行列出兩個網址。
#
# 比對來源預設是 Deploy/site；部署預演時可用環境變數指定別的資料夾：
#   SITE_DIR=/path/to/site scripts/verify.sh http://127.0.0.1:8799
#
# 規則（見 unity-webgl-edu-game SKILL 第 9 節）：
#   *.unityweb  → Accept-Encoding: br 取原始位元組比 md5（不帶 br 時 CDN 會解壓，md5 必然不同）
#   *.loader.js → Accept-Encoding: identity 比 md5（帶 br 會拿到 CDN 即時壓縮版）
#   StreamingAssets/scene/*（main.N、parts.json）→ Accept-Encoding: identity 比 md5
#   index.html  → 平台會注入內容，不比 md5；改 grep Build 檔名與 mobile-patch 標記
#
# 本機模式（網址是 localhost／127.0.0.1）：python3 -m http.server 不讀 _headers、也不送
# Content-Encoding: br，所以只能驗「位元組一致、檔案 200、index 內容正確」；wasm 標頭檢查標為 SKIP。
set -uo pipefail

cd "$(dirname "$0")/.."
SITE_DIR="${SITE_DIR:-Deploy/site}"
[ -d "$SITE_DIR/Build" ] || { echo "找不到 $SITE_DIR/Build，請先跑 scripts/deploy.sh（或用 SITE_DIR 指定）"; exit 1; }
[ $# -gt 0 ] || set -- https://liuruan-tiantai.netlify.app https://liuruan-tiantai.pages.dev

fail=0
TMP=$(mktemp)
HDR=$(mktemp)
trap '/bin/rm -f "$TMP" "$HDR"' EXIT
CURL=(curl -s --max-time 120)

check_base() {
  local base="${1%/}" is_local=0
  case "$base" in http://localhost*|http://127.0.0.1*) is_local=1 ;; esac
  echo "== $base$([ $is_local = 1 ] && echo '（本機模式：不驗 br 標頭）')"

  local f name enc want got code
  for f in "$SITE_DIR"/Build/*; do
    name=$(basename "$f")
    case "$name" in
      *.unityweb)  enc="br" ;;
      *.loader.js) enc="identity" ;;
      *) continue ;;
    esac
    want=$(md5 -q "$f")
    code=$("${CURL[@]}" -D "$HDR" -o "$TMP" -w '%{http_code}' -H "Accept-Encoding: $enc" "$base/Build/$name")
    got=$(md5 -q "$TMP")
    if [ "$code" = "200" ] && [ "$want" = "$got" ]; then
      echo "  OK    $name ($enc)"
      if [ "$is_local" = 0 ] && [[ "$name" = *.wasm.unityweb ]]; then
        if grep -qi '^content-type: application/wasm' "$HDR" && grep -qi '^content-encoding: br' "$HDR"; then
          echo "  OK    wasm GET 標頭 br＋application/wasm"
        else
          echo "  FAIL  wasm GET 標頭不正確"; fail=1
        fi
      fi
    else
      echo "  FAIL  $name ($enc) http=$code 本機=$want 線上=$got"; fail=1
    fi
  done

  # R30：Main 場景切塊（main.N 無副檔名＋parts.json）。Netlify 會即時再壓 br，一律 identity 取原始位元組比 md5。
  local n=0
  for f in "$SITE_DIR"/StreamingAssets/scene/*; do
    [ -f "$f" ] || continue
    name=$(basename "$f"); n=$((n + 1))
    want=$(md5 -q "$f")
    code=$("${CURL[@]}" -o "$TMP" -w '%{http_code}' -H 'Accept-Encoding: identity' "$base/StreamingAssets/scene/$name")
    got=$(md5 -q "$TMP")
    if [ "$code" = "200" ] && [ "$want" = "$got" ]; then
      echo "  OK    scene/$name (identity)"
    else
      echo "  FAIL  scene/$name (identity) http=$code 本機=$want 線上=$got"; fail=1
    fi
  done
  [ "$n" -ge 2 ] || { echo "  FAIL  $SITE_DIR/StreamingAssets/scene 缺 parts.json 或切塊"; fail=1; }

  local html
  html=$("${CURL[@]}" -H 'Accept-Encoding: identity' -H 'Cache-Control: no-cache' "$base/")
  if grep -q 'mobile-patch' <<<"$html"; then
    echo "  OK    index.html 含 mobile-patch"
  else
    echo "  FAIL  index.html 缺 mobile-patch 標記"; fail=1
  fi
  for f in "$SITE_DIR"/Build/*; do
    name=$(basename "$f")
    # 只檢查 index.html 本來就引用的檔名（loader 與三個 .unityweb）
    grep -q "$name" "$SITE_DIR/index.html" || continue
    if grep -q "$name" <<<"$html"; then
      echo "  OK    index.html 引用 $name"
    else
      echo "  FAIL  index.html 沒有引用 $name（可能還是舊版或 CDN 未更新）"; fail=1
    fi
  done

  if [ $is_local = 1 ]; then
    echo "  SKIP  wasm 標頭（本機 http.server 不送 Content-Encoding: br，請在線上驗）"
    return
  fi
  local hdr
  hdr=$("${CURL[@]}" -I -H 'Accept-Encoding: br' "$base/Build/$(cd "$SITE_DIR/Build" && ls *.wasm.unityweb | head -1)")
  if grep -qi '^content-encoding: br' <<<"$hdr" && grep -qi '^content-type: application/wasm' <<<"$hdr"; then
    echo "  OK    wasm 標頭 br＋application/wasm"
  else
    echo "  FAIL  wasm 標頭不對（_headers 沒生效？）"; fail=1
  fi
}

for u in "$@"; do check_base "$u"; done

if [ "$fail" = 0 ]; then
  echo "全部通過。最後一步：用瀏覽器實開，看到操作說明卡才算數；手機修正請用真機確認。"
else
  echo "有項目失敗，不能說部署完成。"; exit 1
fi
