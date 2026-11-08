using System;
using UnityEngine;

namespace Liuruan
{
    [DefaultExecutionOrder(100)]
    public sealed class OffscreenArrow : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform target;
        [SerializeField, Range(0, .49f)] float inset = .05f;
        readonly float[] vp = new float[16];
        public OffscreenArrowResult Result { get; private set; }
        public bool HasResult { get; private set; }
        public event Action<OffscreenArrowResult> OnChanged;
        public event Action OnCleared;
        public void SetTarget(Transform value) { target = value; }
        void LateUpdate()
        {
            if (!viewCamera || !target || viewCamera.pixelHeight <= 0)
            { Clear(); return; }
            var matrix = viewCamera.projectionMatrix * viewCamera.worldToCameraMatrix;
            for (int row = 0; row < 4; row++) for (int col = 0; col < 4; col++) vp[row * 4 + col] = matrix[row, col];
            var p = target.position;
            Result = OffscreenArrowCore.Project(vp, new WorldPosition(p.x, p.y, p.z), inset,
                (float)viewCamera.pixelWidth / viewCamera.pixelHeight);
            HasResult = true; OnChanged?.Invoke(Result);
        }
        void OnDisable() { Clear(); }
        void Clear() { if (!HasResult) return; HasResult = false; OnCleared?.Invoke(); }
    }
}
