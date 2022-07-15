using System;
using System.Globalization;
using System.Linq;
using TimeSlots.Domain;
using Xunit;
using Xunit.Abstractions;

namespace TimeSlots.Test
{
    public class RunTimeScheduleTest
    {
        private readonly ITestOutputHelper _output;
        private IServiceProvider _serviceProvider;

        public RunTimeScheduleTest(ITestOutputHelper output) =>
            _output = output;

        [Fact]
        public void GivenRunTimeSchedule_WhenSchedulerOptionsAreUnavailable_ArgumentNullExceptionIsThrown()
        {
            object func() => new RunTimeSchedule(null);
            Assert.Throws<ArgumentNullException>(func);
        }

        [Theory]
        [InlineData("0 0 5 * * *", 1)]
        [InlineData("0 10-14/4 * * *", 2)]
        [InlineData("30 4-16/6 * * *", 3)]
        public void GivenRunTimeSchedule_WhenCronExpressionIsConfigured_ScheduleIsReturned(string schedules, int expectedCount)
        {
            var today = new DateTimeOffset(DateTime.Today);
            var endOfToday = today.AddDays(1).AddMilliseconds(-1);

            var jobSchedule =  new RunTimeSchedule(new FetchSchedulerOptions { Schedules = new []{ schedules } });
            Assert.NotNull(jobSchedule);
            Assert.NotEmpty(jobSchedule.Schedules(today, endOfToday));
            Assert.Equal(expectedCount, jobSchedule.Schedules(today, endOfToday).Count());
        }

        [Theory] [InlineData("* * *")]
        [InlineData("*")]
        [InlineData("? ? ? ? ? ? ?")]
        [InlineData("MON-FRI")]
        [InlineData("aa")]
        public void GivenRunTimeSchedule_WhenHavingInvalidCronExpression_ExceptionIsThrown(string invalidCronExpression)
        {
            var jobSchedule = new RunTimeSchedule(new FetchSchedulerOptions { Schedules = new[] { invalidCronExpression } });
            object func() => jobSchedule.NextRunTime();
            Assert.Throws<Cronos.CronFormatException>(func);
        }

        [Fact]
        public void GivenRunTimeSchedule_WhenSameScheduleExistsMultipleTimes_ReturnSchedulesWithoutDuplicate()
        {
            var jobSchedule = new RunTimeSchedule(new FetchSchedulerOptions 
            { 
                Schedules = new[]
                {
                    "30 4-16/6 * * SUN-SAT",
                    "30 4-16/1 * * SUN-SAT" // this will produce 3 duplicate schedules
                }
            });

            var today = new DateTimeOffset(DateTime.Today);
            var endOfToday = new DateTimeOffset(DateTime.Today.AddDays(1).AddMilliseconds(-1));

            var schedules = jobSchedule.Schedules(today, endOfToday);
            Assert.Equal(13, schedules.Count());
        }

        [Theory]
        [InlineData(new[] { "0 3 * * SUN-SAT" }, "2021-01-01-00.00.00", "2021-01-01-03.00.00")]
        [InlineData(new[] { "0 3-4/1 * * SUN-SAT" }, "2021-01-01-03.15.00", "2021-01-01-04.00.00")]
        [InlineData(new[] { "0 3-4/1 * * SUN-SAT", "0 7 * * SUN-SAT" }, "2021-01-01-04.01.00", "2021-01-01-07.00.00")]
        [InlineData(new[] { "0 3-17/14 * * SUN-SAT" }, "2021-01-01-23.59.00", "2021-01-02-03.00.00")]
        [InlineData(new[] { "0 17 * * SUN-SAT", "0 3-4/1 * * SUN-SAT" }, "2021-01-01-23.59.00", "2021-01-02-03.00.00")]
        [InlineData(new[] { "0 15 * * SUN-SAT", "0 3 * * SUN-SAT", "30 4 * * SUN-SAT" }, "2021-01-01-22.45.00", "2021-01-02-03.00.00")]
        [InlineData(new[] { "0 3 * * SUN-SAT" }, "2021-01-01-03.00.00", "2021-01-02-03.00.00")]
        public void GivenRunTimeSchedule_WhenRunTimesAreConfigured_NextRunTimeIsCalculated(string[] cronExpressions, string fromWhen, string runtime)
        {
            const string format = "yyyy-MM-dd-HH.mm.ss";

            var currentTime = DateTimeOffset.ParseExact(fromWhen, format, CultureInfo.InvariantCulture);
            var expectedRunTime = DateTimeOffset.ParseExact(runtime, format, CultureInfo.InvariantCulture);
            var jobSchedule = new RunTimeSchedule(new FetchSchedulerOptions { Schedules = cronExpressions });

            Assert.Equal(expectedRunTime, jobSchedule.NextRunTime(currentTime));
        }

        [Fact(Skip = "This is to simulate PINSURANCE-37988 which causing the timer to fail when scheduling delay with 0 or negative interval")]
        public void GivenRunTimeSchedule_WhenNextRunTimeIsCurrentTime_ReturnCurrentTimeAsNextRunTime()
        {
            var jobSchedule = new RunTimeSchedule(new FetchSchedulerOptions
            {
                Schedules = new[]
                {
                    "0 2-21/1 * * MON-FRI"
                }
            });

            var today = DateTimeOffset.ParseExact("2021-07-30-05.00.00.0000000", "yyyy-MM-dd-HH.mm.ss.fffffff", CultureInfo.InvariantCulture);
            Assert.Equal(today, jobSchedule.NextRunTime(today));
        }

        [Fact]
        public void GivenRunTimeSchedule_WhenNextRunTimeIsCurrentTime_ReturnNextRunTime()
        {
            var jobSchedule = new RunTimeSchedule(new FetchSchedulerOptions
            {
                Schedules = new[]
                {
                    "0 2-21/1 * * MON-FRI"
                }
            });

            var today = DateTimeOffset.ParseExact("2021-07-30-05.00.00.0000000", "yyyy-MM-dd-HH.mm.ss.fffffff", CultureInfo.InvariantCulture);
            Assert.Equal(today.AddHours(1), jobSchedule.NextRunTime(today));
        }
    }
}
