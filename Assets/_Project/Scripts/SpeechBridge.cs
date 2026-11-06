using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Liuruan
{
    /// 朗讀（Round14）：優先播 StreamingAssets/voice/ 的大乃老師克隆聲音預錄檔，對照表沒有的字串才退回瀏覽器 Web Speech
    /// （Assets/Plugins/WebGL/LiuruanSpeech.jslib）。對照表 key＝正規化文字（去 rich text 標籤、去所有空白），
    /// 規則與 staging/Round14/tools/inventory.py 的 norm() 相同；改了台詞文字就查不到 → 自動走 Web Speech 備援。
    /// 呼叫端 API 不變：Supported／Init／Speak／Stop／HideLoading。
    public static class SpeechBridge
    {
        const string VoiceDir = "voice";
        const string ManifestFile = "manifest.json";
        const int ClipCacheMax = 12;
        const float ManifestWait = 4f;   // Speak 早於對照表載完時最多等幾秒，逾時改用 Web Speech

        [System.Serializable] class Entry { public string k; public string f; }
        [System.Serializable] class Manifest { public string ext = "mp3"; public Entry[] items; }

        static Dictionary<string, string> map;   // 正規化文字 → 檔名（不含副檔名）
        static string ext = "mp3";
        static bool manifestLoading, manifestFailed;
        static int serial;                        // 每次 Speak/Stop 遞增；下載回來時序號不符就丟棄
        static Runner runner;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static readonly LinkedList<string> clipOrder = new LinkedList<string>();

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int LiuruanSpeechSupported();
        [DllImport("__Internal")] static extern void LiuruanSpeechInit();
        [DllImport("__Internal")] static extern void LiuruanSpeak(string text, float rate);
        [DllImport("__Internal")] static extern void LiuruanSpeechStop();
        [DllImport("__Internal")] static extern void LiuruanHideLoading();

        static bool WebSpeech => LiuruanSpeechSupported() == 1;
        static void WebInit() { if (WebSpeech) LiuruanSpeechInit(); }
        static void WebSpeak(string text) { if (WebSpeech) LiuruanSpeak(text, 0.8f); }
        static void WebStop() { if (WebSpeech) LiuruanSpeechStop(); }
        public static void HideLoading() => LiuruanHideLoading();
        static bool VoiceAvailable => true;   // WebGL 一律可播 AudioClip；對照表讀不到時每句都走 Web Speech
#else
        static bool WebSpeech => false;
        static void WebInit() { }
        static void WebSpeak(string text) { }
        static void WebStop() { }
        public static void HideLoading() { }
        // 編輯器／其他平台：StreamingAssets/voice/manifest.json 存在才啟用（方便在 Play 模式試聽）
        static bool? voiceFile;
        static bool VoiceAvailable
        {
            get
            {
                if (voiceFile == null)
                    voiceFile = System.IO.File.Exists(System.IO.Path.Combine(Application.streamingAssetsPath, VoiceDir, ManifestFile));
                return voiceFile.Value;
            }
        }
#endif

        /// 能朗讀：有預錄聲音或瀏覽器支援 Web Speech（朗讀鈕、M 鍵是否出現看這個）
        public static bool Supported => VoiceAvailable || WebSpeech;

        public static void Init()
        {
            WebInit();
            if (VoiceAvailable) LoadManifest();
        }

        public static void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Stop();
            int my = serial;
            if (!VoiceAvailable) { WebSpeak(text); return; }
            LoadManifest();
            EnsureRunner().StartCoroutine(PlayVoice(text, my));
        }

        public static void Stop()
        {
            serial++;
            if (runner && runner.source) runner.source.Stop();
            WebStop();
        }

        /// 正規化：去掉 &lt;...&gt; 標籤與所有空白（必須與 inventory.py 的 norm() 一致）
        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '<')
                {
                    int close = s.IndexOf('>', i + 1);
                    if (close >= 0) { i = close; continue; }
                }
                if (!char.IsWhiteSpace(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        // ---------- 內部 ----------

        class Runner : MonoBehaviour { public AudioSource source; }

        static Runner EnsureRunner()
        {
            if (runner) return runner;
            var go = new GameObject("SpeechVoice");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;   // 2D；音量照 AudioListener.volume（AudioManager 靜音＝0）
            src.volume = 1f;
            runner.source = src;
            return runner;
        }

        static string Url(string file)
        {
            string p = Application.streamingAssetsPath + "/" + VoiceDir + "/" + file;
            return p.Contains("://") ? p : "file://" + p;
        }

        static void LoadManifest()
        {
            if (map != null || manifestLoading || manifestFailed) return;
            manifestLoading = true;
            EnsureRunner().StartCoroutine(FetchManifest());
        }

        static IEnumerator FetchManifest()
        {
            using (var req = UnityWebRequest.Get(Url(ManifestFile)))
            {
                yield return req.SendWebRequest();
                manifestLoading = false;
                if (req.result != UnityWebRequest.Result.Success)
                {
                    manifestFailed = true;
                    Debug.LogWarning("[SpeechBridge] voice manifest 讀取失敗，改用 Web Speech：" + req.error);
                    yield break;
                }
                Manifest m = null;
                try { m = JsonUtility.FromJson<Manifest>(req.downloadHandler.text); }
                catch (System.Exception e) { Debug.LogWarning("[SpeechBridge] voice manifest 解析失敗：" + e.Message); }
                if (m == null || m.items == null) { manifestFailed = true; yield break; }
                var d = new Dictionary<string, string>(m.items.Length);
                foreach (var it in m.items) if (it != null && !string.IsNullOrEmpty(it.k)) d[it.k] = it.f;
                if (!string.IsNullOrEmpty(m.ext)) ext = m.ext;
                map = d;
            }
        }

        static IEnumerator PlayVoice(string text, int my)
        {
            for (float t = 0f; map == null && !manifestFailed && t < ManifestWait; t += Time.unscaledDeltaTime) yield return null;
            if (my != serial) yield break;
            string file;
            if (map == null || !map.TryGetValue(Normalize(text), out file)) { WebSpeak(text); yield break; }

            AudioClip clip;
            if (!clips.TryGetValue(file, out clip) || !clip)
            {
                clip = null;
                using (var req = UnityWebRequestMultimedia.GetAudioClip(Url(file + "." + ext), ext == "ogg" ? AudioType.OGGVORBIS : AudioType.MPEG))
                {
                    var dh = (DownloadHandlerAudioClip)req.downloadHandler;
                    dh.compressed = true;   // CompressedInMemory：iOS 靜音模式下 DecompressOnLoad 的聲音會聽不到（Unity Web 音訊手冊）
                    dh.streamAudio = false; // 瀏覽器不支援串流解碼
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) clip = DownloadHandlerAudioClip.GetContent(req);
                    else Debug.LogWarning("[SpeechBridge] 音檔讀取失敗 " + file + "：" + req.error);
                }
                if (!clip) { if (my == serial) WebSpeak(text); yield break; }
                Remember(file, clip);
            }
            else
            {
                clipOrder.Remove(file);
                clipOrder.AddLast(file);
            }
            if (my != serial) yield break;   // 下載期間玩家已跳下一句／關掉
            var src = EnsureRunner().source;
            src.Stop();
            src.clip = clip;
            src.Play();
        }

        static void Remember(string file, AudioClip clip)
        {
            clips[file] = clip;
            clipOrder.Remove(file);
            clipOrder.AddLast(file);
            while (clipOrder.Count > ClipCacheMax)
            {
                string old = clipOrder.First.Value;
                clipOrder.RemoveFirst();
                if (clips.TryGetValue(old, out var c))
                {
                    clips.Remove(old);
                    if (c && !(runner && runner.source && runner.source.clip == c)) Object.Destroy(c);
                }
            }
        }
    }
}
