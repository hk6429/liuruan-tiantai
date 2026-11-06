#!/usr/bin/env python3
"""讓 FB／Messenger 與直向手機有可關閉的遊戲入口提示。"""
from pathlib import Path
import sys

PATCH = '''<!-- mobile-entry-patch -->
<style>
#rotate-hint{display:none!important}
@media(orientation:portrait) and (max-width:600px){html.netlify-touch body{background:#231f20}html.netlify-touch #unity-container.unity-mobile{height:calc(100% - 64px)}}
#mobile-entry{position:fixed;z-index:10001;left:12px;right:12px;bottom:max(12px,env(safe-area-inset-bottom));max-width:520px;margin:auto;padding:18px;box-sizing:border-box;border:2px solid #caa65c;border-radius:12px;background:#fff8e9;color:#362619;box-shadow:0 4px 24px #0007;font:16px/1.6 system-ui,"PingFang TC","Microsoft JhengHei",sans-serif}
#mobile-entry[hidden]{display:none}
#mobile-entry h2{font-size:20px;margin:0 0 8px}
#mobile-entry p{margin:0 0 12px}
#mobile-entry .entry-actions{display:flex;flex-wrap:wrap;gap:10px}
#mobile-entry button{flex:1;min-width:120px;min-height:44px;padding:8px 12px;border:1px solid #805b25;border-radius:7px;background:#fff8e9;color:#362619;font:inherit;cursor:pointer}
#mobile-entry #entry-continue{background:#d9ae53;font-weight:700}
#mobile-entry button:focus-visible,#mobile-entry input:focus-visible{outline:3px solid #6d361b;outline-offset:2px}
#mobile-entry input{box-sizing:border-box;width:100%;font:16px/1.5 system-ui;padding:8px;margin-top:10px}
#entry-status{font-size:14px;margin-top:8px}
@media(max-height:420px){#mobile-entry{padding:12px;max-height:calc(100% - 24px);overflow:auto}#mobile-entry h2{font-size:18px}#mobile-entry p{font-size:14px;margin-bottom:8px}}
</style>
<script>
(function () {
  var ua = navigator.userAgent;
  var embedded = /FBAN|FBAV|FB_IAB|FB4A|FBIOS|Messenger/i.test(ua);
  var mobile = /iPhone|iPad|iPod|Android/i.test(ua) || (navigator.maxTouchPoints > 1 && /Macintosh/.test(ua));
  if (mobile && location.hostname.endsWith('.netlify.app')) document.documentElement.classList.add('netlify-touch');
  if (!mobile && !embedded) return;
  var notice = document.createElement('section');
  notice.id = 'mobile-entry';
  notice.setAttribute('role', 'region');
  notice.setAttribute('aria-label', '手機遊玩提示');
  notice.innerHTML = '<h2></h2><p></p><div class="entry-actions"><button type="button" id="entry-continue">繼續遊戲</button><button type="button" id="entry-copy">複製遊戲網址</button></div><div id="entry-status" role="status" aria-live="polite"></div><input id="entry-url" aria-label="遊戲網址，可長按複製" readonly hidden>';
  notice.querySelector('h2').textContent = embedded ? '從 Facebook 或 Messenger 開啟' : '橫向遊玩會更清楚';
  notice.querySelector('p').textContent = embedded ? '可以先點「繼續遊戲」。若無法載入、不能轉向或操作不順，請用右上角「⋯」選單，以 Safari 或 Chrome 開啟；也可以先複製遊戲網址。' : '建議將手機轉成橫向；若無法轉向，也可以先繼續遊戲。';
  var dismissed = false;
  function place() { notice.hidden = dismissed || (!embedded && window.innerWidth >= window.innerHeight); }
  document.body.appendChild(notice);
  place();
  window.addEventListener('resize', place);
  notice.querySelector('#entry-continue').addEventListener('click', function () {
    dismissed = true;
    notice.remove();
    window.removeEventListener('resize', place);
    var canvas = document.querySelector('canvas');
    if (canvas) canvas.focus();
  });
  notice.querySelector('#entry-copy').addEventListener('click', async function () {
    var url = 'https://liuruan-tiantai.pages.dev/';
    var status = notice.querySelector('#entry-status');
    try {
      if (!navigator.clipboard || !navigator.clipboard.writeText) throw new Error('clipboard unavailable');
      await navigator.clipboard.writeText(url);
      status.textContent = '網址已複製，可貼到 Safari 或 Chrome 開啟。';
    } catch (error) {
      var input = notice.querySelector('#entry-url');
      input.value = url;
      input.hidden = false;
      input.focus();
      input.select();
      status.textContent = '這個瀏覽器無法自動複製，請長按下方網址並選擇複製。';
    }
  });
  ['keydown', 'keyup', 'keypress'].forEach(function (name) {
    notice.addEventListener(name, function (event) { event.stopPropagation(); });
  });
})();
</script>
'''


def patch(path):
    original = path.read_text()
    if '<!-- mobile-entry-patch -->' in original:
        return False
    if 'mobile-patch' not in original or 'rotate-hint' not in original or original.count('</body>') != 1:
        raise ValueError('需要已套用 mobile-patch 的唯一 WebGL 頁面')
    path.write_text(original.replace('</body>', PATCH + '</body>'))
    return True


if __name__ == '__main__':
    patch(Path(sys.argv[1]))
