using System;

namespace HalfGripObrez
{
    internal static class PresentationMath
    {
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
