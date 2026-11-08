using System;
using System.IO;
using System.Text;
using Liuruan;
using TMPro;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class R3Validation
{
    static Vector3 preFall;
    const string Phase = "Liuruan.R3.Phase";
    const string SaveKey = "liuruan_r1_progress";
    static TestRunnerApi runner;
    static double started, next;
    static int step;
    static double actionAt;
    static Vector3 driftStart;
    static int lastFrame = -1, lastAct, recoveryStage, endingStage;
    static float closest = float.MaxValue;
    static string activePoint;
    static NavMeshPath walkPath;
    static int corner, expectedRecoveries, hintLevels, introFrame, mobileStage, oldSize;
    static bool capturedStory, capturedTutorial, capturedTerm, stallComplete, secondVisit, capturedGuide;
    static readonly System.Collections.Generic.HashSet<string> capturedDialogues = new System.Collections.Generic.HashSet<string>();
    static int mobileFrame;
    static double stallAt;
    static GameFlow introFlow;
    static Transform arrowShot;
    static GameFlow observedFlow;
    static readonly System.Collections.Generic.HashSet<string> endedDialogues = new System.Collections.Generic.HashSet<string>();
    static void DialogueEnded(string id) { endedDialogues.Add(id); }
    static void HintLevel(StallHint h) { hintLevels |= 1 << (h.Level - 1); }
    static GameFlow reloadedFrom;
    static readonly string[] IntKeys = { "liuruan_help_seen", "liuruan_mute", "liuruan_quality_high", "liuruan_bigfont", "liuruan_fontlevel", R3Journey.StoryKey, R3Journey.TutorialKey };
    static readonly string[] StringKeys = { SaveKey, "liuruan_collected" };
    static bool chasing, climbing, climbShot;
    static readonly string[] Route = { "Act1_Bark_1", "Act1_Bark_2", "Act1_Bark_3", "Act1_Bark_4", "Act1_Bark_5", "Act2_ClimbBase", "Act2_PeachTree", "Act3_Drift_1", "Act3_Drift_2", "Act3_Drift_3", "Act3_Wade", "Act4_Women", "Act4_Home", "Act4_House", "Act4_Return", "Act5_Return", "Act5_Villager", "Act5_Grandson" };
    static R3Validation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            File.AppendAllText("logs/U-R3-playstate.md", DateTime.Now.ToString("HH:mm:ss.fff") + " " + state + " phase=" + SessionState.GetString(Phase, "") + " step=" + SessionState.GetInt("Liuruan.R3.Step", -1) + (state == PlayModeStateChange.ExitingPlayMode ? "\n" + Environment.StackTrace : "") + "\n");
            if (state != PlayModeStateChange.EnteredEditMode || SessionState.GetString(Phase, "") != "play") return;
            RestorePrefs();
            SessionState.SetString(Phase, "failed");
            File.AppendAllText("logs/U-R3-failure.md", "Play 驗證提前停止，已還原測試前進度。\n");
        };
        Application.logMessageReceived += (message, stack, type) =>
        {
            string phase = SessionState.GetString(Phase, "");
            if (phase != "" && phase != "done" && phase != "failed" && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            {
                SessionState.SetInt("Liuruan.R3.Errors", SessionState.GetInt("Liuruan.R3.Errors", 0) + 1);
                File.AppendAllText("logs/U-R3-failure.md", type + ": " + message + "\n" + stack + "\n");
            }
        };
    }
    [MenuItem("Tools/Liuruan/R3/Validate All")]
    public static void Begin()
    {
        // 清除舊輪次的流程，避免安裝期間 domain reload 接續殘留作業。
        RestorePrefs();
        SessionState.SetString("Liuruan.R1.Phase", "");
        SessionState.SetString("Liuruan.R2.Phase", "");
        SessionState.SetString(Phase, "");
        SessionState.SetInt("Liuruan.R3.Step", 0);
        SessionState.SetFloat("Liuruan.R3.PlayAt", 0);
        started = next = actionAt = 0; chasing = climbing = climbShot = false;
        capturedStory = capturedTutorial = capturedTerm = stallComplete = secondVisit = false;
        capturedGuide = false; capturedDialogues.Clear(); endedDialogues.Clear(); observedFlow = null;
        introFlow = null; hintLevels = introFrame = mobileStage = 0; stallAt = 0;
        lastFrame = -1; lastAct = recoveryStage = endingStage = 0; activePoint = null;
        File.WriteAllText("logs/U-R3-failure.md", "# R3 本輪失敗紀錄\n\n");
        if (EditorApplication.isPlaying) throw new InvalidOperationException("請先離開 Play 模式。");
        SessionState.SetString("Liuruan.R3.CaptureAfter", DateTime.UtcNow.Ticks.ToString());
        Directory.CreateDirectory("Captures/r3");
        SessionState.SetInt("Liuruan.R3.Errors", 0);
        SessionState.SetString(Phase, "install");
        try { R3Setup.Install(); Require(SessionState.GetInt("Liuruan.R3.Errors", 0) == 0, "安裝期間出現 console error"); }
        catch (Exception e) { Fail(e.ToString()); return; }
        started = 0; next = 0;
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
            File.WriteAllText("logs/U-R3-tests.md", text.ToString());
            if (r.FailCount != 0 || r.PassCount == 0 || r.SkipCount != 0) { Fail("EditMode 測試未全數通過。"); return; }
            BackupPrefs();
            ClearTestPrefs();
            SessionState.SetString(Phase, "play");
            SessionState.SetInt("Liuruan.R3.Step", 0);
            File.WriteAllText("logs/U-R3-play.md", "# Play 真人可達驗證\n\n|互動點|最近距離 m|半徑 m|半徑內|耗時 s|結果|\n|---|---:|---:|---|---:|---|\n");
            // Test Runner 還要還原場景，不能在回呼裡直接進 Play；交給 Tick 延後進入。
            SessionState.SetString(Phase, "startplay");
            SessionState.SetFloat("Liuruan.R3.PlayAt", (float)EditorApplication.timeSinceStartup + 3f);
        }
        static void Append(ITestResultAdaptor r, StringBuilder s)
        {
            if (!r.HasChildren) s.AppendLine("- " + r.FullName + "：" + r.ResultState + " " + r.Message);
            else foreach (var child in r.Children) Append(child, s);
        }
    }
    static void BackupPrefs()
    {
        SessionState.SetBool("Liuruan.R3.PrefsBackedUp", true);
        foreach (var key in StringKeys)
        {
            SessionState.SetBool("R3.Has." + key, PlayerPrefs.HasKey(key));
            SessionState.SetString("R3.Old." + key, PlayerPrefs.GetString(key, ""));
        }
        foreach (var key in IntKeys)
        {
            SessionState.SetBool("R3.Has." + key, PlayerPrefs.HasKey(key));
            SessionState.SetInt("R3.Old." + key, PlayerPrefs.GetInt(key, 0));
        }
        for (int id = 0; id <= 100; id++) foreach (string field in new[] { "tries", "solved", "plain" })
        {
            string key = "liuruan_q" + id + "_" + field;
            SessionState.SetInt(key, PlayerPrefs.GetInt(key, -1)); PlayerPrefs.DeleteKey(key);
        }
    }
    static void RestorePrefs()
    {
        if (!SessionState.GetBool("Liuruan.R3.PrefsBackedUp", false)) return;
        foreach (var key in StringKeys)
        {
            if (SessionState.GetBool("R3.Has." + key, false)) PlayerPrefs.SetString(key, SessionState.GetString("R3.Old." + key, ""));
            else PlayerPrefs.DeleteKey(key);
        }
        foreach (var key in IntKeys)
        {
            if (SessionState.GetBool("R3.Has." + key, false)) PlayerPrefs.SetInt(key, SessionState.GetInt("R3.Old." + key, 0));
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.DeleteKey("liuruan_r3_probe");
        for (int id = 0; id <= 100; id++) foreach (string field in new[] { "tries", "solved", "plain" })
        {
            string key = "liuruan_q" + id + "_" + field;
            int value = SessionState.GetInt(key, -1);
            if (value < 0) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetInt(key, value);
        }
        PlayerPrefs.Save();
        SessionState.SetBool("Liuruan.R3.PrefsBackedUp", false);
    }
    static void Tick()
    {
        string phase = SessionState.GetString(Phase, "");
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            if (phase == "startplay" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.timeSinceStartup < SessionState.GetFloat("Liuruan.R3.PlayAt", 0)) return;
                SessionState.SetString(Phase, "play");
                EditorApplication.isPlaying = true;
                return;
            }
            if (phase == "play" && EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                if (Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                if (started == 0) { started = EditorApplication.timeSinceStartup; step = SessionState.GetInt("Liuruan.R3.Step", 0); }
                Require(EditorApplication.timeSinceStartup - started < 900, "Play 驗證超過 900 秒。");
                var flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
                if (!flow || !flow.GetComponentInChildren<Canvas>()) return;
                ScrollUI.I.charInterval = .001f;
                var journey = flow.journey;
                if (!journey) throw new InvalidOperationException("R3Journey 未安裝。");
                if (!ReferenceEquals(observedFlow, flow)) { observedFlow = flow; journey.dialogue.OnSceneEnd += DialogueEnded; }
                if (journey.StoryVisible)
                {
                    if (!capturedStory) { ScreenCapture.CaptureScreenshot("Captures/r3/r3_story.png", 3); capturedStory = true; }
                    if (introFrame++ % 12 == 0) journey.Next(); return;
                }
                if (journey.Busy)
                {
                    if (journey.dialogue.IsActive)
                    {
                        string id = journey.dialogue.Core.SceneId;
                        if ((id == "meet_women" || id == "meet_grandson") && capturedDialogues.Add(id)) ScreenCapture.CaptureScreenshot("Captures/r3/r3_dialogue_" + (id == "meet_women" ? "women" : "grandson") + ".png", 3);
                    }
                    if (introFrame++ % 12 == 0) journey.Next(); return;
                }
                if (step == Route.Length) { FinishJourney(flow); return; }
                if (recoveryStage == 0)
                {
                    // CC：Opening 協程先 yield 一格才開故事卡，給 10 秒寬限避免競態誤判。
                    if (!(capturedStory && journey.TutorialStep > 0) && EditorApplication.timeSinceStartup - started < 10) return;
                    Require(capturedStory && journey.StoryCount == 1 && journey.TutorialStep > 0, "首次故事／教學未出現。");
                    ScreenCapture.CaptureScreenshot("Captures/r3/r3_tutorial.png", 3); capturedTutorial = true;
                    preFall = flow.Player.transform.position; // CC：救援回「最後安全點」（RescueCore），不一定是幕起點
                    flow.MovePlayer(new Vector3(0, -20, -104));
                    recoveryStage = 1; actionAt = EditorApplication.timeSinceStartup; return;
                }
                if (recoveryStage == 1)
                {
                    if (EditorApplication.timeSinceStartup - actionAt < .3) return;
                    Require(flow.Player.transform.position.y > -10 && Horizontal(flow.Player.transform.position, preFall) < 1.5f, "掉落未回最後安全點。");
                    recoveryStage = 2; expectedRecoveries = flow.RecoveryCount;
                }
                if (secondVisit && !ReferenceEquals(introFlow,flow)) expectedRecoveries = flow.RecoveryCount;
                Require(flow.RecoveryCount == expectedRecoveries, "步行路線觸發額外救回，不可算成可達。" );
                if (flow.Reading)
                {
                    if (!capturedTerm && !ScrollUI.I.Typing)
                    {
                        journey.ReadingLink("t:穀皮"); Require(journey.TermVisible, "詞卡未顯示。");
                        ScreenCapture.CaptureScreenshot("Captures/r3/r3_term.png", 3); capturedTerm = true; next = EditorApplication.timeSinceStartup + .3; return;
                    }
                    if (journey.TermVisible) { if (EditorApplication.timeSinceStartup < next) return; journey.CloseTerm(); }
                    AdvanceCard(flow); return;
                }
                if (ScrollUI.I.IsBusy) return;
                if (step == 1 && !stallComplete)
                {
                    if (journey.TutorialStep > 0) return;
                    if (ReferenceEquals(introFlow,null))
                    {
                        Require(capturedTutorial && journey.TutorialCount == 1, "首次教學未完成。");
                        introFlow = flow; secondVisit = true;
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name); return;
                    }
                    if (secondVisit)
                    {
                        if (introFlow == flow) return;
                        Require(journey.StoryCount == 0 && journey.TutorialCount == 0, "第二次載入重播故事／教學。");
                        Require(flow.State.bark == 1, "引導測試重載遺失進度。");
                        secondVisit = false; journey.hints.OnHint += HintLevel;
                        journey.hints.Progress(); stallAt = EditorApplication.timeSinceStartup;
                        ScreenCapture.CaptureScreenshot("Captures/r3/r3_objective.png", 3);
                        arrowShot = new GameObject("R3ArrowTestCamera").transform;
                        arrowShot.position = flow.Player.cam.position;
                        arrowShot.rotation = Quaternion.LookRotation(flow.Player.cam.position - GameObject.Find("ActPoints/Act1_Bark_2").transform.position);
                        PlayerController.CamShot = arrowShot; next = EditorApplication.timeSinceStartup + .5;
                        return;
                    }
                    if (arrowShot)
                    {
                        if (EditorApplication.timeSinceStartup < next) return;
                        Require(journey.ArrowVisible, "畫面外箭頭未出現。");
                        ScreenCapture.CaptureScreenshot("Captures/r3/r3_arrow.png", 3);
                        PlayerController.CamShot = null; UnityEngine.Object.Destroy(arrowShot.gameObject); arrowShot = null;
                    }
                    Require(EditorApplication.timeSinceStartup - stallAt < 180, "95 秒停滯模擬未完成。");
                    if (journey.hints.Core.ElapsedSeconds < 95) return;
                    Require(hintLevels == 7, "停滯 L1／L2／L3 未全部觸發。");
                    journey.hints.OnHint -= HintLevel;
                    File.AppendAllText("logs/U-R3-play.md", "- PASS：首次故事／互動教學、第二次載入不重播、停滯 95 秒三級提示、畫面外箭頭。\n");
                    stallComplete = true;
                }
                if (stallComplete && mobileStage < 3)
                {
                    if (mobileStage == 0) { TouchInput.ForceTouch = true; oldSize = MobileSize(); mobileFrame = Time.frameCount; mobileStage = 1; return; }
                    if (Time.frameCount - mobileFrame < 5) return;
                    if (mobileStage == 1)
                    {
                        File.AppendAllText("logs/U-R3-play.md", "- 手機 GameView 實際 " + Screen.width + "×" + Screen.height + "（WARN：GameView 未切到直式，手機版面改瀏覽器實測）。\n");
                        ScreenCapture.CaptureScreenshot("Captures/r3/r3_mobile.png", 3); mobileStage = 2; mobileFrame = Time.frameCount; return;
                    }
                    RestoreSize(); mobileStage = 3; return;
                }
                if (lastAct != flow.Act)
                {
                    lastAct = flow.Act;
                    // 唯一的路線放置入口：每幕起點；互動點一律步行接近。
                    flow.MovePlayer(GameFlow.ActStart(flow.Act));
                    ScreenCapture.CaptureScreenshot("Captures/r3/r3_act" + flow.Act + ".png", 3);
                }
                string key = Route[step];
                var it = GameObject.Find("ActPoints/" + key)?.GetComponent<Interactable>();
                if (flow.SeasonPlaying) return;
                Require(it && it.enabled, key + " 互動點不存在或未解鎖。");
                if (activePoint != key)
                {
                    activePoint = key; closest = float.MaxValue; actionAt = EditorApplication.timeSinceStartup;
                    walkPath = null; corner = 0; chasing = climbing = false;
                }
                float distance = Horizontal(flow.Player.transform.position, it.transform.position);
                closest = Mathf.Min(closest, distance);
                bool inside = distance <= it.radius && (it.GetComponent<DriftPickup>() || it.GetComponent<ClimbZone>() || Mathf.Abs(flow.Player.transform.position.y - it.transform.position.y) <= 3);
                if (EditorApplication.timeSinceStartup - actionAt > 90)
                {
                    RecordPoint(it, inside, "FAIL");
                    throw new TimeoutException(key + " 真人步行不可達；最近距離=" + closest.ToString("F2") + " 半徑=" + it.radius);
                }
                if (climbing)
                {
                    if (flow.climb.IsLocked)
                    {
                        if (!climbShot && flow.Player.transform.position.y > 12f) { climbShot = true; ScreenCapture.CaptureScreenshot("Captures/r3/r3_climb.png", 3);
                            var c = Camera.main.transform; var pp = flow.Player.transform.position + Vector3.up * 1.2f; var ray = new Ray(c.position, pp - c.position);
                            var sb = new System.Text.StringBuilder("CLIMBCAM cam=" + c.position + " fwd=" + c.forward + " player=" + flow.Player.transform.position + " blockers:");
                            foreach (var rr in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (rr.enabled && rr.bounds.IntersectRay(ray, out var dd) && dd < Vector3.Distance(c.position, pp) && !rr.transform.IsChildOf(flow.Player.transform)) sb.Append(" " + rr.name + "<" + (rr.transform.parent ? rr.transform.parent.name : "") + ">@" + dd.ToString("0.0") + "[" + rr.shadowCastingMode + " size=" + rr.bounds.size.magnitude.ToString("0") + " c=" + rr.bounds.center + "]");
                            Debug.Log(sb.ToString()); }
                        flow.climb.SetMoveInput(Vector2.up); return;
                    }
                    Require(flow.State.climbed && flow.Player.transform.position.y > 17.8f, "未真正攀至崖頂。");
                    Require(it.enabled, "攀爬完成後攀援點被關閉。");
                    CompleteStep(); return;
                }
                var pickup = it.GetComponent<DriftPickup>();
                var climb = it.GetComponent<ClimbZone>();
                if (pickup && distance <= 3.9f && !chasing)
                { driftStart = it.transform.position; next = EditorApplication.timeSinceStartup + .5; chasing = true; return; }
                if (pickup && chasing)
                {
                    Require(Vector3.Distance(driftStart, it.transform.position) < .005f, key + " 玩家四公尺內仍漂流。");
                    if (EditorApplication.timeSinceStartup < next) return;
                }
                if (inside && (!pickup || pickup.CanInteract) && (!climb || climb.CanInteract) && Interactable.Nearest(flow.Player.transform.position) == it)
                {
                    RecordPoint(it, true, "PASS");
                    if (key == "Act4_House") ScreenCapture.CaptureScreenshot("Captures/r3/r3_act4_door.png", 3);
                    it.Interact();
                    if (climb) { Require(climb.IsLocked, "攀援未啟動。"); climbing = true; flow.climb.SetMoveInput(Vector2.up); return; }
                    if (pickup) Require(!it.gameObject.activeSelf, key + " 未收集。");
                    CompleteStep(); return;
                }
                Walk(flow, it.transform.position);
            }
            else if (phase == "capture" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestorePrefs();
                string capture = "Captures/r3";
                foreach (string name in new[] { "r3_act1", "r3_act2", "r3_act3", "r3_act4", "r3_act4_door", "r3_act5", "r3_story", "r3_tutorial", "r3_objective", "r3_dialogue_women", "r3_dialogue_grandson", "r3_term", "r3_epilogue_guide", "r3_mobile", "r3_arrow", "r3_climb" })
                {
                    var info = new FileInfo(capture + "/" + name + ".png");
                    Require(info.Exists && info.Length > 1024 && info.LastWriteTimeUtc.Ticks >= long.Parse(SessionState.GetString("Liuruan.R3.CaptureAfter", "0")), "缺少本輪有效截圖：" + name);
                }
                byte[] mobile = File.ReadAllBytes(capture + "/r3_mobile.png");
                // CC：GameView 切換解析度在此環境不生效，手機版面改由 CC 在瀏覽器窄視窗實測。
                File.WriteAllText("logs/U-R3-captures.md", "# R3 實際步行截圖\n\n" + string.Join("\n", Directory.GetFiles(capture, "r3_*.png")));
                SessionState.SetString(Phase, "build");
            }
            else if (phase == "build" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(Phase, "building");
                EditorUserBuildSettings.development = false;
                // 沿用已存在且經確認的選單，不另外啟動 Unity。
                if (!EditorApplication.ExecuteMenuItem("Tools/Liuruan/Build/WebGL + Scene Bundle")) throw new InvalidOperationException("找不到建置選單。");
                Require(File.Exists("Build/WebGL/.bundle-done") && File.GetLastWriteTimeUtc("Build/WebGL/.bundle-done").Ticks >= long.Parse(SessionState.GetString("Liuruan.R3.CaptureAfter", "0")), "建置沒有本輪成功旗標");
                Require(PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Brotli, "專案壓縮不是 Brotli");
                Require(Directory.GetFiles("Build/WebGL/Build", "*.unityweb").Length > 0, "deploy.sh 需要 *.unityweb，請檢查現有 decompression fallback 設定");
                var dataFiles = Directory.GetFiles("Build/WebGL/Build", "*.data*", SearchOption.TopDirectoryOnly);
                Require(dataFiles.Length > 0, "找不到 WebGL data 檔");
                var text = new StringBuilder("# R3b WebGL Release\n\ndevelopment=false\nCompression=" + PlayerSettings.WebGL.compressionFormat + "\nDecompressionFallback=" + PlayerSettings.WebGL.decompressionFallback + "\n");
                var wasmFiles = Directory.GetFiles("Build/WebGL/Build", "*.wasm*");
                Require(wasmFiles.Length > 0, "找不到 wasm 檔");
                foreach (string file in wasmFiles) text.AppendLine(file + "：" + new FileInfo(file).Length + " bytes");
                foreach (string file in dataFiles)
                {
                    long bytes = new FileInfo(file).Length;
                    text.AppendLine(file + "：" + bytes + " bytes");
                    File.WriteAllText("logs/U-R3-build.md", text.ToString());
                    Require(bytes > 0 && bytes < 26214400, "data 超過限制或為空");
                }
                Require(SessionState.GetInt("Liuruan.R3.Errors", 0) == 0, "R3 流程出現 console error");
                text.AppendLine("PASS：.bundle-done=" + File.ReadAllText("Build/WebGL/.bundle-done"));
                File.WriteAllText("logs/U-R3-build.md", text.ToString());
                SessionState.SetString(Phase, "done");
            }
        }
        catch (Exception e) { Fail(e.ToString()); }
    }
    static void ClearTestPrefs()
    {
        foreach (var key in StringKeys) PlayerPrefs.DeleteKey(key);
        foreach (var key in IntKeys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
    static Button FindButton(string name)
    {
        foreach (var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (b.name == name && b.gameObject.activeInHierarchy) return b;
        return null;
    }
    static float Horizontal(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
    static void CompleteStep()
    { step++; SessionState.SetInt("Liuruan.R3.Step", step); activePoint = null; }
    static void RecordPoint(Interactable it, bool inside, string result)
    {
        File.AppendAllText("logs/U-R3-play.md", "|" + it.name + "|" + closest.ToString("F2") + "|" + it.radius.ToString("F2") + "|" + inside + "|" + (EditorApplication.timeSinceStartup - actionAt).ToString("F2") + "|" + result + "|\n");
    }
    static void Walk(GameFlow flow, Vector3 target)
    {
        var player = flow.Player;
        Vector3 destination = target;
        // 崖頂沿平台直走；地面採既有 NavMesh 路點，但每步仍由 CharacterController 碰撞決定。
        if (player.transform.position.y < 10)
        {
            if (walkPath == null || (walkPath.corners.Length > 0 && Horizontal(walkPath.corners[walkPath.corners.Length - 1], target) > 2))
            {
                walkPath = new NavMeshPath(); corner = 0;
                if (NavMesh.SamplePosition(player.transform.position, out var from, 2, NavMesh.AllAreas) && NavMesh.SamplePosition(target, out var to, 1, NavMesh.AllAreas))
                    NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, walkPath);
            }
            if (walkPath.status == NavMeshPathStatus.PathComplete && walkPath.corners.Length > 0)
            {
                while (corner < walkPath.corners.Length - 1 && Horizontal(player.transform.position, walkPath.corners[corner]) < .4f) corner++;
                destination = walkPath.corners[corner];
            }
        }
        Vector3 offset = destination - player.transform.position; offset.y = 0;
        float budget = player.moveSpeed * flow.Hunger.SpeedMultiplier * Time.deltaTime;
        Vector3 delta = Vector3.ClampMagnitude(offset, budget);
        Require(delta.magnitude <= player.moveSpeed * Time.deltaTime + .0001f, "步行步長超出跑速。");
        PlayerController.MoveDisplacement(player.GetComponent<CharacterController>(), delta);
        player.FaceDirection(offset);
    }
    static void FinishJourney(GameFlow flow)
    {
        if (flow.Reading) { AdvanceCard(flow); return; }
        if (flow.journey.Busy) { flow.journey.Next(); return; }
        if (endingStage == 0)
        {
            if (!capturedGuide)
            {
                ScreenCapture.CaptureScreenshot("Captures/r3/r3_epilogue_guide.png", 3); capturedGuide = true; next = EditorApplication.timeSinceStartup + .3; return;
            }
            if (EditorApplication.timeSinceStartup < next) return;
            Require(endedDialogues.Count == 19, "19 段對話未全部 Next 到結束：" + string.Join(",",endedDialogues));
            Require(flow.State.ended && flow.State.bark == 31 && flow.State.drift == 7 && flow.State.halfYearComplete, "五幕進度未完成。");
            VerifyEnding(flow);
            reloadedFrom = flow; endingStage = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); return;
        }
        if (endingStage == 1)
        {
            if (flow == reloadedFrom) return;
            Require(flow.State.ended && FindButton("Restart"), "結束後重載沒有結局頁與重新開始。");
            PlayerPrefs.SetInt("liuruan_r3_probe", 1);
            reloadedFrom = flow; endingStage = 2;
            FindButton("Restart").onClick.Invoke(); return;
        }
        if (flow == reloadedFrom) return;
        Require(flow.Act == 1 && !flow.State.ended && flow.State.bark == 0 && flow.State.drift == 0, "重新開始未回第一幕。");
        Require(!PlayerPrefs.HasKey(SaveKey) && !PlayerPrefs.HasKey("liuruan_r3_probe") && !QuizStats.Solved(QuizStats.InferId), "重新開始有殘留存檔。");
        Require(SessionState.GetInt("Liuruan.R3.Errors", 0) == 0, "Play console error 不為零。");
        File.AppendAllText("logs/U-R3-play.md", "\nPASS：每點碰撞步行可達、y=-20 掉落救回、漂流物靠近暫停、結局重載及按鈕重開回第一幕；console error=0。\n");
        SessionState.SetString(Phase, "capture"); EditorApplication.isPlaying = false;
    }
    static void VerifyEnding(GameFlow flow)
    {
        var model = flow.epilogue.Model;
        var panel = GameObject.Find("JourneySystems/JourneyHUD/JourneyEnding").transform;
        var text = panel.Find("Label").GetComponent<TMP_Text>();
        var nextPage = panel.Find("EndingNext").GetComponent<Button>();
        foreach (string guide in flow.journey.GuidePages)
        {
            Require(text.text.Contains(guide), "結局導讀缺頁。");
            nextPage.onClick.Invoke();
        }
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
        Require(text.text.Contains("成績卡") && text.text.Contains("10／10"), "成績卡未顯示全十題首答結果");
        File.AppendAllText("logs/U-R3-play.md", "- PASS：全文各段、三選一作答／解析、比較表各列與成績卡，均經實際面板按鈕巡覽。\n");
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
    static int PngInt(byte[] b, int offset) => (b[offset] << 24) | (b[offset+1] << 16) | (b[offset+2] << 8) | b[offset+3];
    static EditorWindow GameView() => EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView", true));
    static int MobileSize()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var sizes = assembly.GetType("UnityEditor.GameViewSizes", true);
        object instance = sizes.GetProperty("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType", true);
        object group = sizes.GetMethod("GetGroup").Invoke(instance, new[] { Enum.Parse(groupType,"Standalone") });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
        var type = assembly.GetType("UnityEditor.GameViewSizeType", true);
        object size = Activator.CreateInstance(sizeType, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null,
            new[] { Enum.Parse(type,"FixedResolution"), (object)1080, 1920, "R3b 手機驗收" }, null);
        int index = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null) + (int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[] { size });
        var view = GameView(); var prop = view.GetType().GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        int old = (int)prop.GetValue(view); prop.SetValue(view,index); view.Repaint(); return old;
    }
    static void RestoreSize()
    {
        TouchInput.ForceTouch = false;
        if (mobileStage == 0 || mobileStage == 3) return;
        var view = GameView(); view.GetType().GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(view,oldSize);
        mobileStage = 3;
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void Fail(string error)
    {
        SessionState.SetString(Phase, "failed");
        File.AppendAllText("logs/U-R3-failure.md", "FAIL：" + error + "\n");
        if (File.Exists("Build/WebGL/.bundle-done") && File.GetLastWriteTimeUtc("Build/WebGL/.bundle-done").Ticks >= long.Parse(SessionState.GetString("Liuruan.R3.CaptureAfter", "0")))
            File.Delete("Build/WebGL/.bundle-done");
        RestoreSize(); RestorePrefs();
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        Debug.LogError("[R3Validation] " + error);
    }
}
