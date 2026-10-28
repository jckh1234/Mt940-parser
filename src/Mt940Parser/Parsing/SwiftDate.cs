using System;
using System.Globalization;

namespace Mt940Parser.Parsing
{
    /// <summary>
    /// SWIFT dates are YYMMDD. Years 00-79 map to 2000-2079 and years 80-99 map to 1980-1999.
    /// </summary>
    internal static class SwiftDate
    {
        public static bool TryParseYyMmDd(string text, out DateTime date)
        {
            date = default(DateTime);
            if (!IsDigits(text, 6))
                return false;

            var year = ExpandYear(int.Parse(text.Substring(0, 2), CultureInfo.InvariantCulture));
            var month = int.Parse(text.Substring(2, 2), CultureInfo.InvariantCulture);
            var day = int.Parse(text.Substring(4, 2), CultureInfo.InvariantCulture);
            return TryCreate(year, month, day, out date);
        }

        public static bool TryParseMmDd(string text, DateTime valueDate, out DateTime date)
        {
            date = default(DateTime);
            if (!IsDigits(text, 4))
                return false;

            var month = int.Parse(text.Substring(0, 2), CultureInfo.InvariantCulture);
            var day = int.Parse(text.Substring(2, 2), CultureInfo.InvariantCulture);
            if (!TryCreate(valueDate.Year, month, day, out date))
                return false;

            var delta = (date - valueDate).TotalDays;
            if (delta > 180d)
                return TryCreate(valueDate.Year - 1, month, day, out date);
            if (delta < -180d)
                return TryCreate(valueDate.Year + 1, month, day, out date);
            return true;
        }

        public static int ExpandYear(int yy)
        {
            return yy >= 80 ? 1900 + yy : 2000 + yy;
        }

        public static bool IsDigits(string text, int length)
        {
            if (text == null || text.Length != length)
                return false;
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                    return false;
            }

            return true;
        }

        public static bool IsAllDigits(string text, int index, int length)
        {
            if (text == null || index < 0 || length < 0 || index + length > text.Length)
                return false;
            for (int i = index; i < index + length; i++)
            {
                if (!char.IsDigit(text[i]))
                    return false;
            }

            return true;
        }

        private static bool TryCreate(int year, int month, int day, out DateTime date)
        {
            date = default(DateTime);
            if (year < 1 || year > 9999 || month < 1 || month > 12)
                return false;
            if (day < 1 || day > DateTime.DaysInMonth(year, month))
                return false;
            date = new DateTime(year, month, day);
            return true;
        }
    }
}
