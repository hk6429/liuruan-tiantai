using System;

namespace Liuruan
{
    public sealed class MinimapFogCore
    {
        public const int Size = 32;
        private readonly bool[] explored = new bool[Size * Size];
        public float MinX { get; }
        public float MinZ { get; }
        public float Width { get; }
        public float Depth { get; }
        public bool FogEnabled { get; private set; } = true;

        public MinimapFogCore(float minX, float minZ, float width, float depth)
        {
            HungerCore.RequireFinite(minX, nameof(minX));
            HungerCore.RequireFinite(minZ, nameof(minZ));
            HungerCore.RequireFinite(width, nameof(width));
            HungerCore.RequireFinite(depth, nameof(depth));
            if (width <= 0 || depth <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            MinX = minX; MinZ = minZ; Width = width; Depth = depth;
        }

        // 最大邊界屬於最後一格；範圍外不夾入邊格，避免誤揭露。
        public bool TryWorldToCell(float x, float z, out int column, out int row)
        {
            HungerCore.RequireFinite(x, nameof(x)); HungerCore.RequireFinite(z, nameof(z));
            double u = ((double)x - MinX) / Width, v = ((double)z - MinZ) / Depth;
            column = row = -1;
            if (u < 0 || u > 1 || v < 0 || v > 1) return false;
            column = Math.Min(Size - 1, (int)(u * Size));
            row = Math.Min(Size - 1, (int)(v * Size));
            return true;
        }

        public bool Reveal(float x, float z, int radiusCells)
        {
            if (radiusCells < 0 || radiusCells >= Size) throw new ArgumentOutOfRangeException(nameof(radiusCells));
            if (!TryWorldToCell(x, z, out int cx, out int cz)) return false;
            bool changed = false;
            for (int row = Math.Max(0, cz - radiusCells); row <= Math.Min(Size - 1, cz + radiusCells); row++)
            for (int col = Math.Max(0, cx - radiusCells); col <= Math.Min(Size - 1, cx + radiusCells); col++)
            {
                if ((col - cx) * (col - cx) + (row - cz) * (row - cz) > radiusCells * radiusCells) continue;
                int index = row * Size + col;
                if (!explored[index]) { explored[index] = true; changed = true; }
            }
            return changed;
        }

        public void SetAct(int act)
        {
            if (act < 1 || act > 5) throw new ArgumentOutOfRangeException(nameof(act));
            FogEnabled = act == 1;
        }

        public byte MaskAlpha(int column, int row)
        {
            if (column < 0 || column >= Size || row < 0 || row >= Size)
                throw new ArgumentOutOfRangeException(nameof(column));
            return (byte)(FogEnabled && !explored[row * Size + column] ? 255 : 0);
        }
    }
}
