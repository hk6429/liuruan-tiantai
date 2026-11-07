using UnityEngine;

namespace Liuruan
{
    /// 第三人稱控制：WASD／方向鍵移動、按住滑鼠右鍵轉視角、E 互動。相機為簡單跟隨相機。
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 4f;
        public float turnSpeed = 720f;
        public float gravity = -20f;

        public Transform cam;
        public float camDistance = 6f;
        public float camHeight = 1.5f;
        public float mouseSensitivity = 3f;
        public LayerMask camCollision = ~0;

        public Animator animator; // 參數 Speed (float)

        /// 演出時由 CluePerformance 指定的固定鏡頭；null 時恢復跟隨
        public static Transform CamShot;

        CharacterController cc;
        HungerSystem hunger;
        float yaw, pitch = 20f, vy;
        float interactHintAfter;
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            hunger = GetComponent<HungerSystem>();
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            yaw = transform.eulerAngles.y;
            RestoreFoliage();
            foliage = null; // 關掉 Domain Reload 時靜態清單會殘留上一局
        }

        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.LookRotation(direction);
            yaw = transform.eulerAngles.y;
        }

        void Update()
        {
            // hotfix1：HTML 輸入框開著時不吃鍵盤（WASD、E 是在打字）
            bool busy = (ScrollUI.I != null && ScrollUI.I.IsBusy);
            Vector3 move = Vector3.zero;

            if (!busy)
            {
                if (Input.GetMouseButton(1))
                {
                    yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                    pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -10f, 60f);
                }
                Vector2 look = TouchInput.ConsumeLook() * 0.15f;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, -10f, 60f);
                Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
                input += new Vector3(TouchInput.Move.x, 0, TouchInput.Move.y);
                move = Quaternion.Euler(0, yaw, 0) * Vector3.ClampMagnitude(input, 1f);

                var near = Interactable.Nearest(transform.position);
                ScrollUI.I?.SetPrompt(near ? near.CurrentPrompt : null);
                bool interactPressed = Input.GetKeyDown(KeyCode.E) | TouchInput.ConsumeDown();
                if (interactPressed)
                {
                    if (near) near.Interact();
                    else if (Time.unscaledTime >= interactHintAfter)
                    {
                        interactHintAfter = Time.unscaledTime + 2.5f;
                        ScrollUI.I?.Toast(Txt.InteractTooFar);
                    }
                }
            }
            else TouchInput.ConsumeLook();

            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);

            vy = cc.isGrounded ? -1f : vy + gravity * Time.deltaTime;
            Vector3 before = transform.position;
            float speed = moveSpeed * (hunger ? hunger.SpeedMultiplier : 1f);
            MoveDisplacement(cc, (move * speed + Vector3.up * vy) * Time.deltaTime);
            GroundFeet();

            // 讀實際水平速度（撞牆就停步；鍵盤、觸控搖桿都走 move → cc.Move，一樣有效）
            if (animator)
            {
                var v = (transform.position - before) / Mathf.Max(Time.deltaTime, 0.0001f); v.y = 0f;
                animator.SetFloat(SpeedHash, v.magnitude, 0.08f, Time.deltaTime);
            }
        }

        public static CollisionFlags MoveDisplacement(CharacterController controller, Vector3 delta)
        {
            // 每次不跨過單級踏面，低幀率也能逐階抬腳；限制落下時的迴圈成本。
            int count = Mathf.Clamp(Mathf.CeilToInt(delta.magnitude / 0.12f), 1, 32);
            var flags = CollisionFlags.None;
            for (int i = 0; i < count; i++) flags |= controller.Move(delta / count);
            return flags;
        }

        // hotfix1：腳貼地。膠囊圓底踩在台階邊緣時，模型腳底會陷進上一階（最多一階高）或浮在下一階；
        // 改用腳尖、腳中、腳跟三條射線量到的最高走路面當腳底，模型只上下微調（±0.1～0.3m），碰撞體不動。
        Transform foot; float footBase, footOff;
        void GroundFeet()
        {
            if (!foot)
            {
                if (!animator || !animator.transform.parent) return;
                foot = animator.transform.parent; footBase = foot.localPosition.y;
            }
            float bottom = transform.position.y + cc.center.y - cc.height * 0.5f;
            float best = float.NegativeInfinity;
            var fw = transform.forward;
            for (int i = -1; i <= 1; i++)
            {
                var p = transform.position + fw * (0.14f * i);
                if (Physics.Raycast(new Vector3(p.x, bottom + 0.4f, p.z), Vector3.down, out var h, 0.65f, CamOcclusion.WalkMask & ~(1 << 2), QueryTriggerInteraction.Ignore)
                    && h.normal.y > 0.6f && h.point.y > best) best = h.point.y;
            }
            float want = cc.isGrounded && best > float.NegativeInfinity ? Mathf.Clamp(best - bottom, -0.1f, 0.3f) : 0f;
            footOff = Mathf.MoveTowards(footOff, want, 1.5f * Time.deltaTime);
            var lp = foot.localPosition;
            float s = foot.parent ? foot.parent.lossyScale.y : 1f;
            lp.y = footBase + footOff / (s > 0.001f ? s : 1f);
            foot.localPosition = lp;
        }

        void LateUpdate()
        {
            if (!cam) return;
            if (CamShot) { cam.SetPositionAndRotation(CamShot.position, CamShot.rotation); RestoreFoliage(); HideSelf(false); return; }
            Vector3 pivot = transform.position + Vector3.up * camHeight;
            // 低矮屋簷下（亭子內抬高視角）鏡頭會被樑擋到貼著後腦：暫時把俯角放低，挑退得最遠的角度（不改玩家設定的 pitch）
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            float want = CamClearDistance(pivot, rot * Vector3.back, camDistance, transform);
            if (want < 1.2f)
                for (float p = pitch - 10f; p >= -10f; p -= 10f)
                {
                    var r2 = Quaternion.Euler(p, yaw, 0);
                    float w2 = CamClearDistance(pivot, r2 * Vector3.back, camDistance, transform);
                    if (w2 > want + 0.5f) { rot = r2; want = w2; }
                    if (want >= 2f) break;
                }
            // 撞到就立刻拉近；離開障礙時慢慢退回，不會一抽一抽
            camDist = want < camDist ? want : Mathf.MoveTowards(camDist, want, 4f * Time.deltaTime);
            cam.SetPositionAndRotation(pivot + rot * Vector3.back * camDist, rot);
            UpdateFoliage(cam.position, transform.position);
            // 背貼牆時鏡頭只剩 0.35～0.8 m，後腦會佔掉半個畫面：人物改成只投影子（不動模型本身）
            HideSelf(camDist < 0.8f);
        }

        Renderer[] selfRenderers;
        UnityEngine.Rendering.ShadowCastingMode[] selfModes;
        bool selfHidden;
        void HideSelf(bool hide)
        {
            if (hide == selfHidden) return;
            selfHidden = hide;
            if (selfRenderers == null)
            {
                selfRenderers = GetComponentsInChildren<Renderer>(true);
                selfModes = new UnityEngine.Rendering.ShadowCastingMode[selfRenderers.Length];
                for (int i = 0; i < selfRenderers.Length; i++) selfModes[i] = selfRenderers[i].shadowCastingMode;
            }
            for (int i = 0; i < selfRenderers.Length; i++)
                if (selfRenderers[i]) selfRenderers[i].shadowCastingMode = hide ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : selfModes[i];
        }

        float camDist = 5f;
        const float CamRadius = 0.22f, CamMin = 0.35f;

        /// R17-U：鏡頭可退多遠。SphereCastAll 取最近的非玩家碰撞（含 CamOcclusion 鏡頭層），
        /// 再檢查鏡頭點本身有沒有埋在物件裡（SphereCast 起點就重疊時會漏掉），有就一路往前拉近。
        public static float CamClearDistance(Vector3 pivot, Vector3 dir, float maxDist, Transform self)
        {
            float best = maxDist;
            foreach (var h in Physics.SphereCastAll(pivot, CamRadius, dir, maxDist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.distance <= 0f || IsSelf(h.collider, self)) continue;
                if (h.distance < best) best = h.distance;
            }
            best = Mathf.Max(CamMin, best - 0.05f);
            for (int i = 0; i < 12 && best > CamMin; i++)
            {
                if (!Blocked(pivot + dir * best, self)) break;
                best = Mathf.Max(CamMin, best - 0.4f);
            }
            return best;
        }

        static readonly Collider[] overlap = new Collider[8];
        static bool Blocked(Vector3 p, Transform self)
        {
            int n = Physics.OverlapSphereNonAlloc(p, CamRadius * 0.8f, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (!IsSelf(overlap[i], self)) return true;
            return false;
        }

        static bool IsSelf(Collider c, Transform self) =>
            (self && c.transform.IsChildOf(self)) || c.GetComponentInParent<PlayerController>() || c is CharacterController;

        // 樹冠沒有碰撞體：沿用暫時只投影，兼看鏡頭所在處與鏡頭到角色的視線。
        static readonly string[] FoliageKeys = { "maple", "willow", "pine", "wutong", "bamboo", "banana", "tree" };
        static System.Collections.Generic.List<Renderer> foliage;
        static readonly System.Collections.Generic.Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> hidden = new System.Collections.Generic.Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();
        static readonly System.Collections.Generic.List<Renderer> toShow = new System.Collections.Generic.List<Renderer>();

        void OnDisable() { RestoreFoliage(); HideSelf(false); }

        static void RestoreFoliage()
        {
            foreach (var item in hidden) if (item.Key) item.Key.shadowCastingMode = item.Value;
            hidden.Clear();
        }

        static void UpdateFoliage(Vector3 camPos, Vector3 feet)
        {
            if (foliage == null)
            {
                foliage = new System.Collections.Generic.List<Renderer>();
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (r.bounds.size.y < 1f) continue;
                    bool tree = false;
                    for (var t = r.transform; t && !tree; t = t.parent)
                    {
                        string n = t.name.ToLowerInvariant();
                        foreach (var k in FoliageKeys) if (n.Contains(k)) { tree = true; break; }
                    }
                    if (tree) foliage.Add(r);
                }
            }
            toShow.Clear();
            foreach (var item in hidden)
                if (!item.Key || !ObscuresPlayer(item.Key.bounds, camPos, feet)) toShow.Add(item.Key);
            foreach (var r in toShow)
            {
                var mode = hidden[r]; hidden.Remove(r);
                if (r) r.shadowCastingMode = mode;
            }
            foreach (var r in foliage)
                if (r && r.enabled && !hidden.ContainsKey(r) && ObscuresPlayer(r.bounds, camPos, feet)
                    && r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                {
                    hidden.Add(r, r.shadowCastingMode);
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
        }

        static bool ObscuresPlayer(Bounds b, Vector3 camera, Vector3 feet)
        {
            // shortcut: 用樹的包圍盒保護角色輪廓；若鬆散樹冠隱藏過多，再改成網格判定。
            return CamOcclusion.BlocksView(b, camera, feet + Vector3.up * 0.3f)
                || CamOcclusion.BlocksView(b, camera, feet + Vector3.up * 1f)
                || CamOcclusion.BlocksView(b, camera, feet + Vector3.up * 1.7f);
        }
    }
}
