using UnityEngine;
using UnityEngine.AI;

namespace Liuruan
{
    public sealed class R3Npc : MonoBehaviour
    {
        public Transform Destination;
        public int act;
        Renderer[] renderers;
        void Start() { renderers = GetComponentsInChildren<Renderer>(); }
        NavMeshPath path;
        int corner;
        void Update()
        {
            var flow = FindFirstObjectByType<GameFlow>();
            if (!flow) return;
            foreach (var r in renderers) r.enabled = flow.Act == act;
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
                    transform.position = Vector3.MoveTowards(transform.position, path.corners[corner], 2 * Time.deltaTime);
                }
            }
            Vector3 delta = flow.Player.transform.position - transform.position; delta.y = 0;
            if (delta.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(delta);
        }
    }
}
