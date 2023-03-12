using App.Domain.Config;
using Microsoft.Extensions.Options;
using System;
using TimeZoneConverter;

namespace App.Infrastructure.Utility.Common
{
    public interface IDateTimeUtil
    {
        DateTime GetCurrentTime();
        DateTime? GetMinDateTime(DateTime? dateA, DateTime? dateB);
        DateTime? GetMaxDateTime(DateTime? dateA, DateTime? dateB);
        DateTime? CovertDaysOfWeekTime(DateTime? specialDayShopCollectionStartTime, string toString);
        bool IsInBetween(DateTime? timeToCompare, DateTime? fromTime, DateTime? toTime);
    }

    public class DateTimeUtil : IDateTimeUtil
    {
        private readonly AppSettingConfig _appsettingConfig;

        public DateTimeUtil(IOptions<AppSettingConfig> appsettingConfig)
        {
            _appsettingConfig = appsettingConfig.Value;
        }

        public DateTime GetCurrentTime()
        {
            var timezoneInfo = TZConvert.GetTimeZoneInfo(_appsettingConfig.TimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                timezoneInfo);
        }

        public DateTime? GetMinDateTime(DateTime? dateA, DateTime? dateB)
        {
            return dateA > dateB ? dateB : dateA;
        }

        public DateTime? GetMaxDateTime(DateTime? dateA, DateTime? dateB)
        {
            return dateA > dateB ? dateA : dateB;
        }

        public DateTime? CovertDaysOfWeekTime(DateTime? inputDate, string dayOfWeek)
        {
            var pad = 0;
            if (inputDate.Value.Hour < 7)
                pad = 1;

            if (dayOfWeek.Equals(DayOfWeek.Monday.ToString()))
                return new DateTime(2000, 1, 1, inputDate.Value.Hour, inputDate.Value.Minute, inputDate.Value.Second).AddDays(pad);
            if (dayOfWeek.Equals(DayOfWeek.Tuesday.ToString()))
                return new DateTime(2000, 1, 2, inputDate.Value.Hour, inputDate.Value.Minute, inputDate.Value.Second).AddDays(pad);
            if (dayOfWeek.Equals(DayOfWeek.Wednesday.ToString()))
                return new DateTime(2000, 1, 3, inputDate.Value.Hour, inputDate.Value.Minute, inputDate.Value.Second).AddDays(pad);

            if (dayOfWeek.Equals(DayOfWeek.Thursday.ToString()))
                return new DateTime(2000, 1, 4, inputDate.Value.Hour, inputDate.Value.Minute, inputDate.Value.Second).AddDays(pad);

            if (dayOfWeek.Equals(DayOfWeek.Friday.ToString()))
                return new DateTime(2000, 1, 5, inputDate.Value.Hour, inputDate.Value.Minute, inputDate.Value.Second).AddDays(pad);

            if (dayOfWeek.Equals(DayOfWeek.Saturday.ToString()))
                return new DateTime(2000, 1, 6, inputDate.Value.Hour, inputDate.Value.Minute, inputDate.Value.Second).AddDays(pad);

            return dayOfWeek.Equals(DayOfWeek.Sunday.ToString()) ? new DateTime(2000, 1, 7, inputDate.Value.Hour, inputDate.Value.Minute, inputDate.Value.Second).AddDays(pad) : inputDate;
        }

        public bool IsInBetween(DateTime? timeToCompare, DateTime? fromTime, DateTime? toTime)
        {
            return timeToCompare.Value.Ticks >= fromTime.Value.Ticks && timeToCompare.Value.Ticks <= toTime.Value.Ticks;
        }
    }
}