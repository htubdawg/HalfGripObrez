using System;

namespace HalfGripObrez
{
    internal static class PresentationMath
    {
        internal static float MedianPositive(float[] values)
        {
            float[] sorted = Array.FindAll(values, value => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value));
            if (sorted.Length == 0) return 0f;
            Array.Sort(sorted);
            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) * 0.5f : sorted[middle];
        }

        internal static float NativeIconPixelDensity(int width, int height, float[] nativeWidths, float[] nativeHeights)
        {
            // Image.SetNativeSize uses rect / sprite PPU * canvas reference PPU.
            // Matching native rect/PPU sizes makes the canvas and screen scale cancel.
            // Width and height are measured from real catalog icons, not screen pixels.
            float maxWidth = MedianPositive(nativeWidths), maxHeight = MedianPositive(nativeHeights);
            if (maxWidth <= 0f || maxHeight <= 0f) return 0f;
            return FitPixelDensity(width, height, maxWidth, maxHeight);
        }
        internal static float FitPixelDensity(int width, int height, float maxWidth, float maxHeight)
        {
            return Math.Max(width / Math.Max(0.001f, maxWidth), height / Math.Max(0.001f, maxHeight));
        }
        internal static bool NearReceiver(float x, float y, float depth, float portX, float portY, float portDepth, float gunWidth)
        {
            if (depth <= 0 || portDepth <= 0) return false;
            float radius = Math.Max(14f, gunWidth * 0.11f);
            float dx = x - portX, dy = y - portY;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
