using System.Text;

namespace SoftwareWorker.BYO.CLI.Shell
{
    /// <summary>
    /// A token of an interactive shell input line.
    /// </summary>
    /// <param name="Value">The token value with quotes removed.</param>
    /// <param name="Start">Index of the first character of the token in the input line.</param>
    /// <param name="End">Index just after the last character of the token in the input line.</param>
    /// <param name="IsQuoted">Whether any part of the token was quoted.</param>
    /// <param name="IsTerminated">False when the token contains an opening quote without its closing quote.</param>
    internal sealed record ShellToken(string Value, int Start, int End, bool IsQuoted, bool IsTerminated);

    /// <summary>
    /// Splits an input line into tokens the same way for highlighting, completion and execution.
    /// Whitespace separates tokens, and single or double quotes group text that contains whitespace.
    /// Backslashes are kept as-is so Windows paths do not need escaping.
    /// </summary>
    internal static class ShellTokenizer
    {
        public static List<ShellToken> Tokenize(string text)
        {
            var tokens = new List<ShellToken>();
            var index = 0;

            while (index < text.Length)
            {
                if (char.IsWhiteSpace(text[index]))
                {
                    index++;
                    continue;
                }

                var start = index;
                var value = new StringBuilder();
                var isQuoted = false;
                var isTerminated = true;

                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                {
                    var current = text[index];
                    if (current != '"' && current != '\'')
                    {
                        value.Append(current);
                        index++;
                        continue;
                    }

                    isQuoted = true;
                    var closingQuote = text.IndexOf(current, index + 1);
                    if (closingQuote < 0)
                    {
                        value.Append(text, index + 1, text.Length - index - 1);
                        index = text.Length;
                        isTerminated = false;
                        break;
                    }

                    value.Append(text, index + 1, closingQuote - index - 1);
                    index = closingQuote + 1;
                }

                tokens.Add(new ShellToken(value.ToString(), start, index, isQuoted, isTerminated));
            }

            return tokens;
        }

        /// <summary>
        /// Quotes a value when it would otherwise be split into several tokens.
        /// </summary>
        public static string Quote(string value)
        {
            if (value.Length > 0 && !value.Any(c => char.IsWhiteSpace(c) || c == '"' || c == '\''))
            {
                return value;
            }

            return value.Contains('"') ? $"'{value}'" : $"\"{value}\"";
        }
    }
}
