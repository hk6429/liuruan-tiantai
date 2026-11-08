using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class StallHintSystem : MonoBehaviour
    {
        [SerializeField] TextAsset hintsJson;
        public StallHintCore Core { get; private set; }
        public event Action<string> OnError;
        bool paused;
        public event Action<StallHint> OnHint;
        void Update() { if (Core != null) Core.Tick(Time.deltaTime, paused); }
        public void SetPaused(bool readingOrDialogue) { paused = readingOrDialogue; }
        public void SetDevice(bool touch) { if (Core != null) Core.Device = touch ? HintDevice.Touch : HintDevice.Desktop; }
        public bool SetStep(int act, string id) => Core != null && Core.SetStep(act, id);
        public void Progress() { Core?.Progress(); }

        void Awake() { Load(); }
        public bool Load()
        {
            try { Core = new StallHintCore(R3Json.Read<HintDocument>(hintsJson)); }
            catch (ArgumentException e)
            {
                Core = null;
                Debug.LogError("StallHintSystem 資料無法讀取：" + e.Message, this);
                OnError?.Invoke(e.Message); return false;
            }
            Core.OnHint += hint => OnHint?.Invoke(hint);
            return true;
        }
    }
}
