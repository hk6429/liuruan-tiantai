using UnityEngine;
using UnityEngine.AI;

namespace Liuruan
{
    public sealed class R3Npc : MonoBehaviour
    {
        public Transform Destination;
        public int act;
        Renderer[] renderers;
        // 靜態模型沒有骨架：自己做步態（上下起伏＋左右擺＋微前傾），走路時面向前進方向，等玩家跟上再走。
        Transform visual; Vector3 visualPos; Quaternion visualRot; float gait, moveBlend;
        const float WalkSpeed = 1.5f, WaitDistance = 7f;
        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            visual = transform.Find("Visual");
            if (visual)
            {
                var bob = visual.GetComponent<CharacterBob>(); if (bob) Destroy(bob);
                visualPos = visual.localPosition; visualRot = visual.localRotation;
            }
        }
        NavMeshPath path;
        int corner;
        void Update()
        {
            var flow = FindFirstObjectByType<GameFlow>();
            if (!flow) return;
            foreach (var r in renderers) r.enabled = flow.Act == act;
            bool moving = false; Vector3 heading = Vector3.zero;
            if (flow.Act == 4 && Destination && !flow.journey.Busy && !flow.Reading)
            {
                if (path == null)
                {
                    path = new NavMeshPath();
                    if (NavMesh.SamplePosition(transform.position, out var a, 3, NavMesh.AllAreas) && NavMesh.SamplePosition(Destination.position, out var b, 3, NavMesh.AllAreas))
                        NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
                }
                if (path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 0)
                {
                    while (corner < path.corners.Length - 1 && Vector3.Distance(transform.position, path.corners[corner]) < .3f) corner++;
                    Vector3 toPlayer = flow.Player.transform.position - transform.position; toPlayer.y = 0;
                    Vector3 step = path.corners[corner] - transform.position; step.y = 0;
                    if (toPlayer.magnitude < WaitDistance && step.sqrMagnitude > .01f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, path.corners[corner], WalkSpeed * Time.deltaTime);
                        moving = true; heading = step;
                    }
                }
            }
            moveBlend = Mathf.MoveTowards(moveBlend, moving ? 1 : 0, 4 * Time.deltaTime);
            Vector3 look = moving ? heading : flow.Player.transform.position - transform.position; look.y = 0;
            if (look.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 1 - Mathf.Exp(-8 * Time.deltaTime));
            Gait();
        }
        void Gait()
        {
            if (!visual) return;
            gait += Time.deltaTime * Mathf.Lerp(1.2f, 9f, moveBlend); // 走路約每秒 1.4 步週期
            float s = Mathf.Sin(gait);
            float bounce = Mathf.Lerp(Mathf.Sin(gait * .25f) * .01f, Mathf.Abs(s) * .07f, moveBlend);
            float roll = s * 5f * moveBlend, pitch = 5f * moveBlend, yaw = s * 4f * moveBlend;
            visual.localPosition = visualPos + Vector3.up * bounce;
            visual.localRotation = visualRot * Quaternion.Euler(pitch, yaw, roll);
        }
    }
}

// R3 gait
