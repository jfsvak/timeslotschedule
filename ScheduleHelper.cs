using System;

namespace TimeSlots.Domain
{
    public class ScheduleHelper
    {
        //public static void ValidateDelay(double delayInMillisecond, string schedulerName)
        //{
        //    if (delayInMillisecond <= 0)
        //        throw new ArgumentException($"Next {schedulerName} occurrence cannot be same as scheduling time or back in time");

        //    if (delayInMillisecond > TimeSpan.MaxValue.TotalMilliseconds)
        //        throw new ArgumentException($"Next {schedulerName} occurrence is too far in the future ({delayInMillisecond} ms). " +
        //                                    $"Cannot be further than {TimeSpan.FromMilliseconds(TimeSpan.MaxValue.TotalMilliseconds)} in the future");
        //}
    }
}
