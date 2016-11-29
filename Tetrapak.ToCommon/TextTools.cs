using System;
using System.Text;

namespace TetraPak
{
	/// <summary>
	/// Utility class for common string operations.
	/// </summary>
	public static class TextTools
	{
		private const string Escape = "\\";
		private const string EncodedEscape = Escape + Escape;
		public const string NewLine = Escape + "nl";
        public static string ReplaceAll(this string s, string pattern, string replace, StringComparison stringComparison = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            var at = s.IndexOf(pattern, stringComparison);
            var sb = new StringBuilder();
            var idx = 0;
            var idx2 = 0;
            while (at != -1)
            {
                sb.Append(s.Substring(idx, at - idx));
                sb.Append(replace);
                idx = idx2 = at + pattern.Length;
                at = s.IndexOf(pattern, idx, stringComparison);
            }
            if (idx2 + pattern.Length < s.Length)
                sb.Append(s.Substring(idx2));
            return sb.ToString();
        }

        public static string ReplaceLast(this string s, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            var at = s.LastIndexOf(oldValue, StringComparison.Ordinal);
            if (at == -1)
                return s;

            return s.Substring(0, at) + newValue + s.Substring(at + oldValue.Length);
        }

        public delegate string ReplaceCallback(string all, string oldSubstring, int index);

        public static string ReplaceAll(this string s, string oldValue, ReplaceCallback replaceCallback)
        {
            if (string.IsNullOrEmpty(s)) throw new ArgumentNullException("s");
            try
            {
                var at = s.IndexOf(oldValue, StringComparison.Ordinal);
                while (at != -1)
                {
                    s = s.Replace(oldValue, replaceCallback(s, oldValue, at));
                    at = s.IndexOf(oldValue, StringComparison.Ordinal);
                }
                return s;
            }
            catch (Exception)
            {
                throw;
            }
        }
        
	}
}
