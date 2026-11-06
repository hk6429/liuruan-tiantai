using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Liuruan
{
    /// 省電：限幀＋讀卷軸時暫停 3D 場景。自動建立（不必掛在場景裡），跨場景重載保留。
    /// - 幀率上限：桌機 60、手機（含觸控過的平板）30；面板開著時桌機 30、手機 20。
    ///   主相機 cullingMask = 0、純色清除、關後製與陰影 → 只剩清畫面＋UI。關閉後原樣恢復。
    ///   （不直接 disable 相機：沒有相機時 Screen Space Overlay UI 在部分平台不保證重畫）
    public class RenderGovernor : MonoBehaviour
    {
        // 桌機 -1＝交給瀏覽器 requestAnimationFrame（WebGL 設定 targetFrameRate 可能改走 setTimeout，高更新率螢幕會抖）
        const int DesktopFps = -1, MobileFps = 30, DesktopReadingFps = 30, MobileReadingFps = 20;
        static readonly Color PausedBg = new Color(0.12f, 0.09f, 0.06f, 1f);

        Camera paused;
        int savedMask;
        CameraClearFlags savedFlags;
        Color savedBg;
        bool savedPost, savedShadows, hadData;
        int lastFps = -2;

        /// 目前是「讀卷軸省電」狀態（3D 場景暫停、限幀）。QualityToggle 量幀率時要排除這段。
        public static bool Reading { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("RenderGovernor");
            DontDestroyOnLoad(go);
            go.AddComponent<RenderGovernor>();
        }

        void Awake() => QualitySettings.vSyncCount = 0; // 桌機上 vSync 會蓋掉 targetFrameRate

        void Update()
        {
            var ui = ScrollUI.I;
            bool reading = ui && !ui.Cinematic && (ui.PanelOpen || ui.ExternalUI);
            Reading = reading;
            bool mobile = TouchInput.Enabled;
            int fps = reading ? (mobile ? MobileReadingFps : DesktopReadingFps) : (mobile ? MobileFps : DesktopFps);
            if (fps != lastFps) { Application.targetFrameRate = fps; lastFps = fps; }

            if (reading && !paused) Pause(Camera.main);
            else if (!reading && paused) Resume();
            else if (paused && paused != Camera.main) Resume(); // 場景重載或換相機
        }

        void Pause(Camera cam)
        {
            if (!cam) return;
            paused = cam;
            savedMask = cam.cullingMask;
            savedFlags = cam.clearFlags;
            savedBg = cam.backgroundColor;
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            hadData = data;
            if (data)
            {
                savedPost = data.renderPostProcessing;
                savedShadows = data.renderShadows;
                data.renderPostProcessing = false;
                data.renderShadows = false;
            }
            cam.cullingMask = 0;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PausedBg;
        }

        void Resume()
        {
            var cam = paused;
            paused = null;
            if (!cam) return;
            cam.cullingMask = savedMask;
            cam.clearFlags = savedFlags;
            cam.backgroundColor = savedBg;
            if (hadData)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data) { data.renderPostProcessing = savedPost; data.renderShadows = savedShadows; }
            }
        }
    }
}
