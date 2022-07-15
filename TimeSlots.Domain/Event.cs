using System;

namespace Timeslots.Domain
{
    public class Event
    {
        public readonly DateTimeOffset When;
        public readonly EventType What;

        public Event(DateTimeOffset when, EventType what)
        {
            When = when;
            What = what;
        }
    }

    public enum EventType
    {
        Begin,
        End
    }
}
