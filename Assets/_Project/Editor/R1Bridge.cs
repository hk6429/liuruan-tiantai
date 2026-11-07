using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor 腳本備援入口；只接受本輪固定作業，不開啟另一個 Unity。
[InitializeOnLoad]
public static class R1Bridge
{
    const string Request = "Assets/_Project/Editor/R1Command.txt";
    static string last;
    static R1Bridge() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        string command = File.ReadAllText(Request).Trim();
        if (command.Length == 0) { last = null; return; }
        if (command == last) return; if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
        last = command;
        string round = command.StartsWith("r2") ? "R2" : "R1";
        try
        {
            File.WriteAllText("logs/U-" + round + "-bridge.md", DateTime.Now.ToString("o") + " " + command + " ready " + Application.dataPath);
            File.WriteAllText(Request, "");
            if (command.StartsWith("refresh")) AssetDatabase.Refresh();
            if (command.StartsWith("r2")) R2Validation.Begin();
            else if (command.StartsWith("all")) R1Validation.Begin();
        }
        catch (Exception e) { File.AppendAllText("logs/U-" + round + "-failure.md", e + "\n"); Debug.LogException(e); }
    }
}
