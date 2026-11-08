using System;
using System.Collections.Generic;
using UnityEngine;

namespace Liuruan
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ClimbZone : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Behaviour playerMovement;
        [SerializeField] private HungerSystem hunger;
        [SerializeField] private Transform planeOrigin;
        [SerializeField] private Transform topLanding;
        [SerializeField, Min(0.01f)] private float width = 3;
        [SerializeField, Min(0.01f)] private float height = 6;
        public event Action<ClimbState> OnStateChanged;
        public event Action<bool> OnMovementLockChanged;
        public event Action<bool> OnPromptChanged;
        public event Action OnTopOut;
        public ClimbState State => core != null ? core.State : ClimbState.Grounded;
        public bool IsLocked => core != null && core.IsLocked;
        public bool CanInteract => isActiveAndEnabled && playerMovement && playerMovement.enabled && inside.Count > 0 && State == ClimbState.Grounded;
        private readonly HashSet<Collider> inside = new HashSet<Collider>();
        private ClimbCore core;
        private CharacterController controller;
        private bool movementWasEnabled, controllerWasEnabled, ownsMovement, prompt;
        private Vector2 moveInput;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            if (!player || !playerMovement || !planeOrigin || !topLanding || playerMovement == this)
            { Debug.LogError("ClimbZone 需要玩家、一般移動元件、藤蔓平面原點與頂端落點。", this); enabled = false; return; }
            controller = player.GetComponent<CharacterController>();
            core = new ClimbCore(width, height);
            core.OnStateChanged += state => OnStateChanged?.Invoke(state);
        }
        public bool TryInteract()
        {
            if (core == null || !CanInteract) return false;
            Vector3 offset = player.position - planeOrigin.position;
            movementWasEnabled = playerMovement.enabled;
            controllerWasEnabled = controller && controller.enabled;
            ownsMovement = true;
            playerMovement.enabled = false;
            if (controller) controller.enabled = false;
            var animator = player.GetComponentInChildren<Animator>();
            if (animator) { animator.SetFloat("Speed", 0); animator.SetBool("Climbing", true); }
            OnMovementLockChanged?.Invoke(true);
            core.Attach(Vector3.Dot(offset, planeOrigin.right), Vector3.Dot(offset, Vector3.up));
            ApplyPlanePosition(); SetPrompt(false); return true;
        }
        public void SetMoveInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1);
        public void Release()
        {
            if (core == null || !ownsMovement) return;
            core.Release(); RestoreMovement(); core.Finish();
        }
        private void Update()
        {
            SetPrompt(CanInteract);
            if (!ownsMovement) return;
            bool wasTopOut = State == ClimbState.TopOut;
            Vector2 input = moveInput + new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            core.Tick(Time.deltaTime, input.x, input.y, hunger ? hunger.Value : 5);
            if (!wasTopOut) ApplyPlanePosition();
            if (State == ClimbState.TopOut)
            {
                player.position = Vector3.MoveTowards(player.position, topLanding.position, ClimbCore.Speed * Time.deltaTime);
                if ((player.position - topLanding.position).sqrMagnitude <= 0.000001f)
                { RestoreMovement(); core.Finish(); OnTopOut?.Invoke(); }
            }
            else if (State == ClimbState.Release) { RestoreMovement(); core.Finish(); }
        }
        // 攀爬時一般移動元件被關掉，它的 LateUpdate 鏡頭跟隨也停了 → 這裡接手：從崖外側斜後方看人物。
        private void LateUpdate()
        {
            if (!ownsMovement) return;
            var cam = Camera.main; if (!cam) return;
            Vector3 pivot = player.position + Vector3.up * 1.2f;
            Vector3 away = planeOrigin.forward; // 玩家面向 -forward（崖壁），鏡頭放在 +forward 側
            Vector3 dir = (away * 0.85f + Vector3.up * 0.35f + planeOrigin.right * 0.25f).normalized;
            float dist = CamClear(pivot, dir, 7f, player);
            if (dist < 1.5f)
            {
                Vector3 alt = (-away * 0.85f + Vector3.up * 0.35f).normalized;
                float d2 = CamClear(pivot, alt, 7f, player);
                if (d2 > dist) { dir = alt; dist = d2; }
            }
            Vector3 want = pivot + dir * dist;
            cam.transform.position = Vector3.Lerp(cam.transform.position, want, 1f - Mathf.Exp(-8f * Time.deltaTime));
            cam.transform.rotation = Quaternion.LookRotation(pivot - cam.transform.position, Vector3.up);
            HideBlockers(cam.transform.position, pivot);
        }
        // 樹木等道具沒有碰撞體，擋在鏡頭與人物之間時暫時只投影子（爬完恢復）。
        private Renderer[] nearby;
        private readonly Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> hidden = new Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();
        private readonly List<Renderer> clear = new List<Renderer>();
        private void HideBlockers(Vector3 from, Vector3 to)
        {
            if (nearby == null)
            {
                var list = new List<Renderer>();
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (!r.transform.IsChildOf(player) && r.bounds.size.magnitude < 40f && Vector3.Distance(r.bounds.center, planeOrigin.position) < 30f) list.Add(r);
                nearby = list.ToArray();
            }
            var ray = new Ray(from, to - from); float len = Vector3.Distance(from, to);
            clear.Clear();
            foreach (var r in nearby)
            {
                if (!r) continue;
                bool block = r.bounds.Contains(from) || (r.bounds.IntersectRay(ray, out float d) && d < len - 0.3f);
                if (block && !hidden.ContainsKey(r)) { hidden[r] = r.shadowCastingMode; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; }
                else if (!block && hidden.ContainsKey(r)) clear.Add(r);
            }
            foreach (var r in clear) { r.shadowCastingMode = hidden[r]; hidden.Remove(r); }
        }
        private void RestoreBlockers()
        {
            foreach (var kv in hidden) if (kv.Key) kv.Key.shadowCastingMode = kv.Value;
            hidden.Clear();
        }
        private float CamClear(Vector3 pivot, Vector3 dir, float max, Transform self)
        {
            float best = max;
            foreach (var h in Physics.SphereCastAll(pivot, 0.22f, dir, max, ~0, QueryTriggerInteraction.Ignore))
                if (h.distance > 0f && !h.collider.transform.IsChildOf(self) && !(h.collider is CharacterController) && h.distance < best) best = h.distance;
            return Mathf.Max(0.5f, best - 0.1f);
        }
        private void ApplyPlanePosition()
        {
            // 平面保持垂直，planeOrigin 只能繞世界 Y 旋轉。
            player.position = planeOrigin.position + planeOrigin.right * core.X + Vector3.up * core.Y;
            player.rotation = Quaternion.LookRotation(-planeOrigin.forward, Vector3.up);
        }
        private void RestoreMovement()
        {
            if (!ownsMovement) return;
            ownsMovement = false; moveInput = Vector2.zero; RestoreBlockers();
            if (controller) controller.enabled = controllerWasEnabled;
            if (playerMovement) playerMovement.enabled = movementWasEnabled;
            var animator = player.GetComponentInChildren<Animator>();
            if (animator) animator.SetBool("Climbing", false);
            OnMovementLockChanged?.Invoke(false);
        }
        private bool IsPlayer(Collider other) => player && (other.transform == player || other.transform.IsChildOf(player));
        private void OnTriggerEnter(Collider other) { if (IsPlayer(other)) { inside.Add(other); SetPrompt(CanInteract); } }
        private void OnTriggerExit(Collider other) { if (inside.Remove(other)) SetPrompt(CanInteract); }
        private void SetPrompt(bool show)
        { if (prompt != show) { prompt = show; OnPromptChanged?.Invoke(show); } }
        private void OnDisable() { Release(); inside.Clear(); SetPrompt(false); }
    }
}
