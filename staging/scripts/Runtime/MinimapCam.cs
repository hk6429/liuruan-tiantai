using System;
using UnityEngine;

namespace Liuruan
{
    [RequireComponent(typeof(Camera))]
    public sealed class MinimapCam : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform companion;
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 worldMin = new Vector2(-128, -128);
        [SerializeField] private Vector2 worldSize = new Vector2(256, 256);
        [SerializeField, Range(0, 31)] private int revealRadiusCells = 2;
        [SerializeField, Min(1)] private float viewHalfSize = 24;
        [SerializeField, Min(1)] private float cameraHeight = 100;
        [SerializeField, Min(64)] private float hudSize = 192;
        [SerializeField] private LayerMask mapLayers = ~0;
        public RenderTexture Output { get; private set; }
        public Texture2D FogMask { get; private set; }
        public event Action<bool> OnFogChanged;
        private MinimapFogCore core;
        private Camera mapCamera;
        private Texture2D arrow, dot;
        private readonly Color32[] maskPixels = new Color32[32 * 32];

        private void Awake()
        {
            core = new MinimapFogCore(worldMin.x, worldMin.y, worldSize.x, worldSize.y);
            mapCamera = GetComponent<Camera>();
            mapCamera.orthographic = true; mapCamera.orthographicSize = viewHalfSize;
            mapCamera.cullingMask = mapLayers; mapCamera.aspect = 1;
            mapCamera.nearClipPlane = 0.1f; mapCamera.farClipPlane = cameraHeight * 2;
            Output = new RenderTexture(256, 256, 16) { name = "LiuruanMinimap" };
            Output.Create(); mapCamera.targetTexture = Output;
            FogMask = new Texture2D(32, 32, TextureFormat.RGBA32, false)
                { name = "LiuruanFog", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            arrow = MakeIcon(true); dot = MakeIcon(false); UpdateMask();
        }
        public void SetAct(int act)
        {
            if (core == null) throw new InvalidOperationException("請在 MinimapCam.Awake 後設定幕次。");
            bool before = core.FogEnabled; core.SetAct(act); UpdateMask();
            if (before != core.FogEnabled) OnFogChanged?.Invoke(core.FogEnabled);
        }
        public void SetTarget(Transform currentTarget) => target = currentTarget;
        private void LateUpdate()
        {
            if (!player) return;
            transform.SetPositionAndRotation(player.position + Vector3.up * cameraHeight, Quaternion.Euler(90, 0, 0));
            if (core.FogEnabled && core.Reveal(player.position.x, player.position.z, revealRadiusCells)) UpdateMask();
        }
        private void UpdateMask()
        {
            for (int row = 0; row < 32; row++)
            for (int col = 0; col < 32; col++) maskPixels[row * 32 + col] = new Color32(0, 0, 0, core.MaskAlpha(col, row));
            FogMask.SetPixels32(maskPixels); FogMask.Apply(false, false);
        }
        private void OnGUI()
        {
            if (!player || !Output || Event.current.type != EventType.Repaint) return;
            Color oldColor = GUI.color; Matrix4x4 oldMatrix = GUI.matrix; GUI.color = Color.white;
            var rect = new Rect(Screen.width - hudSize - 16, Screen.height - hudSize - 16, hudSize, hudSize);
            GUI.BeginGroup(rect);
            GUI.DrawTexture(new Rect(0, 0, hudSize, hudSize), Output);
            if (core.FogEnabled)
            {
                float scale = hudSize / (viewHalfSize * 2);
                var bounds = new Rect((worldMin.x - player.position.x + viewHalfSize) * scale,
                    (player.position.z + viewHalfSize - worldMin.y - worldSize.y) * scale,
                    worldSize.x * scale, worldSize.y * scale);
                GUI.DrawTexture(bounds, FogMask);
                GUI.color = Color.black;
                Fill(0, 0, hudSize, Mathf.Clamp(bounds.yMin, 0, hudSize));
                Fill(0, Mathf.Clamp(bounds.yMax, 0, hudSize), hudSize, hudSize - Mathf.Clamp(bounds.yMax, 0, hudSize));
                Fill(0, 0, Mathf.Clamp(bounds.xMin, 0, hudSize), hudSize);
                Fill(Mathf.Clamp(bounds.xMax, 0, hudSize), 0, hudSize - Mathf.Clamp(bounds.xMax, 0, hudSize), hudSize);
            }
            DrawIcon(companion, Color.cyan, dot, false);
            DrawIcon(target, Color.yellow, dot, false);
            DrawIcon(player, Color.white, arrow, true);
            GUI.EndGroup(); GUI.matrix = oldMatrix; GUI.color = oldColor;
        }
        private void Fill(float x, float y, float w, float h)
        { if (w > 0 && h > 0) GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); }
        private void DrawIcon(Transform subject, Color color, Texture2D texture, bool rotate)
        {
            if (!subject) return;
            float scale = hudSize / (viewHalfSize * 2);
            Vector2 p = new Vector2(hudSize / 2 + (subject.position.x - player.position.x) * scale,
                hudSize / 2 - (subject.position.z - player.position.z) * scale);
            if (p.x < 0 || p.y < 0 || p.x > hudSize || p.y > hudSize) return;
            Matrix4x4 before = GUI.matrix;
            GUI.color = color;
            if (rotate) GUIUtility.RotateAroundPivot(subject.eulerAngles.y, p);
            GUI.DrawTexture(new Rect(p.x - 6, p.y - 6, 12, 12), texture);
            GUI.matrix = before;
        }
        private static Texture2D MakeIcon(bool triangle)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color32[256];
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool visible = triangle ? Math.Abs(x - 7.5f) <= (15 - y) * 0.5f
                    : (x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f) <= 49;
                pixels[y * 16 + x] = new Color32(255, 255, 255, (byte)(visible ? 255 : 0));
            }
            texture.SetPixels32(pixels); texture.Apply(); return texture;
        }
        private void OnDestroy()
        {
            if (mapCamera && mapCamera.targetTexture == Output) mapCamera.targetTexture = null;
            if (Output) { Output.Release(); Destroy(Output); }
            if (FogMask) Destroy(FogMask);
            if (arrow) Destroy(arrow);
            if (dot) Destroy(dot);
        }
    }
}
