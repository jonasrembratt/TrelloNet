using System;
using System.Text;
using Trello.net.api;

namespace Trello.net
{
    class ArgsParser
    {
        public static char AssignmentQualifier { get; set; } = '=';
        public static string PeriodSeparator { get; set; } = "..";

        public string GetArgQualifier(string s, ref int index, out bool equalsFound)
        {
            string foundTerminator;
            var argQualifier = eatUntilWhiteSpaceOrTerminator(s, ref index, out foundTerminator, AssignmentQualifier.ToString());
            equalsFound = foundTerminator == AssignmentQualifier.ToString();
            return argQualifier;
        }

        public static DateTime? GetDate(string s, ref int index, out string foundTerminator, string terminator = null)
        {
            var i = index;
            var sDate = eatUntilWhiteSpaceOrTerminator(s, ref i, out foundTerminator, terminator);
            DateTime value;
            if (!DateTime.TryParse(sDate, out value))
                return default(DateTime);

            index = i;
            return value;
        }

        public static Period GetPeriod(string s, ref int index)
        {
            string sepFound;
            var from = GetDate(s, ref index, out sepFound, PeriodSeparator);
            if (!from.HasValue)
                return null;

            if (sepFound != PeriodSeparator)
                return new Period(from.Value, default(TimeGranularity));

            var to = GetDate(s, ref index, out sepFound);
            return to.HasValue
                ? new Period(from.Value, to.Value, default(TimeGranularity))
                : new Period(from.Value, default(TimeGranularity));
        }

        public TimeGranularity? GetTimeGranularity(string s, ref int index)
        {
            var sGranularity = eatUntilWhiteSpace(s, ref index);
            if (sGranularity == null)
                return null;

            TimeGranularity granularity;
            return Enum.TryParse(sGranularity, true, out granularity) ? (TimeGranularity?) granularity : null;
        }

        private static void eatLeading(string s, ref int index)
        {
            while (index < s.Length && char.IsWhiteSpace(s[index++])) { }
            --index;
        }

        private static string eatUntilWhiteSpace(string s, ref int index)
        {
            string _;
            return eatUntilWhiteSpaceOrTerminator(s, ref index, out _);
        }

        private static string eatUntilWhiteSpaceOrTerminator(string s, ref int index, out string foundTerminator, string terminator = null)
        {
            var sb = new StringBuilder();
            var leading = true;
            for (; index < s.Length; index++)
            {
                var c = s[index];
                if (char.IsWhiteSpace(c))
                {
                    if (!leading)
                    {
                        foundTerminator = c.ToString();
                        return sb.ToString();
                    }
                    eatLeading(s, ref index);
                    if (index < s.Length)
                        c = s[index];
                }
                leading = false;
                if (isTerminator(s, ref index, terminator))
                {
                    foundTerminator = terminator;
                    return sb.ToString();
                }
                sb.Append(c);
            }
            foundTerminator = null;
            return sb.ToString();
        }

        private static bool isTerminator(string s, ref int index, string terminator)
        {
            if (terminator == null)
                return false;

            var tIndex = 0;
            for (var i = index; i < s.Length && tIndex < terminator.Length; i++, tIndex++)
            {
                if (s[i] != terminator[tIndex])
                    return false;
            }
            index += terminator.Length;
            return true;
        }
    }
}
