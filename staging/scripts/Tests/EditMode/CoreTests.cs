using System;
using NUnit.Framework;

namespace Liuruan.Tests
{
    internal static class Check
    {
        public static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
        }
        public static void Near(double expected, double actual)
        {
            if (double.IsNaN(actual) || Math.Abs(expected - actual) > 0.00001)
                throw new Exception($"Expected {expected}, got {actual}");
        }
        public static void Throws(Action action)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new Exception("Expected ArgumentOutOfRangeException");
        }
    }

    public sealed class HungerCoreTests
    {
        [Test] public void StartsWithThreeAndNormalSpeed()
        {
            var c = new HungerCore();
            Check.Near(3, c.Value); Check.Near(24, c.IntervalSeconds);
            Check.Near(1, c.SpeedMultiplier); Check.Equal(false, c.IsPaused);
        }
        [TestCase(-1f, 0f)] [TestCase(0f, 0f)] [TestCase(5f, 5f)] [TestCase(6f, 5f)]
        public void ClampsInitialValue(float initial, float expected)
        { Check.Near(expected, new HungerCore(initial).Value); }
        [Test] public void DropsAtExactIntervalAndPreservesRemainder()
        {
            var c = new HungerCore();
            c.Tick(23); Check.Near(3, c.Value);
            c.Tick(1); Check.Near(2.5, c.Value);
            c.Tick(49); Check.Near(1.5, c.Value);
            c.Tick(23); Check.Near(1, c.Value);
        }
        [Test] public void SupportsCustomInterval()
        { var c = new HungerCore(3, 2); c.Tick(2); Check.Near(2.5, c.Value); }
        [Test] public void PauseResumeIsIdempotentAndKeepsRemainder()
        {
            var c = new HungerCore(); c.Tick(12); c.Pause(); c.Pause(); c.Tick(1000);
            Check.Equal(true, c.IsPaused); Check.Near(3, c.Value);
            c.Resume(); c.Resume(); c.Tick(12); Check.Near(2.5, c.Value);
        }
        [Test] public void StarvesOncePerCrossingAndRecoversWithoutDying()
        {
            var c = new HungerCore(); int count = 0; c.OnStarving += () => count++;
            c.Tick(144); Check.Near(0, c.Value); Check.Near(0.6, c.SpeedMultiplier);
            c.Tick(10000); c.Eat(0); Check.Equal(1, count);
            c.Eat(0.25f); Check.Near(1, c.SpeedMultiplier);
            c.Tick(24); Check.Near(0, c.Value); Check.Equal(2, count);
            c.FillAll(); Check.Near(5, c.Value); Check.Near(1, c.SpeedMultiplier);
        }
        [Test] public void EatClampFillAndChangeEvents()
        {
            var c = new HungerCore(); int events = 0; float observed = 0;
            c.OnChanged += v => { events++; observed = v; };
            c.Eat(0); Check.Equal(0, events);
            c.Eat(0.25f); Check.Near(3.25, observed);
            c.Eat(float.MaxValue); Check.Near(5, c.Value);
            c.FillAll(); Check.Equal(2, events);
            c.Tick(24); Check.Near(4.5, observed); Check.Equal(3, events);
            c.FillAll(); Check.Near(5, observed); Check.Equal(4, events);
        }
        [Test] public void FoodWorksWhilePausedAndDoesNotResetActiveClock()
        {
            var c = new HungerCore(); c.Tick(12); c.Pause(); c.Eat(1); c.FillAll();
            Check.Near(5, c.Value); c.Resume(); c.Tick(12); Check.Near(4.5, c.Value);
        }
        [Test] public void LargeTickClampsAndRecoveryGetsFreshInterval()
        {
            var c = new HungerCore(); c.Tick(float.MaxValue); Check.Near(0, c.Value);
            c.FillAll(); c.Tick(23); Check.Near(5, c.Value); c.Tick(1); Check.Near(4.5, c.Value);
        }
        [Test] public void ZeroTickDoesNothing()
        { var c = new HungerCore(); c.Tick(0); Check.Near(3, c.Value); }
        [Test] public void RejectsInvalidInputs()
        {
            foreach (float v in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check.Throws(() => new HungerCore(v)); Check.Throws(() => new HungerCore(3, v));
                var c = new HungerCore(); Check.Throws(() => c.Tick(v)); Check.Throws(() => c.Eat(v));
            }
            Check.Throws(() => new HungerCore(3, 0)); Check.Throws(() => new HungerCore(3, -1));
            Check.Throws(() => new HungerCore().Tick(-1)); Check.Throws(() => new HungerCore().Eat(-1));
        }
    }

    public sealed class CompanionCoreTests
    {
        private static WorldPosition P(float x, float y = 0, float z = 0) => new WorldPosition(x, y, z);
        private static CompanionState Step(CompanionCore c, float dt, float distance)
            => c.Tick(dt, P(0), P(distance), P(0, 0, 1));

        [TestCase(0f, CompanionState.Idle)] [TestCase(2.499f, CompanionState.Idle)]
        [TestCase(2.5f, CompanionState.Follow)] [TestCase(12f, CompanionState.Follow)]
        [TestCase(12.001f, CompanionState.Catchup)] [TestCase(30f, CompanionState.Catchup)]
        [TestCase(30.001f, CompanionState.Teleport)]
        public void DistanceBoundaries(float distance, CompanionState expected)
        { Check.Equal(expected, Step(new CompanionCore(), 0, distance)); }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void AllSixteenDistanceDrivenTransitions(int sourceIndex)
        {
            float[] distances = { 1, 3, 20, 31 };
            CompanionState[] states = { CompanionState.Idle, CompanionState.Follow,
                CompanionState.Catchup, CompanionState.Teleport };
            for (int target = 0; target < distances.Length; target++)
            {
                var c = new CompanionCore();
                Check.Equal(states[sourceIndex], Step(c, 0, distances[sourceIndex]));
                Check.Equal(states[target], Step(c, 0, distances[target]));
            }
        }
        [Test] public void MovesThroughEveryStateAndBack()
        {
            var c = new CompanionCore(); Check.Equal(CompanionState.Idle, c.State);
            Check.Equal(CompanionState.Follow, Step(c, 0, 3));
            Check.Equal(CompanionState.Catchup, Step(c, 0, 13));
            Check.Equal(CompanionState.Follow, Step(c, 0, 12));
            Check.Equal(CompanionState.Idle, Step(c, 0, 2));
            Check.Equal(CompanionState.Teleport, Step(c, 0, 31));
            Check.Equal(CompanionState.Idle, Step(c, 0, 2));
        }
        [Test] public void TeleportIsTwoMetresBehindNormalizedHorizontalForward()
        {
            var c = new CompanionCore(); c.Tick(0, P(10, 4, 10), P(100), P(3, 7, 4));
            Check.Near(8.8, c.Destination.X); Check.Near(4, c.Destination.Y); Check.Near(8.4, c.Destination.Z);
        }
        [Test] public void ZeroForwardUsesWorldBack()
        {
            var c = new CompanionCore(); c.Tick(0, P(10, 4, 10), P(100), P(0));
            Check.Near(10, c.Destination.X); Check.Near(4, c.Destination.Y); Check.Near(8, c.Destination.Z);
        }
        [Test] public void UsesThreeDimensionalDistance()
        {
            var c = new CompanionCore();
            Check.Equal(CompanionState.Catchup, c.Tick(0, P(0), P(0, 13), P(0)));
        }
        [Test] public void StuckAtExactlyThreeSecondsTeleportsThenResets()
        {
            var c = new CompanionCore(); Step(c, 0, 5);
            Check.Equal(CompanionState.Follow, Step(c, 2.999f, 5));
            Check.Equal(CompanionState.Teleport, Step(c, 0.002f, 5));
            Check.Equal(CompanionState.Follow, Step(c, 0, 5));
        }
        [Test] public void ExactStuckBoundary()
        {
            var c = new CompanionCore(); Step(c, 0, 5);
            Check.Equal(CompanionState.Teleport, Step(c, 3, 5));
        }
        [Test] public void ProgressAndIdleResetStuckTimer()
        {
            var c = new CompanionCore(); Step(c, 0, 5); Step(c, 2, 5);
            Check.Equal(CompanionState.Follow, Step(c, 2, 5.1f));
            Step(c, 2, 5.1f); Step(c, 100, 1);
            Check.Equal(CompanionState.Follow, Step(c, 0, 5.1f));
        }
        [Test] public void SlowProgressAccumulatesAgainstAnchor()
        {
            var c = new CompanionCore(); Step(c, 0, 5);
            Step(c, 1, 5.02f); Step(c, 1, 5.04f);
            Check.Equal(CompanionState.Follow, Step(c, 1, 5.06f));
        }
        [Test] public void FiveCentimetresResetsStuckTimer()
        {
            var c = new CompanionCore(); Step(c, 0, 5); Step(c, 2, 5);
            Check.Equal(CompanionState.Follow, Step(c, 1, 5.05f));
            Check.Equal(CompanionState.Follow, Step(c, 2.99f, 5.05f));
        }
        [Test] public void CatchupCanAlsoGetStuck()
        {
            var c = new CompanionCore(); Step(c, 0, 20);
            Check.Equal(CompanionState.Teleport, Step(c, 3, 20));
        }
        [Test] public void IdleNeverCountsAsStuck()
        { var c = new CompanionCore(); Step(c, 0, 1); Check.Equal(CompanionState.Idle, Step(c, 100, 1)); }
        [Test] public void CallCooldownAndExactReadyBoundary()
        {
            var c = new CompanionCore(); Check.Near(0, c.CooldownRatio);
            Check.Equal(true, c.Call()); Check.Near(1, c.CooldownRatio);
            Check.Equal(false, c.Call()); Step(c, 4, 1); Check.Near(0.5, c.CooldownRatio);
            Check.Equal(false, c.Call()); Check.Near(0.5, c.CooldownRatio);
            Step(c, 4, 1); Check.Near(0, c.CooldownRatio); Check.Equal(true, c.Call());
            Step(c, 100, 1); Check.Near(0, c.CooldownRatio);
        }
        [Test] public void CallAcceleratesUntilReunion()
        {
            var c = new CompanionCore(); c.Call();
            Check.Equal(CompanionState.Catchup, Step(c, 0, 3));
            Check.Equal(CompanionState.Catchup, Step(c, 8, 4));
            Check.Equal(CompanionState.Idle, Step(c, 0, 2));
            Check.Equal(CompanionState.Follow, Step(c, 0, 3));
            Check.Near(0, c.Destination.X);
        }
        [Test] public void TeleportClearsCallButPreservesCooldown()
        {
            var c = new CompanionCore(); c.Call(); Step(c, 1, 31);
            Check.Near(0.875, c.CooldownRatio);
            Check.Equal(CompanionState.Follow, Step(c, 0, 3));
        }
        [Test] public void RejectsInvalidTimeAndCoordinates()
        {
            var c = new CompanionCore(); Check.Throws(() => Step(c, -1, 3));
            foreach (float v in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check.Throws(() => Step(c, v, 3)); Check.Throws(() => P(v));
                Check.Throws(() => P(0, v)); Check.Throws(() => P(0, 0, v));
            }
        }
    }
}
