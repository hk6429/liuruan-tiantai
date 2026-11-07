using System;

namespace Liuruan
{
    public sealed class DriftCollectionCore
    {
        private int collected;
        public int Count { get; private set; }
        public event Action<int> OnCollected;
        public event Action OnAllCollected;
        public bool IsCollected(int id) { ValidateId(id); return (collected & (1 << id)) != 0; }
        public bool Collect(int id)
        {
            if (IsCollected(id)) return false;
            collected |= 1 << id; Count++;
            bool all = Count == 3;
            OnCollected?.Invoke(id);
            if (all) OnAllCollected?.Invoke();
            return true;
        }
        internal static void ValidateId(int id)
        {
            if (id < 1 || id > 3) throw new ArgumentOutOfRangeException(nameof(id));
        }
    }

    public sealed class DriftCore
    {
        private readonly WorldPosition[] points;
        private readonly DriftCollectionCore collection;
        private readonly float cycleSeconds;
        private double elapsed;
        public int Id { get; }
        public bool IsCollected => collection.IsCollected(Id);
        public WorldPosition Position { get; private set; }
        public event Action<int> OnPickedUp;

        public DriftCore(int id, WorldPosition[] waypoints, DriftCollectionCore collection, float cycleSeconds = 30)
        {
            DriftCollectionCore.ValidateId(id);
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (waypoints == null || waypoints.Length < 2) throw new ArgumentException("至少需要兩個路徑點。", nameof(waypoints));
            HungerCore.RequireFinite(cycleSeconds, nameof(cycleSeconds));
            if (cycleSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(cycleSeconds));
            Id = id; points = (WorldPosition[])waypoints.Clone(); this.collection = collection;
            this.cycleSeconds = cycleSeconds; Position = Sample(0);
        }
        public void Tick(float dt)
        {
            HungerCore.RequireFinite(dt, nameof(dt));
            if (dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            if (IsCollected) return;
            elapsed = (elapsed + dt) % cycleSeconds;
            Position = Sample((float)(elapsed / cycleSeconds));
        }
        public bool CanPickUp(WorldPosition player) => !IsCollected && WorldPosition.Distance(player, Position) <= 2;
        public bool TryPickUp(WorldPosition player)
        {
            if (!CanPickUp(player)) return false;
            if (!collection.Collect(Id)) return false;
            OnPickedUp?.Invoke(Id); return true;
        }
        // shortcut: 均勻參數 Catmull–Rom，彎道速度略變；若需固定 m/s 再加弧長表。
        public WorldPosition Sample(float t)
        {
            HungerCore.RequireFinite(t, nameof(t));
            if (t < 0 || t > 1) throw new ArgumentOutOfRangeException(nameof(t));
            double scaled = (double)t * (points.Length - 1);
            int i = Math.Min(points.Length - 2, (int)scaled);
            double u = scaled - i;
            WorldPosition a = points[Math.Max(0, i - 1)], b = points[i], c = points[i + 1], d = points[Math.Min(points.Length - 1, i + 2)];
            return new WorldPosition(Curve(a.X, b.X, c.X, d.X, u), Curve(a.Y, b.Y, c.Y, d.Y, u), Curve(a.Z, b.Z, c.Z, d.Z, u));
        }
        private static float Curve(double a, double b, double c, double d, double t)
            => (float)(0.5 * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t));
    }
}
