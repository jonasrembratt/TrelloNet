using System;
using System.Linq;
using System.Text;
using Tetrapak.ToCommon;
using Trello.net.api;

namespace Trello.net
{
    class Program
    {
        private static MyTrelloApi _s_api;
        private static IUserInterface UI = new ConsoleUI();

        private const string CmdGetAnalysis = "getAnalysis";
        private const string CmdGetAnalysisShort2 = "analyze";
        private const string CmdGetAnalysisShort3 = "analyse";
        private const string CmdGetAnalysisShort = "analysis";
        private const string CmdGetAnalysisAbbreviated = "ga";
        private const string CmdGetLeadTimes = "getLeadtimes";
        private const string CmdGetLeadTimesShort = "leadtimes";
        private const string CmdGetLeadTimesAbbreviated = "gl";
        private const string CmdClear = "clear";
        private const string CmdQuit = "quit";
        private const string CmdHelp = "help";
        private const string CmdHelpShort = "?";
        private const string ArgPeriod = "period";
        private const string ArgPeriodAbbreviated = "p";
        private const string ArgResolution = "resolution";
        private const string ArgResolutionAbbreviated = "r";

        static void Main(string[] args)
        {
            //showUnicode();
            string authorizationToken;
            string boardId;
            string[] doingIds, doneIds;
            parse(args, out authorizationToken, out boardId, out doingIds, out doneIds);
            _s_api = new MyTrelloApi(authorizationToken, UI);
            if (boardId != null)
                _s_api.SelectBoard(boardId);
            if (doingIds?.Length > 0)
                _s_api.SetDoingListsIds(doingIds);
            if (doneIds?.Length > 0)
                _s_api.SetDoneListsIds(doneIds);

            var quit = false;
            while (!_s_api.IsAuthorized && !quit)
                tryAuthorize(out quit);

            while (runCommand())
            {
            }
        }

        private static void showUnicode()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            for (var i = 0; i <= 3000; i++)
            {
                Console.Write((char) i);
                if (i%50 == 0)
                {
                    // break every 50 chars
                    Console.WriteLine();
                }
            }
            Console.ReadKey();
        }

        private static bool runCommand(string command = null)
        {
            string verb;
            string args;
            if (string.IsNullOrEmpty(command))
            {
                Console.Write("Trello: ");
                command = Console.ReadLine();
            }
            parseCommand(command, out verb, out args);
            if (CmdQuit.Equals(verb, StringComparison.OrdinalIgnoreCase))
                return false;

            execute(verb, args);
            return true;
        }

        private static void execute(string verb, string args)
        {
            if (string.IsNullOrEmpty(verb))
                return;

            try
            {
                var v = verb.ToLower();
                switch (v)
                {
                    case CmdGetAnalysis:
                    case CmdGetAnalysisShort:
                    case CmdGetAnalysisAbbreviated:
                    case CmdGetAnalysisShort2:
                    case CmdGetAnalysisShort3:
                        Period period;
                        parseGetStatusArgs(args, out period);
                        var analysisResult = _s_api.AnalyzePeriod(period.Start, period.End, period.Granularity);
                        statusOut(analysisResult, period.Granularity);
                        break;
                    case CmdGetLeadTimes:
                    case CmdGetLeadTimesShort:
                    case CmdGetLeadTimesAbbreviated:
                        parseGetStatusArgs(args, out period);
                        var leadTimesResult = _s_api.GetLeadTimes(period.Start, period.End != default(DateTime) ? (DateTime?)period.End : null, period.Granularity);
                        statusOut(leadTimesResult);
                        break;
                    case CmdClear:
                        Console.Clear();
                        break;
                    case CmdHelp:
                        helpOut(true);
                        break;
                    case CmdHelpShort:
                        helpOut();
                        break;
                    default:
                        error($"Unknown command: '{v}'");
                        break;
                }
            }
            catch (Exception ex)
            {
                UI.Error(ex.Message);
            }
        }

        private static void helpOut(bool full = false)
        {
            helpAnalysis(full);
            helpLeadtimes(full);
            helpClear(full);
            helpQuit(full);
        }

        private static void helpAnalysis(bool full = false)
        {
            var eq = ArgsParser.AssignmentQualifier;
            var sep = ArgsParser.PeriodSeparator;
            if (!full)
            {
                UI.WriteLine($"{CmdGetAnalysis} [<period>{eq}<date>{sep}[<date>]] [<resolution>{eq}<granularity>]");
                return;
            }
            UI.Section("Analysis\n--------");
            var args = $"[<period>{eq}<date>{sep}[<date>]] [<resolution>{eq}<granularity>]";
            UI.WriteLine($"{CmdGetAnalysis} {args}");
            UI.WriteLine($"{CmdGetAnalysisShort} {args}");
            UI.WriteLine($"{CmdGetAnalysisShort2} {args}");
            UI.WriteLine($"{CmdGetAnalysisShort3} {args}");
            UI.WriteLine($"{CmdGetAnalysisAbbreviated} {args}");
            UI.WriteLine("<date>=yyyy-MM-dd");
            UI.WriteLine($"<resolution>={ArgResolution} | {ArgResolutionAbbreviated}");
            UI.WriteLine($"<granularity>={enumValuesToString(typeof(TimeGranularity), " | ")}\n");
            UI.WriteLine("Use this command to build an analysis.\n"+
                         "All other commands rely on this having been done and will automatically\n"+
                         "build one if needed.\n");
            UI.WriteLine("Period:\nSpecifies the start and (optionally) end date of the period to be analysed.\n" +
                         "When omitted the whole project period will be assumed (from start to today).\n" +
                         "When the 'to' element is omitted the current date is assumed.");
            UI.WriteLine("Resolution:\nSpecifies how to split the analysis up into smaller chunks of time.");
            UI.Outdent();
        }

        private static void helpLeadtimes(bool full = false)
        {
            var eq = ArgsParser.AssignmentQualifier;
            var sep = ArgsParser.PeriodSeparator;
            if (!full)
            {
                UI.WriteLine($"{CmdGetLeadTimes} [<period>{eq}<date>{sep}[<date>]] [<resolution>{eq}<granularity>]");
                return;
            }
            UI.Section("Lead times\n----------");
            var args = $"[<period>{eq}<date>{sep}[<date>]] [<resolution>{eq}<granularity>]";
            UI.WriteLine($"{CmdGetLeadTimes} {args}");
            UI.WriteLine($"{CmdGetLeadTimesShort} {args}");
            UI.WriteLine($"{CmdGetLeadTimesAbbreviated} {args}");
            UI.WriteLine("<date>=yyyy-MM-dd");
            UI.WriteLine($"<resolution>={ArgResolution} | {ArgResolutionAbbreviated}");
            UI.WriteLine($"<granularity>={enumValuesToString(typeof(TimeGranularity), " | ")}\n");
            UI.WriteLine("Use this command to get project lead times over time.\n");
            UI.WriteLine("Period:\nSpecifies the start and (optionally) end date of the period to be analysed.\n." +
                         "When omitted the whole project period will be assumed (from start to today).\n" +
                         "When the 'to' element is omitted the current date is assumed.");
            UI.WriteLine("Resolution:\nSpecifies how to split the analysis up into smaller chunks of time.");
            UI.Outdent();
        }

        private static void helpClear(bool full = false)
        {
            if (!full)
            {
                UI.WriteLine(CmdClear);
                return;
            }
            UI.Section("CLEAR\n-----");
            UI.WriteLine($"{CmdClear}\n");
            UI.WriteLine("Clears the console window.");
            UI.Outdent();
        }

        private static void helpQuit(bool full = false)
        {
            if (!full)
            {
                UI.WriteLine(CmdQuit);
                return;
            }
            UI.Section("QUIT\n----");
            UI.WriteLine($"{CmdQuit}\n");
            UI.WriteLine("Quits the application.");
            UI.Outdent();
        }

        private static string enumValuesToString(Type enumType, string separator = ",")
        {
            var values = enumType.GetEnumValues();
            var sb = new StringBuilder(values.GetValue(0).ToString());
            for (var i = 1; i < values.Length; i++)
            {
                sb.Append(separator);
                sb.Append(values.GetValue(i));
            }
            return sb.ToString();
        }

        private static void statusOut(LeadTimesResult result)
        {
            var gran = result.Granularity;
            Console.WriteLine($"{(gran != TimeGranularity.Month ? $"{gran};\t" : "")}Period;\tLead times;");
            var maxLength = 0;
            foreach (var p in result.Periods)
            {
                var w = gran != TimeGranularity.Month ? p.WeekNumber.ToString() : "";
                var line = $"{(gran != TimeGranularity.Month ? $"{w};\t" : "")}{p.Start:yy-MM-dd};\t{p.LeadTimes:dd':'hh};";
                Console.WriteLine(line);
                maxLength = Math.Max(maxLength, line.Length);
            }
            Console.WriteLine(new string('-', maxLength));
            Console.WriteLine($"Overall average lead times: {result.LeadTimes:dd':'hh}");
        }

        private static void statusOut(BoardAnalysisResult result, TimeGranularity gran)
        {
            Console.WriteLine($"{(gran != TimeGranularity.Month ? $"{gran};\t" : "")}Period;\tDoing;\tDone;\tOther;Still ongoing;");
            foreach (var p in result.Periods)
            {
                var w = gran != TimeGranularity.Month ? p.WeekNumber.ToString() : "";
                Console.WriteLine($"{(gran != TimeGranularity.Month ? $"{w};\t" : "")}{p.Start:yy-MM-dd};\t{p.Doing.Count};\t{p.Done.Count};\t{p.Other.Count};\t{p.StillOngoing.Count()}");
            }
        }

        private static void parseGetStatusArgs(string args, out Period period)
        {
            // todo
            // [period=starDate>..[<endDate>]] [resolution=day]
            // [p=starDate>..[<endDate>]] [res=day]

            var prs = new ArgsParser();
            var index = 0;
            period = null;
            while (index < args.Length)
            {
                bool equalsFound;
                var identifier = prs.GetArgQualifier(args, ref index, out equalsFound);
                switch (identifier.ToLowerInvariant())
                {
                    case "period":
                    case "p":
                        if (!equalsFound)
                            throw error($"Expected period value after '{identifier}'.");

                        var p = ArgsParser.GetPeriod(args, ref index);
                        if (p == null)
                            throw error($"Expected period value after '{identifier}'.");

                        period = period ?? p;
                        break;

                    case "resolution":
                    case "res":
                        if (!equalsFound)
                            throw error($"Expected granularity value after '{identifier}'.");

                        var granularity = prs.GetTimeGranularity(args, ref index);
                        if (granularity == null)
                            throw error($"Expected granularity value after '{identifier}'.");

                        if (period != null)
                            period.Granularity = granularity.Value;
                        else
                            period = new Period {Granularity = granularity.Value};
                        break;

                    default:
                        break;
                }
            }
            period = period ?? new Period(DateTime.MinValue, DateTime.Today.AddDays(1).Subtract(TimeSpan.FromSeconds(1)), TimeGranularity.Week);

        }

        private static Exception error(string msg)
        {
            //UI.Error(msg);
            return new Exception(msg);
        }

        private static void parseCommand(string cmd, out string verb, out string args)
        {
            if (string.IsNullOrEmpty(cmd))
            {
                verb = args = null;
                return;
            }
            var verbEnds = cmd.IndexOf(' ');
            if (verbEnds == -1)
            {
                verb = cmd;
                args = "";
                return;
            }
            verb = cmd.Substring(0, verbEnds);
            args = cmd.Substring(verbEnds).Trim();
        }

        //private static void testStuff()
        //{
        //    var nisse = _s_api.AnalyzePeriod(new DateTime(2016, 11, 1), DateTime.Today, TimeGranularity.Week);
        //    var bugsPerMonth = _s_api.GetBugsPerMonth();
        //    var avgLeadTimes = _s_api.GetAverageLeadTimes();
        //}

        private static bool tryAuthorize(out bool quit)
        {
            Console.WriteLine("Please authenticate here ...");
            Console.WriteLine(_s_api.AuthorizationUrl);
            Console.WriteLine("... then submit token (or type 'quit').");
            var authenticated = false;
            while (!authenticated)
            {
                Console.Write("token:");
                var token = Console.ReadLine();
                quit = token?.Equals("quit", StringComparison.OrdinalIgnoreCase) ?? false;
                if (quit)
                    break;

                authenticated = _s_api.TryAuthenticate(token);
            }
            quit = false;
            return authenticated;
        }

        private const string TokenIdent = "token";
        private const string BoardIdent = "board";
        private const string DoingIdent = "doing";
        private const string DoneIdent = "done";

        private static void parse(string[] args, out string authorizationToken, out string boardId, out string[] doingIds, out string[] doneIds)
        {
            authorizationToken = boardId = null;
            doingIds = doneIds = null;
            foreach (var arg in args)
            {
                string key, value;
                parseKeyValue(arg, out key, out value);
                if (key.Equals(TokenIdent, StringComparison.OrdinalIgnoreCase))
                    authorizationToken = value;
                else if (key.Equals(BoardIdent, StringComparison.OrdinalIgnoreCase))
                    boardId = value;
                else if (key.Equals(DoingIdent, StringComparison.OrdinalIgnoreCase))
                    doingIds = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                else if (key.Equals(DoneIdent, StringComparison.OrdinalIgnoreCase))
                    doneIds = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        private static void parseKeyValue(string s, out string key, out string value)
        {
            var split = s.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 1)
            {
                key = value = split[0];
                return;
            }
            key = split[0];
            value = split[1];
        }
    }
}
