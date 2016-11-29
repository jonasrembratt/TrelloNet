using System;

namespace Tetrapak.ToCommon
{
    // tocommon
    public interface ILog
    {
        string SectionDelimiter { get; set; }

        int IndentLevel { get; }

        string Indentation { get; set; }

        void Indent(int countLevels = 1);

        void Outdent();

        void ResetIndent(int value = 0);

        void Section(string message, bool indent = true);

        void WriteLine(string message = null);

        void Write(string message);

        void Error(string message);

        void Error(Exception exception);
    }
}