using System;

namespace Liuruan
{
    public sealed class HungerCore
    {
        public const float Capacity = 5f;
        public float Value { get; private set; }
        public float IntervalSeconds { get; }
        public bool IsPaused { get; private set; }
        public float SpeedMultiplier => Value <= 0f ? 0.6f : 1f;
        public event Action<float> OnChanged;
        public event Action OnStarving;
        private double elapsed;

        public HungerCore(float initialValue = 3f, float intervalSeconds = 24f)
        {
            RequireFinite(initialValue, nameof(initialValue));
            RequireFinite(intervalSeconds, nameof(intervalSeconds));
            if (intervalSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            Value = Math.Max(0f, Math.Min(Capacity, initialValue));
            IntervalSeconds = intervalSeconds;
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public void Tick(float deltaSeconds)
        {
            RequireFinite(deltaSeconds, nameof(deltaSeconds));
            if (deltaSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (IsPaused || Value <= 0f) return;
            elapsed += deltaSeconds;
            double steps = Math.Floor(elapsed / IntervalSeconds);
            if (steps < 1) return;
            elapsed %= IntervalSeconds;
            SetValue((float)Math.Max(0d, Value - steps * 0.5d));
        }

        public void Eat(float amount)
        {
            RequireFinite(amount, nameof(amount));
            if (amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            SetValue(Math.Min(Capacity, Value + amount));
        }

        public void FillAll() => SetValue(Capacity);

        private void SetValue(float next)
        {
            if (next == Value) return;
            bool starving = Value > 0f && next <= 0f;
            bool recovering = Value <= 0f && next > 0f;
            Value = next;
            if (starving || recovering) elapsed = 0;
            OnChanged?.Invoke(Value);
            if (starving) OnStarving?.Invoke();
        }

        internal static void RequireFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
