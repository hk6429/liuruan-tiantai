using System;
using UnityEngine;
using UnityEngine.Events;

namespace Liuruan
{
    public sealed class HungerSystem : MonoBehaviour
    {
        [SerializeField, Range(0f, 5f)] private float initialValue = 3f;
        [SerializeField, Min(0.01f)] private float intervalSeconds = 24f;
        [SerializeField] private UnityEvent<float> onChanged = new UnityEvent<float>();
        [SerializeField] private UnityEvent onStarving = new UnityEvent();
        [SerializeField] private UnityEvent<string> onToast = new UnityEvent<string>();
        public event Action<float> OnChanged;
        public event Action OnStarving;
        public event Action<string> OnToast;
        private HungerCore core;
        private HungerCore Core
        {
            get
            {
                if (core == null)
                {
                    core = new HungerCore(initialValue, intervalSeconds);
                    core.OnChanged += value => { onChanged.Invoke(value); OnChanged?.Invoke(value); };
                    core.OnStarving += HandleStarving;
                }
                return core;
            }
        }
        public float Value => Core.Value;
        public float SpeedMultiplier => Core.SpeedMultiplier;
        private void Awake() { _ = Core; }
        private void Update() => Core.Tick(Time.deltaTime);
        public void Pause() => Core.Pause();
        public void Resume() => Core.Resume();
        public void Eat(float amount) => Core.Eat(amount);
        public void FillAll() => Core.FillAll();
        public string StarvingMessage = "肚子餓了，去找些食物吧。";
        private void HandleStarving()
        {
            onStarving.Invoke();
            OnStarving?.Invoke();
            string message = StarvingMessage;
            onToast.Invoke(message);
            OnToast?.Invoke(message);
        }
    }
}
