using System;
using System.Globalization;

namespace Trello.net.api
{
    public class Period
    {
        public const string Separator = "..";

        private Period _previous;
        private Period _next;

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TimeGranularity Granularity { get; set; }

        public int WeekNumber { get; set; }

        internal void SetPrevious(PeriodCardsStatus prev)
        {
            _previous = prev;
        }

        internal void SetNext(PeriodCardsStatus next)
        {
            _next = next;
        }

        public override string ToString()
        {
            return End != default(DateTime)
                ? $"{Granularity} : {Start:yy-MM-dd}{Separator}{End:yy-MM-dd}"
                : $"{Granularity} : {Start:yy-MM-dd}{Separator}";
        }

        #region .  Equality  .
        protected bool Equals(Period other)
        {
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Period)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        } 

        public static bool operator ==(Period left, Period right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Period left, Period right)
        {
            return !Equals(left, right);
        }
        #endregion

        public virtual Period Next(bool create = true)
        {
            if (_next != null || !create)
                return _next;

            _next = OnMakeNext(End.AddSeconds(1), Granularity);
            _next._previous = this;
            return _next;
        }

        public virtual Period Previous()
        {
            return _previous;
        }

        protected virtual Period OnMakeNext(DateTime start, TimeGranularity granularity)
        {
            return new Period(start, granularity);
        }

        private void day(DateTime start)
        {
            Start = new DateTime(start.Year, start.Month, start.Day);
            End = start.AddHours(24).Subtract(TimeSpan.FromSeconds(1));
            Granularity = TimeGranularity.Day;
            setWeekNumber();
        }

        private void week(DateTime start)
        {
            Start = GetFirstDateOfWeek(start, CultureInfo.CurrentCulture);
            End = Start.AddDays(7).Subtract(TimeSpan.FromSeconds(1));
            Granularity = TimeGranularity.Week;
            setWeekNumber();
        }

        private void month(DateTime start)
        {
            Start = new DateTime(start.Year, start.Month, 1);
            End = start.AddMonths(1).Subtract(TimeSpan.FromSeconds(1));
            Granularity = TimeGranularity.Month;
        }

        private void setWeekNumber()
        {
            var w = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(Start, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            if (w > 52)
                w = 1;
            WeekNumber = w;
        }

        public static DateTime GetFirstDateOfWeek(DateTime dayInWeek, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            var firstDay = culture.DateTimeFormat.FirstDayOfWeek;
            var firstDayInWeek = dayInWeek.Date;
            while (firstDayInWeek.DayOfWeek != firstDay)
                firstDayInWeek = firstDayInWeek.AddDays(-1);

            return firstDayInWeek;
        }

        public Period(DateTime start, TimeGranularity granularity)
        {
            Start = start;
            switch (granularity)
            {
                case TimeGranularity.Day:
                    day(start);
                    break;

                case TimeGranularity.Week:
                    week(start);
                    break;

                case TimeGranularity.Month:
                    month(start);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(granularity), granularity, null);
            }
        }

        public Period(DateTime start, DateTime end, TimeGranularity granularity)
        {
            Start = start;
            End = end;
            Granularity = granularity;
        }

        public Period()
        {
        }
    }
}