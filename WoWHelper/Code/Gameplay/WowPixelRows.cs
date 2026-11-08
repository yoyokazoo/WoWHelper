using System;
using System.Drawing;

namespace WoWHelper
{
    // The addon's pixel rows (InitializePixelRow() in UIFunctions.lua), top to bottom. Each row
    // is PixelSize physical pixels tall, pinned to the screen's literal top-left corner, so the
    // layout is the same on every resolution. Order MUST match the Lua side's -- a one-sided
    // change doesn't fail loudly, it silently misreads every row it shifts.
    public enum PixelRow
    {
        // Bars: filled left to right, filled pixel count = percent. Row 0 also carries the
        // addon-loaded sentinel box right after the bar (see AddonLoadedPosition).
        PlayerHealthBar = 0,
        ResourceBar = 1,
        TargetHealthBar = 2,

        // Bit rows (see PixelRowBits).
        MapX = 3,
        MapY = 4,
        FacingDegrees = 5,
        MultiBoolOne = 6,
        MultiBoolTwo = 7,
        ClassBoolOne = 8,
        ClassBoolTwo = 9,
        MultiIntOne = 10,
    }

    // Layout constants and readers for the pixel rows. Every constant here MUST match its
    // counterpart in the addon (UIFunctions.lua / MathFunctions.lua). We always read the CENTER
    // pixel of a box or bar row, for margin against edge blur.
    public static class WowPixelRows
    {
        public const int PixelSize = 3;                 // Lua: PIXEL_SIZE
        public const int StatusBarWidth = 100;          // Lua: PIXEL_BAR_WIDTH
        public const int BitsPerRow = 24;               // Lua: PIXEL_ROW_BITS
        public const int FixedPointFractionBits = 15;   // Lua: FIXED_POINT_FRACTION_BITS

        private const int CenterOffset = PixelSize / 2;

        // Lua: ADDON_LOADED_COLOR. Exact match means the addon is loaded and the rows are real;
        // anything else (the login screen, another window covering WoW, ...) means they aren't.
        public static readonly Color ADDON_LOADED_COLOR = Color.FromArgb(96, 255, 117);

        public static int RowCenterY(PixelRow row) => (int)row * PixelSize + CenterOffset;

        private static int BoxCenterX(int boxIndex) => boxIndex * PixelSize + CenterOffset;

        // The sentinel box sits right after the player health bar, in row 0.
        public static Point AddonLoadedPosition =>
            new Point(StatusBarWidth + CenterOffset, RowCenterY(PixelRow.PlayerHealthBar));

        // The bottom-right-most pixel any reader here touches, for sizing the screen capture.
        public static Point FarthestReadPixel =>
            new Point(Math.Max(AddonLoadedPosition.X, BoxCenterX(BitsPerRow - 1)), RowCenterY(PixelRow.MultiIntOne));

        public static bool ReadAddonLoaded(Bitmap bmp)
        {
            Color color = bmp.GetPixel(AddonLoadedPosition.X, AddonLoadedPosition.Y);
            return color.R == ADDON_LOADED_COLOR.R
                && color.G == ADDON_LOADED_COLOR.G
                && color.B == ADDON_LOADED_COLOR.B;
        }

        // A bar fills left to right, so "is pixel x filled" is monotonic (true...true,
        // false...false) and the filled-pixel count -- the percent -- is the index of the first
        // unfilled pixel. Binary search for it: at most 7 pixel reads for 0-100. Invariant: every
        // pixel < lo is filled, every pixel >= hi is empty.
        //
        // "Filled" = the bar's fill channel is at least half-bright. Empty is black, so a
        // partially-covered edge pixel counts once it's more than half covered -- i.e. the result
        // rounds to the nearest percent.
        public static int ReadStatusBarPercent(Bitmap bmp, PixelRow row, Func<Color, byte> fillChannel)
        {
            int y = RowCenterY(row);
            int lo = 0;
            int hi = StatusBarWidth;

            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (fillChannel(bmp.GetPixel(mid, y)) >= 128)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }

        // A bit row's BitsPerRow boxes, green = 1 and red = 0, as one unsigned integer: box 1
        // (leftmost) is bit 0.
        public static PixelRowBits ReadBits(Bitmap bmp, PixelRow row)
        {
            int y = RowCenterY(row);
            uint value = 0;

            for (int i = 0; i < BitsPerRow; i++)
            {
                Color color = bmp.GetPixel(BoxCenterX(i), y);
                if (color.G > color.R)
                {
                    value |= 1u << i;
                }
            }

            return new PixelRowBits(value);
        }
    }

    // One decoded bit row, read back the way the Lua side encoded it (MathFunctions.lua's
    // Encode* helpers): as bool fields, three bytes, or a fixed-point number.
    public struct PixelRowBits
    {
        public uint Value { get; }

        public PixelRowBits(uint value)
        {
            Value = value;
        }

        // 1-based, to match the Lua field lists (MULTI_BOOL_ONE_FIELDS etc.): this[1] is the
        // leftmost box. Legacy names: Rn = box n, Gn = box 8+n, Bn = box 16+n.
        public bool this[int boxNumber] => ((Value >> (boxNumber - 1)) & 1) != 0;

        // byteIndex 0-2: boxes 1-8, 9-16, 17-24 (Lua: EncodeBytes' three arguments in order).
        public int Byte(int byteIndex) => (int)((Value >> (8 * byteIndex)) & 0xFF);

        // Lua: EncodeFixedPoint.
        public float FixedPoint => Value / (float)(1 << WowPixelRows.FixedPointFractionBits);
    }
}
