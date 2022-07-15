using System;
using System.Collections.Generic;
using System.Text;

namespace Timeslots.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsBefore(this DateTimeOffset thisDateTimeOffset, DateTimeOffset otherDateTimeOffset)
        {
            return thisDateTimeOffset.CompareTo(otherDateTimeOffset) < 0;
        }
        public static bool IsBeforeOrSame(this DateTimeOffset thisDateTimeOffset, DateTimeOffset otherDateTimeOffset)
        {
            return thisDateTimeOffset.CompareTo(otherDateTimeOffset) <= 0;
        }

        public static bool IsAfter(this DateTimeOffset thisDateTimeOffset, DateTimeOffset otherDateTimeOffset)
        {
            return thisDateTimeOffset.CompareTo(otherDateTimeOffset) > 0;
        }

        public static bool IsAfterOrSame(this DateTimeOffset thisDateTimeOffset, DateTimeOffset otherDateTimeOffset)
        {
            return thisDateTimeOffset.CompareTo(otherDateTimeOffset) >= 0;
        }

        /// <summary>
        /// Calculates the DateTimeOffset for the next DayOfWeek at an optional timeOfDay
        /// </summary>
        /// <param name="now"></param>
        /// <param name="day"></param>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public static DateTimeOffset Next(this DateTimeOffset now, DayOfWeek day, TimeSpan timeOfDay = default)
        {
            int nextDayOfWeek = (int) day;
            int today = (int) now.DayOfWeek;
            bool sameDay = now.DayOfWeek == day;
            TimeSpan nextTimeOfDay = timeOfDay == default ? now.TimeOfDay : timeOfDay;

            int daysToAdd = sameDay
                ? now.TimeOfDay <= nextTimeOfDay ? 0 : 7 // later today otherwise next week
                : nextDayOfWeek <= today ? nextDayOfWeek + 7 - today : nextDayOfWeek - today;

            return now
                .AddDays(daysToAdd)
                .Subtract(now.TimeOfDay)
                .Add(nextTimeOfDay);
        }
    }
}
