#!/usr/bin/env python3
"""在部署副本加入 Unity 載入失敗的繁中提示與手動恢復入口。"""
from pathlib import Path
import sys

MARKER = '<!-- loading-recovery-patch -->'
STYLE = '''<!-- loading-recovery-patch -->
<style>
#loading-recovery{position:fixed;z-index:10002;left:12px;right:12px;top:50%;transform:translateY(-50%);max-width:520px;max-height:calc(100% - 32px);overflow:auto;margin:auto;padding:20px;box-sizing:border-box;border:2px solid #caa65c;border-radius:12px;background:#fff8e9;color:#362619;box-shadow:0 4px 24px #0009;font:16px/1.6 system-ui,"PingFang TC","Microsoft JhengHei",sans-serif}
#loading-recovery h2{font-size:21px;margin:0 0 8px}
#loading-recovery p{margin:0 0 12px}
#loading-recovery .recovery-actions{display:flex;flex-wrap:wrap;gap:10px}
#loading-recovery button,#loading-recovery a{flex:1;min-width:120px;min-height:44px;box-sizing:border-box;padding:8px 12px;border:1px solid #805b25;border-radius:7px;background:#fff8e9;color:#362619;font:inherit;text-align:center;text-decoration:none;cursor:pointer}
#loading-recovery button{background:#d9ae53;font-weight:700}
#loading-recovery button:focus-visible,#loading-recovery a:focus-visible{outline:3px solid #6d361b;outline-offset:2px}
#loading-recovery .recovery-save{font-size:14px;margin:12px 0 0}
</style>
'''
RECOVERY = '''      function showUnityLoadError(message) {
        console.error('Unity 載入失敗：', message);
        if (document.querySelector('#loading-recovery')) return true;
        document.querySelector('#unity-loading-bar').style.display = 'none';
        var notice = document.createElement('section');
        notice.id = 'loading-recovery';
        notice.setAttribute('role', 'alert');
        notice.setAttribute('aria-labelledby', 'loading-recovery-title');
        notice.innerHTML = '<h2 id="loading-recovery-title">遊戲未能載入</h2><p>請確認網路連線，再按「重新載入」。若仍無法開啟，可以改用備用網站；手機也可以用 Safari 或 Chrome 開啟。</p><div class="recovery-actions"><button type="button">重新載入</button><a>前往備用網站</a></div><p class="recovery-save">重新載入會保留本網站已儲存的進度；尚未儲存的操作可能不會保留。備用網站的進度分開儲存，不會自動同步。</p>';
        notice.querySelector('a').href = location.hostname === 'liuruan-tiantai.pages.dev' ? 'https://liuruan-tiantai.netlify.app/' : 'https://liuruan-tiantai.pages.dev/';
        notice.querySelector('button').addEventListener('click', function () { location.reload(); });
        ['keydown', 'keyup', 'keypress'].forEach(function (name) {
          notice.addEventListener(name, function (event) { event.stopPropagation(); });
        });
        document.body.appendChild(notice);
        notice.querySelector('button').focus();
        return true;
      }
      config.errorHandler = showUnityLoadError;

'''


def patch(path):
    original = path.read_text()
    if MARKER in original:
        return False
    replacements = [
        ('</head>', STYLE + '</head>'),
        ('function unityShowBanner(msg, type) {',
         "function unityShowBanner(msg, type) {\n        if (type === 'error') { showUnityLoadError(msg); return; }"),
        ('      var script = document.createElement("script");',
         RECOVERY + '      var script = document.createElement("script");'),
        ('      script.src = loaderUrl;',
         "      script.src = loaderUrl;\n      script.onerror = function () { showUnityLoadError('loader download failed'); };"),
        ('                alert(message);', '                showUnityLoadError(message);'),
    ]
    if '<!-- mobile-entry-patch -->' not in original:
        raise ValueError('請先套用 mobile-entry-patch')
    for before, _ in replacements:
        if original.count(before) != 1:
            raise ValueError('Unity WebGL 模板不符，停止套用：' + before)
    for before, after in replacements:
        original = original.replace(before, after)
    path.write_text(original)
    return True


if __name__ == '__main__':
    patch(Path(sys.argv[1]))
