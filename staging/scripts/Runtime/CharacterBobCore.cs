using System;

namespace Liuruan
{
    public readonly struct BobPose
    {
        public float Height { get; }
        public float RollDegrees { get; }
        public float LeanDegrees { get; }
        public float Scale { get; }
        public BobPose(float height, float roll, float lean, float scale)
        { Height = height; RollDegrees = roll; LeanDegrees = lean; Scale = scale; }
    }

    public sealed class CharacterBobCore
    {
        private readonly float maxSpeed, baseFrequency, frequencyPerSpeed, bounce, sway, lean, breath, breathFrequency;
        private double stepPhase, breathPhase;
        public CharacterBobCore(float maxSpeed = 8, float baseFrequency = 1, float frequencyPerSpeed = .4f,
            float bounce = .06f, float sway = 3, float lean = 8, float breath = .015f, float breathFrequency = .3f)
        {
            foreach (float value in new[] { maxSpeed, baseFrequency, frequencyPerSpeed, bounce, sway, lean, breath, breathFrequency })
            {
                HungerCore.RequireFinite(value, nameof(value));
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            }
            if (maxSpeed == 0) throw new ArgumentOutOfRangeException(nameof(maxSpeed));
            if (breath > .015f) throw new ArgumentOutOfRangeException(nameof(breath));
            this.maxSpeed = maxSpeed; this.baseFrequency = baseFrequency; this.frequencyPerSpeed = frequencyPerSpeed;
            this.bounce = bounce; this.sway = sway; this.lean = lean; this.breath = breath; this.breathFrequency = breathFrequency;
        }
        public float Frequency(float speed)
        {
            speed = ClampSpeed(speed);
            return speed == 0 ? 0 : (float)Math.Min(float.MaxValue, baseFrequency + (double)frequencyPerSpeed * speed);
        }
        public BobPose Tick(float deltaSeconds, float speed)
        {
            HungerCore.RequireFinite(deltaSeconds, nameof(deltaSeconds));
            if (deltaSeconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            speed = ClampSpeed(speed);
            const double tau = Math.PI * 2;
            breathPhase = (breathPhase + deltaSeconds * (double)breathFrequency * tau) % tau;
            if (speed == 0)
            {
                stepPhase = 0;
                return new BobPose(0, 0, 0, 1 + breath * (float)Math.Sin(breathPhase));
            }
            stepPhase = (stepPhase + deltaSeconds * (double)Frequency(speed) * tau) % tau;
            float strength = Math.Min(1, speed / maxSpeed);
            return new BobPose(bounce * strength * (float)Math.Abs(Math.Sin(stepPhase)),
                sway * strength * (float)Math.Sin(stepPhase), lean * strength, 1);
        }
        private float ClampSpeed(float speed)
        {
            HungerCore.RequireFinite(speed, nameof(speed));
            if (speed < 0) throw new ArgumentOutOfRangeException(nameof(speed));
            return Math.Min(maxSpeed, speed);
        }
    }
}
