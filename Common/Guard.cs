using System;

namespace SoapUI.Common
{
    /// <summary>
    /// Lightweight precondition helpers. Keeps constructors and service
    /// entry points free of repetitive null-check boilerplate.
    /// </summary>
    internal static class Guard
    {
        public static void AgainstNull(object value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void AgainstNullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", paramName);
            }
        }

        public static void AgainstNegative(double value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, "Value cannot be negative.");
            }
        }
    }
}
