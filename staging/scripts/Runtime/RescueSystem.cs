using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class RescueSystem : MonoBehaviour
    {
        [SerializeField] Vector3 initialSafePoint;
        [SerializeField] Vector2 minXZ = new Vector2(-128, -128), maxXZ = new Vector2(128, 128);
        [SerializeField] float minY = -10;
        public RescueCore Core { get; private set; }
        public event Action<Vector3> OnRescue;
        public event Action<string> OnError;
        void Awake() { ResetSafePoint(initialSafePoint); }
        public bool ResetSafePoint(Vector3 point)
        {
            try { Core = new RescueCore(Position(point), minXZ.x, maxXZ.x, minXZ.y, maxXZ.y, minY); }
            catch (ArgumentException e)
            {
                Core = null; Debug.LogError("救援設定無效：" + e.Message, this);
                OnError?.Invoke(e.Message); return false;
            }
            return true;
        }
        public bool Sample(float deltaTime, Vector3 point, bool grounded, bool inWater)
        {
            if (Core == null || !Core.Tick(deltaTime, Position(point), grounded, inWater)) return false;
            var safe = Core.LastSafePoint;
            OnRescue?.Invoke(new Vector3(safe.X, safe.Y, safe.Z)); return true;
        }
        static WorldPosition Position(Vector3 point) => new WorldPosition(point.x, point.y, point.z);
    }
}
