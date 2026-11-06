using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Liuruan
{
    /// 畫質高／低（右上「畫質」鈕）。自動建立（不必掛在場景裡），跨場景重載保留；選擇存在 PlayerPrefs。
    /// 舊版只開關 SSAO，但 WebGL 用的 Mobile_Renderer 根本沒有 SSAO、場景裡也沒掛元件，按了沒反應（審查技術 #4）。
    /// 現在「低」是實際降級：renderScale ×0.8（下限 0.5）、關主光陰影（shadowDistance 0）、關 HDR、關 Bloom；
    /// 「高」＝目前的管線設定（啟動時記下的原值）。預設：觸控裝置低、桌機高。
    /// 裝置判斷與 RenderGovernor 共用 TouchInput.Enabled（審查 #18）：iPadOS Safari 回報為桌機，
    /// 第一次觸控後才知道是平板；玩家還沒自己選過畫質時，此刻自動改成低。
    /// Round8 自動降畫質（只降不升、每次開遊戲最多一次、不寫 PlayerPrefs）：玩家沒選過畫質、目前是高、
    /// 而且正在走動（不在讀卷軸省電、不在演出）時量幀率；進場 6 秒暖機後，連續 8 秒平均低於 24fps 才降為低。
    /// 手機預設就是低、上限 30fps，不受影響；主要救學校的老筆電／內顯桌機。編輯器不啟用（Editor 幀率不代表 WebGL）。
    public class QualityToggle : MonoBehaviour
    {
        public static QualityToggle I { get; private set; }
        const string Key = "liuruan_quality_high";
        const float LowScaleFactor = 0.8f, MinScale = 0.5f;
        const float AutoWarmup = 6f, AutoWindow = 8f, AutoMinFps = 24f, AutoMaxFrame = 0.5f;

        public bool High { get; private set; }

        UniversalRenderPipelineAsset urp;
        float origScale, origShadow;
        int origMsaa;
        bool origHdr, captured, touchChecked, autoDone;
        // R30：桌機「高」比手機管線再往上一級（WebGL 一律用 Mobile_RPAsset）
        const float DesktopScale = 1f, DesktopShadow = 45f;
        const int DesktopMsaa = 4; // 不動 cascade 數：build 會依管線資產剝掉 cascade 變體
        float autoWarm, autoTime;
        int autoFrames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (I) return;
            var go = new GameObject("QualityToggle");
            DontDestroyOnLoad(go);
            go.AddComponent<QualityToggle>();
        }

        void Awake()
        {
            if (I && I != this) { Destroy(this); return; }
            I = this;
            urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp)
            {
                origScale = urp.renderScale;
                origShadow = urp.shadowDistance;
                origHdr = urp.supportsHDR;
                origMsaa = urp.msaaSampleCount;
                captured = true;
            }
            int def = TouchInput.Enabled ? 0 : 1;
            High = PlayerPrefs.GetInt(Key, def) == 1;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start() => Apply(High);

        void Update()
        {
            AutoDowngrade();
            if (touchChecked || !TouchInput.Enabled) return;
            touchChecked = true;
            if (High && !PlayerPrefs.HasKey(Key)) Apply(false); // 不寫 PlayerPrefs：仍算「沒選過」；ScrollUI 會同步按鈕文字
        }

        void AutoDowngrade()
        {
            if (autoDone || Application.isEditor) return;
            if (!High || PlayerPrefs.HasKey(Key)) { autoDone = true; return; }
            float dt = Time.unscaledDeltaTime;
            var ui = ScrollUI.I;
            // 讀卷軸（場景暫停、限 30fps）、演出、切到別的分頁回來的長幀（> 0.5 秒）都不算；暖機期間也不算（著色器第一次編譯會卡）
            if (RenderGovernor.Reading || (ui && ui.Cinematic) || dt > AutoMaxFrame) { autoTime = 0; autoFrames = 0; return; }
            if (autoWarm < AutoWarmup) { autoWarm += dt; return; }
            autoTime += dt; autoFrames++;
            if (autoTime < AutoWindow) return;
            if (autoFrames / autoTime < AutoMinFps) { Apply(false); autoDone = true; } // ScrollUI 每幀同步畫質鈕文字
            autoTime = 0; autoFrames = 0;
        }

        void OnSceneLoaded(Scene s, LoadSceneMode m) => Apply(High); // 新場景的 Volume 也要套 Bloom 開關

        public void Toggle()
        {
            autoDone = true; // 玩家自己選過就不再自動調
            Apply(!High);
            PlayerPrefs.SetInt(Key, High ? 1 : 0);
            PlayerPrefs.Save();
        }

        void Apply(bool high)
        {
            High = high;
            if (urp && captured)
            {
                bool desktop = high && !TouchInput.Enabled;
                urp.renderScale = desktop ? DesktopScale : high ? origScale : Mathf.Max(MinScale, origScale * LowScaleFactor);
                urp.shadowDistance = desktop ? DesktopShadow : high ? origShadow : 0f;
                urp.msaaSampleCount = desktop ? DesktopMsaa : origMsaa;
                urp.supportsHDR = high && origHdr;
            }
            SetBloom(high);
        }

        static void SetBloom(bool on)
        {
            foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                var prof = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;
                if (prof && prof.TryGet<Bloom>(out var bloom)) bloom.active = on;
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
#if UNITY_EDITOR
            // 編輯器中別把管線資產／Volume Profile 留成低畫質
            if (urp && captured) { urp.renderScale = origScale; urp.shadowDistance = origShadow; urp.supportsHDR = origHdr; urp.msaaSampleCount = origMsaa; }
            SetBloom(true);
#endif
        }
    }
}
