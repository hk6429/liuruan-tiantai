using System;
using UnityEngine;

namespace Liuruan
{
    public sealed class SeasonSwap : MonoBehaviour
    {
        [Serializable] public struct Palette
        {
            public Color canopy, ground, fog, light;
            public Palette(Color canopy, Color ground, Color fog, Color light)
            { this.canopy = canopy; this.ground = ground; this.fog = fog; this.light = light; }
        }
        [SerializeField] private Palette spring = new Palette(new Color(.55f,.8f,.4f), new Color(.5f,.65f,.3f), new Color(.8f,.9f,.8f), new Color(1,.95f,.8f));
        [SerializeField] private Palette summer = new Palette(new Color(.2f,.65f,.25f), new Color(.35f,.55f,.2f), new Color(.7f,.85f,.85f), new Color(1,1,.9f));
        [SerializeField] private Palette autumn = new Palette(new Color(.9f,.5f,.2f), new Color(.6f,.45f,.25f), new Color(.8f,.7f,.6f), new Color(1,.8f,.6f));
        [SerializeField] private Palette winter = new Palette(new Color(.7f,.75f,.8f), new Color(.75f,.8f,.85f), new Color(.8f,.85f,.9f), new Color(.7f,.8f,1));
        [SerializeField] private Renderer[] canopyRenderers = new Renderer[0];
        [SerializeField] private Renderer[] groundRenderers = new Renderer[0];
        [SerializeField] private string colorProperty = "_BaseColor";
        [SerializeField] private Light sun;
        [SerializeField] private GameObject immortalVillageRoot, homeVillageRoot;
        [SerializeField] private Material skyReturn;
        [SerializeField] private Color returnLight = new Color(.62f,.7f,.82f);
        public event Action OnBirdsSing;
        public event Action OnHomeSwapped;
        public bool IsPlaying => core.IsPlaying;
        public bool IsHome => core.IsHome;
        private readonly SeasonCore core = new SeasonCore();
        private MaterialPropertyBlock block;
        private void Awake()
        {
            block = new MaterialPropertyBlock();
            core.OnBirdsSing += BirdsSing;
        }
        public void PlayHalfYear()
        { if (core.PlayHalfYear()) ApplySeason(); }
        private void Update()
        {
            if (!core.IsPlaying) return;
            core.Tick(Time.deltaTime);
            if (!core.IsHome) ApplySeason();
        }
        private void BirdsSing()
        {
            ApplySeason();
            OnBirdsSing?.Invoke();
        }
        public void SwapToHome()
        {
            if (core.IsHome) return;
            if (!immortalVillageRoot || !homeVillageRoot || immortalVillageRoot == homeVillageRoot || !skyReturn || !sun)
            { Debug.LogError("SeasonSwap 需要兩組不同的村落根物件、sky_return 材質與主光源。", this); return; }
            if (immortalVillageRoot.transform.IsChildOf(homeVillageRoot.transform) ||
                homeVillageRoot.transform.IsChildOf(immortalVillageRoot.transform) ||
                transform.IsChildOf(immortalVillageRoot.transform) || transform.IsChildOf(homeVillageRoot.transform))
            { Debug.LogError("SeasonSwap 流程物件須在兩村之外，且兩村根物件不可互相包含。", this); return; }
            if (!core.SwapToHome()) return;
            immortalVillageRoot.SetActive(false); homeVillageRoot.SetActive(true);
            RenderSettings.skybox = skyReturn; sun.color = returnLight;
            OnHomeSwapped?.Invoke();
        }
        private void ApplySeason()
        {
            var blend = core.Blend;
            Palette a = GetPalette(blend.Current), b = GetPalette(blend.Next);
            Paint(canopyRenderers, Color.Lerp(a.canopy, b.canopy, blend.Weight));
            Paint(groundRenderers, Color.Lerp(a.ground, b.ground, blend.Weight));
            RenderSettings.fogColor = Color.Lerp(a.fog, b.fog, blend.Weight);
            if (sun) sun.color = Color.Lerp(a.light, b.light, blend.Weight);
        }
        private Palette GetPalette(Season season)
        {
            switch (season)
            {
                case Season.Summer: return summer;
                case Season.Autumn: return autumn;
                case Season.Winter: return winter;
                default: return spring;
            }
        }
        private void Paint(Renderer[] renderers, Color color)
        {
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;
                renderer.GetPropertyBlock(block); block.SetColor(colorProperty, color); renderer.SetPropertyBlock(block);
            }
        }
    }
}
