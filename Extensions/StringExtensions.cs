using System.Globalization;

namespace WorkoutTrackerWeb.Extensions
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(text.ToLower());
        }
    }
}