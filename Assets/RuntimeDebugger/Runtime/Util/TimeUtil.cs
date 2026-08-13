using System.Diagnostics;

namespace RuntimeDebugger
{
    /// <summary>
    /// High-resolution monotonic timestamp provider (milliseconds).
    /// Uses Stopwatch — unaffected by system clock changes.
    /// </summary>
    public static class TimeUtil
    {
        private static readonly long s_epoch = Stopwatch.GetTimestamp();
        private static readonly double s_msPerTick = 1000.0 / Stopwatch.Frequency;

        public static long NowMs()
        {
            return (long)((Stopwatch.GetTimestamp() - s_epoch) * s_msPerTick);
        }

        public static long ElapsedMs(long sinceMs)
        {
            return NowMs() - sinceMs;
        }
    }
}
