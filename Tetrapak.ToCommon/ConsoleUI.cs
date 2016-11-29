using System;
using System.Text;
using TetraPak;

namespace Tetrapak.ToCommon
{
    public class ConsoleUI : IUserInterface
    {
        private string _indentation = "  ";
        private string _indent;
        private int _indentLevel;
        private string _sectionDelimiter = "-";
        private bool _omitIndent;
        private IBusyControl _busyControl;

        public string SectionDelimiter
        {
            get { return _sectionDelimiter; }
            set
            {
                if (value == _sectionDelimiter || value?.Length == 0)
                    return;
                _sectionDelimiter = value;
            }
        }

        public int IndentLevel
        {
            get { return _indentLevel; }
            private set
            {
                if (value == _indentLevel)
                    return;

                _indentLevel = value;
                buildIndent();
            }
        }

        public string Indentation
        {
            get { return _indentation; }
            set
            {
                if (value == _indentation)
                    return;
                _indentation = value;
                buildIndent();
            }
        }

        private void buildIndent()
        {
            if (IndentLevel == 0 || _indentation?.Length == 0)
            {
                _indent = "";
                return;
            }
            var sb = new StringBuilder();
            for (var i = 0; i < IndentLevel; i++)
            {
                sb.Append(_indentation);
            }
            _indent = sb.ToString();

        }

        public void Indent(int countLevels = 1)
        {
#if DEBUG
            if (countLevels < 1) throw new ArgumentOutOfRangeException(nameof(countLevels), "Indentation must be a positive number.");
#endif
            IndentLevel += countLevels;
        }

        public void Outdent()
        {
#if DEBUG
            if (IndentLevel == 0)
                throw new InvalidOperationException("Indent level is already zero (0).");
#endif
            --IndentLevel;
        }

        public void ResetIndent(int value = 0)
        {
            IndentLevel = value;
        }

        public void Section(string message, bool indent = true)
        {
            WriteLine(sectionDelimiter());
            if (!string.IsNullOrEmpty(message))
                WriteLine(message);
            if (indent)
                Indent();
        }

        private string sectionDelimiter()
        {
            var sb = new StringBuilder(_indent);
            if (SectionDelimiter.Length == 1)
            {
                sb.Append(new string(SectionDelimiter[0], Console.WindowWidth));
                return sb.ToString();
            }
            while (sb.Length + SectionDelimiter.Length < Console.WindowWidth)
                sb.Append(SectionDelimiter);
            return sb.ToString();
        }

        public void WriteLine(string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine();
                _omitIndent = false;
                return;
            }
            Console.WriteLine($"{indent(message)}");
            _omitIndent = false;
        }

        public void Write(string message)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));
            Console.Write($"{indent(message)}");
            _omitIndent = true;
        }

        public void Error(string message)
        {
            var resetFgColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine(indent(message));
            Console.ForegroundColor = resetFgColor;
        }

        public void Error(Exception exception)
        {
            Error(exception.ToString());
        }

        private string indent()
        {
            return _omitIndent ? "" : _indent;
        }

        private string indent(string text)
        {
            return _omitIndent ? "" : _indent + text.ReplaceAll("\n", $"\n{indent()}");
        }

        public string Ask(string message, params string[] options)
        {
            var sOptions = new StringBuilder($"({options[0]}");
            for (var i = 1; i < options.Length; i++)
            {
                sOptions.Append("/");
                sOptions.Append(options[i]);
            }
            sOptions.Append(")");
            string answer = null;
            int index;
            while (answer == null || (index = answer.Match(options)) == -1)
            {
                WriteLine(message);
                Write($"{sOptions} : ");
                answer = Console.ReadLine();
                _omitIndent = false;
            }
            return options[index];
        }

        public char ReadKey()
        {
            return Console.ReadKey().KeyChar;
        }

        public void ShowBusy(int value = 0, int maxValue = 100, string message = null)
        {
            if (_busyControl == null)
                _busyControl = new ConsoleProgressControl();
            _busyControl.ShowBusy(value, maxValue, message);
        }

        public void UpdateBusy(int value, string message = null)
        {
            _busyControl?.UpdateBusy(value, message);
        }

        public void HideBusy()
        {
            _busyControl?.HideBusy();
        }

        class ConsoleProgressControl : IBusyControl
        {
            private string Message { get; set; }

            private decimal Value { get; set; }

            private decimal MaxValue { get; set; }

            private decimal PositionX { get; set; }

            private int PositionY { get; set; }

            private decimal Size { get; set; }

            private const char Character = '|';

            private void drawProgressBar()
            {
                Console.CursorVisible = false;
                var perc = Value / MaxValue;
                var chars = (int)Math.Floor(perc / (1 / Size));

                var p1 = new string(Character, chars);
                var p2 = new string(Character, (int) (Size - chars));

                Console.CursorLeft = (int) PositionX;
                Console.CursorTop = PositionY;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(p1);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write(p2);

                var txt = $" {perc*100:N2}% {Message ?? ""}";
                if (txt.Length + PositionX > Console.WindowWidth)
                    txt = txt.Substring(0, Console.WindowWidth - 6) + " ...";
                else
                {
                    var fillSize = Math.Max(0, Console.WindowWidth - (int)PositionX - txt.Length);
                    txt += new string(' ', fillSize);
                }
                Console.ResetColor();
                Console.CursorTop = PositionY + 1;
                Console.CursorLeft = (int)PositionX;
                Console.Write(txt);
            }

            public void ShowBusy(int value = 0, int maxValue = 100, string message = null)
            {
                var margin = Math.Max(2, Console.CursorLeft);
                Message = message;
                Value = value;
                MaxValue = maxValue;
                PositionX = margin;
                PositionY = Console.CursorTop;
                Size = Console.WindowWidth - margin*2;
                drawProgressBar();
            }

            public void UpdateBusy(int value, string message = null)
            {
                Value = value;
                if (!string.IsNullOrEmpty(message))
                    Message = message;
                drawProgressBar();
            }

            public void HideBusy()
            {
                Console.CursorLeft = (int) PositionX;
                Console.CursorTop = PositionY;
                var s = new string(' ', (int) Size);
                Console.WriteLine(s);
                Console.WriteLine(s);
                Console.CursorTop = PositionY;
            }
        }
    }
}