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
public static class R2Validation
{
    const string Phase = "Liuruan.R2.Phase";
    const string SaveKey = "liuruan_r1_progress";
    static TestRunnerApi runner;
    static double started, next;
    static int step;
    static double actionAt;
    static Vector3 driftStart;
    static bool chasing, climbing;
    static readonly string[] Route = { "Act1_Bark_1", "Act1_Bark_2", "Act1_Bark_3", "Act1_Bark_4", "Act1_Bark_5", "Act2_ClimbBase", "Act2_PeachTree", "Act3_Drift_1", "Act3_Drift_2", "Act3_Drift_3", "Act3_Wade", "Act4_Women", "Act4_House", "Act4_Board", "ReturnHome", "Act5_Return" };
    static R2Validation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            File.AppendAllText("logs/U-R2-playstate.md", DateTime.Now.ToString("HH:mm:ss.fff") + " " + state + " phase=" + SessionState.GetString(Phase, "") + " step=" + SessionState.GetInt("Liuruan.R2.Step", -1) + (state == PlayModeStateChange.ExitingPlayMode ? "\n" + Environment.StackTrace : "") + "\n");
            if (state != PlayModeStateChange.EnteredEditMode || SessionState.GetString(Phase, "") != "play") return;
            RestorePrefs();
            SessionState.SetString(Phase, "failed");
            File.AppendAllText("logs/U-R2-failure.md", "Play 驗證提前停止，已還原測試前進度。\n");
        };
        Application.logMessageReceived += (message, stack, type) =>
        {
            string phase = SessionState.GetString(Phase, "");
            if (phase != "" && phase != "done" && phase != "failed" && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetInt("Liuruan.R2.Errors", SessionState.GetInt("Liuruan.R2.Errors", 0) + 1);
        };
    }
    [MenuItem("Tools/Liuruan/R2/Validate All")]
    public static void Begin()
    {
        // 清除舊輪次的流程，避免安裝期間 domain reload 接續殘留作業。
        RestorePrefs();
        SessionState.SetString("Liuruan.R1.Phase", "");
        SessionState.SetString(Phase, "");
        SessionState.SetInt("Liuruan.R2.Step", 0);
        SessionState.SetFloat("Liuruan.R2.PlayAt", 0);
        started = next = actionAt = 0; chasing = climbing = false;
        if (EditorApplication.isPlaying) throw new InvalidOperationException("請先離開 Play 模式。");
        SessionState.SetString("Liuruan.R2.CaptureAfter", DateTime.UtcNow.Ticks.ToString());
        Directory.CreateDirectory("Captures/r2");
        SessionState.SetInt("Liuruan.R2.Errors", 0);
        SessionState.SetString(Phase, "install");
        try { R2Setup.Install(); Require(SessionState.GetInt("Liuruan.R2.Errors", 0) == 0, "安裝期間出現 console error"); }
        catch (Exception e) { Fail(e.ToString()); return; }
        started = 0; next = 0;
        SessionState.SetInt("Liuruan.R2.Errors", 0);
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
            File.WriteAllText("logs/U-R2-tests.md", text.ToString());
            if (r.FailCount != 0 || r.PassCount == 0 || r.SkipCount != 0) { Fail("EditMode 測試未全數通過。"); return; }
            BackupPrefs();
            PlayerPrefs.DeleteKey(SaveKey);
            SessionState.SetString(Phase, "play");
            SessionState.SetInt("Liuruan.R2.Step", 0);
            File.WriteAllText("logs/U-R2-play.md", "# Play 五幕驗證\n\n");
            // Test Runner 還要還原場景，不能在回呼裡直接進 Play；交給 Tick 延後進入。
            SessionState.SetString(Phase, "startplay");
            SessionState.SetFloat("Liuruan.R2.PlayAt", (float)EditorApplication.timeSinceStartup + 3f);
        }
        static void Append(ITestResultAdaptor r, StringBuilder s)
        {
            if (!r.HasChildren) s.AppendLine("- " + r.FullName + "：" + r.ResultState + " " + r.Message);
            else foreach (var child in r.Children) Append(child, s);
        }
    }
    static void BackupPrefs()
    {
        SessionState.SetBool("Liuruan.R2.PrefsBackedUp", true);
        SessionState.SetString("Liuruan.R2.OldSave", PlayerPrefs.GetString(SaveKey, ""));
        for (int id = 0; id <= 100; id++) foreach (string field in new[] { "tries", "solved", "plain" })
        {
            string key = "liuruan_q" + id + "_" + field;
            SessionState.SetInt(key, PlayerPrefs.GetInt(key, -1)); PlayerPrefs.DeleteKey(key);
        }
    }
    static void RestorePrefs()
    {
        if (!SessionState.GetBool("Liuruan.R2.PrefsBackedUp", false)) return;
        string old = SessionState.GetString("Liuruan.R2.OldSave", "");
        if (old.Length == 0) PlayerPrefs.DeleteKey(SaveKey); else PlayerPrefs.SetString(SaveKey, old);
        for (int id = 0; id <= 100; id++) foreach (string field in new[] { "tries", "solved", "plain" })
        {
            string key = "liuruan_q" + id + "_" + field;
            int value = SessionState.GetInt(key, -1);
            if (value < 0) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetInt(key, value);
        }
        PlayerPrefs.Save();
        SessionState.SetBool("Liuruan.R2.PrefsBackedUp", false);
    }
    static void Tick()
    {
        string phase = SessionState.GetString(Phase, "");
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            if (phase == "startplay" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.timeSinceStartup < SessionState.GetFloat("Liuruan.R2.PlayAt", 0)) return;
                SessionState.SetInt("Liuruan.R2.Errors", 0);
                SessionState.SetString(Phase, "play");
                EditorApplication.isPlaying = true;
                return;
            }
            if (phase == "play" && EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                if (EditorApplication.timeSinceStartup < next) return;
                next = EditorApplication.timeSinceStartup + 0.15;
                if (started == 0) { started = EditorApplication.timeSinceStartup; step = SessionState.GetInt("Liuruan.R2.Step", 0); }
                if (EditorApplication.timeSinceStartup - started > 300) throw new TimeoutException("Play 五幕驗證超過 300 秒。");
                var flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
                if (!flow || !flow.GetComponentInChildren<Canvas>()) return;
                ScrollUI.I.charInterval = 0.001f;
                if (flow.Reading) { AdvanceCard(flow); return; }
                if (step == Route.Length)
                {
                    Require(flow.Act == 5 && flow.State.ended, "結局面板未完成");
                    Require(flow.State.bark == 31 && flow.State.drift == 7 && flow.State.inferred, "收集或推論進度錯誤");
                    Require(flow.driftCollection.Count == 3, "漂流物未共用收集器");
                    Require(flow.State.villageStep == 3, "仙村順序錯誤");
                    Require(SessionState.GetInt("Liuruan.R2.Errors", 0) == 0, "Play 期間出現 console error");
                    Require(flow.seasons.IsHome && GameObject.Find("R2HomeVillage") && !GameObject.Find("Village"), "歸鄉房屋切換失敗");
                    Require(flow.epilogue.Model != null && flow.epilogue.Model.Score.DriftCount == 3 && flow.epilogue.Model.Score.BarkCount == 5, "結局資料模型／成績錯誤");
                    Require(GameObject.Find("JourneySystems/JourneyHUD/JourneyEnding"), "結局面板未顯示");
                    VerifyEnding(flow);
                    File.AppendAllText("logs/U-R2-play.md", "\nPASS：五幕實際互動、五棵穀皮、三件漂流物、閱讀卡作答、求歸、結局；捕捉到的 console error=0。\n");
                    SessionState.SetString(Phase, "capture"); EditorApplication.isPlaying = false; return;
                }
                if (flow.Act >= 2) Require(flow.minimap.FogMask.GetPixel(31, 31).a == 0, "第二幕起仍有迷霧");
                string key = Route[step];
                if (key == "Act2_ClimbBase" && climbing)
                {
                    if (!flow.State.climbed)
                    {
                        Require(flow.Player.animator.GetBool("Climbing"), "攀藤未設定 Climbing 動畫");
                        Require(flow.climb.IsLocked, "攀藤未到頂就失去鎖定");
                        flow.climb.SetMoveInput(Vector2.up);
                        return;
                    }
                    Require(flow.Player.enabled && flow.Player.GetComponent<CharacterController>().enabled, "攀藤後沒有還原控制");
                    Require(flow.Player.transform.position.y > 17.8f, "未真正攀至崖頂");
                    Require(!flow.Player.animator.GetBool("Climbing"), "攀藤結束未還原動畫");
                    flow.climb.SetMoveInput(Vector2.zero); climbing = false;
                    File.AppendAllText("logs/U-R2-play.md", "- PASS：ClimbZone 經 Attaching/Climbing/TopOut 到平台，控制已還原。\n");
                    step++; SessionState.SetInt("Liuruan.R2.Step", step); return;
                }
                if (key.StartsWith("Act3_Drift_"))
                {
                    var item = GameObject.Find("ActPoints/" + key);
                    Require(item, key + " 提前消失");
                    var pickup = item.GetComponent<DriftPickup>();
                    if (!chasing)
                    {
                        flow.MovePlayer(item.transform.position + new Vector3(-6, 0, 0));
                        chasing = true; actionAt = EditorApplication.timeSinceStartup; driftStart = item.transform.position; return;
                    }
                    if (EditorApplication.timeSinceStartup - actionAt < 1) return;
                    Require((item.transform.position - driftStart).magnitude > 0.02f, "漂流物沒有移動");
                    if (!pickup.CanInteract)
                    {
                        var cc = flow.Player.GetComponent<CharacterController>();
                        Vector3 offset = item.transform.position - flow.Player.transform.position; offset.y = 0;
                        PlayerController.MoveDisplacement(cc, Vector3.ClampMagnitude(offset, 0.7f));
                        return;
                    }
                    Require(Interactable.Nearest(flow.Player.transform.position) == item.GetComponent<Interactable>(), "移動漂流物不是最近互動點");
                    item.GetComponent<Interactable>().Interact();
                    Require(!item.activeSelf, "漂流物撿起後沒有隱藏");
                    Require(flow.driftCollection.Count == step - 6, "共用漂流收集計數錯誤");
                    if (step < 9) Require(!flow.Reading && !flow.State.inferred, "未集滿三件就觸發推論");
                    File.AppendAllText("logs/U-R2-play.md", "- PASS：追上移動漂流物並互動 " + key + "\n");
                    chasing = false; step++; SessionState.SetInt("Liuruan.R2.Step", step); return;
                }
                if (step == 0)
                {
                    Require(Mathf.Approximately(flow.Hunger.Value, 3), "開場飢餓值不是三格");
                    Require(flow.Companion.Call() && flow.Companion.CooldownRatio > 0.99f && !flow.Companion.Call(), "呼喚冷卻錯誤");
                    Require(flow.minimap && flow.minimap.Output && flow.minimap.FogMask, "小地圖未初始化");
                    Require(flow.minimap.FogMask.GetPixel(31, 31).a > 0.9f, "第一幕沒有迷霧");
                    foreach (var animator in new[] { flow.Player.animator, flow.Companion.animator })
                    { Require(animator && animator.avatar.isValid && animator.avatar.isHuman && !animator.applyRootMotion, "Humanoid／Root Motion 設定錯誤"); }
                    ScreenCapture.CaptureScreenshot("Captures/r2/r2_hud.png");
                }
                if (key == "ReturnHome")
                {
                    if (flow.SeasonPlaying) { Require(!flow.CanReturn, "半年演出尚未結束就能求歸"); return; }
                    Require(flow.State.halfYearComplete && flow.CanReturn, "求歸尚未解鎖");
                    var button = GameObject.Find("JourneySystems/JourneyHUD/ReturnHome").GetComponent<Button>();
                    button.onClick.Invoke();
                }
                else
                {
                    var it = GameObject.Find("ActPoints/" + key).GetComponent<Interactable>();
                    Require(it.enabled, key + " 尚未解鎖");
                    flow.MovePlayer(it.transform.position + Vector3.right);
                    if (key == "Act2_ClimbBase" && !flow.climb.CanInteract) return;
                    Require(Interactable.Nearest(flow.Player.transform.position) == it, key + " 並非最近互動點");
                    int act = flow.Act;
                    it.Interact();
                    if (key == "Act2_ClimbBase")
                    {
                        Require(flow.climb.IsLocked && !flow.State.climbed, "攀援仍使用瞬移流程");
                        climbing = true; flow.climb.SetMoveInput(Vector2.up); return;
                    }
                    File.AppendAllText("logs/U-R2-play.md", "- Act" + act + " 實際 Interactable.Interact：" + key + "\n");
                    if (key == "Act2_PeachTree") Require(Mathf.Approximately(flow.Hunger.Value, 5), "摘桃沒有補滿飢餓值");
                }
                step++; SessionState.SetInt("Liuruan.R2.Step", step);
            }
            else if (phase == "capture" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestorePrefs();
                string capture = R30Capture.Run("r2");
                Require(Directory.GetFiles(capture, "r2_*.png").Length == 8, "截圖未達六機位＋HUD＋人物近景共八張");
                foreach (string file in Directory.GetFiles(capture, "r2_*.png"))
                {
                    var info = new FileInfo(file);
                    Require(info.Length > 1024 && info.LastWriteTimeUtc.Ticks >= long.Parse(SessionState.GetString("Liuruan.R2.CaptureAfter", "0")), "截圖無效或屬於舊輪：" + file);
                }
                File.WriteAllText("logs/U-R2-captures.md", "# 六機位\n\n" + string.Join("\n", Directory.GetFiles(capture, "r2_*.png")));
                SessionState.SetString(Phase, "build");
            }
            else if (phase == "build" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(Phase, "building");
                EditorUserBuildSettings.development = false;
                // 沿用已存在且經確認的選單，不另外啟動 Unity。
                if (!EditorApplication.ExecuteMenuItem("Tools/Liuruan/Build/WebGL + Scene Bundle")) throw new InvalidOperationException("找不到建置選單。");
                Require(File.Exists("Build/WebGL/.bundle-done") && File.GetLastWriteTimeUtc("Build/WebGL/.bundle-done").Ticks >= long.Parse(SessionState.GetString("Liuruan.R2.CaptureAfter", "0")), "建置沒有本輪成功旗標");
                Require(PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Brotli, "專案壓縮不是 Brotli");
                Require(Directory.GetFiles("Build/WebGL/Build", "*.unityweb").Length > 0, "deploy.sh 需要 *.unityweb，請檢查現有 decompression fallback 設定");
                var dataFiles = Directory.GetFiles("Build/WebGL/Build", "*.data*", SearchOption.TopDirectoryOnly);
                Require(dataFiles.Length > 0, "找不到 WebGL data 檔");
                var text = new StringBuilder("# R2b WebGL Release\n\ndevelopment=false\nCompression=" + PlayerSettings.WebGL.compressionFormat + "\nDecompressionFallback=" + PlayerSettings.WebGL.decompressionFallback + "\n");
                var wasmFiles = Directory.GetFiles("Build/WebGL/Build", "*.wasm*");
                Require(wasmFiles.Length > 0, "找不到 wasm 檔");
                foreach (string file in wasmFiles) text.AppendLine(file + "：" + new FileInfo(file).Length + " bytes");
                foreach (string file in dataFiles)
                {
                    long bytes = new FileInfo(file).Length;
                    text.AppendLine(file + "：" + bytes + " bytes");
                    File.WriteAllText("logs/U-R2-build.md", text.ToString());
                    Require(bytes > 0 && bytes < 26214400, "data 超過限制或為空");
                }
                Require(SessionState.GetInt("Liuruan.R2.Errors", 0) == 0, "R2 流程出現 console error");
                text.AppendLine("PASS：.bundle-done=" + File.ReadAllText("Build/WebGL/.bundle-done"));
                File.WriteAllText("logs/U-R2-build.md", text.ToString());
                SessionState.SetString(Phase, "done");
            }
        }
        catch (Exception e) { Fail(e.ToString()); }
    }
    static void VerifyEnding(GameFlow flow)
    {
        var model = flow.epilogue.Model;
        var panel = GameObject.Find("JourneySystems/JourneyHUD/JourneyEnding").transform;
        var text = panel.Find("Label").GetComponent<TMP_Text>();
        var nextPage = panel.Find("EndingNext").GetComponent<Button>();
        foreach (string paragraph in model.Paragraphs)
        {
            Require(text.text.Contains(paragraph), "結局全文頁遺漏原文");
            nextPage.onClick.Invoke();
        }
        Require(text.text == model.Question.Prompt, "結局推論題未顯示");
        for (int i = 0; i < 3; i++)
        {
            var option = panel.Find("EndingOption" + i).GetComponent<Button>();
            Require(option.gameObject.activeInHierarchy && option.GetComponentInChildren<TMP_Text>().text == model.Question.Options[i], "結局選項錯誤");
        }
        panel.Find("EndingOption" + model.Question.AnswerIndex).GetComponent<Button>().onClick.Invoke();
        Require(QuizStats.FirstTryCorrect(QuizStats.InferId) && text.text.Contains(model.Question.Explanation), "結局作答／解析未接線");
        nextPage.onClick.Invoke();
        foreach (var row in model.Comparison)
        {
            Require(text.text.Contains(row.Aspect) && text.text.Contains(row.Liuruan) && text.text.Contains(row.Taohua), "比較表頁遺漏內容");
            nextPage.onClick.Invoke();
        }
        Require(text.text.Contains("成績卡") && text.text.Contains("6／6"), "成績卡未顯示全六題首答結果");
        File.AppendAllText("logs/U-R2-play.md", "- PASS：全文各段、三選一作答／解析、比較表各列與成績卡，均經實際面板按鈕巡覽。\n");
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
        File.AppendAllText("logs/U-R2-failure.md", error + "\n");
        RestorePrefs();
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        Debug.LogError("[R2Validation] " + error);
    }
}
