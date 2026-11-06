using UnityEngine;

namespace Liuruan
{
    /// 人物動作驅動（第六輪 Mixamo）：掛在帶 Animator 的人物模型上。依 docs/anim-cues.md：
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimDriver : MonoBehaviour
    {
        public string speaker;

        static readonly int TalkHash = Animator.StringToHash("Talk");
        static readonly int LaughHash = Animator.StringToHash("Laugh");

        Animator anim;
        bool speaking;
        float laughUntil = -1f;

        void Awake() => anim = GetComponent<Animator>();

        void Start()
        {
            // 同一份 idle 的 NPC 錯開相位，免得整園子同步擺手
            if (anim && anim.runtimeAnimatorController && anim.isActiveAndEnabled && anim.GetCurrentAnimatorStateInfo(0).speed > 0f)
                anim.Play(0, 0, Random.value);
        }

        /// 讓 host（或其子物件）上的人物笑 seconds 秒
        public static void LaughFor(Transform host, float seconds)
        {
            if (!host) return;
            var d = host.GetComponentInChildren<CharacterAnimDriver>();
            if (!d) return;
            d.laughUntil = Mathf.Max(d.laughUntil, Time.unscaledTime + seconds);
            d.Apply();
        }

        public void SetSpeaking(bool value) { speaking = value; Apply(); }

        void Update()
        {
            float now = Time.unscaledTime;
            if (laughUntil > 0f && now >= laughUntil) { laughUntil = -1f; Apply(); }
        }

        void Apply()
        {
            bool laugh = laughUntil > Time.unscaledTime;
            bool talk = speaking;
            SetBool(LaughHash, laugh);
            SetBool(TalkHash, talk && !laugh);
        }

        void SetBool(int hash, bool v)
        {
            if (!anim || !anim.runtimeAnimatorController || !anim.isActiveAndEnabled) return;
            foreach (var p in anim.parameters)
                if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Bool) { anim.SetBool(hash, v); return; }
        }
    }
}
