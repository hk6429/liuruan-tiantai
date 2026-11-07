using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Liuruan
{
    /// 所有 UI（以程式建立）：互動提示、Toast、卷軸問答、線索冊、操作說明、
    /// 結局（證據卡 → 推論題 → 反思 → 成績卡；選文全文從成績卡進入）。
    /// 長文固定可讀字級，超過一頁用「上一頁／下一頁」分頁。
    public class ScrollUI : MonoBehaviour
    {
        public static ScrollUI I { get; private set; }

        [SerializeField] TMP_FontAsset font;
        public float charInterval = 0.06f;

        static readonly Color Paper = new Color(0.96f, 0.91f, 0.78f, 1f);
        static readonly Color Ink = new Color(0.22f, 0.16f, 0.10f);
        static readonly Color Brown = new Color(0.50f, 0.33f, 0.18f);
        static readonly Color BtnColor = new Color(0.86f, 0.76f, 0.56f);
        static readonly Color WrongColor = new Color(0.85f, 0.55f, 0.50f);

        public const string HelpSeenKey = "liuruan_help_seen";
        const string BigFontKey = "liuruan_bigfont";      // 舊版兩段字級（只在沒有新設定時讀一次，相容）
        const string FontLevelKey = "liuruan_fontlevel";  // 0 標準／1 大／2 特大
        const float TouchScale = 1.4f;   // 觸控裝置字級倍率
        static readonly float[] LevelScale = { 1f, 1.2f, 1.4f };
        // 最小字級（參考解析度 1920×1080 單位）。觸控值以最壞情況 iPhone SE 橫式（667×375 CSS px，縮放約 0.347）換算：
        // 35 ≈ 12 CSS px、40 ≈ 14 CSS px。審查建議桌機下限 22。
        const float MinDesk = 16f, MinTouch = 22f;          // 一般文字（說明、成績、按鈕）
        const float UserMinDesk = 22f, UserMinTouch = 35f;  // 選項、回饋
        const float PageMinDesk = 24f, PageMinTouch = 40f;  // 分頁正文（不自動縮字）

        GameObject touchPad, lookArea;
        GameObject promptBox, toastBox, scrollPanel, bookPanel, endingPanel, sayBox, inkBox, fullPanel, helpPanel;
        GameObject scoreBtnGo;
        readonly List<GameObject> topRow = new List<GameObject>(); // 右上按鈕列（成績鈕另外判斷）
        RectTransform hudBg;
        TMP_Text promptText, toastText, hudText, titleText, originalText, questionText, feedbackText, bookText, sayText, muteText, qualityText, scoreText, fullText, fullTitle, helpText, endText;
        readonly List<TMP_Text> fontTexts = new List<TMP_Text>(); // 字級鈕文字：右上按鈕列＋卷軸標題列左側
        TMP_Text[] optionLabels;
        CanvasScaler scaler;
        int lastW, lastH, hudGot = -1, hudTotal = -1, hudTouch = -1, qualityShown = -1;
        int relayoutFrames;   // 螢幕尺寸改變後，等 CanvasScaler 更新再重算分頁（審查 #14）
        int typingFrame;      // 開始打字的那一幀：開卷那一下（E／點擊）不算跳過
        Pager readPager, bookPager, fullPager;
        readonly List<Pager> pagers = new List<Pager>();
        readonly HashSet<TMP_Text> paged = new HashSet<TMP_Text>();
        string readOrig, readPlain;   // 卷軸閱讀區：原文（可能含標亮）＋白話（按「看白話」後才有）
        bool collapsed;                // 答對後選項收起、回饋放大
        int correctSlot;
        Action fullDone, onEndingCollected;
        Image fadeImg;
        RectTransform inkFill;
        Button[] optionBtns;
        Button closeBtn, plainBtn, speakBtn, fullNextBtn;
        GameObject muteBtnGo; // 右上「朗讀：開／關」
        GameObject scrollFontGo;   // 卷軸標題列的「字級」鈕：只在觸控版顯示（桌機右上按鈕列開卷軸時仍在，見 ApplyLayout）
        Coroutine toastRoutine;
        bool typing, touchLayout, noScale;
        /// hotfix1：原文打字中（KeyNav 的空白鍵／Enter 這一下讓給「顯示全文」）
        public bool Typing => typing;
        
        int fontLevel;    // 0 標準／1 大／2 特大
        int[] order;      // 顯示位置 → 原選項索引（每次開卷打亂）
        int wrongCount;   // 本次開卷答錯次數
        ClueData current;
        // R17 任務層：ShowStage 進來的關卡（不記 QuizStats、用自訂標題、可延後展開原文）
        bool stageMode, stageFirst, stageHideOrig;
        string stageTitle;
        /// 任務模式的 HUD 文字（null＝經典模式的卷數 HUD）
        public string HudOverride;
        string hudShown;
        GameObject bookBtnGo;

        // 版面：桌機錨點與觸控錨點；字級：最小、基準最大、是否受「字級」切換影響
        readonly List<(RectTransform rt, Vector4 desk, Vector4 touch)> layouts = new List<(RectTransform, Vector4, Vector4)>();
        readonly Dictionary<TMP_Text, (float min, float max, bool user)> sizes = new Dictionary<TMP_Text, (float, float, bool)>();

        /// 線索演出進行中（鎖住玩家操作）
        public bool Cinematic;
        public bool ExternalUI;
        public bool JourneyHud;
        public TMP_FontAsset Font => font;
        bool AnyPanel => scrollPanel.activeSelf || bookPanel.activeSelf || endingPanel.activeSelf || fullPanel.activeSelf || helpPanel.activeSelf;
        public bool IsBusy => Cinematic || ExternalUI || AnyPanel;
        /// 有閱讀面板開著（RenderGovernor 用來暫停 3D 主相機）。操作說明不算：開場要看得到園子。
        public bool PanelOpen => scrollPanel.activeSelf || bookPanel.activeSelf || endingPanel.activeSelf || fullPanel.activeSelf;
        public static float TextScale => I == null ? 1f : (I.touchLayout ? TouchScale : 1f) * UserScale;
        public static float UserScale => I == null ? 1f : LevelScale[I.fontLevel];
        public static int FontLevel => I == null ? 0 : I.fontLevel;
        public static void CycleFont() { if (I) I.ToggleFont(); }

        /// 啟動時解析字級（純函式，staging/Round7 有 scratch 測試）：有新鍵就用新鍵（夾在 0..count-1）；
        /// 沒有新鍵才看舊鍵 liuruan_bigfont（1 → 大，其餘 → 標準）。不回寫新鍵：玩家按「字級」時才寫。
        internal static int ResolveFontLevel(bool hasLevel, int level, int legacyBig, int count) => hasLevel ? (level < 0 ? 0 : level >= count ? count - 1 : level) : (legacyBig == 1 ? 1 : 0);

        void Awake()
        {
            I = this;
            fontLevel = ResolveFontLevel(PlayerPrefs.HasKey(FontLevelKey), PlayerPrefs.GetInt(FontLevelKey, 0),
                PlayerPrefs.GetInt(BigFontKey, 0), LevelScale.Length);
            Build();
            ApplyScale();
        }

        void Start()
        {
            // 不支援朗讀（LINE／FB 內建瀏覽器的部分 WebView、編輯器）：鈕按了也沒作用 → 移出按鈕列並隱藏
            if (!SpeechBridge.Supported || !AudioManager.I) { topRow.Remove(muteBtnGo); muteBtnGo.SetActive(false); }
            SyncReadButton();
            if (QualityToggle.I) qualityText.text = QualityToggle.I.High ? Txt.QualityHigh : Txt.QualityLow;
            SpeechBridge.Init();
            SpeechBridge.HideLoading();
            if (!DeferHelp && PlayerPrefs.GetInt(HelpSeenKey, 0) == 0) ShowHelp();
        }

        /// R17：開場先出任務選單時延後操作說明（選經典模式才呼叫 MaybeShowHelp）
        public static bool DeferHelp;
        public void MaybeShowHelp() { if (PlayerPrefs.GetInt(HelpSeenKey, 0) == 0) ShowHelp(); }

        /// R17 通用分頁面板（任務說明）
        public void ShowPagedPublic(string title, string body, string doneLabel, Action done) => ShowPaged(title, body, doneLabel, done);

        /// R17 任務關卡（choice／who／appraise，或演出後的選擇題）：沿用卷軸版面，不記經典成績。
        /// done(firstTry)：一次答對為 true。hideOrig：作答前不顯示原文（原文會露出答案時）。
        public void ShowStage(ClueData clue, string title, string questId, string id, bool hideOrig, Texture image, Action<bool> done)
        {
            stageMode = true; stageFirst = true; stageHideOrig = hideOrig && !string.IsNullOrEmpty(clue.original);
            stageTitle = title;
            SetStageImage(image);
            ShowClue(clue, () => { bool f = stageFirst; stageMode = false; stageHideOrig = false; SetStageImage(null); done(f); });
        }

        // R17-U：任務關卡的情境插圖（quests.json 的 image）。放在閱讀區右上，原文欄讓出右側；沒有圖就是原本版面。
        RectTransform stageImgFrame;
        RawImage stageImg;
        AspectRatioFitter stageImgFit;
        static readonly Vector4 OrigDesk = new Vector4(0f, 0.52f, 0.86f, 0.89f), OrigTouch = new Vector4(0f, 0.535f, 0.83f, 0.865f);
        static readonly Vector4 OrigDeskImg = new Vector4(0f, 0.52f, 0.56f, 0.89f), OrigTouchImg = new Vector4(0f, 0.535f, 0.54f, 0.865f);

        void SetStageImage(Texture tex)
        {
            if (tex == null && (stageImgFrame == null || !stageImgFrame.gameObject.activeSelf)) return;
            if (tex != null && stageImgFrame == null)
            {
                stageImgFrame = Rect(scrollPanel.transform, "StageImage", new Vector2(0.575f, 0.655f), new Vector2(0.855f, 0.885f));
                T(stageImgFrame, 0.555f, 0.59f, 0.825f, 0.86f);
                stageImgFrame.gameObject.AddComponent<RectMask2D>();
                var inner = Rect(stageImgFrame, "Img", Vector2.zero, Vector2.one);
                stageImgFit = inner.gameObject.AddComponent<AspectRatioFitter>();
                stageImgFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                stageImg = inner.gameObject.AddComponent<RawImage>();
                stageImg.raycastTarget = false;
            }
            if (stageImgFrame != null) stageImgFrame.gameObject.SetActive(tex != null);
            if (tex != null) { stageImg.texture = tex; stageImgFit.aspectRatio = (float)tex.width / Mathf.Max(1, tex.height); }
            var ort = originalText.rectTransform;
            for (int i = 0; i < layouts.Count; i++)
                if (layouts[i].rt == ort) layouts[i] = (ort, tex != null ? OrigDeskImg : OrigDesk, tex != null ? OrigTouchImg : OrigTouch);
            ApplyLayout();
            relayoutFrames = 2;
        }

        void Update()
        {
            // 原文打字中：點擊／空白鍵／Enter／E／觸控／觸控互動鍵 → 第一次按直接顯示全文（不會連帶進下一步）
            if (typing && Time.frameCount > typingFrame && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0) || TouchInput.ConsumeDown())) typing = false;

            bool keys = true; // hotfix1：HTML 輸入框開著時鍵盤全部讓給輸入框
            if (keys && Input.GetKeyDown(KeyCode.M)) ToggleMute();
            bool othersOpen = scrollPanel.activeSelf || endingPanel.activeSelf || fullPanel.activeSelf || helpPanel.activeSelf;
            if (keys && !Cinematic && !ExternalUI && !othersOpen && (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.J)))
                ToggleBook(!bookPanel.activeSelf);
            if (keys && bookPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) ToggleBook(false);

            var cm = ClueManager.I;
            TouchInput.Poll();
            if (TouchInput.Enabled != touchLayout || Screen.width != lastW || Screen.height != lastH)
            {
                touchLayout = TouchInput.Enabled;
                lastW = Screen.width; lastH = Screen.height;
                ApplyLayout(); ApplyScale();
                relayoutFrames = 2; // CanvasScaler 下一幀才更新根尺寸：同一幀算的頁數是舊的，晚兩幀再算一次
            }
            else if (relayoutFrames > 0 && --relayoutFrames == 0)
            {
                foreach (var pg in pagers) pg.Refresh();
                FitHudBg();
            }
            if (HudOverride != null)
            {
                if (!ReferenceEquals(HudOverride, hudShown)) { hudShown = HudOverride; hudText.text = HudOverride; FitHudBg(); }
            }
            else if (cm)
            {
                // HUD 只在數字或操作模式改變時重建字串（避免每幀 GC）
                int got = cm.ScrollsCollected, total = cm.ScrollCount, touch = TouchInput.Enabled ? 1 : 0;
                // hotfix1：線索冊那行後面接「下一站：○○（往○○）」，方向跟著鏡頭變（每 0.25 秒重算一次）
                if (got != hudGot || total != hudTotal || touch != hudTouch)
                {
                    hudGot = got; hudTotal = total; hudTouch = touch;
                    hudText.text = touch == 1 ? Txt.HudTouch(got, total) : Txt.HudDesk(got, total);
                    hudShown = null;
                    FitHudBg();
                }
            }
            // 成績卡關掉後可叫回：結局收進線索冊（成績卡出現過）才顯示「成績」鈕
            // 觸控版全螢幕面板開著時收起右上按鈕列：面板佔滿畫面，按鈕列會疊在標題／朗讀／分頁鈕上（此時線索冊、成績鈕本來就不作用）
            bool yapai = false;
            var hudGo = hudText.transform.parent.gameObject;
            // R18：演出中（兩種版面）與觸控版任何面板／外部 UI 開著時也收起 HUD（原本會從半透明底透出來）
            bool hudOn = !JourneyHud && !yapai && !Cinematic && !(touchLayout && IsBusy);
            if (hudGo.activeSelf != hudOn) hudGo.SetActive(hudOn);
            bool rowOn = !JourneyHud && !(touchLayout && PanelOpen) && !yapai;
            foreach (var g in topRow) if (g.activeSelf != rowOn) g.SetActive(rowOn);
            bool showScore = rowOn && cm && cm.IsCollected(ClueManager.EndingId);
            if (scoreBtnGo && scoreBtnGo.activeSelf != showScore) scoreBtnGo.SetActive(showScore);
            // 面板／成績卡／演出時收掉背後的「E 查看」：PlayerController 忙碌時不再呼叫 SetPrompt，提示會殘留
            if (promptBox.activeSelf && IsBusy) promptBox.SetActive(false);
            // 畫質鈕文字跟著 QualityToggle（觸控後自動降為低畫質時同步）
            int q = QualityToggle.I ? (QualityToggle.I.High ? 1 : 0) : -1;
            if (q != qualityShown) { qualityShown = q; if (q >= 0) qualityText.text = q == 1 ? Txt.QualityHigh : Txt.QualityLow; }
            bool padOn = TouchInput.Enabled && !IsBusy;
            if (lookArea.activeSelf != (TouchInput.Enabled && !IsBusy)) lookArea.SetActive(TouchInput.Enabled && !IsBusy);
            if (touchPad.activeSelf != padOn) touchPad.SetActive(padOn);
        }

        public void SetPrompt(string text)
        {
            if (JourneyHud) { promptBox.SetActive(false); return; }
            promptBox.SetActive(!string.IsNullOrEmpty(text) && !IsBusy);
            if (promptBox.activeSelf) promptText.text = TouchInput.Enabled && text.StartsWith("E ") ? Txt.TouchPromptPrefix + text.Substring(2) : text;
        }

        public void Toast(string text)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(text));
        }

        IEnumerator ToastRoutine(string text)
        {
            toastText.text = text;
            toastBox.SetActive(true);
            yield return new WaitForSeconds(4f);
            toastBox.SetActive(false);
        }

        // ---------- 卷軸問答＋分派 ----------

        /// review = 已收集重讀：有選擇題的 match / minigame 直接看卷軸，不再重玩
        public void ShowClue(ClueData clue, Action onCorrect, bool review = false)
        {
            promptBox.SetActive(false);
            bookPanel.SetActive(false);
            sayBox.SetActive(false);
            AudioManager.Play(AudioManager.I ? AudioManager.I.whoosh : null);
            SpeechBridge.Stop();
            StopAllCoroutines();
            toastBox.SetActive(false);

            scrollPanel.SetActive(true);
            StartCoroutine(ClueRoutine(clue, onCorrect, review && clue.HasQuiz, review));
        }

        static string TitleOf(ClueData c)
        {
            if (c.id == QuizStats.InferId) return Txt.InferTitle;
            var cm = ClueManager.I;
            return $"{(cm ? cm.LabelOf(c) : Txt.RollPrefix)}·{c.title}";
        }

        /// skipStage = 不進配對（重讀或配對已完成），直接到選擇題；review = 已收集重讀：只看卷軸、不再出題（Round14）
        IEnumerator ClueRoutine(ClueData clue, Action onCorrect, bool skipStage, bool review)
        {
            current = clue;
            collapsed = false;
            ApplyLayout(); // 還原上一卷收起選項時改過的錨點
            titleText.text = stageMode ? stageTitle : TitleOf(clue);
            readOrig = stageHideOrig ? Txt.StageOrigHidden : clue.original ?? "";
            readPlain = null;
            RenderReading(true);
            questionText.text = "";
            feedbackText.text = "";
            plainBtn.gameObject.SetActive(false);
            speakBtn.gameObject.SetActive(!stageHideOrig && CanSpeak(clue));
            closeBtn.gameObject.SetActive(false);
            foreach (var b in optionBtns) b.gameObject.SetActive(false);

            // 打字中按點擊／空白鍵／Enter／E／觸控 → 立刻顯示全文（Update 把 typing 設 false）；之後的步驟仍要再按按鈕。
            // 依 deltaTime 累加，不再每個字 new WaitForSeconds（審查 #17：WebGL GC）
            typing = !skipStage;
            typingFrame = Time.frameCount;
            originalText.maxVisibleCharacters = 0;
            originalText.ForceMeshUpdate();
            var ti = originalText.textInfo;
            int total = ti.pageCount > 1 ? ti.pageInfo[0].lastCharacterIndex + 1 : ti.characterCount;
            float acc = 0f;
            for (int shown = 0; typing && shown < total;)
            {
                yield return null;
                acc += Time.deltaTime;
                while (acc >= charInterval && shown < total) { acc -= charInterval; shown++; }
                originalText.maxVisibleCharacters = shown;
            }
            typing = false;
            originalText.maxVisibleCharacters = 99999;

            // 先讀原文：白話收在按鈕後面
            yield return new WaitForSeconds(0.4f);
            plainBtn.gameObject.SetActive(!stageHideOrig && !string.IsNullOrEmpty(clue.plain));

            // 重讀：不再出題、不重記成績，只留「關閉」（Round14）
            if (review)
            {
                ShowCloseBtn(Txt.Close, onCorrect);
                yield break;
            }

            if (!clue.HasQuiz) // 資料沒有題目：直接收卷
            {
                ShowCloseBtn(ClueManager.IsEnding(clue.id) || clue.id == QuizStats.InferId ? Txt.Continue : Txt.Collect, onCorrect);
                yield break;
            }

            // 多頁卷軸：翻到最後一頁（含按「看白話」跳過去）才出題；之前題目區換成提示（Round14）
            if (!readPager.OnLast)
            {
                questionText.text = Txt.ReadToEndHint;
                while (!readPager.OnLast)
                {
                    if (!scrollPanel.activeSelf) yield break;
                    yield return null;
                }
            }
            questionText.text = Txt.QuestionPrefix + clue.question;
            wrongCount = 0;
            order = new int[Mathf.Min(clue.options.Length, optionBtns.Length)];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (int i = 0; i < optionBtns.Length; i++)
            {
                var btn = optionBtns[i];
                bool show = i < order.Length;
                btn.gameObject.SetActive(show);
                if (!show) continue;
                int slot = i, idx = order[i];
                btn.interactable = true;
                btn.image.color = BtnColor;
                optionLabels[i].text = $"{Txt.OptionLetters[i]}. {clue.options[idx]}";
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => Answer(clue, slot, idx, onCorrect));
            }
        }

        void ShowCloseBtn(string label, Action onClick)
        {
            closeBtn.GetComponentInChildren<TMP_Text>().text = label;
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(() => { SpeechBridge.Stop(); scrollPanel.SetActive(false); onClick(); });
            closeBtn.gameObject.SetActive(true);
        }

        void ShowPlain()
        {
            if (current == null || string.IsNullOrEmpty(current.plain)) return;
            plainBtn.gameObject.SetActive(false);
            readPlain = current.plain;
            RenderReading(false);
            JumpToPlain();
            if (!stageMode) QuizStats.MarkPlain(current.id);
        }

        /// 閱讀區＝原文＋（白話），同一個分頁文字框
        void RenderReading(bool firstPage)
        {
            originalText.text = readPlain == null ? readOrig
                : $"{readOrig}<page><color=#1F5C5C>{Txt.PlainPrefix}{readPlain}</color>"; // Round14：白話另起一頁、深青色
            if (firstPage) originalText.pageToDisplay = 1;
            readPager.Refresh();
        }

        /// 翻到白話開頭所在的頁
        void JumpToPlain()
        {
            if (readPlain == null) return;
            originalText.ForceMeshUpdate();
            var ti = originalText.textInfo;
            int idx = Mathf.Clamp(ti.characterCount - (Txt.PlainPrefix.Length + StripTags(readPlain).Length), 0, Mathf.Max(0, ti.characterCount - 1));
            if (ti.characterCount > 0) originalText.pageToDisplay = ti.characterInfo[idx].pageNumber + 1;
            readPager.Refresh();
        }

        void JumpToChar(int idx)
        {
            originalText.ForceMeshUpdate();
            var ti = originalText.textInfo;
            if (ti.characterCount <= 0) return;
            idx = Mathf.Clamp(idx, 0, ti.characterCount - 1);
            originalText.pageToDisplay = ti.characterInfo[idx].pageNumber + 1;
            readPager.Refresh();
        }

        static string StripTags(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]+>", "");

        void Answer(ClueData clue, int slot, int idx, Action onCorrect)
        {
            bool correct = idx == clue.answer;
            if (!stageMode) QuizStats.Record(clue.id, correct);
            else if (!correct) stageFirst = false;
            string label = $"{Txt.OptionLetters[slot]}. {clue.options[idx]}";
            if (!correct)
            {
                optionBtns[slot].interactable = false;
                optionBtns[slot].image.color = WrongColor;
                optionLabels[slot].text = $"{Txt.WrongMark}<s>{label}</s>"; // 色盲也分得出：×＋刪除線
                AudioManager.Play(AudioManager.I ? AudioManager.I.wrong : null);
                var hints = clue.wrongHints;
                bool hasPlain = !string.IsNullOrEmpty(clue.plain);
                feedbackText.text = hints != null && idx < hints.Length && !string.IsNullOrEmpty(hints[idx])
                    ? hints[idx]
                    : !hasPlain ? Txt.WrongTryAgain
                    : plainBtn.gameObject.activeSelf ? Txt.WrongSeePlainBtn
                    : Txt.WrongReadPlain;
                if (++wrongCount == 2) { RevealStageOrig(clue); HighlightKey(clue); }
                return;
            }
            foreach (var b in optionBtns) b.interactable = false;
            optionBtns[slot].image.color = new Color(0.6f, 0.8f, 0.55f);
            optionLabels[slot].text = Txt.CorrectMark + label;
            AudioManager.Play(AudioManager.I ? AudioManager.I.correct : null);
            feedbackText.text = (stageMode ? Txt.StageCorrect : "") + (clue.feedback ?? "");
            if (stageMode && stageHideOrig) { RevealStageOrig(clue); RenderReading(true); }
            correctSlot = slot;
            collapsed = true;
            ApplyCollapse();
            ShowCloseBtn(stageMode || ClueManager.IsEnding(clue.id) || clue.id == QuizStats.InferId ? Txt.Continue : Txt.Collect, onCorrect);
        }

        /// R17：作答後（或答錯兩次）才展開原文
        void RevealStageOrig(ClueData clue)
        {
            if (!stageHideOrig) return;
            stageHideOrig = false;
            readOrig = clue.original ?? "";
            speakBtn.gameObject.SetActive(CanSpeak(clue));
            if (!string.IsNullOrEmpty(clue.plain) && readPlain == null) plainBtn.gameObject.SetActive(true);
        }

        bool CanSpeak(ClueData c) => !string.IsNullOrEmpty(c.original) &&
            SpeakAllowed;

        // 答對後：其他選項收起，正解移到第一列全寬，回饋框放大到原本選項區（回饋不再擠在 6% 高的小框）
        static readonly Vector4 CorrectDesk = new Vector4(0.06f, 0.358f, 0.94f, 0.415f), CorrectTouch = new Vector4(0.02f, 0.33f, 0.82f, 0.41f);
        static readonly Vector4 FeedbackDesk = new Vector4(0f, 0.09f, 1f, 0.35f), FeedbackTouch = new Vector4(0f, 0.14f, 0.83f, 0.325f);

        void ApplyCollapse()
        {
            ScaleQuizText();
            if (!collapsed) return;
            for (int i = 0; i < optionBtns.Length; i++)
                if (i != correctSlot) optionBtns[i].gameObject.SetActive(false);
            SetAnchors((RectTransform)optionBtns[correctSlot].transform, touchLayout ? CorrectTouch : CorrectDesk);
            SetAnchors(feedbackText.rectTransform, touchLayout ? FeedbackTouch : FeedbackDesk);
        }

        static void SetAnchors(RectTransform rt, Vector4 v)
        {
            rt.anchorMin = new Vector2(v.x, v.y);
            rt.anchorMax = new Vector2(v.z, v.w);
        }

        const string Mark = "<mark=#F2C94C66>";
        /// Round14：左上 HUD 白字底下墊半透明深色底，大小貼齊實際文字範圍（TMP 的 mark 會蓋在字上、讓白字變灰，不用）
        void FitHudBg()
        {
            if (!hudBg || string.IsNullOrEmpty(hudText.text)) return;
            hudText.ForceMeshUpdate();
            var b = hudText.textBounds;
            hudBg.anchoredPosition = b.center;
            hudBg.sizeDelta = (Vector2)b.size + new Vector2(28f, 14f);
        }

        /// 第二次答錯：自動打開白話並整句標亮；原文中標亮正解或題目引號內的字句。
        void HighlightKey(ClueData clue)
        {
            bool marked = false;
            int markAt = -1; // 原文標亮起點（去掉標籤後的字元序）
            var orig = clue.original ?? "";
            var keys = new List<string> { clue.options[clue.answer] };
            int a = clue.question.IndexOf('「'), b = clue.question.IndexOf('」');
            if (a >= 0 && b > a + 1) keys.Add(clue.question.Substring(a + 1, b - a - 1));
            foreach (var k in keys)
            {
                if (string.IsNullOrEmpty(k)) continue;
                int at = orig.IndexOf(k, StringComparison.Ordinal);
                if (at >= 0)
                {
                    markAt = StripTags(orig.Substring(0, at)).Length;
                    orig = orig.Insert(at + k.Length, "</mark>").Insert(at, Mark);
                    marked = true;
                    break;
                }
            }
            readOrig = orig;
            if (!string.IsNullOrEmpty(clue.plain))
            {
                plainBtn.gameObject.SetActive(false);
                readPlain = $"{Mark}{clue.plain}</mark>";
                if (!stageMode) QuizStats.MarkPlain(clue.id);
                marked = true;
            }
            RenderReading(true);
            // 翻到標亮所在頁（審查 #11：手機上白話可能在第 4–7 頁，停在第 1 頁會看不到）：原文有標亮就去那頁，否則去白話開頭
            if (markAt >= 0) JumpToChar(markAt); else JumpToPlain();
            if (marked) feedbackText.text += Txt.LookHighlight;
        }

        void Speak()
        {
            if (current == null) return;
            if (!SpeakAllowed) return;
            SpeechBridge.Speak(current.original);
        }

        /// 瀏覽器支援朗讀且「朗讀：開」（AudioManager.Muted 沿用舊名與舊 PlayerPrefs 鍵 liuruan_mute）
        static bool SpeakAllowed => SpeechBridge.Supported && !(AudioManager.I && AudioManager.I.Muted);

        // ---------- 演出用：字幕、淡入淡出、進度條、朗讀開關 ----------

        public void Say(string who, string line)
        {
            sayBox.SetActive(true);
            sayText.text = string.IsNullOrEmpty(who) ? line : $"<color=#E8C98A>{who}</color>　{line}";
        }

        public void HideSay() => sayBox.SetActive(false);

        public IEnumerator Fade(float to, float dur)
        {
            fadeImg.gameObject.SetActive(true);
            float from = fadeImg.color.a;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                fadeImg.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / dur));
                yield return null;
            }
            fadeImg.color = new Color(0, 0, 0, to);
            if (to <= 0) fadeImg.gameObject.SetActive(false);
        }

        public void SetInk(float v)
        {
            inkBox.SetActive(v >= 0);
            inkFill.anchorMax = new Vector2(Mathf.Clamp01(v), 1);
        }

        /// 「朗讀：開／關」鈕與 M 鍵共用。瀏覽器不支援 Web Speech（含編輯器）時整個功能不存在：鈕隱藏、M 鍵不動作。
        void ToggleMute()
        {
            if (!AudioManager.I || !SpeechBridge.Supported) return;
            AudioManager.I.ToggleMute();
            if (AudioManager.I.Muted) SpeechBridge.Stop();
            SyncReadButton();
        }

        void SyncReadButton()
        {
            if (AudioManager.I) muteText.text = AudioManager.I.Muted ? Txt.ReadOff : Txt.ReadOn;
            // 卷軸開著時切換：「朗讀」鈕跟著出現／消失（關了就不留一顆按了沒聲音的鈕）
            if (speakBtn && current != null && scrollPanel.activeSelf)
                speakBtn.gameObject.SetActive(!stageHideOrig && CanSpeak(current));
        }

        /// 字級：標準 → 大 → 特大 → 標準（PlayerPrefs 記住）
        void ToggleFont()
        {
            fontLevel = (fontLevel + 1) % LevelScale.Length;
            PlayerPrefs.SetInt(FontLevelKey, fontLevel);
            PlayerPrefs.Save();
            ApplyScale();
        }

        // ---------- 線索冊 ----------

        void ToggleBook(bool open)
        {
            bookPanel.SetActive(open);
            if (!open) return;
            var cm = ClueManager.I;
            if (!cm) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Txt.BookHeader(cm.ScrollsCollected, cm.ScrollCount));
            foreach (var c in cm.Clues)
            {
                if (c.id == ClueManager.EndingId && !cm.IsCollected(c.id)) continue;
                sb.AppendLine(cm.IsCollected(c.id)
                    ? $"<color=#7F542E><b>{cm.LabelOf(c)}·{c.title}</b></color>　{c.original}\n"
                    : $"<color=#9A8C78>{cm.LabelOf(c)}·{Txt.NotFound}</color>\n");
            }
            bookText.text = sb.ToString();
            bookText.pageToDisplay = 1;
            bookPager.Refresh();
        }

        // ---------- 操作說明 ----------

        void ShowHelp()
        {
            bookPanel.SetActive(false);
            int garden = ClueManager.I ? ClueManager.I.ScrollCount : 0;
            helpText.text = Txt.HelpTitle + (TouchInput.Enabled ? Txt.HelpTouch : Txt.HelpDesk) + Txt.HelpTask(garden) + Txt.Credits;
            helpPanel.SetActive(true);
        }

        void CloseHelp()
        {
            helpPanel.SetActive(false);
            PlayerPrefs.SetInt(HelpSeenKey, 1);
            PlayerPrefs.Save();
        }

        // ---------- 結局：選文 → 推論題 → 揭曉＋成績卡 ----------

        void ResetAll()
        {
            SpeechBridge.Stop();
            QuizStats.Clear();
            ClueManager.I.ResetProgress();
        }

        /// 結局流程（ClueManager 在集齊後觸發結局卷軸時呼叫）：
        /// 證據卡 → 結局推論題（作答）→ scenes.ending 揭曉（EndingDialogue）→ infer → 反思 → 成績卡。
        /// collect：成績卡出現前才呼叫（收進線索冊），中途重整會從頭再走一次結局。
        public void StartEnding(ClueData endClue, Action collect)
        {
            onEndingCollected = collect;
            promptBox.SetActive(false);
            toastBox.SetActive(false);
            var cm = ClueManager.I;
            var cards = cm ? cm.CollectedEvidence() : null;
            Action quiz = () => ShowClue(endClue, () => StartCoroutine(AfterEndingQuiz()));
            if (cards != null && cards.Count > 0) ShowPaged(Txt.EvidenceTitle, EvidenceBody(cards), Txt.StartInfer, quiz);
            else quiz();
        }

        /// 舊入口（相容）：直接從揭曉開始
        public void ShowEnding()
        {
            promptBox.SetActive(false);
            StopAllCoroutines();
            toastBox.SetActive(false);
            StartCoroutine(AfterEndingQuiz());
        }

        IEnumerator AfterEndingQuiz()
        {
            yield return null;
            var infer = InferClue();
            if (infer != null) ShowClue(infer, ShowReflection); else ShowReflection();
        }

        static string EvidenceBody(List<EvidenceCard> cards)
        {
            var cm = ClueManager.I;
            var sb = new System.Text.StringBuilder(Txt.EvidenceIntro);
            foreach (var e in cards)
            {
                string label = e.label;
                if (string.IsNullOrEmpty(label) && cm) { var c = cm.Find(e.clueId); label = c != null ? c.title : ""; }
                sb.Append($"<b><color=#7F542E>{label}</color></b>\n");
                if (!string.IsNullOrEmpty(e.quote)) sb.Append(e.quote).Append('\n');
                sb.Append('\n');
            }
            return sb.ToString();
        }

        void ShowReflection()
        {
            var r = ClueManager.I ? ClueManager.I.ReflectionData : null;
            if (r == null) { ShowScoreCard(); return; }
            var sb = new System.Text.StringBuilder($"<b>{r.question}</b>\n\n");
            if (r.prompts != null)
                foreach (var pr in r.prompts) if (!string.IsNullOrEmpty(pr)) sb.Append("・").Append(pr).Append('\n');
            sb.Append(Txt.ReflectionNote);
            ShowPaged(Txt.ReflectionTitle, sb.ToString(), Txt.ReflectionDone, ShowScoreCard);
        }

        /// Round11 宴後加演小結：分頁面板，最後一頁「回到成績卡」
        public void ShowPagedThenScore(string title, string body) => ShowPaged(title, body, Txt.BackToScore, ShowScoreCard);

        /// 通用分頁面板（證據卡、反思、選文全文）：最後一頁才出現完成鈕
        void ShowPaged(string title, string body, string doneLabel, Action done)
        {
            promptBox.SetActive(false);
            bookPanel.SetActive(false);
            fullPanel.SetActive(true);
            fullTitle.text = title;
            fullText.text = body;
            fullText.pageToDisplay = 1;
            fullDone = done;
            fullNextBtn.GetComponentInChildren<TMP_Text>().text = doneLabel;
            fullPager.Refresh();
        }

        string FullTextBody()
        {
            var sb = new System.Text.StringBuilder();
            var cm = ClueManager.I;
            if (!cm) return "";
            foreach (var c in cm.Clues)
            {
                if (c.id == ClueManager.EndingId || string.IsNullOrEmpty(c.original)) continue;
                sb.Append(sb.Length == 0 ? "　　" : "").Append(c.original);
            }
            var end = cm.Find(ClueManager.EndingId);
            if (end != null && !string.IsNullOrEmpty(end.original)) sb.Append("\n\n　　").Append(end.original);
            return sb.ToString();
        }

        /// 推論題（選填）：只有 clues.json 有 "infer" 區塊才出題；沒有就直接成績卡（結局線索本身可當推論題）
        ClueData InferClue()
        {
            var cm = ClueManager.I;
            var src = cm ? cm.InferData : null;
            if (src == null || !src.HasQuiz) return null;
            src.id = QuizStats.InferId;
            if (string.IsNullOrEmpty(src.original))
            {
                var titles = new List<string>();
                foreach (var c in cm.Clues) if (c.id != ClueManager.EndingId) titles.Add(c.title);
                src.original = Txt.InferOriginalPrefix + string.Join("、", titles);
            }
            return src;
        }

        void ShowScoreCard()
        {
            promptBox.SetActive(false); // 成績卡蓋不到下方的「E 查看」，直接收掉
            if (onEndingCollected != null) { var c = onEndingCollected; onEndingCollected = null; c(); }
            int total = QuizStats.ScoredCount(), first = QuizStats.FirstTryCount();
            string title, line;
            if (first == total) { title = Txt.ScoreTitleTop; line = Txt.ScoreLineTop; }
            else if (first >= total - 2) { title = Txt.ScoreTitleMid; line = Txt.ScoreLineMid; }
            else { title = Txt.ScoreTitleLow; line = Txt.ScoreLineLow; }
            if (ClueManager.I) endText.text = ClueManager.I.EndingText;
            scoreText.text =
                Txt.ScoreStats(first, total, QuizStats.NoPlainCount(), QuizStats.PlainScoredCount(), QuizStats.TotalTries()) +
                Txt.ScoreTitle(title, line) +
                Results() +
                Txt.ScoreShot;
            endingPanel.SetActive(true);
        }

        /// 逐題首答結果：老風流 ○　神仙托生 × …（沒作答顯示 －）
        static string Results()
        {
            var cm = ClueManager.I;
            if (!cm) return "";
            var sb = new System.Text.StringBuilder(Txt.ResultHeader);
            foreach (var c in cm.Clues)
                if (cm.IsScored(c.id)) sb.Append(c.title).Append(' ').Append(ResultMark(c.id)).Append("　");
            if (cm.IsScored(QuizStats.InferId)) sb.Append(Txt.InferShort).Append(' ').Append(ResultMark(QuizStats.InferId));
            return sb.Append(Txt.ResultTail).ToString();
        }

        static string ResultMark(int id) =>
            QuizStats.Tries(id) == 0 ? Txt.ResultNone : QuizStats.FirstTryCorrect(id) ? Txt.ResultRight : Txt.ResultWrong;

        // ---------- 分頁 ----------

        /// 文字框分頁（TMP overflowMode = Page）＋上一頁／下一頁鈕
        class Pager
        {
            public TMP_Text text, label;
            public Button prev, next;
            public Action onChange;

            int cached = 1; // Refresh 時算好的頁數：OnLast 不再重跑 ForceMeshUpdate（審查 #14：長文字尖峰）

            public int Count { get { text.ForceMeshUpdate(); return Mathf.Max(1, text.textInfo.pageCount); } }
            public bool OnLast => text.pageToDisplay >= cached;

            public void Go(int delta)
            {
                text.pageToDisplay = Mathf.Clamp(text.pageToDisplay + delta, 1, Count);
                Refresh();
            }

            public void Refresh()
            {
                if (!text.gameObject.activeInHierarchy) return;
                int n = cached = Count;
                int p = Mathf.Clamp(text.pageToDisplay, 1, n);
                text.pageToDisplay = p;
                bool multi = n > 1;
                prev.gameObject.SetActive(multi);
                next.gameObject.SetActive(multi);
                label.gameObject.SetActive(multi);
                prev.interactable = p > 1;
                next.interactable = p < n;
                label.text = Txt.PageOf(p, n);
                onChange?.Invoke();
            }
        }

        /// 建分頁鈕：容器錨點桌機 desk、觸控 touch；vertical＝上一頁／頁碼／下一頁直排
        Pager MakePager(Transform parent, string name, TMP_Text text, Vector4 desk, Vector4 touch, bool vertical)
        {
            var box = Rect(parent, name, new Vector2(desk.x, desk.y), new Vector2(desk.z, desk.w));
            T(box, touch.x, touch.y, touch.z, touch.w);
            var pg = new Pager { text = text };
            if (vertical)
            {
                // 上一頁／下一頁各佔 42%（觸控版容器高 0.31 面板 → 每顆約 0.13 面板高 ≈ 134 參考單位 ≈ 93px@1334×750）
                pg.prev = Btn(box, "Prev", new Vector2(0, 0.58f), Vector2.one, Txt.PrevPage);
                pg.label = Text(Rect(box, "Page", new Vector2(0, 0.43f), new Vector2(1, 0.57f)), 24, Brown, TextAlignmentOptions.Center);
                pg.next = Btn(box, "Next", Vector2.zero, new Vector2(1, 0.42f), Txt.NextPage);
            }
            else
            {
                pg.prev = Btn(box, "Prev", Vector2.zero, new Vector2(0.37f, 1), Txt.PrevPage);
                pg.label = Text(Rect(box, "Page", new Vector2(0.37f, 0), new Vector2(0.63f, 1)), 24, Brown, TextAlignmentOptions.Center);
                pg.next = Btn(box, "Next", new Vector2(0.63f, 0), Vector2.one, Txt.NextPage);
            }
            pg.prev.onClick.AddListener(() => pg.Go(-1));
            pg.next.onClick.AddListener(() => pg.Go(1));
            text.overflowMode = TextOverflowModes.Page;
            paged.Add(text);
            pagers.Add(pg);
            return pg;
        }

        // ---------- 版面與字級 ----------

        /// 登記觸控版面錨點（x0, y0, x1, y1）；桌機錨點取目前值
        RectTransform T(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            layouts.Add((rt, new Vector4(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y), new Vector4(x0, y0, x1, y1)));
            return rt;
        }

        RectTransform T(Component c, float x0, float y0, float x1, float y1) => T((RectTransform)c.transform, x0, y0, x1, y1);
        RectTransform T(GameObject g, float x0, float y0, float x1, float y1) => T((RectTransform)g.transform, x0, y0, x1, y1);

        void ApplyLayout()
        {
            foreach (var (rt, desk, touch) in layouts)
            {
                var v = touchLayout ? touch : desk;
                rt.anchorMin = new Vector2(v.x, v.y);
                rt.anchorMax = new Vector2(v.z, v.w);
            }
            // 觸控：依長寬比選「以寬或以高」中縮放較大的一邊（寬螢幕手機以寬為準），字不會被 0.5 折衷縮小
            if (scaler) scaler.matchWidthOrHeight = !touchLayout ? 0.5f
                : (Screen.height > 0 && (float)Screen.width / Screen.height > 1920f / 1080f ? 0f : 1f);
            ApplyPortraitHud();
            // 桌機開卷軸時右上按鈕列還在，卷軸標題列那顆是重複的；觸控版右上列會收起，才需要它（Round7）
            if (scrollFontGo) scrollFontGo.SetActive(touchLayout);
            ApplyCollapse();
        }

        bool PortraitTouch => touchLayout && Screen.height > Screen.width;

        void ApplyPortraitHud()
        {
            if (!touchPad || !scaler) return;
            bool portrait = PortraitTouch;
            float width = 1080f * Screen.width / Mathf.Max(1, Screen.height);
            var stick = (RectTransform)touchPad.transform.Find("Stick");
            var knob = (RectTransform)stick.Find("Knob");
            var act = (RectTransform)touchPad.transform.Find("Interact");
            float diameter = portrait ? width * 0.28f : 320f;
            stick.sizeDelta = Vector2.one * diameter;
            stick.anchoredPosition = portrait ? new Vector2(width * 0.18f, 151f) : new Vector2(230, 230);
            knob.sizeDelta = Vector2.one * (portrait ? diameter * 0.4f : 130f);
            knob.anchoredPosition = Vector2.zero;
            TouchInput.Move = Vector2.zero;
            act.sizeDelta = Vector2.one * (portrait ? width * 0.23f : 240f);
            act.anchoredPosition = portrait ? new Vector2(-width * 0.18f, 184f) : new Vector2(-200, 380);
            act.GetComponentInChildren<TMP_Text>().fontSize = portrait ? Mathf.Min(48, width * 0.08f) : 48;
            if (!portrait) return;
            SetAnchors((RectTransform)hudText.transform.parent, new Vector4(0.025f, 0.86f, 0.975f, 0.99f));
            SetAnchors((RectTransform)promptBox.transform, new Vector4(0.29f, 0.22f, 0.71f, 0.28f));
            SetAnchors((RectTransform)toastBox.transform, new Vector4(0.03f, 0.65f, 0.97f, 0.76f));
            string[] names = { "BookBtn", "FontBtn", "QualityBtn", "MuteBtn" };
            for (int i = 0; i < names.Length; i++)
            {
                var rt = (RectTransform)transform.Find(names[i]);
                if (rt) SetAnchors(rt, new Vector4(0.02f + i * 0.96f / names.Length, 0.775f, 0.01f + (i + 1) * 0.96f / names.Length, 0.85f));
            }
            if (scoreBtnGo) SetAnchors((RectTransform)scoreBtnGo.transform, new Vector4(0.02f, 0.775f, 0.202f, 0.85f));
        }

        /// Round14：字級「大／特大」時，題幹與答對後的解析下限也跟著放大（答錯提示框小，維持原下限避免溢出）
        void ScaleQuizText()
        {
            if (questionText == null || feedbackText == null) return;
            float floor = touchLayout ? UserMinTouch : UserMinDesk, k = LevelScale[fontLevel];
            questionText.fontSizeMin = floor * k;
            questionText.fontSizeMax = Mathf.Max(questionText.fontSizeMax, questionText.fontSizeMin);
            feedbackText.fontSizeMin = collapsed ? floor * k : floor;
            feedbackText.fontSizeMax = Mathf.Max(feedbackText.fontSizeMax, feedbackText.fontSizeMin);
        }

        /// 小螢幕（高 ≤ 800 px，如手機橫式 1334×750）：分頁正文用觸控下限，約 14 CSS px（Round14）
        bool SmallScreen => touchLayout || Screen.height <= 800;

        void Size(TMP_Text t, float min, float max, bool user = false)
        {
            if (sizes.ContainsKey(t)) sizes[t] = (min, max, user);
        }

        void ApplyScale()
        {
            string fontLabel = Txt.FontLabel(fontLevel);
            foreach (var ft in fontTexts) ft.text = fontLabel;
            foreach (var kv in sizes)
            {
                var t = kv.Key;
                float f = (touchLayout ? TouchScale : 1f) * (kv.Value.user ? LevelScale[fontLevel] : 1f);
                float max = kv.Value.max * f;
                if (paged.Contains(t))
                {
                    // 分頁正文：固定字級、不自動縮字，超過就分頁
                    t.enableAutoSizing = false;
                    t.fontSize = Mathf.Max(max, SmallScreen ? PageMinTouch : PageMinDesk);
                    continue;
                }
                float floor = kv.Value.user ? (touchLayout ? UserMinTouch : UserMinDesk) : (touchLayout ? MinTouch : MinDesk);
                float min = Mathf.Max(kv.Value.min, floor);
                t.enableAutoSizing = true;
                t.fontSizeMin = min;
                t.fontSizeMax = Mathf.Max(max, min);
            }
            ScaleQuizText();
            if (PortraitTouch)
            {
                hudText.fontSizeMin = 18; hudText.fontSizeMax = 24;
                foreach (var item in layouts)
                    if (item.rt.parent == transform && item.rt.GetComponent<Button>())
                    {
                        var label = item.rt.GetComponentInChildren<TMP_Text>();
                        if (label) { label.fontSizeMin = 14; label.fontSizeMax = 24; }
                    }
            }
            foreach (var pg in pagers) pg.Refresh();
        }

        // ---------- UI 建立 ----------

        void Build()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            var root = transform;

            lookArea = Panel(root, "TouchLook", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
            lookArea.AddComponent<TouchLook>();

            // HUD：容器底下先放深色底（在後＝畫在下面）、再放文字；兩者 pivot 相同，深色底依 textBounds 對齊（Round14）
            var hudBox = T(Rect(root, "HUD", new Vector2(0.01f, 0.88f), new Vector2(0.5f, 0.99f)), 0.01f, 0.86f, 0.41f, 0.99f);
            var hudBgGo = Panel(hudBox, "Bg", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0f, 0f, 0f, 0.45f));
            hudBgGo.GetComponent<Image>().raycastTarget = false; // 不擋觸控轉視角
            hudBg = (RectTransform)hudBgGo.transform;
            hudBg.sizeDelta = Vector2.zero;
            hudText = Text(Rect(hudBox, "Text", Vector2.zero, Vector2.one), 24, Color.white, TextAlignmentOptions.TopLeft);
            hudText.outlineWidth = 0.2f;

            promptBox = Panel(root, "Prompt", new Vector2(0.42f, 0.14f), new Vector2(0.58f, 0.21f), new Color(0, 0, 0, 0.6f));
            T(promptBox, 0.36f, 0.12f, 0.64f, 0.22f);
            promptText = Text(Rect(promptBox.transform, "Text", Vector2.zero, Vector2.one), 30, Color.white, TextAlignmentOptions.Center);

            noScale = true; // 觸控鍵維持原樣
            {
                touchPad = new GameObject("TouchPad", typeof(RectTransform));
                var tp = (RectTransform)touchPad.transform;
                tp.SetParent(root, false);
                tp.anchorMin = Vector2.zero; tp.anchorMax = Vector2.one;
                tp.offsetMin = tp.offsetMax = Vector2.zero;

                var stick = new GameObject("Stick", typeof(RectTransform)).GetComponent<RectTransform>();
                stick.SetParent(tp, false);
                stick.anchorMin = stick.anchorMax = Vector2.zero;
                stick.sizeDelta = new Vector2(320, 320);
                stick.anchoredPosition = new Vector2(230, 230);
                stick.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.22f);
                var knob = new GameObject("Knob", typeof(RectTransform)).GetComponent<RectTransform>();
                knob.SetParent(stick, false);
                knob.sizeDelta = new Vector2(130, 130);
                var knobImg = knob.gameObject.AddComponent<Image>();
                knobImg.color = new Color(1, 1, 1, 0.6f);
                knobImg.raycastTarget = false;
                stick.gameObject.AddComponent<TouchStick>().knob = knob;

                var act = new GameObject("Interact", typeof(RectTransform)).GetComponent<RectTransform>();
                act.SetParent(tp, false);
                act.anchorMin = act.anchorMax = new Vector2(1, 0);
                act.sizeDelta = new Vector2(240, 240);
                // 右下角留給 Netlify 浮標（觸控：x ≥ 0.88、y ≤ 0.18 不放可點元件）：互動鍵底邊抬到 y≈260（0.24）
                act.anchoredPosition = new Vector2(-200, 380);
                act.gameObject.AddComponent<Image>().color = new Color(BtnColor.r, BtnColor.g, BtnColor.b, 0.85f);
                act.gameObject.AddComponent<TouchButton>();
                Text(Rect(act, "Label", Vector2.zero, Vector2.one), 48, Ink, TextAlignmentOptions.Center).text = Txt.TouchInteract;
            }
            noScale = false;

            toastBox = Panel(root, "Toast", new Vector2(0.2f, 0.8f), new Vector2(0.8f, 0.9f), new Color(0.2f, 0.14f, 0.08f, 0.85f));
            T(toastBox, 0.12f, 0.70f, 0.88f, 0.85f);
            toastText = Text(Rect(toastBox.transform, "Text", Vector2.zero, Vector2.one, 20), 28, Paper, TextAlignmentOptions.Center);

            // 版面規則（Round5，配置表見 staging/Round5/README.md）：
            // - 桌機面板頂邊 ≤ 0.90：HUD 兩行（24 號 × 行高 1.44）底邊約在 0.926，右上按鈕列底邊 0.93，面板不再蓋到。
            // - 觸控可點元件高 ≥ 0.13 面板（≈ 0.125 螢幕 ≈ 135 參考單位 ≈ 94px@1334×750，下限 88px）、寬 ≥ 0.08 面板。
            // - 觸控右下角（螢幕 x ≥ 0.88、y ≤ 0.18）留給 Netlify 浮標，不放可點元件。
            // 卷軸（觸控版：全螢幕、選項 2×2、右側直欄＝看白話＋上一頁／下一頁）
            scrollPanel = Panel(root, "ScrollPanel", new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.90f), Paper);
            T(scrollPanel, 0.02f, 0.02f, 0.98f, 0.98f);
            var sp = scrollPanel.transform;
            // 標題列：左「字級」、中標題、右「朗讀」（Round6：觸控版開卷軸時右上按鈕列收起，讀卷軸時也要能切字級）
            titleText = Text(T(Rect(sp, "Title", new Vector2(0.17f, 0.89f), new Vector2(0.83f, 0.98f), 40), 0.18f, 0.88f, 0.80f, 0.99f), 38, Brown, TextAlignmentOptions.Center);
            var scrollFontBtn = Btn(sp, "FontBtn", new Vector2(0.03f, 0.905f), new Vector2(0.16f, 0.97f), Txt.FontNormal);
            T(scrollFontBtn, 0.02f, 0.865f, 0.17f, 0.995f); // 觸控：0.13 面板高 ≈ 94px@1334×750
            fontTexts.Add(scrollFontBtn.GetComponentInChildren<TMP_Text>());
            scrollFontBtn.GetComponentInChildren<TMP_Text>().textWrappingMode = TextWrappingModes.NoWrap;
            scrollFontBtn.onClick.AddListener(ToggleFont);
            scrollFontGo = scrollFontBtn.gameObject;
            speakBtn = Btn(sp, "SpeakBtn", new Vector2(0.84f, 0.905f), new Vector2(0.97f, 0.97f), Txt.Speak);
            T(speakBtn, 0.82f, 0.865f, 0.98f, 0.995f);
            speakBtn.onClick.AddListener(Speak);
            // 閱讀區：原文＋白話同一個分頁文字框；右側直欄放「看白話」與上一頁／下一頁
            originalText = Text(T(Rect(sp, "Original", new Vector2(0, 0.52f), new Vector2(0.86f, 0.89f), 50), 0, 0.535f, 0.83f, 0.865f), 40, Ink, TextAlignmentOptions.TopLeft);
            originalText.lineSpacing = 12;
            plainBtn = Btn(sp, "PlainBtn", new Vector2(0.865f, 0.755f), new Vector2(0.985f, 0.88f), Txt.SeePlain);
            T(plainBtn, 0.84f, 0.725f, 0.99f, 0.855f);
            plainBtn.onClick.AddListener(ShowPlain);
            readPager = MakePager(sp, "ReadPager", originalText, new Vector4(0.865f, 0.525f, 0.985f, 0.74f), new Vector4(0.84f, 0.405f, 0.99f, 0.715f), true);
            questionText = Text(T(Rect(sp, "Question", new Vector2(0, 0.42f), new Vector2(1, 0.52f), 60), 0, 0.415f, 0.83f, 0.535f), 28, Ink, TextAlignmentOptions.Left);
            Size(questionText, 14, 28, true); // Round14：題幹跟字級走（框加高到兩行）
            optionBtns = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                float top = 0.415f - i * 0.065f;
                optionBtns[i] = Btn(sp, "Option" + i, new Vector2(0.06f, top - 0.057f), new Vector2(0.94f, top), "");
                float x0 = i % 2 == 0 ? 0.02f : 0.425f, y1 = i < 2 ? 0.41f : 0.27f;
                T(optionBtns[i], x0, y1 - 0.13f, x0 + 0.395f, y1);
                optionBtns[i].GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
            }
            feedbackText = Text(T(Rect(sp, "Feedback", new Vector2(0, 0.09f), new Vector2(1, 0.15f), 60), 0, 0.005f, 0.83f, 0.135f), 24, new Color(0.2f, 0.45f, 0.2f), TextAlignmentOptions.Center);
            closeBtn = Btn(sp, "CloseBtn", new Vector2(0.38f, 0.015f), new Vector2(0.62f, 0.08f), Txt.Collect);
            T(closeBtn, 0.33f, 0.005f, 0.67f, 0.135f);
            foreach (var t in new[] { originalText, feedbackText }) Size(t, 14, t.fontSize, true);
            optionLabels = new TMP_Text[optionBtns.Length];
            for (int i = 0; i < optionBtns.Length; i++)
            {
                optionLabels[i] = optionBtns[i].GetComponentInChildren<TMP_Text>();
                Size(optionLabels[i], 14, 24, true);
            }
            // 線索冊
            bookPanel = Panel(root, "BookPanel", new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.90f), Paper);
            T(bookPanel, 0.02f, 0.02f, 0.98f, 0.98f);
            bookText = Text(T(Rect(bookPanel.transform, "Text", new Vector2(0, 0.12f), new Vector2(1, 0.88f), 50), 0, 0.155f, 1, 0.855f), 26, Ink, TextAlignmentOptions.TopLeft);
            Size(bookText, 16, 26, true); // Round14：線索冊內文跟字級走
            bookPager = MakePager(bookPanel.transform, "BookPager", bookText, new Vector4(0.6f, 0.89f, 0.98f, 0.98f), new Vector4(0.55f, 0.865f, 0.98f, 0.995f), false);
            var bookClose = Btn(bookPanel.transform, "CloseBtn", new Vector2(0.7f, 0.02f), new Vector2(0.9f, 0.09f), Txt.Close);
            T(bookClose, 0.56f, 0.01f, 0.80f, 0.14f);
            bookClose.onClick.AddListener(() => ToggleBook(false));
            var bookHelp = Btn(bookPanel.transform, "HelpBtn", new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.09f), Txt.Help);
            T(bookHelp, 0.30f, 0.01f, 0.52f, 0.14f);
            bookHelp.onClick.AddListener(ShowHelp);
            var bookReset = Btn(bookPanel.transform, "ResetBtn", new Vector2(0.1f, 0.02f), new Vector2(0.3f, 0.09f), Txt.Restart);
            T(bookReset, 0.04f, 0.01f, 0.26f, 0.14f);
            bookReset.onClick.AddListener(ResetAll);

            // 分頁面板：證據卡／反思題／選文全文共用
            fullPanel = Panel(root, "PagedPanel", new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.90f), Paper);
            T(fullPanel, 0.02f, 0.02f, 0.98f, 0.98f);
            fullTitle = Text(T(Rect(fullPanel.transform, "Title", new Vector2(0, 0.88f), new Vector2(1, 0.98f), 40), 0, 0.87f, 1, 0.99f), 40, Brown, TextAlignmentOptions.Center);
            fullText = Text(T(Rect(fullPanel.transform, "Text", new Vector2(0, 0.14f), new Vector2(1, 0.87f), 70), 0, 0.155f, 1, 0.86f), 34, Ink, TextAlignmentOptions.TopLeft);
            fullText.lineSpacing = 25;
            Size(fullText, 16, 34, true);
            fullNextBtn = Btn(fullPanel.transform, "NextBtn", new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.11f), Txt.Continue);
            T(fullNextBtn, 0.40f, 0.01f, 0.70f, 0.14f);
            fullNextBtn.onClick.AddListener(() =>
            {
                fullPanel.SetActive(false);
                var d = fullDone;
                fullDone = null;
                d?.Invoke();
            });
            fullPager = MakePager(fullPanel.transform, "FullPager", fullText, new Vector4(0.64f, 0.03f, 0.97f, 0.11f), new Vector4(0.02f, 0.01f, 0.36f, 0.14f), false); // 觸控版移到左下：右下角留給 Netlify 浮標
            fullPager.onChange = () => fullNextBtn.gameObject.SetActive(fullPager.OnLast);

            // 結局二：揭曉＋成績卡
            endingPanel = Panel(root, "EndingPanel", new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.85f), Paper);
            T(endingPanel, 0.02f, 0.02f, 0.98f, 0.98f);
            endText = Text(T(Rect(endingPanel.transform, "Text", new Vector2(0, 0.56f), new Vector2(1, 0.98f), 60), 0, 0.57f, 1, 0.99f), 30, Ink, TextAlignmentOptions.Center);
            Size(endText, 16, 30);
            endText.text = Txt.EndingReveal;
            var scoreBg = Panel(endingPanel.transform, "Score", new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.55f), new Color(0.90f, 0.82f, 0.62f));
            T(scoreBg, 0.04f, 0.155f, 0.96f, 0.56f);
            scoreText = Text(Rect(scoreBg.transform, "Text", Vector2.zero, Vector2.one, 20), 30, Ink, TextAlignmentOptions.Center);
            Size(scoreText, 14, 30);
            // Round11：底排四顆（重新開始／選文全文／宴後加演／繼續逛逛）；觸控版右緣停在 0.86（右下角留給 Netlify 浮標）
            var endClose = Btn(endingPanel.transform, "CloseBtn", new Vector2(0.76f, 0.04f), new Vector2(0.96f, 0.13f), Txt.KeepWalking);
            T(endClose, 0.67f, 0.01f, 0.86f, 0.14f);
            endClose.onClick.AddListener(() => endingPanel.SetActive(false));
            var endFull = Btn(endingPanel.transform, "FullTextBtn", new Vector2(0.28f, 0.04f), new Vector2(0.50f, 0.13f), Txt.ReadFullText);
            T(endFull, 0.24f, 0.01f, 0.44f, 0.14f);
            endFull.onClick.AddListener(() =>
            {
                endingPanel.SetActive(false);
                ShowPaged(Txt.FullTitle, FullTextBody(), Txt.BackToScore, () => endingPanel.SetActive(true));
            });
            var endReset = Btn(endingPanel.transform, "ResetBtn", new Vector2(0.04f, 0.04f), new Vector2(0.26f, 0.13f), Txt.Restart);
            T(endReset, 0.02f, 0.01f, 0.22f, 0.14f);
            endReset.onClick.AddListener(ResetAll);
            // 操作說明（放低一點，不擋開場 Toast）
            helpPanel = Panel(root, "HelpPanel", new Vector2(0.27f, 0.1f), new Vector2(0.73f, 0.77f), Paper);
            T(helpPanel, 0.1f, 0.03f, 0.9f, 0.69f);
            helpText = Text(T(Rect(helpPanel.transform, "Text", new Vector2(0, 0.2f), new Vector2(1, 0.97f), 40), 0, 0.24f, 1, 0.98f), 30, Ink, TextAlignmentOptions.Center);
            Size(helpText, 14, 30);
            var helpOk = Btn(helpPanel.transform, "StartBtn", new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.16f), Txt.Start);
            T(helpOk, 0.3f, 0.02f, 0.7f, 0.22f);
            helpOk.onClick.AddListener(CloseHelp);

            // 字幕
            sayBox = Panel(root, "SayBox", new Vector2(0.15f, 0.03f), new Vector2(0.85f, 0.13f), new Color(0.1f, 0.07f, 0.04f, 0.8f));
            sayText = Text(Rect(sayBox.transform, "Text", Vector2.zero, Vector2.one, 30), 30, Paper, TextAlignmentOptions.Center);

            // 進度條
            inkBox = Panel(root, "InkBar", new Vector2(0.35f, 0.16f), new Vector2(0.65f, 0.19f), new Color(0.9f, 0.85f, 0.72f, 0.9f));
            var fillRt = Rect(inkBox.transform, "Fill", Vector2.zero, Vector2.one, 4);
            fillRt.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f);
            inkFill = fillRt;
            inkFill.anchorMax = new Vector2(0, 1);
            inkBox.SetActive(false);

            // 右上按鈕列（觸控版高 0.13 螢幕 ≈ 140 參考單位 ≈ 97px@1334×750；全螢幕面板開著時收起，見 Update）
            var muteBtn = Btn(root, "MuteBtn", new Vector2(0.905f, 0.93f), new Vector2(0.99f, 0.99f), Txt.ReadOn);
            T(muteBtn, 0.89f, 0.86f, 0.99f, 0.99f);
            muteText = muteBtn.GetComponentInChildren<TMP_Text>();
            muteBtnGo = muteBtn.gameObject;
            muteBtn.onClick.AddListener(ToggleMute);
            var qBtn = Btn(root, "QualityBtn", new Vector2(0.815f, 0.93f), new Vector2(0.9f, 0.99f), Txt.QualityHigh);
            T(qBtn, 0.78f, 0.86f, 0.88f, 0.99f);
            qualityText = qBtn.GetComponentInChildren<TMP_Text>();
            qBtn.onClick.AddListener(() =>
            {
                if (!QualityToggle.I) return;
                QualityToggle.I.Toggle();
                qualityText.text = QualityToggle.I.High ? Txt.QualityHigh : Txt.QualityLow;
            });
            var fBtn = Btn(root, "FontBtn", new Vector2(0.71f, 0.93f), new Vector2(0.81f, 0.99f), Txt.FontNormal);
            T(fBtn, 0.65f, 0.86f, 0.77f, 0.99f);
            fontTexts.Add(fBtn.GetComponentInChildren<TMP_Text>());
            fBtn.GetComponentInChildren<TMP_Text>().textWrappingMode = TextWrappingModes.NoWrap;   // R18：觸控窄鈕「字級：標準」不折行（放不下就自動縮字）
            fBtn.onClick.AddListener(ToggleFont);
            var bBtn = Btn(root, "BookBtn", new Vector2(0.625f, 0.93f), new Vector2(0.705f, 0.99f), Txt.Book);
            T(bBtn, 0.54f, 0.86f, 0.64f, 0.99f);
            bBtn.onClick.AddListener(() =>
            {
                if (!Cinematic && !ExternalUI && !scrollPanel.activeSelf && !endingPanel.activeSelf && !fullPanel.activeSelf && !helpPanel.activeSelf)
                    ToggleBook(!bookPanel.activeSelf);
            });

            var sBtn = Btn(root, "ScoreBtn", new Vector2(0.54f, 0.93f), new Vector2(0.62f, 0.99f), Txt.ScoreBtn);
            T(sBtn, 0.43f, 0.86f, 0.53f, 0.99f);
            sBtn.onClick.AddListener(() =>
            {
                if (!Cinematic && !ExternalUI && !scrollPanel.activeSelf && !endingPanel.activeSelf && !fullPanel.activeSelf && !helpPanel.activeSelf)
                { bookPanel.SetActive(false); ShowScoreCard(); }
            });
            scoreBtnGo = sBtn.gameObject;
            bookBtnGo = bBtn.gameObject;
            topRow.Add(muteBtn.gameObject); topRow.Add(qBtn.gameObject); topRow.Add(fBtn.gameObject); topRow.Add(bBtn.gameObject);
            scoreBtnGo.SetActive(false);

            // 淡入淡出（最上層）；Toast 放在它下面、其他面板上面
            fadeImg = Panel(root, "Fade", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0)).GetComponent<Image>();
            fadeImg.raycastTarget = false;
            fadeImg.gameObject.SetActive(false);
            toastBox.transform.SetSiblingIndex(fadeImg.transform.GetSiblingIndex() - 1);
            toastBox.GetComponent<Image>().raycastTarget = false;

            sayBox.SetActive(false);
            promptBox.SetActive(false);
            toastBox.SetActive(false);
            scrollPanel.SetActive(false);
            bookPanel.SetActive(false);
            fullPanel.SetActive(false);
            endingPanel.SetActive(false);
            helpPanel.SetActive(false);
        }

        static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, float pad = 0)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = new Vector2(pad, 0); rt.offsetMax = new Vector2(-pad, 0);
            return rt;
        }

        static GameObject Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var rt = Rect(parent, name, min, max);
            rt.gameObject.AddComponent<Image>().color = color;
            return rt.gameObject;
        }

        TMP_Text Text(RectTransform rt, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (!noScale) sizes[t] = (Mathf.Min(14, size), size, false);
            return t;
        }

        Button Btn(Transform parent, string name, Vector2 min, Vector2 max, string label)
        {
            var go = Panel(parent, name, min, max, BtnColor);
            var btn = go.AddComponent<Button>();
            var t = Text(Rect(go.transform, "Label", Vector2.zero, Vector2.one, 16), 24, Ink, TextAlignmentOptions.Center);
            t.text = label;
            Size(t, 14, 24);
            return btn;
        }
    }
}
