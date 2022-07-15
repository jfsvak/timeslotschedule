using System;
using Timeslots.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Timeslots.Test
{
    public class TimeslotTest
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm";
        private readonly ITestOutputHelper _output;

        public TimeslotTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GivenTimeslot_WhenCreatingEmptyTimeslotWithDatetime_ExceptionIsThrown()
        {
            Assert.Throws<ArgumentException>(() => new Timeslot((DateTimeOffset?)null, (DateTimeOffset?)null));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData(" ", " ")]
        public void GivenTimeslot_WhenCreatingEmptyTimeslot_ExceptionIsThrown(string from, string to)
        {
            Assert.Throws<ArgumentException>(() => new Timeslot(from, to));
        }

        [Theory]
        [InlineData("2021-02-17 10:00", null)]
        [InlineData(null, "2021-02-17 10:00")]
        [InlineData("2021-02-15 03:45", "2021-02-17 10:00")]
        public void GivenTimeSlots_WhenComparing_TheyAreEqual(string begin, string end)
        {
            DateTimeOffset? b = begin != null ? DateTimeOffset.ParseExact(begin, DateTimeFormat, null) : (DateTimeOffset?)null;
            DateTimeOffset? e = end != null ? DateTimeOffset.ParseExact(end, DateTimeFormat, null) : (DateTimeOffset?)null;

            Timeslot timeslot = new Timeslot(b, e);
            Timeslot timeslot2 = new Timeslot(b, e);
            Assert.True(timeslot.Equals(timeslot2));
        }

        [Theory]
        [InlineData("2021-02-17 10:00", null, null, "2021-02-17 10:00")]
        [InlineData(null, "2021-02-17 10:00", "2021-02-17 10:00", "2021-02-17 10:01")]
        [InlineData("2021-02-15 03:45", "2021-02-17 10:00", "2021-02-17 10:00", "2021-02-18 03:45")]
        public void GivenTimeSlots_WhenComparingDifferentTimeSlots_TheyAreNotEqual(string firstBegin, string firstEnd, string secondBegin, string secondEnd)
        {
            DateTimeOffset? b = firstBegin != null ? DateTimeOffset.ParseExact(firstBegin, DateTimeFormat, null) : (DateTimeOffset?)null;
            DateTimeOffset? e = firstEnd != null ? DateTimeOffset.ParseExact(firstEnd, DateTimeFormat, null) : (DateTimeOffset?)null;
            DateTimeOffset? b2 = secondBegin != null ? DateTimeOffset.ParseExact(secondBegin, DateTimeFormat, null) : (DateTimeOffset?)null;
            DateTimeOffset? e2 = secondEnd != null ? DateTimeOffset.ParseExact(secondEnd, DateTimeFormat, null) : (DateTimeOffset?)null;

            Timeslot timeslot = new Timeslot(b, e);
            Timeslot timeslot2 = new Timeslot(b2, e2);
            Assert.False(timeslot.Equals(timeslot2));
        }

        [Theory]
        [InlineData("2021-02-17 10:00", null)]
        [InlineData(null, "2021-02-17 10:00")]
        [InlineData("2021-02-15 03:45", "2021-02-17 10:00")]
        public void GivenTimeSlots_WhenComparingHashCodesSimilarInstances_TheyAreEqual(string begin, string end)
        {
            DateTimeOffset? b = begin != null ? DateTimeOffset.ParseExact(begin, DateTimeFormat, null) : (DateTimeOffset?) null;
            DateTimeOffset? e = end != null ? DateTimeOffset.ParseExact(end, DateTimeFormat, null) : (DateTimeOffset?)null;

            Timeslot timeSlot = new Timeslot(b, e);
            Timeslot timeSlot2 = new Timeslot(b, e);
            Assert.Equal(timeSlot.GetHashCode(), timeSlot2.GetHashCode());
        }

        [Theory]
        [InlineData("2021-02-17 10:00", null, null, "2021-02-17 10:00")]
        [InlineData(null, "2021-02-17 10:00", "2021-02-17 10:00", "2021-02-17 10:01")]
        [InlineData("2021-02-15 03:45", "2021-02-17 10:00", "2021-02-17 10:00", "2021-02-18 03:45")]
        public void GivenTimeSlots_WhenComparingHashCodesOnDifferentInstances_TheyAreNotEqual(string firstBegin, string firstEnd, string secondBegin, string secondEnd)
        {
            DateTimeOffset? begin1 = firstBegin != null ? DateTimeOffset.ParseExact(firstBegin, DateTimeFormat, null) : (DateTimeOffset?)null;
            DateTimeOffset? end1 = firstEnd != null ? DateTimeOffset.ParseExact(firstEnd, DateTimeFormat, null) : (DateTimeOffset?)null;
            DateTimeOffset? begin2 = secondBegin != null ? DateTimeOffset.ParseExact(secondBegin, DateTimeFormat, null) : (DateTimeOffset?)null;
            DateTimeOffset? end2 = secondEnd != null ? DateTimeOffset.ParseExact(secondEnd, DateTimeFormat, null) : (DateTimeOffset?)null;

            Timeslot firstTime = new Timeslot(begin1, end1);
            Timeslot secondTimeSlot = new Timeslot(begin2, end2);
            Assert.NotEqual(firstTime.GetHashCode(), secondTimeSlot.GetHashCode());
        }

        [Theory]
        [InlineData("2021-02-17 10:00", "2021-02-16 23:00")]
        public void GivenTimeSlotInstance_WhenEndIsLesserThanBegin_ThrowException(string begin, string end)
        {
            var actual = Assert.Throws<ArgumentException>(() => new Timeslot(DateTimeOffset.ParseExact(begin, DateTimeFormat, null), DateTimeOffset.ParseExact(end, DateTimeFormat, null)));
        }
    }
}
