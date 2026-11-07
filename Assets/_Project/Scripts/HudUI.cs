using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Liuruan
{
    public sealed class HudUI : MonoBehaviour
    {
        public TMP_FontAsset font;
        GameFlow flow;
        GameObject root, ending;
        Image[] peaches = new Image[5];
        Image cooldown;
        TMP_Text bark, objective, prompt;
        Button returnButton;
        Button[] skills = new Button[3];
        Sprite circle, peach, diamond;
        bool touch;
        void Start()
        {
            flow = GetComponent<GameFlow>();
            ScrollUI.I.JourneyHud = true;
            circle = Shape(0); peach = Shape(1); diamond = Shape(2);
            var canvas = new GameObject("JourneyHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 1;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            root = canvas;
            for (int i = 0; i < 5; i++)
            {
                peaches[i] = Icon("Peach" + i, new Vector2(0, 1), new Vector2(38 + i * 38, -38), new Vector2(31, 35), peach, Color.white);
            }
            bark = Label("Bark", new Vector2(0, 1), new Vector2(120, -78), new Vector2(220, 36), 25);
            objective = Label("Objective", new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(470, 42), 21);
            string[] names = { "剝皮", "取水", "呼喚" };
            string[] keys = { "F", "R", "G" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var image = Icon("Skill" + keys[i], new Vector2(1, 1), new Vector2(-218 + i * 82, -63), new Vector2(76, 76), diamond, new Color(0.24f, 0.3f, 0.25f, 0.96f));
                var button = image.gameObject.AddComponent<Button>();
                button.onClick.AddListener(() => flow.UseSkill(index)); skills[i] = button;
                ChildLabel(image.transform, names[i] + "\n" + keys[i], 19);
            }
            var portrait = Icon("CompanionPortraitFrame", new Vector2(0, 0), new Vector2(75, 78), new Vector2(108, 108), circle, new Color(0.18f, 0.25f, 0.3f));
            cooldown = Icon("CallCooldown", new Vector2(0, 0), new Vector2(75, 78), new Vector2(108, 108), circle, new Color(0.96f, 0.72f, 0.3f));
            cooldown.type = Image.Type.Filled; cooldown.fillMethod = Image.FillMethod.Radial360; cooldown.fillClockwise = true;
            var face = Icon("CompanionPortrait", new Vector2(0, 0), new Vector2(75, 78), new Vector2(88, 88), circle, new Color(0.23f, 0.4f, 0.56f));
            ChildLabel(face.transform, "阮肇", 23);
            var map = Icon("MinimapFrame", new Vector2(1, 0), new Vector2(-94, 100), new Vector2(164, 164), null, new Color(0.7f, 0.61f, 0.4f));
            var mapContent = new GameObject("MinimapRawImage", typeof(RectTransform), typeof(RawImage));
            mapContent.transform.SetParent(map.transform, false);
            var mapRect = (RectTransform)mapContent.transform; mapRect.anchorMin = Vector2.zero; mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = new Vector2(6, 6); mapRect.offsetMax = new Vector2(-6, -6);
            mapContent.GetComponent<RawImage>().color = new Color(0.13f, 0.19f, 0.17f, 0.92f);
            mapContent.GetComponent<RawImage>().raycastTarget = false;
            prompt = Label("InteractPrompt", new Vector2(0.5f, 0), new Vector2(0, 84), new Vector2(450, 42), 26);
            returnButton = ActionButton("ReturnHome", "求歸", new Vector2(0.5f, 0), new Vector2(0, 145), new Vector2(160, 52));
            returnButton.onClick.AddListener(flow.ReturnHome);
            ending = new GameObject("JourneyEnding", typeof(RectTransform), typeof(Image));
            ending.transform.SetParent(root.transform, false);
            var er = (RectTransform)ending.transform; er.anchorMin = new Vector2(0.15f, 0.2f); er.anchorMax = new Vector2(0.85f, 0.8f); er.offsetMin = er.offsetMax = Vector2.zero;
            ending.GetComponent<Image>().color = new Color(0.13f, 0.18f, 0.22f, 0.98f);
            ChildLabel(ending.transform, "天台山・流杯記\n\n五幕旅程結束", 32);
            ending.SetActive(flow.State.ended);
            touch = !TouchInput.Enabled;
            Layout();
            flow.Hunger.OnChanged += DrawHunger; DrawHunger(flow.Hunger.Value);
        }
        void OnDestroy()
        {
            if (flow && flow.Hunger) flow.Hunger.OnChanged -= DrawHunger;
            foreach (var sprite in new[] { circle, peach, diamond }) if (sprite) { Destroy(sprite.texture); Destroy(sprite); }
        }
        void DrawHunger(float value)
        {
            for (int i = 0; i < 5; i++) peaches[i].color = Color.Lerp(new Color(0.2f, 0.22f, 0.18f, 0.7f), new Color(1f, 0.7f, 0.37f), Mathf.Clamp01(value - i));
        }
        void LateUpdate()
        {
            if (!root) return;
            Layout();
            bool busy = flow.Reading || (ScrollUI.I && ScrollUI.I.IsBusy);
            for (int i = 0; i < root.transform.childCount; i++)
            {
                var child = root.transform.GetChild(i).gameObject;
                if (child != ending) child.SetActive(!busy && !flow.State.ended);
            }
            bark.text = "穀皮 ×" + flow.BarkCount;
            objective.text = flow.Objective;
            cooldown.fillAmount = flow.Companion.CooldownRatio;
            var near = Interactable.Nearest(flow.Player.transform.position);
            prompt.text = near ? (TouchInput.Enabled ? "互動　" : "[E] ") + near.prompt : "";
            returnButton.gameObject.SetActive(flow.CanReturn && !busy);
            if (TouchInput.Enabled)
            {
                var p = (RectTransform)cooldown.transform; p.anchoredPosition = new Vector2(75, 250);
                root.transform.Find("CompanionPortrait").GetComponent<RectTransform>().anchoredPosition = p.anchoredPosition;
                root.transform.Find("CompanionPortraitFrame").GetComponent<RectTransform>().anchoredPosition = p.anchoredPosition;
            }
        }
        void Layout()
        {
            if (touch == TouchInput.Enabled) return;
            touch = TouchInput.Enabled;
            for (int i = 0; i < 3; i++) skills[i].image.sprite = touch ? circle : diamond;
        }
        public void ShowEnding()
        {
            if (ending) ending.SetActive(true);
            if (ScrollUI.I) ScrollUI.I.ExternalUI = true;
        }
        Image Icon(string name, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root.transform, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = anchor; r.anchoredPosition = pos; r.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = name.StartsWith("Skill"); return img;
        }
        TMP_Text Label(string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize)
        {
            var img = Icon(name, anchor, pos, size, null, new Color(0.08f, 0.13f, 0.11f, 0.8f));
            return ChildLabel(img.transform, "", fontSize);
        }
        TMP_Text ChildLabel(Transform parent, string text, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(4, 4); r.offsetMax = new Vector2(-4, -4);
            var label = go.GetComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; return label;
        }
        Button ActionButton(string name, string text, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var image = Icon(name, anchor, pos, size, null, new Color(0.35f, 0.4f, 0.24f)); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); ChildLabel(image.transform, text, 25); return button;
        }
        static Sprite Shape(int shape)
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[4096];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float a = (x - 31.5f) / 30f, b = (y - 31.5f) / 30f;
                float q = a * a + b * b - 0.75f;
                bool inside = shape == 0 ? a * a + b * b <= 1 : shape == 2 ? Mathf.Abs(a) + Mathf.Abs(b) <= 1 : q * q * q - a * a * b * b * b <= 0;
                pixels[y * 64 + x] = inside ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels); tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }
    }
}
