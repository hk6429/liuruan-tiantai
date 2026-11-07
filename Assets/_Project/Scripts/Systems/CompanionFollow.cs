using UnityEngine;
using UnityEngine.AI;

namespace Liuruan
{
    public sealed class CompanionFollow : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField, Min(0f)] private float followSpeed = 3f;
        [SerializeField, Min(1f)] private float catchupMultiplier = 1.6f;
        public Animator animator;
        private NavMeshAgent agent;
        private readonly CompanionCore core = new CompanionCore();
        private Vector3 previousPosition;
        public float Speed { get; private set; }
        public float CooldownRatio => core.CooldownRatio;
        public CompanionState State => core.State;
        public bool Call() => core.Call();
        private bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;
        private void Awake() => agent = GetComponent<NavMeshAgent>();
        private void OnEnable() { previousPosition = transform.position; Speed = 0f; previousFacing = transform.position; }

        private void Update()
        {
            float dt = Time.deltaTime;
            Speed = dt > 0f ? Vector3.Distance(transform.position, previousPosition) / dt : 0f;
            previousPosition = transform.position;
            if (player == null)
            {
                if (AgentReady) agent.ResetPath();
                Speed = 0f;
                return;
            }
            CompanionState state = core.Tick(dt, ToCore(player.position),
                ToCore(transform.position), ToCore(player.forward));
            Vector3 target = ToUnity(core.Destination);
            if (state == CompanionState.Teleport)
            {
                // shortcut: 僅搜尋目標附近 2m 的 NavMesh；複雜多層地形由 U 線提供安全落點。
                bool ready = AgentReady;
                if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    target = hit.position;
                if (ready) agent.ResetPath();
                if (!ready || !agent.Warp(target)) transform.position = target;
                previousPosition = transform.position;
                Speed = 0f;
                return;
            }
            float speed = state == CompanionState.Catchup ? followSpeed * catchupMultiplier : followSpeed;
            if (AgentReady)
            {
                agent.stoppingDistance = 2.4f;
                agent.speed = speed;
                agent.isStopped = state == CompanionState.Idle;
                if (state != CompanionState.Idle) agent.SetDestination(target);
            }
            else if (state != CompanionState.Idle)
            {
                float remaining = Mathf.Max(0f, Vector3.Distance(transform.position, target) - 2.4f);
                transform.position = Vector3.MoveTowards(transform.position, target, Mathf.Min(speed * dt, remaining));
            }
        }

        private void LateUpdate()
        {
            if (animator) animator.SetFloat("Speed", Speed);
            Vector3 direction = transform.position - previousFacing; direction.y = 0;
            if (direction.sqrMagnitude > .0001f && direction.sqrMagnitude < 4) transform.rotation = Quaternion.LookRotation(direction);
            previousFacing = transform.position;
        }
        private Vector3 previousFacing;

        private void OnDisable()
        {
            if (AgentReady) agent.ResetPath();
            Speed = 0f;
        }
        private static WorldPosition ToCore(Vector3 p) => new WorldPosition(p.x, p.y, p.z);
        private static Vector3 ToUnity(WorldPosition p) => new Vector3(p.X, p.Y, p.Z);
    }
}
