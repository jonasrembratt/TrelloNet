using System;

namespace Trello.net.api
{
    public class BugStatInPeriod
    {
        public DateTime PeriodStart { get; private set; }
        public DateTime PeriodEnd { get; private set; }
        public float Value { get; private set; }

        public void Add() => ++Value;

        public override string ToString()
        {
            return $"{PeriodStart:yy-MM} = {Value}";
        }

        public BugStatInPeriod(DateTime start, DateTime end)
        {
            PeriodStart = start;
            PeriodEnd = end;
        }
    }
}