using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Liuruan
{
    /// Boot 場景唯一的元件。WebGL 版 Main 場景不在主包：從 StreamingAssets/scene/ 依 parts.json 依序下載各塊、
    /// 拼回完整 bundle 再 LoadFromMemoryAsync，最後載入 Main（Editor/SceneBundles.cs 產生這些檔）。
    /// Editor 直接 LoadScene("Main")（Main 仍在 Build Settings）。畫面文字只用英文／數字，避免 TMP 中文缺字。
    public class BootLoader : MonoBehaviour
    {
        public const string MainScene = "Main", BundleName = "main", Dir = "scene", IndexFile = "parts.json";
        public const int PartSize = 20 * 1024 * 1024; // 每塊 20 MiB，低於 Cloudflare Pages 單檔 25 MiB

        /// parts.json：{ "name": "main", "parts": 2, "bytes": 31457280 }
        [Serializable] public class Parts { public string name; public int parts; public long bytes; }

#pragma warning disable CS0169, CS0649 // 下面三個欄位只在 WebGL 分支寫入；Editor 編譯時別洗警告
        static AssetBundle bundle; // 刻意不卸載：「重新開始」用 LoadScene("Main") 重載時還要用
        long doneBytes, totalBytes;
#pragma warning restore CS0169, CS0649

        string status = "Loading...";
        bool failed;
        UnityWebRequest current;
        GUIStyle label, button;

        void Start() => StartCoroutine(Run());

        IEnumerator Run()
        {
            failed = false;
            status = "Loading...";
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!bundle)
            {
                string dir = $"{Application.streamingAssetsPath}/{Dir}/", v = $"?v={Application.buildGUID}"; // ?v= 讓新版不吃到 CDN 舊檔
                byte[] json = null;
                yield return Download(dir + IndexFile + v, d => json = d);
                if (json == null) { Fail("Network error (E1). Please check your connection and retry."); yield break; }
                Parts p = null;
                try { p = JsonUtility.FromJson<Parts>(Encoding.UTF8.GetString(json)); }
                catch (Exception e) { Debug.LogError($"[Boot] parts.json parse failed (got index.html?): {e.Message}"); }
                if (p == null || p.name != BundleName || p.bytes <= 0 || p.bytes > int.MaxValue
                    || p.parts != (p.bytes + PartSize - 1) / PartSize)
                { Fail("Data error (E2). Please reload the page."); yield break; }

                var all = new byte[p.bytes];
                totalBytes = p.bytes;
                doneBytes = 0;
                for (int i = 0; i < p.parts; i++)
                {
                    byte[] part = null;
                    yield return Download($"{dir}{p.name}.{i}{v}", d => part = d);
                    if (part == null) { Fail($"Network error (E3, part {i + 1}/{p.parts}). Please retry."); yield break; }
                    if (doneBytes + part.Length > all.Length) { Fail($"Data error (E4, part {i + 1}). Please reload the page."); yield break; }
                    Buffer.BlockCopy(part, 0, all, (int)doneBytes, part.Length);
                    doneBytes += part.Length;
                }
                if (doneBytes != all.Length) { Fail($"Data error (E4, {doneBytes}/{all.Length}). Please reload the page."); yield break; }

                status = "Unpacking...";
                var load = AssetBundle.LoadFromMemoryAsync(all);
                all = null; // 引擎已有自己的副本，放掉讓 GC 回收
                yield return load;
                bundle = load.assetBundle;
                if (!bundle) { Fail("Load error (E5). Please reload the page."); yield break; }
            }
#endif
            status = "Starting...";
            var op = SceneManager.LoadSceneAsync(MainScene);
            if (op == null) { Fail("Load error (E6). Please reload the page."); yield break; } // Editor：Main 不在 Build Settings
            yield return op;
        }

        void Fail(string message)
        {
            Debug.LogError("[Boot] " + message);
            status = message;
            failed = true;
            current = null;
        }

        /// 網路不穩重試 3 次；全失敗不呼叫 done（呼叫端以 null 判斷）
        IEnumerator Download(string url, Action<byte[]> done)
        {
            for (int tries = 1; tries <= 3; tries++)
                using (var req = UnityWebRequest.Get(url))
                {
                    current = req;
                    yield return req.SendWebRequest();
                    current = null;
                    if (req.result == UnityWebRequest.Result.Success) { done(req.downloadHandler.data); yield break; }
                    Debug.LogWarning($"[Boot] download failed (try {tries}) {url}: {req.error}");
                    if (tries < 3) yield return new WaitForSecondsRealtime(tries);
                }
        }

        void OnGUI()
        {
            int size = Mathf.Max(16, Screen.height / 28);
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
                label.normal.textColor = new Color(0.96f, 0.9f, 0.78f);
                button = new GUIStyle(GUI.skin.button);
            }
            label.fontSize = button.fontSize = size;

            string text = status;
            if (!failed && totalBytes > 0 && status == "Loading...")
            {
                long got = doneBytes + (current != null ? (long)current.downloadedBytes : 0);
                text = $"Loading {Mathf.Clamp(100f * got / totalBytes, 0f, 100f):0}%  ({got / 1048576f:0.0} / {totalBytes / 1048576f:0.0} MB)";
            }
            float w = Mathf.Min(Screen.width - 32, size * 30);
            GUI.Label(new Rect((Screen.width - w) / 2, Screen.height / 2 - size * 2, w, size * 3), text, label);
            if (failed && GUI.Button(new Rect((Screen.width - size * 6) / 2, Screen.height / 2 + size * 1.5f, size * 6, size * 2), "Retry", button))
                StartCoroutine(Run());
        }
    }
}
