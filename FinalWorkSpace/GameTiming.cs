using System;

namespace Peace_of_Tanks
{
    internal static class GameTiming
    {
        public const int TimerIntervalMs = 8;
        private const int LegacyTimerIntervalMs = 25;

        public static double ScaleDistance(double legacyDistance)
        {
            return legacyDistance * TimerIntervalMs / LegacyTimerIntervalMs;
        }

        public static int ScaleLegacyTicks(int legacyTicks)
        {
            return Math.Max(1, (int)Math.Round(legacyTicks * LegacyTimerIntervalMs / (double)TimerIntervalMs));
        }
    }
}
