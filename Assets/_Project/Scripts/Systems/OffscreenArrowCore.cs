using System;

namespace Liuruan
{
    public readonly struct OffscreenArrowResult
    {
        public bool IsOnscreen { get; }
        public bool IsBehind { get; }
        public float X { get; }
        public float Y { get; }
        public float AngleDegrees { get; }
        public OffscreenArrowResult(bool onscreen, bool behind, float x, float y, float angle)
        { IsOnscreen = onscreen; IsBehind = behind; X = x; Y = y; AngleDegrees = angle; }
    }
    public static class OffscreenArrowCore
    {
        // VP 為 row-major、column-vector；Unity CPU clip Z 為 [-W,W]。
        public static OffscreenArrowResult Project(float[] vp, WorldPosition target, float inset = .05f, float aspect = 1)
        {
            if (vp == null || vp.Length != 16) throw new ArgumentException("VP 必須有 16 個元素。");
            foreach (float v in vp) HungerCore.RequireFinite(v, nameof(vp));
            HungerCore.RequireFinite(inset, nameof(inset)); HungerCore.RequireFinite(aspect, nameof(aspect));
            if (inset < 0 || inset >= .5f || aspect <= 0) throw new ArgumentOutOfRangeException(nameof(inset));
            double x = Row(vp, 0, target), y = Row(vp, 4, target), z = Row(vp, 8, target), w = Row(vp, 12, target);
            bool behind = w <= 0;
            bool inside = !behind && Math.Abs(x) <= w && Math.Abs(y) <= w && Math.Abs(z) <= w;
            if (inside) return new OffscreenArrowResult(true, false, (float)(x / w * .5 + .5), (float)(y / w * .5 + .5), 0);
            // 先除 W，再將背後投影翻轉；等同除 |W|，避免 W=0 除零。
            double divisor = Math.Max(Math.Abs(w), 1e-12);
            x /= divisor; y /= divisor;
            if (Math.Abs(x) + Math.Abs(y) < 1e-12) y = -1;
            double edge = .5 - inset, scale = edge / Math.Max(Math.Abs(x), Math.Abs(y));
            return new OffscreenArrowResult(false, behind, (float)(.5 + x * scale), (float)(.5 + y * scale),
                (float)(Math.Atan2(y, x * aspect) * 180 / Math.PI));
        }
        static double Row(float[] m, int i, WorldPosition p) => (double)m[i] * p.X + (double)m[i + 1] * p.Y + (double)m[i + 2] * p.Z + m[i + 3];
    }
}
