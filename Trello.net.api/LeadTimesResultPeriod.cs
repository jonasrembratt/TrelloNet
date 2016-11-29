using System;
using Tetrapak.ToCommon;

namespace Trello.net.api
{
    public class LeadTimesResultPeriod : Period
    {
        public TimeSpan LeadTimes { get; }

        //public LeadTimesResultPeriod(DateTime start, DateTime end, TimeGranularity granularity, TimeSpan leadTimes) obsolete
        //    : base(start, end, granularity)
        //{
        //    LeadTimes = leadTimes;
        //}

        public LeadTimesResultPeriod(Period source, TimeSpan leadTimes)
            : base(source.Start, source.End, source.Granularity)
        {
            this.AssignShallowFrom(source);
            LeadTimes = leadTimes;
        }
    }
}