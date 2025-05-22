using System;
using System.Globalization;

namespace WorkoutTrackerWeb.Extensions
{
    /// <summary>
    /// Extension methods for handling string values safely
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Safely gets a string value by ensuring it's never null.
        /// Returns an empty string for null values.
        /// </summary>
        /// <param name="value">The string value to ensure is not null</param>
        /// <returns>The original string or an empty string if null</returns>
        public static string SafeValue(this string value)
        {
            return value ?? string.Empty;
        }

        /// <summary>
        /// Safely gets a string value and ensures it's not null.
        /// Returns the specified default value for null strings.
        /// </summary>
        /// <param name="value">The string value to ensure is not null</param>
        /// <param name="defaultValue">The default value to use if the string is null</param>
        /// <returns>The original string or the default value if null</returns>
        public static string SafeValue(this string value, string defaultValue)
        {
            return value ?? defaultValue;
        }

        /// <summary>
        /// Extension method for EF Core queries to safely handle potentially null string values.
        /// This is specifically useful for preventing SqlNullValueException when accessing string properties.
        /// </summary>
        /// <param name="value">The string value to check for null</param>
        /// <returns>The original string or empty string if null</returns>
        public static string SqlSafe(this string value)
        {
            return value ?? string.Empty;
        }

        public static string ToTitleCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(text.ToLower());
        }
    }
}