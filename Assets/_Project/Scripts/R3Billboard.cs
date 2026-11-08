using UnityEngine;
namespace Liuruan
{
    public sealed class R3Billboard : MonoBehaviour
    {
        void LateUpdate() { if (Camera.main) transform.rotation = Camera.main.transform.rotation; }
    }
}
