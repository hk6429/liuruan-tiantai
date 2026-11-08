using System;
using System.Collections.Generic;

namespace Liuruan
{
    [Serializable] public sealed class ObjectiveDocument { public ObjectiveAct[] acts = null; }
    [Serializable] public sealed class ObjectiveAct { public int act = 0; public ObjectiveStep[] steps = null; }
    [Serializable] public sealed class ObjectiveStep
    { public string id = null, goal = null, why = null, where = null; public int count = 0; }
    public sealed class ObjectiveCore
    {
        readonly Dictionary<int, ObjectiveStep[]> acts = new Dictionary<int, ObjectiveStep[]>();
        int act, index;
        public int Count { get; private set; }
        public ObjectiveStep CurrentStep => acts.TryGetValue(act, out var steps) ? steps[index] : null;
        public string GoalLine => CurrentStep == null ? "" : CurrentStep.why + "｜目標：" + CurrentStep.goal + " " + Count + "/" + CurrentStep.count;
        public string DirectionLine => CurrentStep == null ? "" : "方向：" + CurrentStep.where;
        public string Text => GoalLine + "\n" + DirectionLine;
        public event Action OnChanged;
        public ObjectiveCore(ObjectiveDocument data)
        {
            foreach (var entry in R3Data.Required(data?.acts, "objectives.acts"))
            {
                R3Data.Act(entry.act);
                if (acts.ContainsKey(entry.act)) throw new ArgumentException("任務幕次重複。");
                var ids = new HashSet<string>();
                foreach (var step in R3Data.Required(entry.steps, "objectives.steps"))
                {
                    R3Data.Text(step.id, "objective.id"); R3Data.Unique(ids, step.id, "objective.id");
                    R3Data.Text(step.goal, "objective.goal"); R3Data.Text(step.why, "objective.why"); R3Data.Text(step.where, "objective.where");
                    if (step.count < 1) throw new ArgumentException("objective.count 必須大於零。");
                }
                acts.Add(entry.act, entry.steps);
            }
        }
        public bool SetStep(int act, string id, int count = 0)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (!acts.TryGetValue(act, out var steps)) return false;
            int next = Array.FindIndex(steps, s => s.id == id);
            if (next < 0) return false;
            this.act = act; index = next; Count = Math.Min(count, steps[index].count); OnChanged?.Invoke(); return true;
        }
        public void SetCount(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (CurrentStep == null) return;
            int next = Math.Min(count, CurrentStep.count);
            if (next == Count) return;
            Count = next; OnChanged?.Invoke();
        }
        public bool NextStep()
        {
            if (CurrentStep == null || index + 1 >= acts[act].Length) return false;
            index++; Count = 0; OnChanged?.Invoke(); return true;
        }
    }
}
