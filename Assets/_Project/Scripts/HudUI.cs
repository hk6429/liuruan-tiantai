using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Liuruan
{
    public sealed class HudUI : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Sprite peachFull, peachHalf, peachEmpty, barkIcon, skillBark, skillWater, skillCall, portraitImage, portraitFrame;
        GameFlow flow;
        GameObject root, ending, skipPanel;
        Button helpButton;
        Image[] peaches = new Image[5];
        Image cooldown;
        TMP_Text bark, objective, prompt, controls;
        Button returnButton;
        Sprite circle;
        TMP_Text endingText, birds;
        EpilogueModel endingModel;
        int endingPage;
        readonly System.Collections.Generic.List<Button> endingOptions = new System.Collections.Generic.List<Button>();
        public void ShowBirds() { birdsUntil = Time.unscaledTime + 5; }
        float birdsUntil;
        void Start()
        {
            flow = GetComponent<GameFlow>();
            ScrollUI.I.JourneyHud = true;
            circle = Shape(0);
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
                peaches[i] = Icon("Peach" + i, new Vector2(0, 1), new Vector2(38 + i * 38, -38), new Vector2(31, 35), peachFull, Color.white);
            }
            Icon("BarkIcon", new Vector2(0, 1), new Vector2(33, -78), new Vector2(30, 30), barkIcon, Color.white);
            bark = Label("Bark", new Vector2(0, 1), new Vector2(140, -78), new Vector2(180, 36), 25);
            objective = Label("Objective", new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(750, 88), 26);
            string[] names = { "剝皮", "取水", "呼喚" };
            string[] keys = { "F", "R", "G" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var image = Icon("Skill" + keys[i], new Vector2(1, 1), new Vector2(-218 + i * 82, -63), new Vector2(76, 76), new[] { skillBark, skillWater, skillCall }[i], Color.white);
                var button = image.gameObject.AddComponent<Button>();
                button.onClick.AddListener(() => flow.UseSkill(index));
                ChildLabel(image.transform, names[i] + "\n" + keys[i], 19);
            }
            var portrait = Icon("CompanionPortraitFrame", new Vector2(0, 0), new Vector2(75, 78), new Vector2(108, 108), portraitFrame, Color.white);
            cooldown = Icon("CallCooldown", new Vector2(0, 0), new Vector2(75, 78), new Vector2(108, 108), circle, new Color(0.96f, 0.72f, 0.3f));
            cooldown.type = Image.Type.Filled; cooldown.fillMethod = Image.FillMethod.Radial360; cooldown.fillClockwise = true;
            Icon("CompanionPortrait", new Vector2(0, 0), new Vector2(75, 78), new Vector2(88, 88), portraitImage, Color.white);
            portrait.transform.SetAsLastSibling();
            var map = Icon("MinimapFrame", new Vector2(1, 0), new Vector2(-104, 104), new Vector2(184, 184), null, Color.clear);
            if (flow.minimap) flow.minimap.HudFrame = (RectTransform)map.transform;
            controls = Label("Controls", new Vector2(.5f, 0), new Vector2(0, 35), new Vector2(720, 34), 18);
            controls.text = TouchInput.Enabled ? "方向盤移動　滑動畫面轉視角　互動鈕操作　? 開啟說明" : "WASD／方向鍵移動　滑鼠右鍵轉視角　E 互動　H／? 開啟說明";
            prompt = Label("InteractPrompt", new Vector2(0.5f, 0), new Vector2(0, 84), new Vector2(450, 42), 26);
            returnButton = ActionButton("ReturnHome", "求歸", new Vector2(0.5f, 0), new Vector2(0, 145), new Vector2(160, 52));
            returnButton.onClick.AddListener(flow.ReturnHome);
            helpButton = ActionButton("Help", "?", new Vector2(0, 1), new Vector2(32, -128), new Vector2(48, 44));
            helpButton.onClick.AddListener(() => ScrollUI.I.ShowHelp());
            var recover = ActionButton("Recover", "回本幕起點", new Vector2(0, 1), new Vector2(155, -128), new Vector2(180, 44));
            recover.onClick.AddListener(flow.RecoverToStart);
            var skip = ActionButton("Skip", "跳過本幕", new Vector2(0, 1), new Vector2(155, -178), new Vector2(180, 44));
            skip.onClick.AddListener(() => { skipPanel.SetActive(true); ScrollUI.I.ExternalUI = true; });
            skipPanel = new GameObject("SkipConfirm", typeof(RectTransform), typeof(Image));
            skipPanel.transform.SetParent(root.transform, false);
            var sr = (RectTransform)skipPanel.transform; sr.anchorMin = new Vector2(.25f, .35f); sr.anchorMax = new Vector2(.75f, .65f); sr.offsetMin = sr.offsetMax = Vector2.zero;
            skipPanel.GetComponent<Image>().color = new Color(.1f, .15f, .12f, .98f);
            ChildLabel(skipPanel.transform, "確定跳過本幕？未完成的收集與作答不計分。", 23).rectTransform.offsetMin = new Vector2(15, 80);
            foreach (bool confirm in new[] { false, true })
            {
                var button = ActionButton(confirm ? "ConfirmSkip" : "CancelSkip", confirm ? "確定跳過" : "繼續本幕", new Vector2(.5f, 0), new Vector2(confirm ? 125 : -125, 40), new Vector2(210, 50));
                button.transform.SetParent(skipPanel.transform, false);
                button.onClick.AddListener(() => { skipPanel.SetActive(false); ScrollUI.I.ExternalUI = false; if (confirm) flow.SkipAct(); });
            }
            skipPanel.SetActive(false);
            birds = Label("Birds", new Vector2(.5f, 1), new Vector2(0, -230), new Vector2(250, 36), 21);
            birds.text = "百鳥鳴呼";
            ending = new GameObject("JourneyEnding", typeof(RectTransform), typeof(Image));
            ending.transform.SetParent(root.transform, false);
            var er = (RectTransform)ending.transform; er.anchorMin = new Vector2(0.05f, 0.05f); er.anchorMax = new Vector2(0.95f, 0.95f); er.offsetMin = er.offsetMax = Vector2.zero;
            ending.GetComponent<Image>().color = new Color(0.13f, 0.18f, 0.22f, 0.98f);
            endingText = ChildLabel(ending.transform, "", 25);
            var textRect = (RectTransform)endingText.transform;
            textRect.offsetMin = new Vector2(35, 100); textRect.offsetMax = new Vector2(-35, -30);
            endingText.alignment = TextAlignmentOptions.TopLeft;
            EndingButton("EndingPrev", "上一頁", new Vector2(-260, 40), () => TurnEnding(-1));
            EndingButton("EndingNext", "下一頁", new Vector2(0, 40), () => TurnEnding(1));
            EndingButton("Restart", "重新開始", new Vector2(260, 40), GameFlow.RestartJourney);
            for (int i = 0; i < 3; i++)
            {
                int answer = i;
                var button = EndingButton("EndingOption" + i, "", new Vector2(0, 340 - i * 90), () => AnswerEnding(answer));
                ((RectTransform)button.transform).sizeDelta = new Vector2(950, 75);
                endingOptions.Add(button);
            }
            ending.SetActive(flow.State.ended);
            flow.Hunger.OnChanged += DrawHunger; DrawHunger(flow.Hunger.Value);
            if (flow.State.ended) ShowEnding();
        }
        void OnDestroy()
        {
            if (flow && flow.Hunger) flow.Hunger.OnChanged -= DrawHunger;
            foreach (var sprite in new[] { circle }) if (sprite) { Destroy(sprite.texture); Destroy(sprite); }
        }
        void DrawHunger(float value)
        {
            for (int i = 0; i < 5; i++)
            {
                float fill = Mathf.Clamp01(value - i);
                peaches[i].sprite = fill >= 1 ? peachFull : fill > 0 ? peachHalf : peachEmpty;
                peaches[i].color = Color.white;
            }
        }
        void LateUpdate()
        {
            if (!root) return;
            bool busy = flow.Reading || flow.journey.Busy || (ScrollUI.I && ScrollUI.I.IsBusy);
            for (int i = 0; i < root.transform.childCount; i++)
            {
                var child = root.transform.GetChild(i).gameObject;
                if (child != ending && child != skipPanel) child.SetActive((child == helpButton.gameObject || !busy) && !flow.State.ended);
            }
            helpButton.interactable = !skipPanel.activeSelf && !(ScrollUI.I && ScrollUI.I.ExternalUI);
            bark.text = "穀皮 ×" + flow.BarkCount;
            objective.text = flow.Objective;
            cooldown.fillAmount = flow.Companion.CooldownRatio;
            controls.text = TouchInput.Enabled ? "方向盤移動　滑動畫面轉視角　互動鈕操作　? 開啟說明" : "WASD／方向鍵移動　滑鼠右鍵轉視角　E 互動　H／? 開啟說明";
            bool tall = Screen.height > Screen.width;
            root.GetComponent<CanvasScaler>().referenceResolution = tall ? new Vector2(720,1280) : new Vector2(1280,720);
            var o = (RectTransform)objective.transform.parent; o.anchoredPosition = new Vector2(0,tall ? -140 : -46); o.sizeDelta = new Vector2(tall ? 680 : 750, tall ? 110 : 88);
            objective.enableAutoSizing = true; objective.fontSizeMin = 24; objective.fontSizeMax = 26;
            objective.transform.parent.gameObject.SetActive(!flow.State.ended);
            var near = Interactable.Nearest(flow.Player.transform.position);
            prompt.text = near ? (TouchInput.Enabled ? "互動　" : "[E] ") + near.prompt : "";
            prompt.transform.parent.gameObject.SetActive(!busy && !string.IsNullOrEmpty(prompt.text));
            returnButton.gameObject.SetActive(flow.CanReturn && !busy);
            birds.gameObject.SetActive(!flow.State.ended && Time.unscaledTime < birdsUntil);
            if (TouchInput.Enabled)
            {
                var p = (RectTransform)cooldown.transform; p.anchoredPosition = new Vector2(75, 250);
                root.transform.Find("CompanionPortrait").GetComponent<RectTransform>().anchoredPosition = p.anchoredPosition;
                root.transform.Find("CompanionPortraitFrame").GetComponent<RectTransform>().anchoredPosition = p.anchoredPosition;
            }
        }
        public void ShowEnding()
        {
            if (!ending) return;
            endingModel = flow.EndingModel();
            if (endingModel == null) return;
            endingPage = -(flow.journey.GuidePages?.Length ?? 0); DrawEnding(); ending.SetActive(true);
            if (ScrollUI.I) ScrollUI.I.ExternalUI = true;
        }
        Button EndingButton(string name, string text, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var button = ActionButton(name, text, new Vector2(.5f, 0), position, new Vector2(210, 50));
            button.transform.SetParent(ending.transform, false); button.onClick.AddListener(action); return button;
        }
        void TurnEnding(int delta)
        { endingPage = Mathf.Clamp(endingPage + delta, -(flow.journey.GuidePages?.Length ?? 0), endingModel.Paragraphs.Count + endingModel.Comparison.Count + 1); DrawEnding(); }
        void DrawEnding()
        {
            int paragraphs = endingModel.Paragraphs.Count;
            foreach (var button in endingOptions) button.gameObject.SetActive(endingPage == paragraphs);
            if (endingPage < 0) endingText.text = "結局導讀\n\n" + flow.journey.GuidePages[endingPage + flow.journey.GuidePages.Length];
            else if (endingPage < paragraphs) endingText.text = "全文　" + (endingPage + 1) + "／" + paragraphs + "\n\n" + endingModel.Paragraphs[endingPage];
            else if (endingPage == paragraphs)
            {
                endingText.text = endingModel.Question.Prompt;
                for (int i = 0; i < endingOptions.Count; i++) endingOptions[i].GetComponentInChildren<TMP_Text>().text = endingModel.Question.Options[i];
            }
            else if (endingPage <= paragraphs + endingModel.Comparison.Count)
            {
                var row = endingModel.Comparison[endingPage - paragraphs - 1];
                endingText.text = "與〈桃花源記〉比較　" + (endingPage - paragraphs) + "／" + endingModel.Comparison.Count + "\n\n" + row.Aspect + "\n\n〈劉晨阮肇入天台〉\n" + row.Liuruan + "\n\n〈桃花源記〉\n" + row.Taohua;
            }
            else
            {
                var score = endingModel.Score;
                endingText.text = "成績卡\n\n穀皮 " + score.BarkCount + "／5\n漂流物 " + score.DriftCount + "／3\n閱讀首答 " + score.ReadingCorrect + "／" + score.ReadingTotal + "（" + (score.ReadingAccuracy * 100).ToString("F0") + "%）\n有效遊玩 " + score.TotalSeconds.ToString("F0") + " 秒";
            }
        }
        void AnswerEnding(int answer)
        {
            bool correct = answer == endingModel.Question.AnswerIndex;
            QuizStats.Record(QuizStats.InferId, correct);
            endingText.text = endingModel.Question.Prompt + "\n" + (correct ? "答對了。" : "再想一想。") + "\n" + endingModel.Question.Explanation;
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
