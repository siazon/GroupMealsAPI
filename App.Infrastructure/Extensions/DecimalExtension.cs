using System;
using System.Globalization;

namespace App.Infrastructure.Extensions
{
    public static class DecimalExtension
    {
        public static string ToPriceString(this decimal? source)
        {
            return source.HasValue ? source.Value.ToPriceString() : string.Empty;
        }

        public static string ToPriceString(this decimal source)
        {
            return source.ToString("c2", CultureInfo.InvariantCulture).Replace("¤", "");
        }

        public static string ToPercentString(this decimal source)
        {
            return source.ToString("p1", CultureInfo.InvariantCulture);
        }

        public static string ToWholeNumber(this decimal? source)
        {
            return source.HasValue ?
                Math.Truncate(source.Value).ToString(CultureInfo.InvariantCulture) :
                string.Empty;
        }
    }
}