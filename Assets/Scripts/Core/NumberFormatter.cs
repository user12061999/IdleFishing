using System;
using System.Globalization;

namespace IdleFishing.Core
{
    /// <summary>
    /// Tiện ích rút gọn các con số lớn trong game thành định dạng ngắn với 1 ký tự hậu tố (K, M, B, T, Q, s, S, O, N, D),
    /// tương tự các game Idle / Tycoon phổ biến (ví dụ: 1000 -> 1K, 1250000 -> 1.25M, 1000000000 -> 1B).
    /// </summary>
    public static class NumberFormatter
    {
        // 1-character suffixes for compact display in idle games
        private static readonly string[] Suffixes =
        {
            "",   // < 1K
            "K",  // Thousand (10^3)
            "M",  // Million (10^6)
            "B",  // Billion (10^9)
            "T",  // Trillion (10^12)
            "Q",  // Quadrillion (10^15)
            "s",  // Sextillion (10^18)
            "S",  // Septillion (10^21)
            "O",  // Octillion (10^24)
            "N",  // Nonillion (10^27)
            "D"   // Decillion (10^30)
        };

        /// <summary>
        /// Rút gọn số thực double thành chuỗi ngắn gọn.
        /// </summary>
        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
            if (value == 0) return "0";

            bool isNegative = value < 0;
            double absValue = Math.Abs(value);

            if (absValue < 999.5)
            {
                // Dưới 1,000 hiển thị số nguyên hoặc tối đa 2 chữ số thập phân nếu lẻ
                return (isNegative ? "-" : "") + absValue.ToString("0.##", CultureInfo.InvariantCulture);
            }

            int suffixIndex = 0;
            while (absValue >= 999.5 && suffixIndex < Suffixes.Length - 1)
            {
                absValue /= 1000.0;
                suffixIndex++;
            }

            string formatted;
            if (absValue >= 100.0)
            {
                formatted = absValue.ToString("0", CultureInfo.InvariantCulture);
            }
            else if (absValue >= 10.0)
            {
                formatted = absValue.ToString("0.#", CultureInfo.InvariantCulture);
            }
            else
            {
                formatted = absValue.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return (isNegative ? "-" : "") + formatted + Suffixes[suffixIndex];
        }

        public static string Format(long value) => Format((double)value);
        public static string Format(int value) => Format((double)value);
        public static string Format(float value) => Format((double)value);

        /// <summary>
        /// Rút gọn số kèm theo ký hiệu tiền tệ (mặc định là 'G' cho Gold).
        /// Ví dụ: 1000 -> "1K G", 1500000 -> "1.5M G"
        /// </summary>
        public static string FormatCurrency(double value, string symbol = "G")
        {
            return $"{Format(value)} {symbol}".Trim();
        }

        public static string FormatCurrency(long value, string symbol = "G") => FormatCurrency((double)value, symbol);
        public static string FormatCurrency(int value, string symbol = "G") => FormatCurrency((double)value, symbol);
        public static string FormatCurrency(float value, string symbol = "G") => FormatCurrency((double)value, symbol);
    }

    /// <summary>
    /// Extension methods để gọi nhanh trực tiếp trên int, long, float, double.
    /// </summary>
    public static class NumberFormatterExtensions
    {
        public static string ToShortString(this int val) => NumberFormatter.Format(val);
        public static string ToShortString(this long val) => NumberFormatter.Format(val);
        public static string ToShortString(this float val) => NumberFormatter.Format(val);
        public static string ToShortString(this double val) => NumberFormatter.Format(val);

        public static string ToShortCurrency(this int val, string symbol = "G") => NumberFormatter.FormatCurrency(val, symbol);
        public static string ToShortCurrency(this long val, string symbol = "G") => NumberFormatter.FormatCurrency(val, symbol);
        public static string ToShortCurrency(this float val, string symbol = "G") => NumberFormatter.FormatCurrency(val, symbol);
        public static string ToShortCurrency(this double val, string symbol = "G") => NumberFormatter.FormatCurrency(val, symbol);
    }
}
