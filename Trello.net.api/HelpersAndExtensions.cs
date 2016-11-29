using System;
using System.Linq;
using System.Text;
using TrelloNet;

namespace Trello.net.api
{
    public static class HelpersAndExtensions
    {
        private static readonly DateTime _s_epochOrigin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime DateCreated(this Card card)
        {
            return DateCreated(card.Id);
        }

        public static DateTime DateCreated(this string id)
        {
            var sEpoch = id.Substring(0, 8);
            var epoch = long.Parse(sEpoch, System.Globalization.NumberStyles.HexNumber);
            return _s_epochOrigin.AddSeconds(epoch);
        }

        public static bool IsBug(this Card card)
        {
            return card.Labels.Any(l => l.Name == "BUG");
        }

        public static TimeSpan LeadTime(this Card card)
        {
            var dateCreated = card.DateCreated();
            var dateLastActivity = card.DateLastActivity;
            return dateLastActivity.Subtract(dateCreated);
        }

        public static string ToString(this string[] sa, char separator = ';')
        {
            var sb = new StringBuilder();
            foreach (var s in sa)
            {
                sb.Append(s);
                sb.Append(separator);
            }
            return sb.ToString().TrimEnd(separator);
        }

    }
}