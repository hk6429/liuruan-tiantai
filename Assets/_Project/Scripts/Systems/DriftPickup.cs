using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class DriftPickup : MonoBehaviour
    {
        [SerializeField, Range(1, 3)] private int id = 1;
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private DriftCollection collection;
        [SerializeField, Min(0.01f)] private float cycleSeconds = 30;
        public event Action<int> OnPickedUp;
        public event Action<int, bool> OnPromptChanged;
        public bool CanInteract => isActiveAndEnabled && core != null && player && core.CanPickUp(ToCore(player.position));
        private DriftCore core;
        private bool showingPrompt;

        private void Start()
        {
            if (!player || !collection || waypoints == null || waypoints.Length < 2)
            { Debug.LogError("DriftPickup 需要玩家、共用收集器與至少兩個路徑點。", this); enabled = false; return; }
            var points = new WorldPosition[waypoints.Length];
            for (int i = 0; i < points.Length; i++)
            {
                if (!waypoints[i]) { Debug.LogError("DriftPickup 路徑點不可留空。", this); enabled = false; return; }
                points[i] = ToCore(waypoints[i].position);
            }
            core = new DriftCore(id, points, collection.Core, cycleSeconds);
            core.OnPickedUp += picked => OnPickedUp?.Invoke(picked);
            if (core.IsCollected) { gameObject.SetActive(false); return; }
            ApplyPosition();
        }
        private void Update()
        {
            if (core == null) return;
            if (core.IsCollected) { gameObject.SetActive(false); return; }
            core.Tick(Time.deltaTime); ApplyPosition(); SetPrompt(CanInteract);
        }
        public bool TryInteract()
        {
            if (!isActiveAndEnabled || !CanInteract || !core.TryPickUp(ToCore(player.position))) return false;
            var animator = player.GetComponentInChildren<Animator>();
            if (animator) animator.SetTrigger("Pickup");
            SetPrompt(false); gameObject.SetActive(false); return true;
        }
        private void ApplyPosition()
        { var p = core.Position; transform.position = new Vector3(p.X, p.Y, p.Z); }
        private void SetPrompt(bool show)
        {
            if (show == showingPrompt) return;
            showingPrompt = show; OnPromptChanged?.Invoke(id, show);
        }
        private void OnDisable() => SetPrompt(false);
        private static WorldPosition ToCore(Vector3 p) => new WorldPosition(p.x, p.y, p.z);
    }
}
