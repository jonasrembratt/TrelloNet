using System;
using System.Collections.Generic;

namespace Trello.net.api
{
    public class LeadTimesResult
    {
        public TimeGranularity Granularity { get; }

        public TimeSpan LeadTimes { get; internal set; }

        public IEnumerable<LeadTimesResultPeriod> Periods { get; } = new List<LeadTimesResultPeriod>();

        internal void AddPeriod(LeadTimesResultPeriod period)
        {
            ((List<LeadTimesResultPeriod>)Periods).Add(period);
        }

        public LeadTimesResult(TimeGranularity granularity)
        {
            Granularity = granularity;
        }
    }
}