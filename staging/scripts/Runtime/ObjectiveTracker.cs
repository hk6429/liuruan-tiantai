using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField] TextAsset objectivesJson;
        public ObjectiveCore Core { get; private set; }
        public event Action<string> OnError;
        public string Text => Core?.Text ?? "";
        public event Action<string> OnChanged;
        public bool SetStep(int act, string id, int count = 0) => Core != null && Core.SetStep(act, id, count);
        public void SetCount(int count) { Core?.SetCount(count); }
        public bool NextStep() => Core != null && Core.NextStep();

        void Awake() { Load(); }
        public bool Load()
        {
            try { Core = new ObjectiveCore(R3Json.Read<ObjectiveDocument>(objectivesJson)); }
            catch (ArgumentException e)
            {
                Core = null;
                Debug.LogError("ObjectiveTracker 資料無法讀取：" + e.Message, this);
                OnError?.Invoke(e.Message); return false;
            }
            Core.OnChanged += () => OnChanged?.Invoke(Core.Text);
            return true;
        }
    }
}
