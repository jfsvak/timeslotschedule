using System;
using Xunit;

namespace TimeSlots.Test
{
    public class SchedulerHelperTest
    {
        [Theory]
        [InlineData("-00:00:00.0000078")]
        [InlineData("-00:00:00.0000008")]
        [InlineData("-00:00:00.0000001")]
        [InlineData("00:00:00.0000000")]
        public void GivenValidateDelay_WhenDelayIsNowOrBackInTimeInMicrosecond_ThrowArgumentException(string backInTimeInMicrosecond)
        {
            const string schedulerName = FetchSchedulerNames.FetchNotification;
            var delay = TimeSpan.Parse(backInTimeInMicrosecond);
            var ex = Record.Exception(() => ScheduleHelper.ValidateDelay(delayInMillisecond: delay.TotalMilliseconds, schedulerName));
            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.Contains($"Next {schedulerName} occurrence cannot be same as scheduling time or back in time", ex.Message);
        }

        [Fact]
        public void GivenValidateDelay_WhenDelayExceededMaxValue_ThrowArgumentException()
        {
            const string schedulerName = FetchSchedulerNames.FetchEpkSlActive;
            var delayInMillisecond = TimeSpan.MaxValue.TotalMilliseconds + 1;
            var ex = Record.Exception(() => ScheduleHelper.ValidateDelay(delayInMillisecond: delayInMillisecond, schedulerName));
            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.Contains($"{schedulerName}", ex.Message);
            Assert.Contains("occurrence is too far in the future", ex.Message);
        }

        [Theory]
        [InlineData("00:00:00.0000078")]
        [InlineData("00:00:00.0000008")]
        [InlineData("00:00:00.0000001")]
        public void GivenValidateDelay_WhenDelayIsInMicrosecond_DoNotThrowException(string backInTimeInMicrosecond)
        {
            const string schedulerName = FetchSchedulerNames.FetchEpkBlDeactivated;
            var delay = TimeSpan.Parse(backInTimeInMicrosecond);
            var ex = Record.Exception(() => ScheduleHelper.ValidateDelay(delayInMillisecond: delay.TotalMilliseconds, schedulerName));
            Assert.Null(ex);
        }

        [Fact]
        public void GivenValidateDelay_WhenDelayIsMaxMillisecond_DoNotThrowException()
        {
            const string schedulerName = FetchSchedulerNames.FetchReadyForMarking;
            var delay = TimeSpan.FromMilliseconds(TimeSpan.MaxValue.TotalMilliseconds);
            var ex = Record.Exception(() => ScheduleHelper.ValidateDelay(delayInMillisecond: delay.TotalMilliseconds, schedulerName));
            Assert.Null(ex);
        }

        [Fact]
        public void GivenValidateDelay_WhenDelayIsMaxTicks_DoNotThrowException()
        {
            const string schedulerName = FetchSchedulerNames.FetchReadyForMarking;
            var delay = TimeSpan.FromTicks(TimeSpan.MaxValue.Ticks);
            var ex = Record.Exception(() => ScheduleHelper.ValidateDelay(delayInMillisecond: delay.TotalMilliseconds, schedulerName));
            Assert.Null(ex);
        }
    }
}
