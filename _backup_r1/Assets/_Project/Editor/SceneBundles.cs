using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Liuruan
{
    /// WebGL 分包（仿 oz-journey ZoneBundles）：Main.unity 打成 LZMA scene bundle「main」，切成 ≤20 MiB 的
    /// StreamingAssets/scene/main.0、.1…＋parts.json；主包只放 Boot.unity（BootLoader 下載拼回後載入 Main）。
    /// EditorBuildSettings 不動：Editor 直接開 Main.unity 按 Play 照舊。
    /// 一鍵：-executeMethod Liuruan.SceneBundles.BuildAll
    public static class SceneBundles
    {
        const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
        const string OutDir = "Build/SceneBundles";      // 增量建置快取；SceneBundles.manifest 給程式碼裁剪用
        const string PlayerDir = "Build/WebGL";
        const string ServeDir = PlayerDir + "/StreamingAssets/" + BootLoader.Dir;
        const string DoneFlag = PlayerDir + "/.bundle-done"; // 全部成功才寫；外部用它判斷建置結束
        const long MaxFile = 25L * 1024 * 1024;          // Cloudflare Pages 單檔上限

        [MenuItem("Tools/Liuruan/Build/Scene Bundle Only")]
        public static void BuildBundleOnlyMenu()
        {
            bool ok = BuildBundle() && (!Directory.Exists(PlayerDir) || CopyToPlayer());
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        /// 建 bundle → 建 WebGL（只含 Boot）→ bundle 切塊放進 Build/WebGL/StreamingAssets/scene → 寫 .bundle-done
        [MenuItem("Tools/Liuruan/Build/WebGL + Scene Bundle")]
        public static void BuildAll()
        {
            if (File.Exists(DoneFlag)) File.Delete(DoneFlag);
            bool ok = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() && EnsureBootScene() && BuildBundle();
            if (ok)
            {
                // PlayerSettings（Brotli、檔名雜湊、模板、記憶體）沿用 ProjectSettings 現有 WebGL 設定
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { BootScenePath },
                    locationPathName = PlayerDir,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    assetBundleManifestPath = $"{OutDir}/{Path.GetFileName(OutDir)}.manifest", // 只在 bundle 用到的型別／引擎模組不被裁掉
                });
                ok = report.summary.result == BuildResult.Succeeded;
                Debug.Log($"[SceneBundles] WebGL {report.summary.result}");
                if (ok) ok = CopyToPlayer(); // BuildPlayer 之後才複製，避免被 StreamingAssets 覆蓋
            }
            if (ok) File.WriteAllText(DoneFlag, DateTime.Now.ToString("o") + "\n");
            else Debug.LogError("[SceneBundles] 建置失敗，未寫 .bundle-done");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool BuildBundle()
        {
            // bundle 讀磁碟上的場景：Main 有未存修改時先問（batch mode 直接回 true）
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            Directory.CreateDirectory(OutDir);
            // 預設 LZMA：檔案最小（CF 以存放大小算 25 MiB；octet-stream 不一定會再壓）
            var manifest = BuildPipeline.BuildAssetBundles(OutDir, new[]
            {
                new AssetBundleBuild { assetBundleName = BootLoader.BundleName, assetNames = new[] { MainScenePath } },
            }, BuildAssetBundleOptions.None, BuildTarget.WebGL);
            if (!manifest) { Debug.LogError("[SceneBundles] bundle 建置失敗"); return false; }
            long size = new FileInfo(Path.Combine(OutDir, BootLoader.BundleName)).Length;
            Debug.Log($"[SceneBundles] {BootLoader.BundleName} {size / 1048576f:F2} MiB");
            return true;
        }

        /// 切成 ≤20 MiB 的 main.0、.1…，寫 parts.json；任何一塊超過 25 MiB 就失敗
        static bool CopyToPlayer()
        {
            if (Directory.Exists(ServeDir)) Directory.Delete(ServeDir, true);
            Directory.CreateDirectory(ServeDir);
            var data = File.ReadAllBytes(Path.Combine(OutDir, BootLoader.BundleName));
            if (data.LongLength == 0 || data.LongLength > int.MaxValue) { Debug.LogError("[SceneBundles] bundle 大小無效"); return false; }
            int n = (int)((data.LongLength + BootLoader.PartSize - 1) / BootLoader.PartSize);
            for (int i = 0; i < n; i++)
            {
                int len = (int)Math.Min(BootLoader.PartSize, data.LongLength - (long)i * BootLoader.PartSize);
                if (len > MaxFile) { Debug.LogError($"[SceneBundles] {BootLoader.BundleName}.{i} 超過 25 MiB"); return false; }
                using (var fs = File.Create(Path.Combine(ServeDir, $"{BootLoader.BundleName}.{i}")))
                    fs.Write(data, i * BootLoader.PartSize, len);
            }
            var parts = new BootLoader.Parts { name = BootLoader.BundleName, parts = n, bytes = data.LongLength };
            File.WriteAllText(Path.Combine(ServeDir, BootLoader.IndexFile), JsonUtility.ToJson(parts, true));
            Debug.Log($"[SceneBundles] 已切 {n} 塊到 {ServeDir}");
            return true;
        }

        /// Boot.unity 不存在就建：黑底相機＋BootLoader（附加開啟後存檔關閉，不動目前場景）
        static bool EnsureBootScene()
        {
            if (File.Exists(BootScenePath)) return true;
            var active = SceneManager.GetActiveScene();
            bool additive = active.IsValid() && !string.IsNullOrEmpty(active.path); // 未命名場景不能附加開新場景（batch mode 常見）
            var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(boot); // 新物件建在 Boot，不弄髒 Main
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.09f, 0.07f);
            new GameObject("BootLoader", typeof(BootLoader));
            bool ok = EditorSceneManager.SaveScene(boot, BootScenePath);
            if (additive)
            {
                SceneManager.SetActiveScene(active);
                EditorSceneManager.CloseScene(boot, true);
            }
            if (!ok) Debug.LogError("[SceneBundles] Boot.unity 建立失敗");
            else AssetDatabase.ImportAsset(BootScenePath);
            return ok;
        }
    }
}
