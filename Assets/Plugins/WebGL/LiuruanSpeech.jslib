mergeInto(LibraryManager.library, {
  LiuruanSpeechSupported: function () {
    try { return (typeof window !== 'undefined' && 'speechSynthesis' in window && typeof SpeechSynthesisUtterance !== 'undefined') ? 1 : 0; }
    catch (e) { return 0; }
  },

  // iOS Safari 要求第一次發聲發生在使用者手勢內；Unity 按鈕事件不在手勢內，
  // 所以在第一次點擊/觸控時先念一段空字串解鎖。
  LiuruanSpeechInit: function () {
    try {
      if (!('speechSynthesis' in window) || window.__liuruanSpeechInit) return;
      window.__liuruanSpeechInit = true;
      var unlock = function () {
        try { var u = new SpeechSynthesisUtterance(''); u.volume = 0; window.speechSynthesis.speak(u); } catch (e) {}
        document.removeEventListener('touchend', unlock, true);
        document.removeEventListener('click', unlock, true);
      };
      document.addEventListener('touchend', unlock, true);
      document.addEventListener('click', unlock, true);
    } catch (e) {}
  },

  LiuruanSpeak: function (textPtr, rate) {
    try {
      var text = UTF8ToString(textPtr);
      var s = window.speechSynthesis;
      s.cancel();
      var u = new SpeechSynthesisUtterance(text);
      u.lang = 'zh-TW';
      u.rate = rate;
      var voices = s.getVoices ? s.getVoices() : [];
      for (var i = 0; i < voices.length; i++) {
        if (voices[i].lang && voices[i].lang.replace('_', '-') === 'zh-TW') { u.voice = voices[i]; break; }
      }
      s.speak(u);
    } catch (e) {}
  },

  // 部分 iOS 瀏覽器 createUnityInstance 的 then 不觸發，載入條會蓋在遊戲上；遊戲啟動後由 C# 主動收掉。
  LiuruanHideLoading: function () {
    try { var b = document.getElementById('unity-loading-bar'); if (b) b.style.display = 'none'; } catch (e) {}
  },

  LiuruanSpeechStop: function () {
    try { if ('speechSynthesis' in window) window.speechSynthesis.cancel(); } catch (e) {}
  }
});
