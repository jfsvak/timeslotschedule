using System;
using System.Globalization;
using Timeslots.Extensions;

namespace Timeslots.Domain
{
    public class Timeslot
    {
        public readonly DateTimeOffset? From;
        public readonly DateTimeOffset? To;

        public Timeslot(DateTimeOffset? from, DateTimeOffset? to)
        {
            From = from;
            To = to;
            Validate();
        }

        public Timeslot(string fromAsString, string toAsString, string DateTimeFormat = "yyyy-MM-dd HH:mm")
        {
            From = string.IsNullOrWhiteSpace(fromAsString) ? (DateTimeOffset?)null : DateTimeOffset.ParseExact(fromAsString, DateTimeFormat, CultureInfo.InvariantCulture);
            To = string.IsNullOrWhiteSpace(toAsString) ? (DateTimeOffset?)null : DateTimeOffset.ParseExact(toAsString, DateTimeFormat, CultureInfo.InvariantCulture);
            Validate();
        }

        private void Validate()
        {
            if (!From.HasValue && !To.HasValue)
                throw new ArgumentException("Cannot have empty Timeslot. Needs either a beginning or an end");

            if (To.HasValue && From.HasValue && To.Value.IsBefore(From.Value))
                throw new ArgumentException("The date/time of 'to' has to be after 'from' date/time", nameof(To));
        }

        public override int GetHashCode()
        {
            return From.GetHashCode() + To.GetHashCode() * 2;
        }

        public override bool Equals(object obj)
        {
            if (obj is not Timeslot other)
                return false;

            return From.Equals(other.From) && To.Equals(other.To);
        }
    }
}
