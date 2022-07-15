using Cronos;
using System;
using System.Collections.Generic;
using System.Linq;
using Timeslots.Extensions;

namespace Timeslots.Domain
{
    public interface ISchedule
    {
        bool Active();
        bool ActiveOn(DayOfWeek day, TimeSpan timeOfDay);
        bool ActiveOn(DateTimeOffset pointInTime);

        IEnumerable<Event> TimeslotEvents(DateTimeOffset pointInTime, TimeSpan backward, TimeSpan forward);
        IEnumerable<Event> TimeslotEvents(DateTimeOffset from, DateTimeOffset to);

        Event PreviousTimeslotEvent();
        Event PreviousTimeslotEvent(DateTimeOffset thatDateTime);

        Event NextTimeslotEvent();
        Event NextTimeslotEvent(DateTimeOffset thatDateTime);

        IEnumerable<Timeslot> Timeslots(DateTimeOffset from, DateTimeOffset to);
    }

    public class ScheduleOptions 
    {
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm";
        public string[] StartCronExpressions { get; set; }
        public string[] StopCronExpressions { get; set; }
        public double MaxTimeForPreviousTimeSlotEventInMinutes { get; set; } = TimeSpan.FromDays(4).TotalMinutes;
        public TimeSpan MaxTimeForPreviousTimeSlotEvent { get => TimeSpan.FromMinutes(MaxTimeForPreviousTimeSlotEventInMinutes); }
    }

    public class Schedule : ISchedule
    {
        private readonly ScheduleOptions _options;

        public Schedule(ScheduleOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool Active() => ActiveOn(DateTimeOffset.Now);
        public bool ActiveOn(DayOfWeek thatDay, TimeSpan thatTimeOfDay) => ActiveOn(DateTimeOffset.Now.Next(thatDay, thatTimeOfDay));

        public bool ActiveOn(DateTimeOffset thatDateTime)
        {
            if (!IsScheduleInitialised())
                return false;

            var timeSlotEventBeforeThatDateTime = PreviousTimeslotEvent(thatDateTime);

            return timeSlotEventBeforeThatDateTime != default 
                && timeSlotEventBeforeThatDateTime.What == EventType.Begin;
        }

        private bool IsScheduleInitialised()
        {
            if (_options == null)
                return false;

            var currentSchedule = _options;

            return (currentSchedule != null 
                && currentSchedule.StartCronExpressions != null
                && currentSchedule.StartCronExpressions.Any()
                && currentSchedule.StopCronExpressions != null
                && currentSchedule.StopCronExpressions.Any());
        }

        public IEnumerable<Event> TimeslotEvents(DateTimeOffset thatDateTime, TimeSpan backward, TimeSpan forward)
            => TimeslotEvents(thatDateTime.Subtract(backward), thatDateTime.Add(forward));

        public IEnumerable<Event> TimeslotEvents(DateTimeOffset fromThatDate, DateTimeOffset toThatDate)
        {
            if (!IsScheduleInitialised())
                return Enumerable.Empty<Event>();

            return CalculateEvents(fromThatDate, toThatDate, _options.StartCronExpressions, EventType.Begin)
                .Concat(CalculateEvents(fromThatDate, toThatDate, _options.StopCronExpressions, EventType.End));
        }

        private IEnumerable<Event> CalculateEvents(DateTimeOffset fromThatDate, DateTimeOffset toThatDate, string[] cronExpressions, EventType eventType)
        {
            // take all cron expressions, 
            // calculate all occurences for a period back and forward in time 
            // and create a corresponding timeslot Event with the given EventType

            IEnumerable<DateTimeOffset> generateOccurrences(string cronExp)
            {
                CronExpression cron = CronExpression.Parse(
                    cronExp,
                    DeduceCronFormat(cronExp));

                return cron.GetOccurrences(
                        fromThatDate,
                        toThatDate,
                        TimeZoneInfo.Local,
                        fromInclusive: true,
                        toInclusive: true);
            }

            return cronExpressions
                .SelectMany(generateOccurrences)
                .Select(o => new Event(o, eventType));
        }

        public Event NextTimeslotEvent() => NextTimeslotEvent(DateTimeOffset.Now);

        public Event NextTimeslotEvent(DateTimeOffset thatDateTime)
        {
            IEnumerable<Event> startEvents = CalculateNextEvents(thatDateTime, _options.StartCronExpressions, EventType.Begin);
            IEnumerable<Event> stopEvents = CalculateNextEvents(thatDateTime, _options.StopCronExpressions, EventType.End);

            return startEvents
                .Concat(stopEvents)
                .OrderBy(e => e.When)
                .FirstOrDefault();
        }

        private static IEnumerable<Event> CalculateNextEvents(DateTimeOffset thatDateTime, string[] cronExpressions, EventType eventType)
        {
            IEnumerable<CronExpression> calculatedCrons = cronExpressions.Select((cronExp) => CronExpression.Parse(cronExp, DeduceCronFormat(cronExp)));
            return calculatedCrons.Select((c) => new Event(c.GetNextOccurrence(thatDateTime, TimeZoneInfo.Local, true) ?? default, eventType));
        }

        public Event PreviousTimeslotEvent() => PreviousTimeslotEvent(DateTimeOffset.Now);

        public Event PreviousTimeslotEvent(DateTimeOffset thatDateTime)
        {
            DateTimeOffset earliestPointInTimeToConsider = thatDateTime.Subtract(_options.MaxTimeForPreviousTimeSlotEvent);

            return TimeslotEvents(earliestPointInTimeToConsider, thatDateTime)
                .OrderBy(e => e.When)
                .LastOrDefault(e => e.When <= thatDateTime);
        }

        public IEnumerable<Timeslot> Timeslots(DateTimeOffset from, DateTimeOffset to)
        {
            Event previousEvent = PreviousTimeslotEvent(from);

            var timeslotEvents = TimeslotEvents(from, to)
                .Prepend(previousEvent)
                .OrderBy((i) => i.When);

            List<Timeslot> timeslots = new List<Timeslot>();

            if (!timeslotEvents.Any())
                return timeslots;

            while (timeslotEvents.Any((e) => e.What == EventType.Begin))
            {
                Event start = timeslotEvents.First((e) => e.What == EventType.Begin);
                Event stop = timeslotEvents.FirstOrDefault((e) => e.What == EventType.End && e.When > start.When);

                Timeslot currentTimeSlot = new Timeslot(start.When, stop?.When);
                timeslots.Add(currentTimeSlot);

                timeslotEvents = timeslotEvents
                    .SkipWhile((e) => e.When <= (currentTimeSlot.To ?? DateTimeOffset.MaxValue))
                    .OrderBy((e) => e.When);
            }

            return timeslots;
        }

        private static CronFormat DeduceCronFormat(string cronExp)
            => cronExp.Split(" ").Length == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;

    }
}
