using System;
using System.Collections.Generic;

namespace Liuruan
{
    [Serializable] public sealed class HintDocument { public HintAct[] acts = null; }
    [Serializable] public sealed class HintAct { public int act = 0; public HintStep[] steps = null; }
    [Serializable] public sealed class HintStep
    { public string id = null; public HintText l1 = null, l2 = null, l3 = null; }
    [Serializable] public sealed class HintText { public string desktop = null, touch = null; }
    public enum HintDevice { Desktop, Touch }
    public readonly struct StallHint
    {
        public int Level { get; }
        public string Text { get; }
        public bool BrightenTarget => Level >= 2;
        public bool GuideCompanion => Level >= 3;
        public bool TurnCamera => Level >= 3;
        public StallHint(int level, string text) { Level = level; Text = text; }
    }
    public sealed class StallHintCore
    {
        readonly Dictionary<int, Dictionary<string, HintStep>> acts = new Dictionary<int, Dictionary<string, HintStep>>();
        readonly float[] thresholds;
        HintStep current;
        int generation;
        public double ElapsedSeconds { get; private set; }
        public int Level { get; private set; }
        public HintDevice Device { get; set; }
        public event Action<StallHint> OnHint;
        public StallHintCore(HintDocument data, float level1 = 30, float level2 = 60, float level3 = 90)
        {
            thresholds = new[] { level1, level2, level3 };
            float previous = 0;
            foreach (float t in thresholds)
            {
                HungerCore.RequireFinite(t, nameof(thresholds));
                if (t <= previous) throw new ArgumentOutOfRangeException(nameof(thresholds));
                previous = t;
            }
            foreach (var act in R3Data.Required(data?.acts, "hints.acts"))
            {
                R3Data.Act(act.act);
                if (acts.ContainsKey(act.act)) throw new ArgumentException("提示幕次重複。");
                var steps = new Dictionary<string, HintStep>();
                foreach (var step in R3Data.Required(act.steps, "hints.steps"))
                {
                    R3Data.Text(step.id, "hint.id");
                    foreach (var text in new[] { step.l1, step.l2, step.l3 })
                    { R3Data.Text(text?.desktop, "hint.desktop"); R3Data.Text(text?.touch, "hint.touch"); }
                    if (steps.ContainsKey(step.id)) throw new ArgumentException("提示步驟重複。");
                    steps.Add(step.id, step);
                }
                acts.Add(act.act, steps);
            }
        }
        public bool SetStep(int act, string id)
        {
            if (id == null || !acts.TryGetValue(act, out var steps) || !steps.TryGetValue(id, out var step)) return false;
            if (current != step) { current = step; Progress(); }
            return true;
        }
        public void Progress() { ElapsedSeconds = 0; Level = 0; generation++; }
        public string TextFor(int level)
        {
            if (level < 1 || level > 3) throw new ArgumentOutOfRangeException(nameof(level));
            if (Device != HintDevice.Desktop && Device != HintDevice.Touch) throw new ArgumentOutOfRangeException(nameof(Device));
            if (current == null) return null;
            var text = level == 1 ? current.l1 : level == 2 ? current.l2 : current.l3;
            return Device == HintDevice.Touch ? text.touch : text.desktop;
        }
        public void Tick(float deltaTime, bool readingOrDialogue = false)
        {
            R3Data.Time(deltaTime);
            if (readingOrDialogue || current == null) return;
            ElapsedSeconds += deltaTime;
            int before = generation;
            while (Level < 3 && ElapsedSeconds >= thresholds[Level])
            {
                var hint = new StallHint(Level + 1, TextFor(Level + 1));
                Level++;
                OnHint?.Invoke(hint);
                if (generation != before) return;
            }
        }
    }
}
