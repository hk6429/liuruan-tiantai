using System;

namespace Liuruan
{
    public sealed class RescueCore
    {
        readonly float minX, maxX, minZ, maxZ, minY;
        double groundedSeconds;
        public WorldPosition LastSafePoint { get; private set; }
        public RescueCore(WorldPosition initialSafePoint, float minX, float maxX, float minZ, float maxZ, float minY = -10)
        {
            foreach (float v in new[] { minX, maxX, minZ, maxZ, minY }) HungerCore.RequireFinite(v, nameof(minX));
            if (minX >= maxX || minZ >= maxZ) throw new ArgumentOutOfRangeException(nameof(minX));
            this.minX = minX; this.maxX = maxX; this.minZ = minZ; this.maxZ = maxZ; this.minY = minY;
            if (NeedsRescue(initialSafePoint)) throw new ArgumentException("初始安全點在救援範圍外。");
            LastSafePoint = initialSafePoint;
        }
        public bool NeedsRescue(WorldPosition point) => point.Y < minY || point.X < minX || point.X > maxX || point.Z < minZ || point.Z > maxZ;
        public bool Tick(float deltaTime, WorldPosition point, bool grounded, bool inWater)
        {
            R3Data.Time(deltaTime);
            bool rescue = NeedsRescue(point);
            if (!grounded || inWater || rescue) groundedSeconds = 0;
            else
            {
                groundedSeconds += deltaTime;
                if (groundedSeconds >= 2) { LastSafePoint = point; groundedSeconds %= 2; }
            }
            return rescue;
        }
    }
}
