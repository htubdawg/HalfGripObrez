using System;

namespace HalfGripObrez
{
    internal static class PresentationMath
    {
        // Image.SetNativeSize at the game's standard 100 reference pixels per unit:
        // a dedicated 64x24 sprite displays as 160x60 UI pixels.
        internal const float GarageWidth = 1.6f, GarageHeight = 0.6f;
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
