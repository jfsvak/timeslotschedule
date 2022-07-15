using System;
using System.Globalization;
using System.Linq;
using Timeslots.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Timeslots.Test
{
    public class CronTimeSlotScheduleTest
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private static ITestOutputHelper _output;


        public CronTimeSlotScheduleTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GivenSchedule_WhenNoStartCronExpressions_ScheduleIsReturned()
        {
            var options = new ScheduleOptions
            {
                StopCronExpressions = new[] { "0 3 * * MON" },
                MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromDays(1).TotalMinutes
            };

            var actual = new Schedule(options);

            Assert.NotNull(actual);
        }

        [Theory]
        [InlineData("0/10 * * * * *", "5/10 * * * * *", 1)]
        public void GivenSchedule_WhenInitialisingThroughDI_ScheduleIsReturned(string beginCron, string endCron, int minutesBefore)
        {
            var options = new ScheduleOptions
            {
                StartCronExpressions = new[] { beginCron },
                StopCronExpressions = new[] { endCron },
                MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromMinutes(minutesBefore).TotalMinutes
            };

            var actual = new Schedule(options);
            Assert.NotNull(actual);
        }

        [Theory]
        [InlineData("0/10 * * * * *", "5/10 * * * * *", "2021-02-20 10:00:01", 1, 1, 24)]
        [InlineData("0 3 * * MON-FRI", "0 7 * * MON-FRI", "2021-02-17 05:00:00", 60*51, 60 * 51, 10)]
        public void GivenSchedule_WhenGettingTimeSlotEvents_TimeSlotEventsAreReturned(string beginCron, string endCron, string timeToTest, int minutesBefore, int minutesAfter, int expectedTimeSlotEventsCount)
        {
            ScheduleOptions options = new ScheduleOptions
            {
                StartCronExpressions = new[] { beginCron },
                StopCronExpressions = new[] { endCron },
                MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromMinutes(minutesBefore).TotalMinutes
            };

            var schedule = new Schedule(options);
            var dateTimeDT = DateTimeOffset.ParseExact(timeToTest, DateTimeFormat, CultureInfo.InvariantCulture);
            Event[] timeSlotEvents = schedule.TimeslotEvents(dateTimeDT, TimeSpan.FromMinutes(minutesBefore), TimeSpan.FromMinutes(minutesAfter)).ToArray();

            Assert.NotNull(timeSlotEvents);
            Assert.NotEmpty(timeSlotEvents);
            Assert.Equal(expectedTimeSlotEventsCount, timeSlotEvents.Count());
        }

        [Theory]
        [InlineData("0/10 * * * * *", "5/10 * * * * *", "2021-02-20 10:00:01", "2021-02-20 10:00:05", EventType.End)]
        [InlineData("0/10 * * * * *", "5/10 * * * * *", "2021-02-20 10:00:06", "2021-02-20 10:00:10", EventType.Begin)]
        [InlineData("0 3 * * SUN", "0 7 * * MON", "2021-02-21 02:59:01", "2021-02-21 03:00:00", EventType.Begin)]
        [InlineData("0 3 * * SUN", "0 7 * * MON", "2021-02-21 10:00:01", "2021-02-22 07:00:00", EventType.End)]
        [InlineData("0 3 * * MON-FRI", "0 7 * * MON-FRI", "2021-02-23 10:00:00", "2021-02-24 03:00:00", EventType.Begin)]
        [InlineData("0 3 * * MON-FRI", "0 7 * * MON-FRI", "2021-02-23 06:00:00", "2021-02-23 07:00:00", EventType.End)]
        public void GivenTimeSlotSchedule_WhenGettingNextTimeSlotEvent_TimeSlotEventIsReturned(string beginCron, string endCron, string timeToTest, string expectedTimeSlotEventDateTime, EventType expectedTimeSlotEventType)
        {
            ScheduleOptions options = new ScheduleOptions
            {
                StartCronExpressions = new[] { beginCron },
                StopCronExpressions = new[] { endCron },
                MaxTimeForPreviousTimeSlotEventInMinutes = TimeSpan.FromMinutes(1).TotalMinutes
            };

            var schedule = new Schedule(options);
            var dateTimeDT = DateTimeOffset.ParseExact(timeToTest, DateTimeFormat, CultureInfo.InvariantCulture);
            var expectedTimeSlotEventDateTimeDT = DateTimeOffset.ParseExact(expectedTimeSlotEventDateTime, DateTimeFormat, CultureInfo.InvariantCulture); 
            var timeSlotEvent = schedule.NextTimeslotEvent(dateTimeDT);

            Assert.Equal(expectedTimeSlotEventDateTimeDT, timeSlotEvent.When);
            Assert.Equal(expectedTimeSlotEventType, timeSlotEvent.What);
        }
    }
}
