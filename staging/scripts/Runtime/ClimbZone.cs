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
        private void ApplyPlanePosition()
        {
            // 平面保持垂直，planeOrigin 只能繞世界 Y 旋轉。
            player.position = planeOrigin.position + planeOrigin.right * core.X + Vector3.up * core.Y;
            player.rotation = Quaternion.LookRotation(-planeOrigin.forward, Vector3.up);
        }
        private void RestoreMovement()
        {
            if (!ownsMovement) return;
            ownsMovement = false; moveInput = Vector2.zero;
            if (controller) controller.enabled = controllerWasEnabled;
            if (playerMovement) playerMovement.enabled = movementWasEnabled;
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
