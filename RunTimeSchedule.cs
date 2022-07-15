using System;
using System.Collections.Generic;
using System.Linq;
using Cronos;

namespace TimeSlots.Domain
{
    public interface IRunTimeSchedule
    {
        DateTimeOffset? NextRunTime(DateTimeOffset fromDateTime = default);
        IEnumerable<DateTimeOffset> Schedules(DateTimeOffset fromThatDate, DateTimeOffset toThatDate);
        IEnumerable<DateTimeOffset> Schedules(DateTimeOffset fromThatDate, DateTimeOffset toThatDate, string[] cronExpressions);
    }

    public class RunTimeSchedule : IRunTimeSchedule
    {
        private readonly FetchSchedulerOptions _schedulerOptions;
        private const int CronFormatIncludeSeconds = 6;
        private const bool IncludeCurrentOccurrence = false;

        public RunTimeSchedule(FetchSchedulerOptions schedulerOptions) =>
            _schedulerOptions = schedulerOptions ?? throw new ArgumentNullException(nameof(schedulerOptions));

        public DateTimeOffset? NextRunTime(DateTimeOffset fromDateTime = default)
        {
            var startDateTimes = _schedulerOptions.Schedules
                .Select(ParseCronExpression)
                .Select(c => c.GetNextOccurrence(fromDateTime == default ? DateTimeOffset.Now : fromDateTime, TimeZoneInfo.Local, IncludeCurrentOccurrence));

            return startDateTimes
                .OrderBy(e => e.Value)
                .FirstOrDefault();
        }

        public IEnumerable<DateTimeOffset> Schedules(DateTimeOffset fromThatDate, DateTimeOffset toThatDate) =>
            !IsInitialized()
                ? Enumerable.Empty<DateTimeOffset>()
                : Schedules(fromThatDate, toThatDate, _schedulerOptions?.Schedules);

        public IEnumerable<DateTimeOffset> Schedules(DateTimeOffset fromThatDate, DateTimeOffset toThatDate, string[] cronExpressions)
        {
            if (cronExpressions == null || !cronExpressions.Any())
                return Enumerable.Empty<DateTimeOffset>();

            IEnumerable<DateTimeOffset> startSelector(string cronExpression) =>
                ParseCronExpression(cronExpression).GetOccurrences(
                        from: fromThatDate,
                        to: toThatDate,
                        zone: TimeZoneInfo.Local,
                        fromInclusive: true,
                        toInclusive: true);

            var schedules = cronExpressions.Select(schedule => schedule).Distinct();
            return schedules.SelectMany(startSelector).Distinct();
        }

        private static CronExpression ParseCronExpression(string cronExpression) =>
            CronExpression.Parse(
                cronExpression,
                cronExpression.Split(" ").Count() == CronFormatIncludeSeconds ? CronFormat.IncludeSeconds : CronFormat.Standard);

        private bool IsInitialized()
        {
            if (_schedulerOptions == null)
                return false;

            return _schedulerOptions?.Schedules != null && _schedulerOptions.Schedules.Any();
        }
    }
}
