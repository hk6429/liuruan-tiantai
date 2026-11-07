using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Liuruan.Tests
{
    public sealed class MinimapFogCoreTests
    {
        [TestCase(-16f, -32f, 0, 0)]
        [TestCase(-15f, -30f, 1, 1)]
        [TestCase(0f, 0f, 16, 16)]
        [TestCase(16f, 32f, 31, 31)]
        public void WorldCoordinates(float x, float z, int col, int row)
        {
            var c = new MinimapFogCore(-16, -32, 32, 64);
            Check.Equal(true, c.TryWorldToCell(x, z, out int actualCol, out int actualRow));
            Check.Equal(col, actualCol); Check.Equal(row, actualRow);
        }
        [TestCase(-16.001f, 0f)] [TestCase(16.001f, 0f)]
        [TestCase(0f, -32.001f)] [TestCase(0f, 32.001f)]
        public void OutsideDoesNotRevealEdge(float x, float z)
        {
            var c = new MinimapFogCore(-16, -32, 32, 64);
            Check.Equal(false, c.TryWorldToCell(x, z, out int col, out int row));
            Check.Equal(-1, col); Check.Equal(-1, row);
            Check.Equal(false, c.Reveal(x, z, 2)); Check.Equal(0, Revealed(c));
        }
        [Test] public void RadiusIsCircularAndRepeatedRevealIsIdempotent()
        {
            var c = new MinimapFogCore(0, 0, 32, 32);
            Check.Equal(0, Revealed(c)); Check.Equal(true, c.Reveal(16, 16, 2));
            Check.Equal(13, Revealed(c)); Check.Equal((byte)0, c.MaskAlpha(18, 16));
            Check.Equal((byte)255, c.MaskAlpha(18, 18));
            Check.Equal(false, c.Reveal(16, 16, 2));
            c.Reveal(20, 20, 0); Check.Equal(14, Revealed(c));
        }
        [Test] public void RadiusClipsAtCornerAndKeepsHistoryAcrossActs()
        {
            var c = new MinimapFogCore(0, 0, 32, 32); c.Reveal(0, 0, 1);
            Check.Equal(3, Revealed(c));
            for (int act = 2; act <= 5; act++)
            { c.SetAct(act); Check.Equal(false, c.FogEnabled); Check.Equal(1024, Revealed(c)); }
            c.SetAct(1); Check.Equal(true, c.FogEnabled); Check.Equal(3, Revealed(c));
            c.Reveal(32, 32, 1); Check.Equal(6, Revealed(c));
        }
        [Test] public void RejectsInvalidInputs()
        {
            Check.Throws(() => new MinimapFogCore(0, 0, 0, 1));
            Check.Throws(() => new MinimapFogCore(0, 0, 1, -1));
            var c = new MinimapFogCore(0, 0, 32, 32);
            Check.Throws(() => c.SetAct(0)); Check.Throws(() => c.SetAct(6));
            Check.Throws(() => c.Reveal(0, 0, -1)); Check.Throws(() => c.Reveal(0, 0, 32));
            Check.Throws(() => c.MaskAlpha(-1, 0)); Check.Throws(() => c.MaskAlpha(0, 32));
            foreach (float f in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check.Throws(() => new MinimapFogCore(f, 0, 1, 1));
                Check.Throws(() => new MinimapFogCore(0, f, 1, 1));
                Check.Throws(() => new MinimapFogCore(0, 0, f, 1));
                Check.Throws(() => new MinimapFogCore(0, 0, 1, f));
                Check.Throws(() => c.Reveal(f, 0, 1)); Check.Throws(() => c.Reveal(0, f, 1));
            }
        }
        private static int Revealed(MinimapFogCore c)
        {
            int count = 0;
            for (int row = 0; row < 32; row++) for (int col = 0; col < 32; col++)
                if (c.MaskAlpha(col, row) == 0) count++;
            return count;
        }
    }

    public sealed class ClimbCoreTests
    {
        private static ClimbCore Climbing()
        { var c = new ClimbCore(4, 6); c.Attach(0, 0); c.Tick(0.1f, 0, 0, 5); return c; }
        [Test] public void FullTopOutSequenceAndEvents()
        {
            var c = new ClimbCore(4, 6); var states = new List<ClimbState>();
            c.OnStateChanged += s => states.Add(s);
            Check.Equal(ClimbState.Grounded, c.State); Check.Equal(false, c.IsLocked);
            Check.Equal(true, c.Attach(0, 0)); Check.Equal(true, c.IsLocked);
            Check.Equal(ClimbState.Attaching, c.State); Check.Equal(false, c.Attach(0, 1));
            c.Tick(0, 0, 1, 5); Check.Equal(ClimbState.Attaching, c.State);
            c.Tick(0.1f, 0, 1, 5); Check.Equal(ClimbState.Climbing, c.State); Check.Near(0, c.Y);
            c.Tick(10, 0, 1, 5); Check.Equal(ClimbState.TopOut, c.State); Check.Near(6, c.Y);
            Check.Equal(true, c.IsLocked); c.Tick(1, 1, -1, 5); Check.Near(6, c.Y);
            c.Finish(); c.Finish(); Check.Equal(false, c.IsLocked);
            Check.Equal("Attaching,Climbing,TopOut,Grounded", string.Join(",", states));
            Check.Equal(true, c.Attach(0, 0));
        }
        [Test] public void DescendingToBottomReleasesAutomatically()
        {
            var c = Climbing(); c.Tick(1, 0, 1, 5); c.Tick(2, 0, -1, 5);
            Check.Equal(ClimbState.Release, c.State); Check.Near(0, c.Y);
            Check.Equal(false, c.IsLocked); c.Finish(); Check.Equal(ClimbState.Grounded, c.State);
        }
        [Test] public void StationaryAtBottomDoesNotRelease()
        { var c = Climbing(); c.Tick(1, 0, 0, 5); Check.Equal(ClimbState.Climbing, c.State); }
        [TestCase(0f, 0.6f)] [TestCase(0.999f, 0.6f)] [TestCase(1f, 1.2f)] [TestCase(5f, 1.2f)]
        public void HungerSpeedBoundary(float hunger, float expected)
        { var c = Climbing(); c.Tick(1, 0, 1, hunger); Check.Near(expected, c.Y); }
        [Test] public void DiagonalSpeedAndSideBounds()
        {
            var c = Climbing(); c.Tick(1, 1, 1, 5);
            Check.Near(1.2 / Math.Sqrt(2), c.X); Check.Near(c.X, c.Y);
            c.Tick(100, 1, 0, 5); Check.Near(2, c.X);
            c.Tick(100, -1, 0, 5); Check.Near(-2, c.X);
        }
        [Test] public void AttachClampsAndExplicitReleaseIsIdempotent()
        {
            var c = new ClimbCore(4, 6); c.Attach(100, -2);
            Check.Near(2, c.X); Check.Near(0, c.Y);
            c.Release(); c.Release(); Check.Equal(ClimbState.Release, c.State);
            c.Finish(); c.Release(); Check.Equal(ClimbState.Grounded, c.State);
            c.Attach(-100, 100); Check.Near(-2, c.X); Check.Near(6, c.Y);
            c.Finish(); Check.Equal(ClimbState.Attaching, c.State);
        }
        [Test] public void RejectsInvalidInputs()
        {
            Check.Throws(() => new ClimbCore(0, 1)); Check.Throws(() => new ClimbCore(1, -1));
            var c = Climbing(); Check.Throws(() => c.Tick(-1, 0, 0, 5)); Check.Throws(() => c.Tick(1, 0, 0, -1));
            foreach (float f in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check.Throws(() => new ClimbCore(f, 1)); Check.Throws(() => new ClimbCore(1, f));
                Check.Throws(() => c.Attach(f, 0)); Check.Throws(() => c.Attach(0, f));
                Check.Throws(() => c.Tick(f, 0, 0, 5)); Check.Throws(() => c.Tick(1, f, 0, 5));
                Check.Throws(() => c.Tick(1, 0, f, 5)); Check.Throws(() => c.Tick(1, 0, 0, f));
            }
        }
    }

    public sealed class DriftCoreTests
    {
        private static WorldPosition P(float x, float z = 0) => new WorldPosition(x, 0, z);
        private static DriftCore Item(int id, DriftCollectionCore collection)
            => new DriftCore(id, new[] { P(0), P(10) }, collection, 10);
        [Test] public void CurveSamplesEndpointsKnotsAndBend()
        {
            var c = new DriftCore(1, new[] { P(0), P(2, 2), P(4) }, new DriftCollectionCore());
            Check.Near(0, c.Sample(0).X); Check.Near(4, c.Sample(1).X);
            Check.Near(2, c.Sample(0.5f).X); Check.Near(2, c.Sample(0.5f).Z);
            Check.Near(0.875, c.Sample(0.25f).X); Check.Near(1.125, c.Sample(0.25f).Z);
        }
        [Test] public void LoopsAtExactPeriodAndPreservesRemainder()
        {
            var c = Item(1, new DriftCollectionCore()); c.Tick(5); Check.Near(5, c.Position.X);
            c.Tick(5); Check.Near(0, c.Position.X);
            c.Tick(25); Check.Near(5, c.Position.X); c.Tick(0); Check.Near(5, c.Position.X);
            c.Tick(float.MaxValue); Check.Equal(true, !float.IsNaN(c.Position.X));
        }
        [TestCase(2f, true)] [TestCase(2.001f, false)]
        public void PickupRadiusBoundary(float distance, bool expected)
        {
            var collection = new DriftCollectionCore(); var c = Item(1, collection);
            Check.Equal(expected, c.CanPickUp(P(distance))); Check.Equal(expected, c.TryPickUp(P(distance)));
            Check.Equal(expected ? 1 : 0, collection.Count);
        }
        [Test] public void CollectedItemsStopAndCannotReappearOrNotifyAgain()
        {
            var collection = new DriftCollectionCore(); var c = Item(1, collection); int picked = 0;
            c.OnPickedUp += id => { Check.Equal(1, id); picked++; };
            c.Tick(5); Check.Equal(true, c.TryPickUp(P(5)));
            c.Tick(100); Check.Near(5, c.Position.X); Check.Equal(true, c.IsCollected);
            Check.Equal(false, c.TryPickUp(P(5))); Check.Equal(1, picked);
            var duplicate = Item(1, collection); Check.Equal(true, duplicate.IsCollected);
            Check.Equal(false, duplicate.CanPickUp(P(0))); Check.Equal(1, collection.Count);
        }
        [Test] public void AllCollectedOnceEvenWithReentrantDuplicatePickup()
        {
            var collection = new DriftCollectionCore(); var ids = new List<int>(); int all = 0;
            collection.OnCollected += id => { ids.Add(id); Check.Equal(false, collection.Collect(id)); };
            collection.OnAllCollected += () => { all++; Check.Equal(false, collection.Collect(3)); };
            Check.Equal(true, Item(2, collection).TryPickUp(P(0))); Check.Equal(0, all);
            Check.Equal(true, Item(1, collection).TryPickUp(P(0))); Check.Equal(0, all);
            Check.Equal(true, Item(3, collection).TryPickUp(P(0))); Check.Equal(1, all);
            Check.Equal(3, collection.Count); Check.Equal("2,1,3", string.Join(",", ids));
            Check.Equal(false, collection.Collect(1)); Check.Equal(1, all);
        }
        [Test] public void PickupUsesThreeDimensionsAndCopiesWaypoints()
        {
            var points = new[] { P(0), P(10) }; var c = new DriftCore(1, points, new DriftCollectionCore());
            points[0] = P(100); Check.Near(0, c.Sample(0).X);
            Check.Equal(false, c.CanPickUp(new WorldPosition(0, 2.01f, 0)));
        }
        [Test] public void RejectsInvalidInputs()
        {
            var collection = new DriftCollectionCore(); var c = Item(1, collection);
            Check.Throws(() => collection.Collect(0)); Check.Throws(() => collection.IsCollected(4));
            Check.Throws(() => Item(4, collection)); Check.Throws(() => c.Tick(-1));
            Check.Throws(() => c.Sample(-0.01f)); Check.Throws(() => c.Sample(1.01f));
            Check.Throws(() => new DriftCore(1, new[] { P(0), P(1) }, collection, 0));
            bool rejected = false;
            try { new DriftCore(1, new WorldPosition[0], collection); } catch (ArgumentException) { rejected = true; }
            Check.Equal(true, rejected); rejected = false;
            try { new DriftCore(1, new[] { P(0), P(1) }, null); } catch (ArgumentNullException) { rejected = true; }
            Check.Equal(true, rejected);
            foreach (float f in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check.Throws(() => c.Tick(f)); Check.Throws(() => c.Sample(f));
                Check.Throws(() => new DriftCore(1, new[] { P(0), P(1) }, collection, f));
            }
        }
    }
}
