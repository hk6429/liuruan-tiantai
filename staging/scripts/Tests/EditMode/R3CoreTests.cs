using System;
using System.IO;
using NUnit.Framework;

namespace Liuruan.Tests
{
    internal static class R3Fixture
    {
        internal static string Root
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                return Path.GetDirectoryName(UnityEngine.Application.dataPath);
#else
                return Environment.GetEnvironmentVariable("LIURUAN_PROJECT_ROOT") ?? Directory.GetCurrentDirectory();
#endif
            }
        }
        internal static T Read<T>(string name, bool content = false)
        {
            string json = File.ReadAllText(Path.Combine(Root, content ? "docs/content/r3" : "staging/scripts/Tests/Fixtures/R3", name + ".json"));
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.FromJson<T>(json);
#else
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
        }
        internal static void Reject(Action action)
        {
            try { action(); } catch (ArgumentException) { return; }
            throw new Exception("Expected ArgumentException");
        }
    }
    public sealed class StallHintCoreTests
    {
        static StallHintCore Create() { var c = new StallHintCore(R3Fixture.Read<HintDocument>("hints")); c.SetStep(1, "bark"); return c; }
        [Test] public void PausedReadingAndDialoguePreserveRemainder()
        {
            var c = Create(); int n = 0; c.OnHint += h => n++;
            c.Tick(29); c.Tick(100, true); Check.Near(29, c.ElapsedSeconds); Check.Equal(0, n);
            c.Tick(1); Check.Equal(1, n); Check.Equal(1, c.Level);
        }
        [Test] public void ExactThresholdsEmitOnceWithActionSignals()
        {
            var c = Create(); var seen = new System.Collections.Generic.List<StallHint>(); c.OnHint += seen.Add;
            c.Tick(30); c.Tick(29); Check.Equal(1, seen.Count);
            c.Tick(1); c.Tick(30); c.Tick(1000); Check.Equal(3, seen.Count);
            for (int i = 0; i < 3; i++)
            {
                Check.Equal(i + 1, seen[i].Level); Check.Equal(i >= 1, seen[i].BrightenTarget);
                Check.Equal(i == 2, seen[i].GuideCompanion); Check.Equal(i == 2, seen[i].TurnCamera);
            }
        }
        [Test] public void LargeTickEmitsAllLevelsInOrder()
        { var c = Create(); string order = ""; c.OnHint += h => order += h.Level; c.Tick(100); Check.Equal("123", order); }
        [Test] public void ProgressAndNewStepResetButSameStepDoesNot()
        {
            var c = Create(); c.Tick(90); c.Progress(); Check.Near(0, c.ElapsedSeconds); Check.Equal(0, c.Level);
            c.Tick(29); c.SetStep(1, "bark"); Check.Near(29, c.ElapsedSeconds);
            Check.Equal(false, c.SetStep(9, "missing")); Check.Near(29, c.ElapsedSeconds);
            c.SetStep(1, "path"); Check.Near(0, c.ElapsedSeconds); c.Tick(30); Check.Equal(1, c.Level);
        }
        [Test] public void DeviceSwitchSelectsCurrentTextAndNextEvent()
        {
            var c = Create(); Check.Equal("桌面提示1", c.TextFor(1)); c.Tick(30);
            c.Device = HintDevice.Touch; Check.Equal("觸控提示1", c.TextFor(1));
            string text = null; c.OnHint += h => text = h.Text; c.Tick(30); Check.Equal("觸控提示2", text);
        }
        [Test] public void ProgressInsideEventStopsOldTick()
        { var c = Create(); int n = 0; c.OnHint += h => { n++; c.Progress(); }; c.Tick(100); Check.Equal(1, n); Check.Equal(0, c.Level); }
        [Test] public void RejectsBadTimeThresholdsAndIncompleteData()
        {
            var c = Create(); Check.Throws(() => c.Tick(-1)); Check.Throws(() => c.Tick(float.NaN));
            Check.Throws(() => c.TextFor(0));
            R3Fixture.Reject(() => new StallHintCore(null));
            Check.Throws(() => new StallHintCore(R3Fixture.Read<HintDocument>("hints"), 30, 20));
            var d = R3Fixture.Read<HintDocument>("hints"); d.acts[0].steps[0].l2.touch = null;
            R3Fixture.Reject(() => new StallHintCore(d));
        }
    }
    public sealed class DialogueCoreTests
    {
        static DialogueCore Create() => new DialogueCore(R3Fixture.Read<DialogueDocument>("dialogue"));
        [Test] public void OrderedLinesTypesAndSingleEndEvent()
        {
            var c = Create(); int ended = 0, changed = 0; c.OnSceneEnd += id => { Check.Equal("meet_women", id); ended++; };
            c.OnLineChanged += l => changed++;
            Check.Equal(true, c.Start("meet_women")); Check.Equal(true, c.IsActive);
            Check.Equal("女子", c.CurrentLine.speaker); Check.Equal("original", c.CurrentLine.type);
            Check.Equal(true, c.Next()); Check.Equal("plain", c.CurrentLine.type); Check.Equal("她們怎麼認得我們？", c.CurrentLine.text);
            Check.Equal(true, c.Next()); Check.Equal(false, c.IsActive); Check.Equal<DialogueLine>(null, c.CurrentLine);
            Check.Equal(false, c.Next()); Check.Equal(1, ended); Check.Equal(2, changed);
        }
        [Test] public void UnknownBusyOnceAndExplicitReplay()
        {
            var c = Create(); Check.Equal(false, c.Start("unknown")); Check.Equal(false, c.Start(null));
            Check.Equal(false, c.TryTrigger("unknown")); Check.Equal(true, c.TryTrigger("interact:Act4_Women"));
            Check.Equal(false, c.Start("leave")); c.Next(); c.Next();
            Check.Equal(false, c.Start("meet_women")); Check.Equal(false, c.TryTrigger("interact:Act4_Women"));
            Check.Equal(true, c.Start("meet_women", true));
        }
        [Test] public void EndHandlerCanStartNextSceneWithoutLosingIt()
        {
            var c = Create(); c.OnSceneEnd += id => c.Start("leave");
            c.Start("meet_women"); c.Next(); c.Next(); Check.Equal("leave", c.SceneId); Check.Equal(true, c.IsActive);
        }
        [Test] public void RejectsDuplicateIdsInvalidTypesAndLongPlainLines()
        {
            R3Fixture.Reject(() => new DialogueCore(null));
            var d = R3Fixture.Read<DialogueDocument>("dialogue"); d.acts[0].scenes[1].id = "meet_women";
            R3Fixture.Reject(() => new DialogueCore(d));
            d = R3Fixture.Read<DialogueDocument>("dialogue"); d.acts[0].scenes[0].lines[0].type = "invented";
            R3Fixture.Reject(() => new DialogueCore(d));
            d = R3Fixture.Read<DialogueDocument>("dialogue"); d.acts[0].scenes[0].lines[1].text = new string('字', 21);
            R3Fixture.Reject(() => new DialogueCore(d));
        }
    }
    public sealed class ObjectiveCoreTests
    {
        static ObjectiveCore Create() => new ObjectiveCore(R3Fixture.Read<ObjectiveDocument>("objectives"));
        [Test] public void TwoLinesIncludeMotivationCountAndDirection()
        {
            var c = Create(); c.SetStep(1, "bark", 2);
            Check.Equal("糧食快吃完了｜目標：剝楮樹皮 2/5\n方向：沿小徑往北，找有光柱的樹", c.Text);
            c.SetCount(99); Check.Equal(5, c.Count); Check.Equal(2, c.Text.Split('\n').Length);
        }
        [Test] public void StepAndActSwitchResetCountAndUnknownKeepsState()
        {
            var c = Create(); int n = 0; c.OnChanged += () => n++;
            c.SetStep(1, "bark", 3); c.SetCount(3); Check.Equal(1, n);
            Check.Equal(false, c.SetStep(9, "bad")); Check.Equal(3, c.Count);
            Check.Equal(true, c.NextStep()); Check.Equal("path", c.CurrentStep.id); Check.Equal(0, c.Count);
            Check.Equal(false, c.NextStep()); c.SetStep(2, "climb"); Check.Equal("climb", c.CurrentStep.id);
            c.SetStep(1, "bark", 4); Check.Equal(4, c.Count);
        }
        [Test] public void RejectsMissingDataAndNegativeCounts()
        {
            var c = Create(); Check.Throws(() => c.SetCount(-1)); Check.Throws(() => c.SetStep(1, "bark", -1));
            R3Fixture.Reject(() => new ObjectiveCore(null));
            var d = R3Fixture.Read<ObjectiveDocument>("objectives"); d.acts[0].steps[0].count = 0;
            R3Fixture.Reject(() => new ObjectiveCore(d));
        }
    }
    public sealed class RescueCoreTests
    {
        static RescueCore Create() => new RescueCore(new WorldPosition(0, 1, 0), -10, 10, -20, 20);
        [Test] public void SavesOnlyAfterTwoContinuousGroundedDrySeconds()
        {
            var c = Create(); var next = new WorldPosition(2, 1, 2);
            c.Tick(1.99f, next, true, false); Check.Near(0, c.LastSafePoint.X);
            c.Tick(.02f, next, true, false); Check.Near(2, c.LastSafePoint.X);
            c.Tick(2, new WorldPosition(3, 1, 3), false, false); Check.Near(2, c.LastSafePoint.X);
            c.Tick(2, new WorldPosition(4, 1, 4), true, true); Check.Near(2, c.LastSafePoint.X);
        }
        [Test] public void AirAndWaterResetTimerAndLargeTickSamplesOnce()
        {
            var c = Create(); var next = new WorldPosition(2, 1, 2);
            c.Tick(1, next, true, false); c.Tick(0, next, false, false); c.Tick(1, next, true, false);
            Check.Near(0, c.LastSafePoint.X); c.Tick(0, next, true, true); c.Tick(1, next, true, false);
            Check.Near(0, c.LastSafePoint.X); c.Tick(1, next, true, false); Check.Near(2, c.LastSafePoint.X);
            c.Tick(100, new WorldPosition(3, 1, 3), true, false); Check.Near(3, c.LastSafePoint.X);
        }
        [TestCase(-10f, 0f, 0f, false)] [TestCase(10f, 0f, 0f, false)]
        [TestCase(0f, -10f, 20f, false)] [TestCase(0f, -10.01f, 0f, true)]
        [TestCase(-10.01f, 0f, 0f, true)] [TestCase(10.01f, 0f, 0f, true)]
        [TestCase(0f, 0f, -20.01f, true)] [TestCase(0f, 0f, 20.01f, true)]
        public void FallAndRectangleEdges(float x, float y, float z, bool expected)
        {
            var c = Create(); Check.Equal(expected, c.Tick(10, new WorldPosition(x, y, z), true, false));
            if (expected) Check.Near(0, c.LastSafePoint.X);
        }
        [Test] public void RejectsUnsafeInitialPointAndInvalidSettings()
        {
            Check.Throws(() => Create().Tick(-1, new WorldPosition(), true, false));
            Check.Throws(() => new RescueCore(new WorldPosition(), 10, -10, 0, 1));
            R3Fixture.Reject(() => new RescueCore(new WorldPosition(11, 0, 0), -10, 10, -10, 10));
            Check.Throws(() => new RescueCore(new WorldPosition(), float.NaN, 10, -10, 10));
        }
    }
    public sealed class OffscreenArrowCoreTests
    {
        // 測試相機看向 +Z，clip W=world Z，clip Z=0。
        static float[] VP => new float[] { 1,0,0,0, 0,1,0,0, 0,0,0,0, 0,0,1,0 };
        [TestCase(2f, 0f, .95f, .5f, 0f)] [TestCase(-2f, 0f, .05f, .5f, 180f)]
        [TestCase(0f, 2f, .5f, .95f, 90f)] [TestCase(0f, -2f, .5f, .05f, -90f)]
        public void FourEdges(float x, float y, float sx, float sy, float angle)
        {
            var r = OffscreenArrowCore.Project(VP, new WorldPosition(x, y, 1));
            Check.Equal(false, r.IsOnscreen); Check.Near(sx, r.X); Check.Near(sy, r.Y); Check.Near(angle, r.AngleDegrees);
        }
        [Test] public void InsideBoundaryAndDepthClipping()
        {
            var r = OffscreenArrowCore.Project(VP, new WorldPosition(.5f, -.5f, 1));
            Check.Equal(true, r.IsOnscreen); Check.Near(.75, r.X); Check.Near(.25, r.Y);
            Check.Equal(true, OffscreenArrowCore.Project(VP, new WorldPosition(1, 1, 1)).IsOnscreen);
            var vp = VP; vp[10] = 2;
            Check.Equal(false, OffscreenArrowCore.Project(vp, new WorldPosition(0, 0, 1)).IsOnscreen);
        }
        [Test] public void BehindFlipsProjectedDirectionAndAxisHasFiniteFallback()
        {
            var r = OffscreenArrowCore.Project(VP, new WorldPosition(2, 0, -1));
            Check.Equal(true, r.IsBehind); Check.Equal(false, r.IsOnscreen); Check.Near(.95, r.X); Check.Near(0, r.AngleDegrees);
            foreach (float z in new[] { -1f, 0f })
            { r = OffscreenArrowCore.Project(VP, new WorldPosition(0, 0, z)); Check.Near(.5, r.X); Check.Near(.05, r.Y); Check.Near(-90, r.AngleDegrees); }
        }
        [Test] public void DiagonalAspectAndTranslatedMatrix()
        {
            var r = OffscreenArrowCore.Project(VP, new WorldPosition(2, 2, 1), .1f, 2);
            Check.Near(.9, r.X); Check.Near(.9, r.Y); Check.Near(Math.Atan2(1, 2) * 180 / Math.PI, r.AngleDegrees);
            var vp = VP; vp[3] = -5;
            Check.Equal(true, OffscreenArrowCore.Project(vp, new WorldPosition(5, 0, 1)).IsOnscreen);
        }
        [Test] public void RejectsInvalidMatrixInsetAndAspect()
        {
            R3Fixture.Reject(() => OffscreenArrowCore.Project(null, new WorldPosition()));
            R3Fixture.Reject(() => OffscreenArrowCore.Project(new float[15], new WorldPosition()));
            Check.Throws(() => OffscreenArrowCore.Project(VP, new WorldPosition(), .5f));
            Check.Throws(() => OffscreenArrowCore.Project(VP, new WorldPosition(), aspect: 0));
            var vp = VP; vp[0] = float.NaN; Check.Throws(() => OffscreenArrowCore.Project(vp, new WorldPosition()));
        }
    }
    public sealed class ReadingCoreTests
    {
        static ReadingCore Create() => new ReadingCore(R3Fixture.Read<SentenceDocument>("sentences"), R3Fixture.Read<GlossaryDocument>("glossary_cards"));
        [Test] public void ParagraphModeAndTermCardsRoundTrip()
        {
            var c = Create(); var p = c.GetParagraph(1); Check.Equal(2, p.sentences.Length);
            Check.Equal(p.sentences[0].orig, c.Display(p.sentences[0]));
            int n = 0; c.OnModeChanged += m => n++; c.SetMode(ReadingMode.OriginalAndPlain); c.SetMode(ReadingMode.OriginalAndPlain);
            Check.Equal(p.sentences[0].orig + "\n" + p.sentences[0].plain, c.Display(p.sentences[0])); Check.Equal(1, n);
            c.SetMode(ReadingMode.Original); Check.Equal(2, n); Check.Equal(0, c.MissingTerms.Count);
            foreach (int id in new[] { 1, 2 }) foreach (var s in c.GetParagraph(id).sentences)
            foreach (string term in s.terms) Check.Equal(term, c.FindTerm(term).term);
            Check.Equal("ㄍㄨˇ ㄆㄧˊ", c.FindTerm("穀皮").reading); Check.Equal<GlossaryCard>(null, c.FindTerm(null));
            Check.Equal<GlossaryCard>(null, c.FindTerm("未知")); Check.Equal<ReadingParagraph>(null, c.GetParagraph(99));
        }
        [Test] public void MissingTermsAreListedUniquelyAndReadingRemainsAvailable()
        {
            var s = R3Fixture.Read<SentenceDocument>("sentences"); s.paragraphs[0].sentences[0].terms = new[] { "缺詞", "缺詞", "穀皮" };
            var c = new ReadingCore(s, R3Fixture.Read<GlossaryDocument>("glossary_cards"));
            Check.Equal(1, c.MissingTerms.Count); Check.Equal("缺詞", c.MissingTerms[0]); Check.Equal(1, c.GetParagraph(1).p);
        }
        [Test] public void RejectsDuplicateOrMissingDataAndInvalidMode()
        {
            R3Fixture.Reject(() => new ReadingCore(null, null));
            var g = R3Fixture.Read<GlossaryDocument>("glossary_cards"); g.terms[1].term = g.terms[0].term;
            R3Fixture.Reject(() => new ReadingCore(R3Fixture.Read<SentenceDocument>("sentences"), g));
            var s = R3Fixture.Read<SentenceDocument>("sentences"); s.paragraphs[1].p = 1;
            R3Fixture.Reject(() => new ReadingCore(s, R3Fixture.Read<GlossaryDocument>("glossary_cards")));
            Check.Throws(() => Create().SetMode((ReadingMode)99));
        }
    }
}

namespace Liuruan.Tests
{
    public sealed class R3ContentCoreTests
    {
        [Test] public void ActualContentObjectivesAndHintsMatchEveryStepAndDevice()
        {
            var data = R3Fixture.Read<ObjectiveDocument>("objectives", true);
            var objectives = new ObjectiveCore(data);
            var hints = new StallHintCore(R3Fixture.Read<HintDocument>("hints", true));
            Check.Equal(5, data.acts.Length);
            foreach (var act in data.acts) foreach (var step in act.steps)
            {
                Check.Equal(true, objectives.SetStep(act.act, step.id)); Check.Equal(true, hints.SetStep(act.act, step.id));
                foreach (var device in new[] { HintDevice.Desktop, HintDevice.Touch })
                {
                    hints.Device = device;
                    for (int level = 1; level <= 3; level++) Check.Equal(false, string.IsNullOrWhiteSpace(hints.TextFor(level)));
                }
                objectives.SetCount(step.count); Check.Equal(step.count, objectives.Count);
            }
        }
        [Test] public void ActualDialoguePlaysAllScenesAndPreservesVariantAndSourceMetadata()
        {
            var data = R3Fixture.Read<DialogueDocument>("dialogue", true);
            var c = new DialogueCore(data); int ends = 0, expected = 0, variants = 0;
            c.OnSceneEnd += id => ends++;
            foreach (var act in data.acts) foreach (var scene in act.scenes)
            {
                expected++; Check.Equal(true, c.Start(scene.id));
                foreach (var line in scene.lines)
                {
                    Check.Equal(line.text, c.CurrentLine.text); Check.Equal(line.type, c.CurrentLine.type);
                    Check.Equal(line.source, c.CurrentLine.source); Check.Equal(line.label, c.CurrentLine.label); Check.Equal(line.orig, c.CurrentLine.orig);
                    if (!string.IsNullOrEmpty(line.label)) variants++;
                    c.Next();
                }
                Check.Equal(false, c.IsActive); Check.Equal(false, c.Start(scene.id));
            }
            Check.Equal(expected, ends); Check.Equal(true, variants > 0);
        }
        [Test] public void ActualReadingHasEveryTermAndReconstructsSevenSourceParagraphs()
        {
            var sentences = R3Fixture.Read<SentenceDocument>("sentences", true);
            var glossary = R3Fixture.Read<GlossaryDocument>("glossary_cards", true);
            var c = new ReadingCore(sentences, glossary);
            Check.Equal("", string.Join("、", c.MissingTerms)); Check.Equal(7, sentences.paragraphs.Length); Check.Equal(40, glossary.terms.Length);
            string[] source = File.ReadAllText(Path.Combine(R3Fixture.Root, "sources/liuruan_tw.txt")).TrimEnd('\r', '\n').Replace("\r\n", "\n").Split('\n');
            foreach (var p in sentences.paragraphs)
            {
                string orig = "";
                foreach (var sentence in c.GetParagraph(p.p).sentences)
                {
                    orig += sentence.orig;
                    foreach (string term in sentence.terms) Check.Equal(term, c.FindTerm(term).term);
                }
                Check.Equal(source[p.p - 1], orig);
            }
            foreach (var card in glossary.terms) Check.Equal(card.meaning, c.FindTerm(card.term).meaning);
            Check.Equal("ㄧㄠ", c.FindTerm("要").reading);
        }
    }
}
