namespace App.Infrastructure.Utility.Common
{
    public class DecimalUtil
    {
        public static bool AreTheSame(decimal? a, decimal? b, int precision)
        {
            if (!a.HasValue && !b.HasValue)
                return true;

            return decimal.Round(a.Value, precision) == decimal.Round(b.Value, precision);
        }
    }
}