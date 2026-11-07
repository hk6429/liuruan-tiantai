using System;

namespace Liuruan
{
    public enum Season { Spring, Summer, Autumn, Winter }

    public readonly struct SeasonBlend
    {
        public Season Current { get; }
        public Season Next { get; }
        public float Weight { get; }
        public SeasonBlend(Season current, Season next, float weight)
        { Current = current; Next = next; Weight = weight; }
    }

    public sealed class SeasonCore
    {
        public const float DurationSeconds = 6f;
        public float Progress { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsHome { get; private set; }
        public SeasonBlend Blend => Sample(Progress);
        public event Action OnBirdsSing;
        private bool started;
        private double elapsed;

        public static SeasonBlend Sample(float progress)
        {
            HungerCore.RequireFinite(progress, nameof(progress));
            float p = Math.Max(0, Math.Min(1, progress));
            if (p == 1) return new SeasonBlend(Season.Spring, Season.Summer, 0);
            float scaled = p * 4;
            int index = (int)scaled;
            return new SeasonBlend((Season)index, (Season)((index + 1) % 4), scaled - index);
        }
        public bool PlayHalfYear()
        {
            if (started || IsHome) return false;
            started = true; IsPlaying = true; return true;
        }
        public void Tick(float deltaSeconds)
        {
            HungerCore.RequireFinite(deltaSeconds, nameof(deltaSeconds));
            if (deltaSeconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (!IsPlaying) return;
            elapsed = Math.Min(DurationSeconds, elapsed + deltaSeconds);
            Progress = (float)(elapsed / DurationSeconds);
            if (elapsed < DurationSeconds) return;
            IsPlaying = false;
            OnBirdsSing?.Invoke();
        }
        public bool SwapToHome()
        {
            if (IsHome) return false;
            IsHome = true; IsPlaying = false; return true;
        }
    }
}
