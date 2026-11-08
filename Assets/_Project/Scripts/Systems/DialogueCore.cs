using System;
using System.Collections.Generic;

namespace Liuruan
{
    [Serializable] public sealed class DialogueDocument { public DialogueAct[] acts = null; }
    [Serializable] public sealed class DialogueAct { public int act = 0; public DialogueScene[] scenes = null; }
    [Serializable] public sealed class DialogueScene
    { public string id = null, trigger = null; public DialogueLine[] lines = null; }
    [Serializable] public sealed class DialogueLine { public string speaker = null, text = null, type = null; public string source = null, label = null, orig = null; }
    public sealed class DialogueCore
    {
        readonly Dictionary<string, DialogueScene> scenes = new Dictionary<string, DialogueScene>();
        readonly List<DialogueScene> ordered = new List<DialogueScene>();
        readonly HashSet<string> played = new HashSet<string>();
        DialogueScene current;
        int line;
        public bool IsActive => current != null;
        public DialogueLine CurrentLine => current == null ? null : current.lines[line];
        public string SceneId => current?.id;
        public event Action<string> OnSceneEnd;
        public event Action<DialogueLine> OnLineChanged;
        public DialogueCore(DialogueDocument data)
        {
            var acts = new HashSet<int>();
            foreach (var act in R3Data.Required(data?.acts, "dialogue.acts"))
            {
                R3Data.Act(act.act); R3Data.Unique(acts, act.act, "dialogue.act");
                foreach (var scene in R3Data.Required(act.scenes, "dialogue.scenes"))
                {
                    R3Data.Text(scene.id, "scene.id"); R3Data.Text(scene.trigger, "scene.trigger");
                    foreach (var entry in R3Data.Required(scene.lines, "scene.lines"))
                    {
                        R3Data.Text(entry.speaker, "line.speaker"); R3Data.Text(entry.text, "line.text");
                        if (entry.type != "original" && entry.type != "plain") throw new ArgumentException("line.type 必須為 original 或 plain。");
                        if (entry.type == "plain" && new System.Globalization.StringInfo(entry.text).LengthInTextElements > 20)
                            throw new ArgumentException("白話台詞不可超過 20 字。");
                    }
                    if (scenes.ContainsKey(scene.id)) throw new ArgumentException("scene.id 重複。");
                    scenes.Add(scene.id, scene); ordered.Add(scene);
                }
            }
        }
        public bool Start(string sceneId, bool replay = false)
        {
            if (IsActive || sceneId == null || !scenes.TryGetValue(sceneId, out var scene) || (!replay && played.Contains(sceneId))) return false;
            current = scene; line = 0; played.Add(sceneId);
            OnLineChanged?.Invoke(CurrentLine); return true;
        }
        public bool TryTrigger(string trigger)
        {
            foreach (var scene in ordered) if (scene.trigger == trigger && Start(scene.id)) return true;
            return false;
        }
        public bool Next()
        {
            if (!IsActive) return false;
            if (line + 1 < current.lines.Length)
            { line++; OnLineChanged?.Invoke(CurrentLine); return true; }
            string id = current.id; current = null; line = 0;
            OnSceneEnd?.Invoke(id); return true;
        }
    }
}
