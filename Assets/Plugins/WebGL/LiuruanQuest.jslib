// R17 任務層：創作題的 HTML 輸入框（注音／倉頡、手機鍵盤都能用）＋成果卡 PNG 下載／長按儲存。
// 輸入框蓋在 Unity 畫布上，位置用畫布比例（0–1，左上為原點）；C# 每幀輪詢狀態，不用 SendMessage。
mergeInto(LibraryManager.library, {
  LiuruanSavePngOv__deps: ['LiuruanSavePng'],
  // 開啟輸入框。multi=1 為多行 textarea，0 為單行 input。x,y,w,h：畫布比例（左上原點）
  // hotfix1：「寫好了」與字數放在框內上方工具列；框一律夾在畫布可見範圍內，
  // 視窗縮放／捲動／全螢幕／手機鍵盤（visualViewport）時每 250ms 重算位置。
  LiuruanCssPixelRatio: function () {
    var canvas = Module['canvas'] || document.querySelector('canvas');
    var width = canvas ? canvas.getBoundingClientRect().width : 0;
    var ratio = width > 0 ? canvas.width / width : 1;
    return isFinite(ratio) && ratio > 0 ? ratio : 1;
  },

  LiuruanTextOpen: function (initPtr, placeholderPtr, doneLabelPtr, maxChars, multi, x, y, w, h, readOnly) {
    try {
      var init = UTF8ToString(initPtr), ph = UTF8ToString(placeholderPtr), doneLabel = UTF8ToString(doneLabelPtr);
      var old = document.getElementById('ll-text-wrap'); if (old) { if (old.__llStop) old.__llStop(); old.parentNode.removeChild(old); }
      var canvas = Module['canvas'] || document.querySelector('canvas');
      var wrap = document.createElement('div');
      wrap.id = 'll-text-wrap';
      var el = document.createElement(multi ? 'textarea' : 'input');
      el.id = 'll-text';
      if (!multi) el.type = 'text';
      el.value = init; el.placeholder = ph;
      el.readOnly = !!readOnly;
      el.setAttribute('aria-label', readOnly ? '成果閱讀' : (ph || '文字輸入'));
      if (maxChars > 0) el.maxLength = maxChars;
      var bar = document.createElement('div');
      var count = document.createElement('span');
      count.id = 'll-text-count';
      var btn = document.createElement('button');
      btn.id = 'll-text-done';
      btn.type = 'button';
      btn.textContent = doneLabel;
      var upd = function () { count.textContent = readOnly ? '成果閱讀' : el.value.length + (maxChars > 0 ? ' ／ ' + maxChars : ''); };
      el.addEventListener('input', upd); upd();
      btn.addEventListener('click', function (e) { e.preventDefault(); window.__llTextDone = 1; });
      // 單行輸入按 Enter 也算完成；多行按 Ctrl／⌘＋Enter 完成（中文輸入法選字時 isComposing 為 true，不攔）
      el.addEventListener('keydown', function (e) {
        e.stopPropagation();
        if (e.key === 'Enter' && !e.isComposing && (!multi || e.ctrlKey || e.metaKey)) { e.preventDefault(); window.__llTextDone = 1; }
      });
      el.addEventListener('keyup', function (e) { e.stopPropagation(); });
      el.addEventListener('keypress', function (e) { e.stopPropagation(); });
      bar.appendChild(count); bar.appendChild(btn);
      wrap.appendChild(bar); wrap.appendChild(el);
      var last = '';
      var place = function () {
        try {
          // 全螢幕時 body 底下的東西看不到：掛到全螢幕元素裡
          var fe = document.fullscreenElement || document.webkitFullscreenElement;
          var host = (fe && fe.tagName !== 'CANVAS') ? fe : document.body;
          if (wrap.parentNode !== host) host.appendChild(wrap);
          var r = canvas.getBoundingClientRect();
          var vv = window.visualViewport;
          var vx = vv ? vv.offsetLeft : 0, vy = vv ? vv.offsetTop : 0;
          var vw = vv ? vv.width : window.innerWidth, vh = vv ? vv.height : window.innerHeight;
          // 可用範圍＝畫布 ∩ 可見視窗（留 4px 邊）
          var L = Math.max(r.left, vx) + 4, T = Math.max(r.top, vy) + 4;
          var R = Math.min(r.right, vx + vw) - 4, B = Math.min(r.bottom, vy + vh) - 4;
          if (R - L < 120) { L = vx + 4; R = vx + vw - 4; }
          if (B - T < 120) { T = vy + 4; B = vy + vh - 4; }
          var fs = Math.max(16, Math.min(26, r.height * 0.04));
          var barH = Math.max(44, fs * 2);
          var minH = barH + fs * 1.5 * (multi ? 3 : 1) + 30;
          var left = r.left + r.width * x, top = r.top + r.height * y, wid = r.width * w, hei = r.height * h;
          // 手機／平板螢幕鍵盤彈出（可見高度明顯變小）：框改貼可見範圍上半部
          var kb = vv && vv.height < window.innerHeight * 0.75;
          if (kb) { top = T; hei = Math.min(B - T, Math.max(minH, (B - T) * 0.9)); }
          wid = Math.min(Math.max(wid, 200), R - L);
          hei = Math.min(Math.max(hei, minH), B - T);
          left = Math.min(Math.max(left, L), R - wid);
          top = Math.min(Math.max(top, T), B - hei);
          var key = [left, top, wid, hei, fs].map(function (v) { return Math.round(v); }).join(',');
          if (key === last) return;
          last = key;
          wrap.style.cssText = 'position:fixed;z-index:9999;box-sizing:border-box;display:flex;flex-direction:column;gap:6px;padding:6px;' +
            'background:#FFF9EC;border:3px solid #8A631E;border-radius:8px;box-shadow:0 2px 10px rgba(0,0,0,0.35);' +
            'left:' + left + 'px;top:' + top + 'px;width:' + wid + 'px;height:' + hei + 'px;';
          bar.style.cssText = 'flex:0 0 auto;display:flex;align-items:center;justify-content:space-between;gap:8px;min-height:' + barH + 'px;';
          count.style.cssText = 'color:#6B4A1A;font-size:' + Math.max(14, fs * 0.75) + 'px;font-family:sans-serif;';
          btn.style.cssText = 'flex:0 0 auto;min-height:44px;min-width:88px;padding:6px 22px;border:2px solid #8A631E;border-radius:6px;background:#D4A84A;color:#3A1610;' +
            'font-size:' + Math.max(16, fs * 0.85) + 'px;font-weight:bold;font-family:"Noto Serif TC","PingFang TC","Microsoft JhengHei",serif;cursor:pointer;';
          el.style.cssText = 'flex:1 1 auto;min-height:0;width:100%;box-sizing:border-box;resize:none;padding:8px 10px;border:1px solid #C9A86A;border-radius:4px;' +
            'background:#FFFDF6;color:#3A1610;font-size:' + fs + 'px;line-height:1.5;font-family:"Noto Serif TC","PingFang TC","Microsoft JhengHei",serif;outline:none;';
        } catch (e) {}
      };
      place();
      var timer = setInterval(place, 250);
      var evs = ['resize', 'scroll', 'fullscreenchange', 'webkitfullscreenchange', 'orientationchange'];
      for (var i = 0; i < evs.length; i++) window.addEventListener(evs[i], place, true);
      document.addEventListener('fullscreenchange', place);
      if (window.visualViewport) { window.visualViewport.addEventListener('resize', place); window.visualViewport.addEventListener('scroll', place); }
      wrap.__llStop = function () {
        clearInterval(timer);
        for (var i = 0; i < evs.length; i++) window.removeEventListener(evs[i], place, true);
        document.removeEventListener('fullscreenchange', place);
        if (window.visualViewport) { window.visualViewport.removeEventListener('resize', place); window.visualViewport.removeEventListener('scroll', place); }
      };
      window.__llTextDone = 0;
      setTimeout(function () { try { (readOnly ? btn : el).focus(); } catch (e) {} }, 50);
    } catch (e) { console.warn('LiuruanTextOpen', e); }
  },

  // hotfix1：觸控平板偵測。iPadOS Safari 預設「桌面版網站」回報 Macintosh，只能看 maxTouchPoints；
  // Android 平板 UA 有 Android 但沒有 Mobile。觸控筆電（Windows）主要指標是滑鼠（pointer: fine），不算。
  LiuruanTouchTablet: function () {
    try {
      var ua = navigator.userAgent || '', tp = navigator.maxTouchPoints || 0;
      if (/iPad|Android/i.test(ua) && tp > 0) return 1;
      if (/Macintosh/i.test(ua) && tp > 1) return 1;
      if (tp > 1 && window.matchMedia && window.matchMedia('(pointer: coarse)').matches) return 1;
      return 0;
    } catch (e) { return 0; }
  },

  // 輸入框是否開著（Unity 端鍵盤快捷鍵讓路用）
  LiuruanTextIsOpen: function () { return document.getElementById('ll-text-wrap') ? 1 : 0; },

  // 1＝玩家按了「寫好了」
  LiuruanTextState: function () { return window.__llTextDone ? 1 : 0; },

  LiuruanTextGet: function () {
    var el = document.getElementById('ll-text');
    var s = el ? el.value : '';
    var n = lengthBytesUTF8(s) + 1;
    var p = _malloc(n);
    stringToUTF8(s, p, n);
    return p;
  },

  LiuruanTextClose: function () {
    try {
      var old = document.getElementById('ll-text-wrap'); if (old) { if (old.__llStop) old.__llStop(); old.parentNode.removeChild(old); }
      window.__llTextDone = 0;
      var c = Module['canvas']; if (c && c.focus) c.focus();
    } catch (e) {}
  },

  // 成果卡：PNG（base64）→ 畫面上蓋一張圖（手機長按儲存）＋桌機直接下載
  LiuruanSavePng: function (b64Ptr, namePtr, tipPtr, closePtr) {
    try {
      var b64 = UTF8ToString(b64Ptr), name = UTF8ToString(namePtr), tip = UTF8ToString(tipPtr), closeLabel = UTF8ToString(closePtr);
      var url = 'data:image/png;base64,' + b64;
      var old = document.getElementById('ll-card-wrap'); if (old) old.parentNode.removeChild(old);
      var wrap = document.createElement('div');
      wrap.id = 'll-card-wrap';
      wrap.style.cssText = 'position:fixed;inset:0;z-index:10000;background:rgba(20,10,5,0.88);display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;padding:12px;box-sizing:border-box;';
      var img = document.createElement('img');
      img.src = url; img.alt = name;
      img.style.cssText = 'max-width:96vw;max-height:78vh;border:3px solid #D4A84A;-webkit-touch-callout:default;user-select:auto;';
      var p = document.createElement('div');
      p.textContent = tip;
      p.style.cssText = 'color:#F3E9D2;font-size:16px;font-family:sans-serif;text-align:center;';
      var row = document.createElement('div');
      row.style.cssText = 'display:flex;gap:12px;';
      var a = document.createElement('a');
      a.href = url; a.download = name; a.textContent = '⬇ PNG';
      a.style.cssText = 'min-height:44px;line-height:44px;padding:0 20px;border-radius:6px;background:#D4A84A;color:#3A1610;font-weight:bold;text-decoration:none;font-family:sans-serif;';
      var x = document.createElement('button');
      x.textContent = closeLabel;
      x.style.cssText = 'min-height:44px;padding:0 20px;border-radius:6px;border:2px solid #D4A84A;background:#F3E9D2;color:#3A1610;font-size:16px;font-weight:bold;';
      x.addEventListener('click', function () { wrap.parentNode.removeChild(wrap); });
      row.appendChild(a); row.appendChild(x);
      wrap.appendChild(img); wrap.appendChild(p); wrap.appendChild(row);
      document.body.appendChild(wrap);
      // 桌機（非觸控）直接觸發下載；手機留給使用者長按圖片
      var touch = ('ontouchstart' in window) || navigator.maxTouchPoints > 0;
      if (!touch) { try { a.click(); } catch (e) {} }
    } catch (e) { console.warn('LiuruanSavePng', e); }
  },

  // R18：玩家自由輸入的文字改由瀏覽器字型顯示（靜態字型子集沒有的字也不會變方框）。
  // json = {"fs":字級(占畫布高比例),"lh":行高,"c":"#色","blocks":[{"t":"文字","b":0/1,"c":"#色","gap":0/1}]}
  // x,y,w,h：畫布比例（左上原點）。同 id 再呼叫＝更新。
  LiuruanOverlaySet: function (idPtr, jsonPtr, x, y, w, h) {
    try {
      var id = 'll-ov-' + UTF8ToString(idPtr), o = JSON.parse(UTF8ToString(jsonPtr));
      var el = document.getElementById(id);
      if (!el) { el = document.createElement('div'); el.id = id; el.className = 'll-ov'; document.body.appendChild(el); }
      el.__ll = { o: o, x: x, y: y, w: w, h: h };
      if (!window.__llOvResize) {
        window.__llOvResize = function (e) { if (e && e.type === 'scroll' && e.target.classList && e.target.classList.contains('ll-ov')) return; var a = document.querySelectorAll('.ll-ov'); for (var i = 0; i < a.length; i++) if (a[i].__llPlace) a[i].__llPlace(); };
        window.addEventListener('resize', window.__llOvResize);
        window.addEventListener('scroll', window.__llOvResize, true);
        document.addEventListener('fullscreenchange', window.__llOvResize);
        document.addEventListener('webkitfullscreenchange', window.__llOvResize);
      }
      el.__llPlace = function () {
        var d = el.__ll, canvas = Module['canvas'] || document.querySelector('canvas'), r = canvas.getBoundingClientRect();
        var fe = document.fullscreenElement || document.webkitFullscreenElement, host = (fe && fe.tagName !== 'CANVAS') ? fe : document.body;
        if (el.parentNode !== host) host.appendChild(el);
        var scrollable = id === 'll-ov-create', previousScroll = el.scrollTop;
        var fs = Math.max(scrollable ? 16 : 8, d.o.fs * r.height);
        el.style.cssText = 'position:fixed;z-index:9000;' + (scrollable ? 'pointer-events:auto;overflow:auto;overscroll-behavior:contain;' : 'pointer-events:none;overflow:hidden;') + 'box-sizing:border-box;white-space:pre-wrap;word-break:break-all;' +
          'left:' + (r.left + r.width * d.x) + 'px;top:' + (r.top + r.height * d.y) + 'px;width:' + (r.width * d.w) + 'px;height:' + (r.height * d.h) + 'px;' +
          'color:' + (d.o.c || '#38291A') + ';line-height:' + (d.o.lh || 1.35) + ';font-family:"Noto Serif TC","PingFang TC","Microsoft JhengHei",serif;font-size:' + fs + 'px;';
        el.innerHTML = '';
        var bl = d.o.blocks || [];
        for (var i = 0; i < bl.length; i++) {
          var b = document.createElement('div');
          b.textContent = bl[i].t || '';
          if (bl[i].b) b.style.fontWeight = 'bold';
          if (bl[i].c) b.style.color = bl[i].c;
          if (bl[i].gap) b.style.marginBottom = '0.6em';
          if (d.o.align) b.style.textAlign = d.o.align;
          el.appendChild(b);
        }
        for (var k = 0; !scrollable && k < 40 && el.scrollHeight > el.clientHeight + 1 && fs > 8; k++) { fs *= 0.93; el.style.fontSize = fs + 'px'; }
        if (scrollable) el.scrollTop = previousScroll;
      };
      el.__llPlace();
    } catch (e) { console.warn('LiuruanOverlaySet', e); }
  },

  // 移除疊字；id 空字串＝全部
  LiuruanOverlayClear: function (idPtr) {
    try {
      var id = UTF8ToString(idPtr);
      var a = id ? [document.getElementById('ll-ov-' + id)] : document.querySelectorAll('.ll-ov');
      for (var i = 0; i < a.length; i++) if (a[i] && a[i].parentNode) a[i].parentNode.removeChild(a[i]);
    } catch (e) {}
  },

  // 成果卡：先把 Unity 畫好的 PNG 鋪上 canvas，再用瀏覽器字型寫上玩家輸入的字，最後走 LiuruanSavePng 同一套蓋圖＋下載。
  // ov = [{"x","y","w","h"(卡片比例，左上原點),"fs"(占卡片高比例),"lh","c","align","blocks":[...]}]
  LiuruanSavePngOv: function (b64Ptr, namePtr, tipPtr, closePtr, ovPtr) {
    var b64 = UTF8ToString(b64Ptr), name = UTF8ToString(namePtr), tip = UTF8ToString(tipPtr), closeLabel = UTF8ToString(closePtr), ov = [];
    try { ov = JSON.parse(UTF8ToString(ovPtr)) || []; } catch (e) {}
    var show = function (url) {
      var al = function (str) { var n = lengthBytesUTF8(str) + 1, p = _malloc(n); stringToUTF8(str, p, n); return p; };
      var p1 = al(url.replace(/^data:image\/png;base64,/, '')), p2 = al(name), p3 = al(tip), p4 = al(closeLabel);
      _LiuruanSavePng(p1, p2, p3, p4);
      _free(p1); _free(p2); _free(p3); _free(p4);
    };
    var img = new Image();
    img.onload = function () {
      try {
        var cv = document.createElement('canvas'); cv.width = img.width; cv.height = img.height;
        var g = cv.getContext('2d'); g.drawImage(img, 0, 0);
        var fam = '"Noto Serif TC","PingFang TC","Microsoft JhengHei",serif';
        for (var i = 0; i < ov.length; i++) {
          var o = ov[i], X = o.x * cv.width, Y = o.y * cv.height, W = o.w * cv.width, H = o.h * cv.height, lh = o.lh || 1.35;
          var fs = o.fs * cv.height, lines;
          for (var k = 0; k < 40; k++) {
            lines = []; var tot = 0;
            for (var j = 0; j < (o.blocks || []).length; j++) {
              var bl = o.blocks[j];
              g.font = (bl.b ? 'bold ' : '') + fs + 'px ' + fam;
              var paras = String(bl.t || '').split('\n');
              for (var q = 0; q < paras.length; q++) {
                var cur = '', chars = Array.from(paras[q]);
                for (var m = 0; m < chars.length; m++) {
                  if (cur && g.measureText(cur + chars[m]).width > W) { lines.push({ t: cur, b: bl.b, c: bl.c }); tot += fs * lh; cur = ''; }
                  cur += chars[m];
                }
                lines.push({ t: cur, b: bl.b, c: bl.c }); tot += fs * lh;
              }
              if (bl.gap) { lines.push({ gap: fs * 0.6 }); tot += fs * 0.6; }
            }
            if (tot <= H || fs < 9) break;
            fs *= 0.93;
          }
          g.save(); g.beginPath(); g.rect(X, Y, W, H); g.clip(); g.textBaseline = 'top';
          var yy = Y;
          for (var n = 0; n < lines.length; n++) {
            var L = lines[n];
            if (L.gap) { yy += L.gap; continue; }
            g.font = (L.b ? 'bold ' : '') + fs + 'px ' + fam;
            g.fillStyle = L.c || o.c || '#38291A';
            var tx = X;
            if (o.align === 'right') tx = X + W - g.measureText(L.t).width;
            g.fillText(L.t, tx, yy + fs * (lh - 1) / 2);
            yy += fs * lh;
          }
          g.restore();
        }
        show(cv.toDataURL('image/png'));
      } catch (e) { console.warn('LiuruanSavePngOv', e); show('data:image/png;base64,' + b64); }
    };
    img.onerror = function () { show('data:image/png;base64,' + b64); };
    img.src = 'data:image/png;base64,' + b64;
  },

  // 成果卡蓋板還開著（Unity 端暫停輸入用）
  LiuruanCardOpen: function () { return document.getElementById('ll-card-wrap') ? 1 : 0; }
});
