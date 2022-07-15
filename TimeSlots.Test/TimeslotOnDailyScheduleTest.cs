using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Timeslots.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Timeslots.Test
{
    public class TimeSlotOnDailyScheduleTest
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm";
        private static ITestOutputHelper _output;

        private static readonly ScheduleOptions scheduleOptions = new ScheduleOptions
        {
            DateTimeFormat = DateTimeFormat,
            StartCronExpressions = new[] { "0 3 * * MON-FRI", "0 16 * * MON-FRI", "0 7 * * SUN" },
            StopCronExpressions = new[] { "0 7 * * MON-FRI", "0 20 * * MON-FRI" },
            MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromDays(8).TotalMinutes
        };

        public TimeSlotOnDailyScheduleTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GivenSchedule_WhenNoOptionsGiven_ExceptionIsThrown()
        {
            static object ctor() => new Schedule(null);
            Assert.Throws<ArgumentNullException>(ctor);
            //static object ctor2() => new TimeSlotSchedule(Options.Create<TimeSlotScheduleOptions>(null));//new TestOptionsMonitor<TimeSlotScheduleOptions>(null));
            //Assert.Throws<ArgumentNullException>(ctor2);
        }

        [Theory]
        [InlineData(DayOfWeek.Monday, 1, true)]
        [InlineData(DayOfWeek.Monday, 3, true)]
        [InlineData(DayOfWeek.Monday, 5, true)]
        [InlineData(DayOfWeek.Monday, 8, false)]
        [InlineData(DayOfWeek.Monday, 17, true)]
        [InlineData(DayOfWeek.Monday, 20, false)]
        [InlineData(DayOfWeek.Tuesday, 2, false)]
        [InlineData(DayOfWeek.Tuesday, 5, true)]
        [InlineData(DayOfWeek.Friday, 12, false)]
        [InlineData(DayOfWeek.Saturday, 12, false)]
        [InlineData(DayOfWeek.Sunday, 6, false)]
        [InlineData(DayOfWeek.Sunday, 8, true)]
        [InlineData(DayOfWeek.Sunday, 24, true)]
        public void GivenSchedule_WhenInOrOutOfScheduleWithDayAndTimeOfDay_CorrectActiveOnIsReturned(DayOfWeek dayOfWeek, int hourOfDay, bool expectedActive)
        {
            ISchedule actualSchedule = new Schedule(scheduleOptions); // new TestOptionsMonitor<TimeSlotScheduleOptions>(scheduleOptions));
            Assert.Equal(expectedActive, actualSchedule.ActiveOn(dayOfWeek, TimeSpan.FromHours(hourOfDay)));
        }

        [Theory]
        [InlineData(2021, 2, 14, 6, 0, 0, false)] // sunday
        [InlineData(2021, 2, 14, 8, 0, 0, true)] // sunday
        [InlineData(2021, 2, 14, 23, 29, 59, true)] // sunday
        [InlineData(2021, 2, 15, 0, 0, 0, true)] // monday morning, still open all night after sunday
        [InlineData(2021, 2, 15, 0, 59, 59, true)] // monday
        [InlineData(2021, 2, 15, 1, 0, 0, true)] // monday
        [InlineData(2021, 2, 15, 3, 0, 0, true)] // monday
        [InlineData(2021, 2, 15, 4, 0, 0, true)] // monday
        [InlineData(2021, 2, 15, 6, 59, 59, true)] // monday
        [InlineData(2021, 2, 15, 7, 0, 0, false)] // monday
        [InlineData(2021, 2, 15, 15, 0, 0, false)] // monday
        [InlineData(2021, 2, 15, 17, 0, 0, true)] // monday
        [InlineData(2021, 2, 15, 20, 0, 0, false)] // Monday
        [InlineData(2021, 2, 17, 4, 0, 0, true)] // Wednesday
        [InlineData(2021, 2, 17, 12, 0, 0, false)] // Wednesday
        public void GivenSchedule_WhenInOrOutOfScheduleWithDateTimeOffset_CorrectActiveOnIsReturned(int year, int month, int date, int hour, int minute, int second, bool expectedActive)
        {
            ISchedule actualSchedule = new Schedule(scheduleOptions); // new TestOptionsMonitor<TimeSlotScheduleOptions>(scheduleOptions));
            DateTimeOffset dateTime = new DateTimeOffset(year, month, date, hour, minute, second, TimeZoneInfo.Local.BaseUtcOffset);

            Assert.Equal(expectedActive, actualSchedule.ActiveOn(dateTime));
        }

        [Fact]
        public void GivenSchedule_WhenCalculatingTimeSlotEventsWithoutTimeSlots_NoTimeSlotsAreReturned()
        {
            ISchedule schedule = new Schedule(new ScheduleOptions()); // new TestOptionsMonitor<TimeSlotScheduleOptions>(new TimeSlotScheduleOptions()));
            var timeslots = schedule.TimeslotEvents(DateTimeOffset.ParseExact("2021-02-17 10:00", DateTimeFormat, null), TimeSpan.FromDays(5), TimeSpan.FromDays(5));
            Assert.Empty(timeslots);
        }

        public static IEnumerable<object[]> DateForTimeSlotEventsTest =>
            new List<object[]>
            {
                new object[] { "2021-02-16 06:00", 1, 1, 
                    new [] { 
                        new Event(DateTimeOffset.ParseExact("2021-02-15 07:00", DateTimeFormat, null), EventType.End),
                        new Event(DateTimeOffset.ParseExact("2021-02-15 16:00", DateTimeFormat, null), EventType.Begin),
                        new Event(DateTimeOffset.ParseExact("2021-02-15 20:00", DateTimeFormat, null), EventType.End),
                        new Event(DateTimeOffset.ParseExact("2021-02-16 03:00", DateTimeFormat, null), EventType.Begin),
                        new Event(DateTimeOffset.ParseExact("2021-02-16 07:00", DateTimeFormat, null), EventType.End),
                        new Event(DateTimeOffset.ParseExact("2021-02-16 16:00", DateTimeFormat, null), EventType.Begin),
                        new Event(DateTimeOffset.ParseExact("2021-02-16 20:00", DateTimeFormat, null), EventType.End),
                        new Event(DateTimeOffset.ParseExact("2021-02-17 03:00", DateTimeFormat, null), EventType.Begin)
                    }
                }
            };

        [Theory]
        [MemberData(nameof(DateForTimeSlotEventsTest))]
        public void GivenSchedule_WhenCalculatingScheduleWithTimeSlots_TimeSlotsAreReturned(string dateTimeToTest, int daysBackward, int daysForward, IEnumerable<Event> expectedTimeSlotEvents)
        {
            ISchedule schedule = new Schedule(scheduleOptions); // new TestOptionsMonitor<TimeSlotScheduleOptions>(scheduleOptions));
            IEnumerable<Event> actualEvents = schedule.TimeslotEvents(
                DateTimeOffset.ParseExact(dateTimeToTest, DateTimeFormat, null),
                TimeSpan.FromDays(daysBackward),
                TimeSpan.FromDays(daysForward));
            Assert.NotEmpty(actualEvents);
            Assert.Equal(expectedTimeSlotEvents.Count(), actualEvents.Count());
            Assert.All(expectedTimeSlotEvents, (i) => actualEvents.Any((ii) => i.When.EqualsExact(ii.When) && i.What == ii.What));
        }

        public static IEnumerable<object[]> TimeSlotInstances_FromAndToDateTimeOffsets_TestData =>
            new List<object[]>
            {
                new object[] {
                    scheduleOptions,
                    "2021-02-22 00:00", 
                    "2021-02-28 23:59",
                    new [] { 
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-21 07:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-22 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-22 16:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-22 20:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-23 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-23 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-23 16:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-23 20:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-24 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-24 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-24 16:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-24 20:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-25 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-25 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-25 16:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-25 20:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-26 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-26 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-26 16:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-26 20:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-28 07:00", DateTimeFormat, null), null)
                    }
                }
            };

        public static IEnumerable<object[]> TimeSlotInstances_FromAndToDateTimeOffsets2_TestData =>
            new List<object[]>
            {
                new object[] {
                    new ScheduleOptions
                    {
                        StartCronExpressions = new[] { "0 3 * * MON-FRI", "0 7 * * SUN" },
                        StopCronExpressions = new[] { "0 7 * * MON-FRI", "0 20 * * MON-FRI" },
                        MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromDays(8).TotalMinutes,
                    },
                    "2021-02-22 00:00", 
                    "2021-02-28 23:59",
                    new [] {
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-21 07:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-22 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-23 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-23 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-24 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-24 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-25 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-25 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-26 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-26 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-28 07:00", DateTimeFormat, null), null)
                    }
                }
            };

        public static IEnumerable<object[]> TimeSlotInstances_FromAndToDateTimeOffsets3_TestData =>
            new List<object[]>
            {
                new object[] {
                    new ScheduleOptions
                    {
                        StartCronExpressions = new[] { "0 2 * * TUE,FRI", "0 3 * * MON-FRI", "0 7 * * SUN" },
                        StopCronExpressions = new[] { "0 7 * * MON-FRI", "0 20 * * MON-FRI", "0 9 * * FRI", },
                        MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromDays(8).TotalMinutes
                    },
                    "2021-02-22 00:00",
                    "2021-02-28 23:59",
                    new [] {
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-21 07:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-22 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-23 02:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-23 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-24 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-24 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-25 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-25 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-26 02:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-26 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-28 07:00", DateTimeFormat, null), null)
                    }
                }
            };

        public static IEnumerable<object[]> TimeSlotInstances_FromAndToDateTimeOffsets4_TestData =>
            new List<object[]>
            {
                new object[] {
                    new ScheduleOptions
                    {
                        StartCronExpressions = new[] { "0 2 * * TUE,FRI", "0 3 * * MON-FRI", "0 7 * * SUN" },
                        StopCronExpressions = new[] { "0 7 * * MON-THU", "0 20 * * MON-FRI", "0 9 * * FRI", },
                        MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromDays(8).TotalMinutes
                    },
                    "2021-02-25 12:00", // thursday
                    "2021-03-01 09:45", // monday
                    new [] {
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-26 02:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-26 09:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-28 07:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-03-01 07:00", DateTimeFormat, null))
                    }
                }
            };

        public static IEnumerable<object[]> TimeSlotInstances_FromAndToDateTimeOffsets5_TestData =>
            new List<object[]>
            {
                new object[] {
                    new ScheduleOptions
                    {
                        StartCronExpressions = new[] { "0 3 * * MON-FRI" },
                        StopCronExpressions = new[] { "0 7 * * MON-FRI" },
                        MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromDays(8).TotalMinutes
                    },
                    "2021-02-22 04:00",
                    "2021-02-25 23:59",
                    new [] {
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-22 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-22 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-23 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-23 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-24 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-24 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-25 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-25 07:00", DateTimeFormat, null))
                    }
                }
            };
        public static IEnumerable<object[]> TimeSlotInstances_FromAndToDateTimeOffsets6_TestData =>
            new List<object[]>
            {
                new object[] {
                    new ScheduleOptions
                    {
                        StartCronExpressions = new[] { "0 3 * * MON-FRI" },
                        StopCronExpressions = new[] { "0 7 * * MON-FRI" },
                        MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromDays(8).TotalMinutes
                    },
                    "2021-02-22 04:00",
                    "2021-02-25 06:00",
                    new [] {
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-22 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-22 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-23 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-23 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-24 03:00", DateTimeFormat, null), DateTimeOffset.ParseExact("2021-02-24 07:00", DateTimeFormat, null)),
                        new Timeslot(DateTimeOffset.ParseExact("2021-02-25 03:00", DateTimeFormat, null), null)
                    }
                }
            };

        public static IEnumerable<object[]> TimeSlotInstances_FromAndToDateTimeOffsets7_TestData =>
            new List<object[]>
            {
                new object[] {
                    new ScheduleOptions
                    {
                        StartCronExpressions = new[] { "0 1 * * TUE-FRI", "0 16 * * MON-FRI", "0 10 * * SAT", "0 5 * * SUN", "01 22 * * MON" },
                        StopCronExpressions = new[] { "0 6 * * MON-FRI", "0 20 * * MON-FRI", "30 21 * * SAT", "59 21 * * SUN" },
                        MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromDays(8).TotalMinutes
                    },
                    "2021-04-12 08:30",
                    "2021-04-19 08:30",
                    new [] {
                        new Timeslot("2021-04-12 16:00", "2021-04-12 20:00"), // mon
                        new Timeslot("2021-04-12 22:01", "2021-04-13 06:00"), // mon-tue
                        new Timeslot("2021-04-13 16:00", "2021-04-13 20:00"), // tue
                        new Timeslot("2021-04-14 01:00", "2021-04-14 06:00"), // wed
                        new Timeslot("2021-04-14 16:00", "2021-04-14 20:00"), // wed
                        new Timeslot("2021-04-15 01:00", "2021-04-15 06:00"), // thu
                        new Timeslot("2021-04-15 16:00", "2021-04-15 20:00"), // thu
                        new Timeslot("2021-04-16 01:00", "2021-04-16 06:00"), // fri
                        new Timeslot("2021-04-16 16:00", "2021-04-16 20:00"), // fri
                        new Timeslot("2021-04-17 10:00", "2021-04-17 21:30"), // sat
                        new Timeslot("2021-04-18 05:00", "2021-04-18 21:59") // sun
                    }
                }
            };

        [Theory]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets2_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets3_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets4_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets5_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets6_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets7_TestData))]
        public void GivenSchedule_WhenCalculatingTimeSlots_CorrectNumberOfSlotsCreated(ScheduleOptions options, string from, string to, IEnumerable<Timeslot> expectedTimeSlots)
        {
            ISchedule schedule = new Schedule(options);
            var actualTimeslots = schedule
                .Timeslots(
                    DateTimeOffset.ParseExact(from, DateTimeFormat, CultureInfo.InvariantCulture),
                    DateTimeOffset.ParseExact(to, DateTimeFormat, CultureInfo.InvariantCulture))
                .ToList();

            Assert.NotEmpty(actualTimeslots);
            _output.WriteLine("Actual:");
            actualTimeslots.ForEach((t) => _output.WriteLine($"TimeSlot[{t.From}]-[{t.To}]"));

            _output.WriteLine("Expected:");
            expectedTimeSlots.ToList().ForEach((t) => _output.WriteLine($"TimeSlot[{t.From}]-[{t.To}]"));

            Assert.Equal(expectedTimeSlots.Count(), actualTimeslots.Count);
        }

        [Theory]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets2_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets3_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets4_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets5_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets6_TestData))]
        [MemberData(nameof(TimeSlotInstances_FromAndToDateTimeOffsets7_TestData))]
        public void GivenSchedule_WhenCalculatingTimeSlots_AllExpectedTimeslotsAreCalculated(ScheduleOptions options, string from, string to, IEnumerable<Timeslot> expectedTimeSlots)
        {
            ISchedule schedule = new Schedule(options);
            var actualTimeslots = schedule
                .Timeslots(
                    DateTimeOffset.ParseExact(from, DateTimeFormat, CultureInfo.InvariantCulture),
                    DateTimeOffset.ParseExact(to, DateTimeFormat, CultureInfo.InvariantCulture))
                .ToList();

            Assert.NotEmpty(actualTimeslots);
            _output.WriteLine("Actual:");
            actualTimeslots.ForEach((t) => _output.WriteLine($"TimeSlot[{t.From}]-[{t.To}]"));

            _output.WriteLine("Expected:");
            expectedTimeSlots.ToList().ForEach((t) => _output.WriteLine($"TimeSlot[{t.From}]-[{t.To}]"));

            Assert.Equal(expectedTimeSlots.Count(), actualTimeslots.Count);

            foreach (Timeslot t in expectedTimeSlots)
                Assert.Contains(t, actualTimeslots);

            foreach (Timeslot t in actualTimeslots)
                Assert.Contains(t, expectedTimeSlots);
        }
    }
}
