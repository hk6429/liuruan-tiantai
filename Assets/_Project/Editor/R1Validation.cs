using System;
using System.IO;
using System.Text;
using Liuruan;
using TMPro;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class R1Validation
{
    const string Phase = "Liuruan.R1.Phase";
    const string SaveKey = "liuruan_r1_progress";
    static TestRunnerApi runner;
    static double started, next;
    static int step;
    static readonly string[] Route = { "Act1_Bark_1", "Act1_Bark_2", "Act1_Bark_3", "Act1_Bark_4", "Act1_Bark_5", "Act2_ClimbBase", "Act2_PeachTree", "Act3_Drift_1", "Act3_Drift_2", "Act3_Drift_3", "Act3_Wade", "Act4_Women", "Act4_House", "Act4_Board", "ReturnHome", "Act5_Return" };
    static R1Validation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            File.AppendAllText("logs/U-R1-playstate.md", DateTime.Now.ToString("HH:mm:ss.fff") + " " + state + " phase=" + SessionState.GetString(Phase, "") + " step=" + SessionState.GetInt("Liuruan.R1.Step", -1) + (state == PlayModeStateChange.ExitingPlayMode ? "\n" + Environment.StackTrace : "") + "\n");
            if (state != PlayModeStateChange.EnteredEditMode || SessionState.GetString(Phase, "") != "play") return;
            RestorePrefs();
            SessionState.SetString(Phase, "failed");
            File.AppendAllText("logs/U-R1-failure.md", "Play 驗證提前停止，已還原測試前進度。\n");
        };
        Application.logMessageReceived += (message, stack, type) =>
        {
            string phase = SessionState.GetString(Phase, "");
            if (phase != "" && phase != "done" && phase != "failed" && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetInt("Liuruan.R1.Errors", SessionState.GetInt("Liuruan.R1.Errors", 0) + 1);
        };
    }
    [MenuItem("Tools/Liuruan/R1/Validate All")]
    public static void Begin()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("請先離開 Play 模式。");
        R1Setup.Install();
        started = 0; next = 0;
        SessionState.SetInt("Liuruan.R1.Errors", 0);
        SessionState.SetString(Phase, "tests");
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Results());
        runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "Liuruan.EditModeTests" } }));
    }
    sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor t) { }
        public void TestStarted(ITestAdaptor t) { }
        public void TestFinished(ITestResultAdaptor r) { }
        public void RunFinished(ITestResultAdaptor r)
        {
            var text = new StringBuilder("# EditMode Test Runner\n\n");
            text.AppendLine("Passed=" + r.PassCount + " Failed=" + r.FailCount + " Skipped=" + r.SkipCount + " Duration=" + r.Duration);
            Append(r, text);
            File.WriteAllText("logs/U-R1-tests.md", text.ToString());
            if (r.FailCount != 0 || r.PassCount == 0 || r.SkipCount != 0) { Fail("EditMode 測試未全數通過。"); return; }
            BackupPrefs();
            PlayerPrefs.DeleteKey(SaveKey);
            SessionState.SetString(Phase, "play");
            SessionState.SetInt("Liuruan.R1.Step", 0);
            File.WriteAllText("logs/U-R1-play.md", "# Play 五幕驗證\n\n");
            // Test Runner 還要還原場景，不能在回呼裡直接進 Play；交給 Tick 延後進入。
            SessionState.SetString(Phase, "startplay");
            SessionState.SetFloat("Liuruan.R1.PlayAt", (float)EditorApplication.timeSinceStartup + 3f);
        }
        static void Append(ITestResultAdaptor r, StringBuilder s)
        {
            if (!r.HasChildren) s.AppendLine("- " + r.FullName + "：" + r.ResultState + " " + r.Message);
            else foreach (var child in r.Children) Append(child, s);
        }
    }
    static void BackupPrefs()
    {
        SessionState.SetString("Liuruan.R1.OldSave", PlayerPrefs.GetString(SaveKey, ""));
        for (int id = 0; id <= 100; id++) foreach (string field in new[] { "tries", "solved", "plain" })
        {
            string key = "liuruan_q" + id + "_" + field;
            SessionState.SetInt(key, PlayerPrefs.GetInt(key, -1)); PlayerPrefs.DeleteKey(key);
        }
    }
    static void RestorePrefs()
    {
        string old = SessionState.GetString("Liuruan.R1.OldSave", "");
        if (old.Length == 0) PlayerPrefs.DeleteKey(SaveKey); else PlayerPrefs.SetString(SaveKey, old);
        for (int id = 0; id <= 100; id++) foreach (string field in new[] { "tries", "solved", "plain" })
        {
            string key = "liuruan_q" + id + "_" + field;
            int value = SessionState.GetInt(key, -1);
            if (value < 0) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetInt(key, value);
        }
        PlayerPrefs.Save();
    }
    static void Tick()
    {
        string phase = SessionState.GetString(Phase, "");
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            if (phase == "startplay" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.timeSinceStartup < SessionState.GetFloat("Liuruan.R1.PlayAt", 0)) return;
                SessionState.SetInt("Liuruan.R1.Errors", 0);
                SessionState.SetString(Phase, "play");
                EditorApplication.isPlaying = true;
                return;
            }
            if (phase == "play" && EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                if (EditorApplication.timeSinceStartup < next) return;
                next = EditorApplication.timeSinceStartup + 0.15;
                var flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
                if (!flow || !flow.GetComponentInChildren<Canvas>()) return;
                if (started == 0) { started = EditorApplication.timeSinceStartup; step = SessionState.GetInt("Liuruan.R1.Step", 0); }
                if (EditorApplication.timeSinceStartup - started > 180) throw new TimeoutException("Play 五幕驗證超過 180 秒。");
                ScrollUI.I.charInterval = 0.001f;
                if (flow.Reading) { AdvanceCard(flow); return; }
                if (step == Route.Length)
                {
                    Require(flow.Act == 5 && flow.State.ended, "結局面板未完成");
                    Require(flow.State.bark == 31 && flow.State.drift == 7 && flow.State.inferred, "收集或推論進度錯誤");
                    Require(flow.State.villageStep == 3, "仙村順序錯誤");
                    Require(SessionState.GetInt("Liuruan.R1.Errors", 0) == 0, "Play 期間出現 console error");
                    Require(GameObject.Find("Village/House_0") && !GameObject.Find("Village/House_1"), "歸鄉房屋切換失敗");
                    Require(GameObject.Find("JourneySystems/JourneyHUD/JourneyEnding"), "結局面板未顯示");
                    File.AppendAllText("logs/U-R1-play.md", "\nPASS：五幕實際互動、五棵穀皮、三件漂流物、閱讀卡作答、求歸、結局；捕捉到的 console error=0。\n");
                    SessionState.SetString(Phase, "capture"); EditorApplication.isPlaying = false; return;
                }
                string key = Route[step];
                if (step == 0)
                {
                    Require(Mathf.Approximately(flow.Hunger.Value, 3), "開場飢餓值不是三格");
                    Require(flow.Companion.Call() && flow.Companion.CooldownRatio > 0.99f && !flow.Companion.Call(), "呼喚冷卻錯誤");
                    ScreenCapture.CaptureScreenshot("Captures/r1/r1_hud.png");
                }
                if (key == "ReturnHome")
                {
                    Require(flow.CanReturn, "求歸尚未解鎖");
                    var button = GameObject.Find("JourneySystems/JourneyHUD/ReturnHome").GetComponent<Button>();
                    button.onClick.Invoke();
                }
                else
                {
                    var it = GameObject.Find("ActPoints/" + key).GetComponent<Interactable>();
                    Require(it.enabled, key + " 尚未解鎖");
                    flow.MovePlayer(it.transform.position + Vector3.right);
                    Require(Interactable.Nearest(flow.Player.transform.position) == it, key + " 並非最近互動點");
                    int act = flow.Act;
                    it.Interact();
                    File.AppendAllText("logs/U-R1-play.md", "- Act" + act + " 實際 Interactable.Interact：" + key + "\n");
                    if (key == "Act2_PeachTree") Require(Mathf.Approximately(flow.Hunger.Value, 5), "摘桃沒有補滿飢餓值");
                }
                step++; SessionState.SetInt("Liuruan.R1.Step", step);
            }
            else if (phase == "capture" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestorePrefs();
                string capture = R30Capture.Run("r1");
                File.WriteAllText("logs/U-R1-captures.md", "# 六機位\n\n" + string.Join("\n", Directory.GetFiles(capture, "r1_*.png")));
                SessionState.SetString(Phase, "build");
            }
            else if (phase == "build" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(Phase, "building");
                EditorUserBuildSettings.development = true;
                // 沿用已存在且經確認的選單，不另外啟動 Unity。
                if (!EditorApplication.ExecuteMenuItem("Tools/Liuruan/Build/WebGL + Scene Bundle")) throw new InvalidOperationException("找不到建置選單。");
                Require(File.Exists("Build/WebGL/.bundle-done"), "建置沒有成功旗標");
                SessionState.SetString(Phase, "done");
                File.WriteAllText("logs/U-R1-build.md", "# WebGL Development\n\n" + File.ReadAllText("Build/WebGL/.bundle-done"));
            }
        }
        catch (Exception e) { Fail(e.ToString()); }
    }
    static void AdvanceCard(GameFlow flow)
    {
        if (ScrollUI.I.Typing) return;
        var card = flow.CurrentCard;
        var buttons = ScrollUI.I.GetComponentsInChildren<Button>();
        foreach (var b in buttons)
            if (b.name == "Next" && b.interactable && b.transform.parent.name == "ReadPager") { b.onClick.Invoke(); return; }
        foreach (var b in buttons)
            if (b.name == "CloseBtn" && b.interactable && b.transform.parent.name == "ScrollPanel") { b.onClick.Invoke(); return; }
        if (card == null || !card.HasQuiz) return;
        foreach (var b in buttons)
        {
            var label = b.GetComponentInChildren<TMP_Text>();
            if (b.interactable && label && label.text.EndsWith(". " + card.options[card.answer], StringComparison.Ordinal))
            { b.onClick.Invoke(); return; }
        }
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void Fail(string error)
    {
        SessionState.SetString(Phase, "failed");
        File.AppendAllText("logs/U-R1-failure.md", error + "\n");
        if (EditorApplication.isPlaying) { RestorePrefs(); EditorApplication.isPlaying = false; }
        Debug.LogError("[R1Validation] " + error);
    }
}
