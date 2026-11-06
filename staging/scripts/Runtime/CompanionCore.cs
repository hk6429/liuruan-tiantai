using System;

namespace Liuruan
{
    public readonly struct WorldPosition
    {
        public readonly float X, Y, Z;
        public WorldPosition(float x, float y, float z)
        {
            HungerCore.RequireFinite(x, nameof(x));
            HungerCore.RequireFinite(y, nameof(y));
            HungerCore.RequireFinite(z, nameof(z));
            X = x; Y = y; Z = z;
        }
        public static double Distance(WorldPosition a, WorldPosition b)
        {
            double x = (double)a.X - b.X, y = (double)a.Y - b.Y, z = (double)a.Z - b.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }
    }

    public enum CompanionState { Follow, Idle, Catchup, Teleport }

    public sealed class CompanionCore
    {
        public const float CallCooldownSeconds = 8f;
        public CompanionState State { get; private set; } = CompanionState.Idle;
        public WorldPosition Destination { get; private set; }
        public float CooldownRatio => (float)(cooldown / CallCooldownSeconds);
        private double cooldown;
        private double stuckSeconds;
        private WorldPosition progressAnchor;
        private bool trackingProgress;
        private bool called;

        public bool Call()
        {
            if (cooldown > 0) return false;
            cooldown = CallCooldownSeconds;
            called = true;
            return true;
        }

        public CompanionState Tick(float deltaSeconds, WorldPosition player,
            WorldPosition companion, WorldPosition playerForward)
        {
            HungerCore.RequireFinite(deltaSeconds, nameof(deltaSeconds));
            if (deltaSeconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            cooldown = Math.Max(0, cooldown - deltaSeconds);
            double distance = WorldPosition.Distance(player, companion);
            Destination = player;
            if (distance < 2.5)
            {
                State = CompanionState.Idle;
                called = false;
                ResetProgress();
                return State;
            }

            if (!trackingProgress || WorldPosition.Distance(progressAnchor, companion) >= 0.05)
            {
                progressAnchor = companion;
                stuckSeconds = 0;
                trackingProgress = true;
            }
            else stuckSeconds += deltaSeconds;

            if (distance > 30 || stuckSeconds >= 3)
            {
                // 只取水平朝向，避免玩家上下坡時把夥伴放進地底。
                double length = Math.Sqrt((double)playerForward.X * playerForward.X +
                    (double)playerForward.Z * playerForward.Z);
                float x = length > 0 ? (float)(playerForward.X / length) : 0f;
                float z = length > 0 ? (float)(playerForward.Z / length) : 1f;
                Destination = new WorldPosition(player.X - 2 * x, player.Y, player.Z - 2 * z);
                State = CompanionState.Teleport;
                called = false;
                ResetProgress();
            }
            else State = distance > 12 || called ? CompanionState.Catchup : CompanionState.Follow;
            return State;
        }

        private void ResetProgress()
        {
            trackingProgress = false;
            stuckSeconds = 0;
        }
    }
}
