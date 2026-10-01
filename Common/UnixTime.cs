using System;

namespace SoapUI.Common
{
    /// <summary>
    /// Converts between Unix epoch seconds and local <see cref="DateTime"/>.
    /// Used when rendering RBXGS stdout message timestamps.
    /// </summary>
    internal static class UnixTime
    {
        private static readonly DateTime Epoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromSeconds(long seconds)
        {
            return Epoch.AddSeconds(seconds).ToLocalTime();
        }

        public static bool TryParse(string text, out long seconds)
        {
            return long.TryParse(text, out seconds);
        }
    }
}
