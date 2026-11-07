using System;

namespace Liuruan
{
    public enum ClimbState { Grounded, Attaching, Climbing, TopOut, Release }

    public sealed class ClimbCore
    {
        public const float Speed = 1.2f;
        public float Width { get; }
        public float Height { get; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public ClimbState State { get; private set; } = ClimbState.Grounded;
        public bool IsLocked => State == ClimbState.Attaching || State == ClimbState.Climbing || State == ClimbState.TopOut;
        public event Action<ClimbState> OnStateChanged;

        public ClimbCore(float width, float height)
        {
            HungerCore.RequireFinite(width, nameof(width)); HungerCore.RequireFinite(height, nameof(height));
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width; Height = height;
        }
        public bool Attach(float x, float y)
        {
            HungerCore.RequireFinite(x, nameof(x)); HungerCore.RequireFinite(y, nameof(y));
            if (State != ClimbState.Grounded) return false;
            X = Math.Max(-Width / 2, Math.Min(Width / 2, x));
            Y = Math.Max(0, Math.Min(Height, y));
            SetState(ClimbState.Attaching); return true;
        }
        public void Tick(float dt, float horizontal, float vertical, float hunger)
        {
            HungerCore.RequireFinite(dt, nameof(dt)); HungerCore.RequireFinite(horizontal, nameof(horizontal));
            HungerCore.RequireFinite(vertical, nameof(vertical)); HungerCore.RequireFinite(hunger, nameof(hunger));
            if (dt < 0 || hunger < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            if (dt == 0) return;
            if (State == ClimbState.Attaching) { SetState(ClimbState.Climbing); return; }
            if (State != ClimbState.Climbing) return;
            double length = Math.Max(1, Math.Sqrt((double)horizontal * horizontal + (double)vertical * vertical));
            double distance = (double)dt * Speed * (hunger < 1 ? 0.5 : 1);
            X = (float)Math.Max(-Width / 2, Math.Min(Width / 2, X + horizontal / length * distance));
            Y = (float)Math.Max(0, Math.Min(Height, Y + vertical / length * distance));
            if (vertical > 0 && Y >= Height) SetState(ClimbState.TopOut);
            else if (vertical < 0 && Y <= 0) SetState(ClimbState.Release);
        }
        public void Release()
        {
            if (IsLocked) SetState(ClimbState.Release);
        }
        public void Finish()
        {
            if (State == ClimbState.TopOut || State == ClimbState.Release) SetState(ClimbState.Grounded);
        }
        private void SetState(ClimbState next)
        {
            if (State == next) return;
            State = next; OnStateChanged?.Invoke(next);
        }
    }
}
