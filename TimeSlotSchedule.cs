using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pension.Hub.Connector.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TimeSlots.Domain
{
    public interface ITimeSlotSchedule
    {
        bool Active();
        bool ActiveOn(DayOfWeek day, TimeSpan timeOfDay);
        bool ActiveOn(DateTimeOffset dateTime);

        IEnumerable<TimeSlotEvent> TimeSlotEvents(DateTimeOffset thatDateTime, TimeSpan backward, TimeSpan forward);
        IEnumerable<TimeSlotEvent> TimeSlotEvents(DateTimeOffset from, DateTimeOffset to);

        TimeSlotEvent PreviousTimeSlotEvent();
        TimeSlotEvent PreviousTimeSlotEvent(DateTimeOffset thatDateTime);

        TimeSlotEvent NextTimeSlotEvent();
        TimeSlotEvent NextTimeSlotEvent(DateTimeOffset thatDateTime);

        IEnumerable<TimeSlotInstance> Schedule(DateTimeOffset from, DateTimeOffset to);
    }

    public class TimeSlotScheduleOptions 
    {
        public string[] StartCronExpressions { get; set; }
        public string[] StopCronExpressions { get; set; }
        public double MaxTimeForPreviousTimeSlotEventInMinutes { get; set; } = 5760; // == 4 days * 24 hours * 60 minutes 
        public TimeSpan MaxTimeForPreviousTimeSlotEvent { get => TimeSpan.FromMinutes(MaxTimeForPreviousTimeSlotEventInMinutes); }
    }

    public class TimeSlotSchedule : ITimeSlotSchedule
    {
        private readonly TimeSlotScheduleOptions _options;

        public TimeSlotSchedule(TimeSlotScheduleOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool Active()
        {
            return ActiveOn(DateTimeOffset.Now);
        }

        public bool ActiveOn(DayOfWeek thatDay, TimeSpan thatTimeOfDay)
        {
            return ActiveOn(DateTimeOffset.Now.Next(thatDay, thatTimeOfDay));
        }

        public bool ActiveOn(DateTimeOffset thatDateTime)
        {
            if (!IsScheduleInitialised())
                return false;

            var timeSlotEventBeforeThatDateTime = PreviousTimeSlotEvent(thatDateTime);

            return timeSlotEventBeforeThatDateTime != default 
                && timeSlotEventBeforeThatDateTime.What == TimeSlotEventType.Begin;
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

        public IEnumerable<TimeSlotEvent> TimeSlotEvents(DateTimeOffset thatDateTime, TimeSpan backward, TimeSpan forward)
        {
            return TimeSlotEvents(thatDateTime.Subtract(backward), thatDateTime.Add(forward));
        }

        public IEnumerable<TimeSlotEvent> TimeSlotEvents(DateTimeOffset fromThatDate, DateTimeOffset toThatDate)
        {
            if (!IsScheduleInitialised())
                return Enumerable.Empty<TimeSlotEvent>();

            // take all start and stop cron expressions, 
            // calculate all occurences for an period back and forward in time and create 
            // a corresponding TimeSlotEvent

            IEnumerable<TimeSlotEvent> startSelector(string cronExp)
            {
                CronExpression cron = CronExpression.Parse(
                    cronExp,
                    cronExp.Split(" ").Count() == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard);

                return cron.GetOccurrences(
                    fromThatDate,
                    toThatDate,
                    TimeZoneInfo.Local,
                    fromInclusive: true,
                    toInclusive: true)
                    .Select(o => new TimeSlotEvent(o, TimeSlotEventType.Begin));
            }
            IEnumerable<TimeSlotEvent> stopSelector(string cronExp)
            {
                CronExpression cron = CronExpression.Parse(
                    cronExp, 
                    cronExp.Split(" ").Count() == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard);

                return cron.GetOccurrences(
                    fromThatDate,
                    toThatDate,
                    TimeZoneInfo.Local,
                    fromInclusive: true,
                    toInclusive: true)
                    .Select(o => new TimeSlotEvent(o, TimeSlotEventType.End));
            }

            return _options.StartCronExpressions.SelectMany(startSelector)
                .Concat(_options.StopCronExpressions.SelectMany(stopSelector));
        }

        public TimeSlotEvent NextTimeSlotEvent()
        {
            return NextTimeSlotEvent(DateTimeOffset.Now);
        }

        public TimeSlotEvent NextTimeSlotEvent(DateTimeOffset thatDateTime)
        {
            IEnumerable<CronExpression> allStartCrons = _options.StartCronExpressions.Select((cronExp) => CronExpression.Parse(cronExp, cronExp.Split(" ").Count() == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard));
            IEnumerable<CronExpression> allStopCrons = _options.StopCronExpressions.Select((cronExp) => CronExpression.Parse(cronExp, cronExp.Split(" ").Count() == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard));

            IEnumerable<TimeSlotEvent> startDateTimes = allStartCrons.Select((c) => new TimeSlotEvent(c.GetNextOccurrence(thatDateTime, TimeZoneInfo.Local, true) ?? default, TimeSlotEventType.Begin));
            IEnumerable<TimeSlotEvent> stopDateTimes = allStopCrons.Select((c) => new TimeSlotEvent(c.GetNextOccurrence(thatDateTime, TimeZoneInfo.Local, true) ?? default, TimeSlotEventType.End));

            return startDateTimes
                .Concat(stopDateTimes)
                .OrderBy(e => e.When)
                .FirstOrDefault();
        }

        public TimeSlotEvent PreviousTimeSlotEvent()
        {
            return PreviousTimeSlotEvent(DateTimeOffset.Now);
        }

        public TimeSlotEvent PreviousTimeSlotEvent(DateTimeOffset thatDateTime)
        {
            return TimeSlotEvents(thatDateTime.Subtract(_options.MaxTimeForPreviousTimeSlotEvent), thatDateTime)
                .OrderBy(e => e.When)
                .LastOrDefault(e => e.When <= thatDateTime);
        }

        public IEnumerable<TimeSlotInstance> Schedule(DateTimeOffset from, DateTimeOffset to)
        {
            TimeSlotEvent previousEvent = PreviousTimeSlotEvent(from);

            var timeSlotEvents = TimeSlotEvents(from, to)
                .Prepend(previousEvent)
                .OrderBy((i) => i.When);

            List<TimeSlotInstance> timeSlots = new List<TimeSlotInstance>();

            while (timeSlotEvents != default && timeSlotEvents.Any())
            {
                TimeSlotEvent start = timeSlotEvents.FirstOrDefault((e) => e.What == TimeSlotEventType.Begin);

                // if no more start events, break the loot and return what has been found
                if (start == default)
                    break;

                TimeSlotEvent stop = timeSlotEvents.FirstOrDefault((e) => e.What == TimeSlotEventType.End && e.When > start.When);

                DateTimeOffset? stopTime = null;

                if (stop != null)
                    stopTime = stop.When;

                TimeSlotInstance currentTimeSlot = new TimeSlotInstance(start.When, stopTime);
                timeSlots.Add(currentTimeSlot);

                timeSlotEvents = timeSlotEvents
                    .SkipWhile((e) => e.When <= (currentTimeSlot.To ?? DateTimeOffset.MaxValue))
                    .OrderBy((e) => e.When);
            }

            return timeSlots;
        }
    }

    public class TimeSlotInstance
    {
        public readonly DateTimeOffset? From;
        public readonly DateTimeOffset? To;
        
        public TimeSlotInstance(DateTimeOffset? from, DateTimeOffset? to)
        {
            From = from;
            To = to;
            Validate();
        }

        public TimeSlotInstance(string fromAsString, string toAsString)
        {
            const string DateTimeFormat = "yyyy-MM-dd HH:mm";
            From = string.IsNullOrWhiteSpace(fromAsString) ? (DateTimeOffset?) null : DateTimeOffset.ParseExact(fromAsString, DateTimeFormat, CultureInfo.InvariantCulture);
            To = string.IsNullOrWhiteSpace(toAsString) ? (DateTimeOffset?) null : DateTimeOffset.ParseExact(toAsString, DateTimeFormat, CultureInfo.InvariantCulture);
            Validate();
        }

        private void Validate()
        {
            if (To.HasValue && From.HasValue && To.Value.IsBefore(From.Value))
                throw new ArgumentException("The date/time of 'to' has to be after 'from' date/time", nameof(To));
        }

        public override int GetHashCode()
        {
            return From.GetHashCode() + (To.GetHashCode() * 2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TimeSlotInstance other))
                return false;

            return From.Equals(other.From) && To.Equals(other.To);
        }
    }

    public class TimeSlotEvent
    {
        public readonly DateTimeOffset When;
        public readonly TimeSlotEventType What;

        public TimeSlotEvent(DateTimeOffset when, TimeSlotEventType what)
        {
            When = when;
            What = what;
        }
    }

    public enum TimeSlotEventType
    {
        Begin,
        End
    }
}
