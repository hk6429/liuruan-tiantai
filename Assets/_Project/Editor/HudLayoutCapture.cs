using System;
using System.IO;
using Liuruan;
using UnityEditor;
using UnityEngine;

/// R3c：HUD 版面截圖（桌機橫式 1920×1080、觸控橫式 1920×1080、觸控直式 1080×1920）。
/// 選單 Tools/Liuruan/R3/HUD Layout Capture；在目前場景進 Play，等 HUD 不忙時連拍三張到 Captures/r3/r3c_*.png，拍完離開 Play。
[InitializeOnLoad]
public static class HudLayoutCapture
{
    const string Phase = "Liuruan.R3c.Phase";
    static int stage, waitFrame, oldSize = -1, idle;
    static HudLayoutCapture() { EditorApplication.update += Tick; }

    [MenuItem("Tools/Liuruan/R3/HUD Layout Capture")]
    public static void Begin()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("請先離開 Play 模式。");
        Directory.CreateDirectory("Captures/r3");
        foreach (var n in new[] { "desk", "touch_land", "touch_port" }) { var f = "Captures/r3/r3c_" + n + ".png"; if (File.Exists(f)) File.Delete(f); }
        stage = 0; waitFrame = 0; idle = 0; oldSize = -1;
        SessionState.SetFloat("Liuruan.R3c.StartedAt", (float)EditorApplication.timeSinceStartup);
        // 故事卡／教學會蓋住 HUD：暫時標成已看過，拍完還原
        foreach (var key in new[] { R3Journey.StoryKey, R3Journey.TutorialKey })
        { SessionState.SetInt("R3c.Old." + key, PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : -1); PlayerPrefs.SetInt(key, 1); }
        PlayerPrefs.Save();
        SessionState.SetString(Phase, "play");
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (SessionState.GetString(Phase, "") != "play" || EditorApplication.isCompiling) return;
        if (!EditorApplication.isPlaying) return;
        var flow = UnityEngine.Object.FindAnyObjectByType<GameFlow>();
        if (!flow || !ScrollUI.I) return;
        bool busy = flow.Reading || flow.journey.Busy || ScrollUI.I.IsBusy;
        double elapsed = EditorApplication.timeSinceStartup - SessionState.GetFloat("Liuruan.R3c.StartedAt", 0);
        if (stage == 0)
        {
            // 開場旁白對話：每 20 幀按一次「下一句」推進，直到 HUD 不忙
            var dialogue = UnityEngine.Object.FindAnyObjectByType<DialogueDirector>();
            if (busy && Time.frameCount % 20 == 0) { flow.journey.Next(); if (dialogue && dialogue.IsActive) dialogue.Next(); }
            idle = busy ? 0 : idle + 1;
            if ((idle < 30 || elapsed < 4) && elapsed < 40) return;   // 等 HUD 穩定；40 秒仍忙就照拍並記錄
            File.WriteAllText("Captures/r3/r3c_note.txt", "busy=" + busy + " elapsed=" + elapsed.ToString("F1") + "\n");
            oldSize = SetSize(1920, 1080, "R3c 橫式"); TouchInput.ForceTouch = false; waitFrame = Time.frameCount; stage = 1; return;
        }
        if (Time.frameCount - waitFrame < 8) return;
        // EditorApplication.update 在本幀 Update 之前執行，CaptureScreenshot 幀尾才拍：
        // 改狀態（ForceTouch／尺寸）一律放到拍完的下一階段，否則會拍到改完的版面
        if (stage == 1) { ScreenCapture.CaptureScreenshot("Captures/r3/r3c_desk.png", 1); waitFrame = Time.frameCount; stage = 2; return; }
        if (stage == 2) { TouchInput.ForceTouch = true; waitFrame = Time.frameCount; stage = 3; return; }
        if (stage == 3) { ScreenCapture.CaptureScreenshot("Captures/r3/r3c_touch_land.png", 1); waitFrame = Time.frameCount; stage = 4; return; }
        if (stage == 4) { SetSize(1080, 1920, "R3c 直式"); waitFrame = Time.frameCount; stage = 5; return; }
        if (stage == 5) { ScreenCapture.CaptureScreenshot("Captures/r3/r3c_touch_port.png", 1); waitFrame = Time.frameCount; stage = 6; return; }
        if (stage == 6)
        {
            // 截圖非同步落盤，離開 Play 前等檔案出現（最多 300 幀）
            if (!File.Exists("Captures/r3/r3c_touch_port.png") && Time.frameCount - waitFrame < 300) return;
            TouchInput.ForceTouch = false;
            if (oldSize >= 0) SelectSize(oldSize);
            foreach (var key in new[] { R3Journey.StoryKey, R3Journey.TutorialKey })
            { int old = SessionState.GetInt("R3c.Old." + key, -1); if (old < 0) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetInt(key, old); }
            PlayerPrefs.Save();
            SessionState.SetString(Phase, "done");
            EditorApplication.isPlaying = false;
        }
    }

    static EditorWindow GameView() => EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView", true));
    static int SetSize(int w, int h, string name)
    {
        var assembly = typeof(EditorWindow).Assembly;
        var sizes = assembly.GetType("UnityEditor.GameViewSizes", true);
        object instance = sizes.GetProperty("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType", true);
        object group = sizes.GetMethod("GetGroup").Invoke(instance, new[] { Enum.Parse(groupType, "Standalone") });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
        var type = assembly.GetType("UnityEditor.GameViewSizeType", true);
        object size = Activator.CreateInstance(sizeType, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null,
            new[] { Enum.Parse(type, "FixedResolution"), (object)w, h, name }, null);
        int index = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null) + (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int old = SelectSize(index);
        return old;
    }
    static int SelectSize(int index)
    {
        var view = GameView();
        var prop = view.GetType().GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        int old = (int)prop.GetValue(view); prop.SetValue(view, index); view.Repaint(); return old;
    }
}
