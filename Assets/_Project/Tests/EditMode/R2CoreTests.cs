using System;
using System.IO;
using NUnit.Framework;

namespace Liuruan.Tests
{
    public sealed class SeasonCoreTests
    {
        [TestCase(0f, Season.Spring, Season.Summer)]
        [TestCase(.25f, Season.Summer, Season.Autumn)]
        [TestCase(.5f, Season.Autumn, Season.Winter)]
        [TestCase(.75f, Season.Winter, Season.Spring)]
        [TestCase(1f, Season.Spring, Season.Summer)]
        public void ExactBoundaries(float progress, Season current, Season next)
        {
            var b = SeasonCore.Sample(progress);
            Check.Equal(current, b.Current); Check.Equal(next, b.Next); Check.Near(0, b.Weight);
        }
        private static double Mix(float progress)
        {
            var b = SeasonCore.Sample(progress);
            double[] palette = { .2, .8, .4, .1 };
            return palette[(int)b.Current] * (1 - b.Weight) + palette[(int)b.Next] * b.Weight;
        }
        [Test] public void WeightsAndBlendedValuesAreContinuousAcrossEveryBoundary()
        {
            foreach (float boundary in new[] { .25f, .5f, .75f, 1f })
            {
                Check.Near(Mix(boundary), Mix(boundary - .000001f));
                if (boundary < 1) Check.Near(Mix(boundary), Mix(boundary + .000001f));
            }
            for (int i = 0; i < 1000; i++)
            {
                var b = SeasonCore.Sample(i / 1000f);
                Check.Equal(true, b.Weight >= 0 && b.Weight <= 1);
            }
            Check.Near(.5, SeasonCore.Sample(.125f).Weight);
            Check.Equal(Season.Spring, SeasonCore.Sample(-1).Current);
            Check.Equal(Season.Spring, SeasonCore.Sample(2).Current);
        }
        [Test] public void SixSecondsEndsInSpringAndBirdsOnlyOnceEvenWithReentry()
        {
            var c = new SeasonCore(); int birds = 0;
            c.OnBirdsSing += () => { birds++; Check.Equal(false, c.PlayHalfYear()); c.Tick(100); };
            c.Tick(10); Check.Near(0, c.Progress);
            Check.Equal(true, c.PlayHalfYear()); c.Tick(3);
            Check.Equal(false, c.PlayHalfYear()); Check.Near(.5, c.Progress);
            c.Tick(2.99f); Check.Equal(0, birds); c.Tick(.02f);
            Check.Equal(1, birds); Check.Equal(false, c.IsPlaying);
            Check.Equal(Season.Spring, c.Blend.Current); Check.Near(1, c.Progress);
            c.Tick(float.MaxValue); Check.Equal(false, c.PlayHalfYear()); Check.Equal(1, birds);
        }
        [Test] public void ExactDurationAndRepeatedHomeCallsAreIdempotent()
        {
            var c = new SeasonCore(); int birds = 0; c.OnBirdsSing += () => birds++;
            c.PlayHalfYear(); c.Tick(6); Check.Equal(1, birds);
            Check.Equal(true, c.SwapToHome()); Check.Equal(false, c.SwapToHome());
            Check.Equal(false, c.PlayHalfYear()); c.Tick(6); Check.Equal(1, birds);
        }
        [Test] public void EarlyReturnCancelsSeasonWithoutLateBirds()
        {
            var c = new SeasonCore(); int birds = 0; c.OnBirdsSing += () => birds++;
            c.PlayHalfYear(); c.Tick(1); c.SwapToHome(); c.Tick(6);
            Check.Equal(true, c.IsHome); Check.Equal(false, c.IsPlaying); Check.Equal(0, birds);
            Check.Equal(false, c.PlayHalfYear());
            var home = new SeasonCore(); home.SwapToHome(); Check.Equal(false, home.PlayHalfYear());
        }
        [Test] public void RejectsInvalidInputs()
        {
            Check.Throws(() => new SeasonCore().Tick(-1));
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            { Check.Throws(() => SeasonCore.Sample(value)); Check.Throws(() => new SeasonCore().Tick(value)); }
        }
    }

    public sealed class EpilogueCoreTests
    {
        private static string Root
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
        private static T Read<T>(string path)
        {
            string json = File.ReadAllText(Path.Combine(Root, path));
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.FromJson<T>(json);
#else
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
        }
        private static EpilogueClueDocument Clues() => Read<EpilogueClueDocument>("Assets/_Project/Data/clues.json");
        private static EpilogueCompareDocument Compare() => Read<EpilogueCompareDocument>("Assets/_Project/Data/compare.json");
        private static EpilogueModel Build(ReadingResult[] readings = null) => EpilogueCore.Build(Clues(), Compare(), 5, 3, readings ?? new ReadingResult[0], 900);
        private static void Reject(Action action)
        {
            try { action(); } catch (ArgumentException) { return; }
            throw new Exception("Expected ArgumentException");
        }
        [Test] public void RealJsonRowsMatchEveryFieldAndModelCopiesData()
        {
            var clues = Clues(); var compare = Compare();
            var model = EpilogueCore.Build(clues, compare, 0, 0, new ReadingResult[0], 0);
            Check.Equal(compare.rows.Length, model.Comparison.Count);
            for (int i = 0; i < compare.rows.Length; i++)
            {
                Check.Equal(compare.rows[i].aspect, model.Comparison[i].Aspect);
                Check.Equal(compare.rows[i].liuruan, model.Comparison[i].Liuruan);
                Check.Equal(compare.rows[i].taohua, model.Comparison[i].Taohua);
            }
            string first = model.Comparison[0].Aspect;
            compare.rows[0].aspect = "changed"; Check.Equal(first, model.Comparison[0].Aspect);
        }
        [Test] public void EndingParagraphsAreFullSourceVerbatimAndRetainOmission()
        {
            var model = Build();
            string source = File.ReadAllText(Path.Combine(Root, "sources/liuruan_tw.txt")).TrimEnd('\r', '\n').Replace("\r\n", "\n");
            Check.Equal(source, string.Join("\n", model.Paragraphs));
            Check.Equal(7, model.Paragraphs.Count);
            Check.Equal(true, model.Paragraphs[5].StartsWith("……遂停半年。"));
            Check.Equal(false, source.Contains("各就一帳宿"));
        }
        [Test] public void ThreeEvidenceOptionsAndAnswerIndexMatchSourceAndActs()
        {
            var q = Build().Question;
            Check.Equal("為什麼找到的是七世孫？", q.Prompt); Check.Equal(3, q.Options.Count); Check.Equal(1, q.AnswerIndex);
            Check.Equal("「遂停半年」與「問得七世孫」", q.Options[q.AnswerIndex]);
            string source = File.ReadAllText(Path.Combine(Root, "sources/liuruan_tw.txt"));
            foreach (string option in q.Options)
            foreach (string quote in option.Split(new[] { '「', '」' }, StringSplitOptions.RemoveEmptyEntries))
                if (quote != "與") Check.Equal(true, source.Contains(quote));
            string acts = File.ReadAllText(Path.Combine(Root, "docs/content/acts.md"));
            Check.Equal(true, acts.Contains("以異境與人間時間落差為合理推論，不假稱原文明說固定換算率。"));
            Check.Equal(true, q.Explanation.Contains("以異境與人間時間落差為合理推論，不假稱原文明說固定換算率。"));
        }
        [Test] public void ScoresUseAllNonEndingCardsAndFirstTryOnly()
        {
            var model = Build(new[] { new ReadingResult(0, true), new ReadingResult(1, false), new ReadingResult(3, true) });
            Check.Equal(5, model.Score.BarkCount); Check.Equal(3, model.Score.DriftCount);
            Check.Equal(2, model.Score.ReadingCorrect); Check.Equal(6, model.Score.ReadingTotal);
            Check.Near(2d / 6, model.Score.ReadingAccuracy); Check.Near(900, model.Score.TotalSeconds);
            Check.Near(0, Build().Score.ReadingAccuracy);
            Check.Near(0, new EndingScore(0, 0, 0, 0, 0).ReadingAccuracy);
            var all = new System.Collections.Generic.List<ReadingResult>();
            foreach (var clue in Clues().clues) if (!clue.isEnding) all.Add(new ReadingResult(clue.id, true));
            Check.Near(1, Build(all.ToArray()).Score.ReadingAccuracy);
        }
        [Test] public void RejectsDuplicateUnknownAndEndingScoreIds()
        {
            Reject(() => Build(new[] { new ReadingResult(0, true), new ReadingResult(0, true) }));
            Reject(() => Build(new[] { new ReadingResult(999, true) }));
            Reject(() => Build(new[] { new ReadingResult(6, true) }));
        }
        [Test] public void RejectsMissingAmbiguousOrBrokenData()
        {
            Reject(() => EpilogueCore.Build(null, Compare(), 0, 0, new ReadingResult[0], 0));
            Reject(() => EpilogueCore.Build(Clues(), null, 0, 0, new ReadingResult[0], 0));
            var c = Clues(); c.clues[6].isEnding = false;
            Reject(() => EpilogueCore.Build(c, Compare(), 0, 0, new ReadingResult[0], 0));
            c.clues[6].isEnding = true; c.clues[0].isEnding = true;
            Reject(() => EpilogueCore.Build(c, Compare(), 0, 0, new ReadingResult[0], 0));
            var compare = Compare(); compare.rows[0].aspect = " ";
            Reject(() => EpilogueCore.Build(Clues(), compare, 0, 0, new ReadingResult[0], 0));
            c = Clues(); c.clues[0].id = c.clues[1].id;
            Reject(() => EpilogueCore.Build(c, Compare(), 0, 0, new ReadingResult[0], 0));
        }
        [Test] public void RejectsInvalidScoreInputs()
        {
            Check.Throws(() => new EndingScore(-1, 0, 0, 0, 0)); Check.Throws(() => new EndingScore(6, 0, 0, 0, 0));
            Check.Throws(() => new EndingScore(0, 4, 0, 0, 0)); Check.Throws(() => new EndingScore(0, -1, 0, 0, 0));
            Check.Throws(() => new EndingScore(0, 0, 2, 1, 0)); Check.Throws(() => new EndingScore(0, 0, 0, 0, -1));
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Check.Throws(() => new EndingScore(0, 0, 0, 0, value));
        }
    }

    public sealed class CharacterBobCoreTests
    {
        [Test] public void IdleHasNoBounceSwayOrLeanAndBreathStaysWithinOnePointFivePercent()
        {
            var c = new CharacterBobCore(); bool breathing = false;
            c.Tick(.2f, 4);
            for (int i = 0; i < 1000; i++)
            {
                var p = c.Tick(.01f, 0);
                Check.Near(0, p.Height); Check.Near(0, p.RollDegrees); Check.Near(0, p.LeanDegrees);
                Check.Equal(true, p.Scale >= .985f && p.Scale <= 1.015f);
                breathing |= Math.Abs(p.Scale - 1) > .01;
            }
            Check.Equal(true, breathing);
        }
        [Test] public void FrequencyIsMonotonicAndCapsAtMaxSpeed()
        {
            var c = new CharacterBobCore(); Check.Near(0, c.Frequency(0));
            float previous = 0;
            for (int i = 1; i <= 80; i++)
            { float next = c.Frequency(i / 10f); Check.Equal(true, next > previous); previous = next; }
            Check.Near(c.Frequency(8), c.Frequency(float.MaxValue));
        }
        [Test] public void MovementUsesConfigAndZeroTimeDoesNotAdvancePose()
        {
            var c = new CharacterBobCore(4, 1, 0, .1f, 5, 10);
            var pose = c.Tick(.25f, 4);
            Check.Near(.1, pose.Height); Check.Near(5, pose.RollDegrees); Check.Near(10, pose.LeanDegrees); Check.Near(1, pose.Scale);
            var still = c.Tick(0, 4); Check.Near(pose.Height, still.Height); Check.Near(pose.RollDegrees, still.RollDegrees);
        }
        [Test] public void FiniteForHugeInputsAndLongRunningPhase()
        {
            var c = new CharacterBobCore();
            foreach (float speed in new[] { 0, .001f, 1, 8, float.MaxValue })
            foreach (float dt in new[] { 0, .016f, 1000, float.MaxValue })
            {
                var p = c.Tick(dt, speed);
                foreach (float value in new[] { p.Height, p.RollDegrees, p.LeanDegrees, p.Scale })
                    Check.Equal(true, !float.IsNaN(value) && !float.IsInfinity(value));
            }
        }
        [Test] public void RejectsInvalidSpeedTimeAndSettings()
        {
            var c = new CharacterBobCore();
            Check.Throws(() => c.Tick(-1, 0)); Check.Throws(() => c.Tick(0, -1));
            Check.Throws(() => new CharacterBobCore(0)); Check.Throws(() => new CharacterBobCore(breath: .02f));
            Check.Throws(() => new CharacterBobCore(bounce: -1));
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check.Throws(() => c.Tick(value, 0)); Check.Throws(() => c.Tick(0, value));
                Check.Throws(() => c.Frequency(value)); Check.Throws(() => new CharacterBobCore(maxSpeed: value));
            }
        }
    }
}
