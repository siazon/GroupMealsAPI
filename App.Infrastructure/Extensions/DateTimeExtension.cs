using System;

namespace App.Infrastructure.Extensions
{
    public static class DateTimeExtension
    {

        public static DateTime? GetDateValueOnly(this DateTime? source)
        {
            return !source.HasValue ? (DateTime?)null : new DateTime(source.Value.Year, source.Value.Month, source.Value.Day, 0, 0, 0); 
        }
        public static DateTime GetFoodToday(this DateTime source)
        {
            return source.Hour < 7 ? new DateTime(source.Year,source.Month,source.Day).AddDays(-1) : new DateTime(source.Year, source.Month, source.Day);
        }

      
        public static string ToDateUtcFormat(this DateTime? source)
        {
            return source.HasValue ? source.Value.ToString("yyyy-MM-dd") : string.Empty;
        }

        public static string ToDateTimeUtcFormat(this DateTime? source)
        {
            return source.HasValue ? source.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty;
        }

        public static string ToDateTimeUtcFormat(this DateTime source)
        {
            return source.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime? GetIrishTime(this DateTime? source)
        {
            return !source.HasValue ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(source.Value, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        }

        public static DateTime? GetLocalTime(this DateTime? source, string timezoneId)
        {
            return !source.HasValue ? (DateTime?)null : TimeZoneInfo.ConvertTimeFromUtc(source.Value, TimeZoneInfo.FindSystemTimeZoneById(timezoneId));
        }

        public static DateTime GetLocalTime(this DateTime source, string timezoneId)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(source, TimeZoneInfo.FindSystemTimeZoneById(timezoneId));
        }

        public static DateTime? GetLocalTime(this DateTime? source, DateTime? destination)
        {
            return new DateTime(source.Value.Year, source.Value.Month, source.Value.Day, destination.Value.Hour, destination.Value.Minute, destination.Value.Second);
        }
    }
}