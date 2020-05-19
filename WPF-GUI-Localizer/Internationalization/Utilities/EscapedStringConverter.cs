using System.Text;

namespace Internationalization.Utilities
{
    /// <summary>
    /// Converter for removing and readding common special characters
    /// (handles \t, \r, \n \\).
    /// Only for usage in Code, not intended as Converter in Bindings.
    /// </summary>
    public static class EscapedStringConverter
    {
        /// <summary>
        /// Replaces placeholders introdiced by <see cref="ToEscapedString"/> with corresponding characters.
        /// </summary>
        /// <param name="escapedString">The string whose placeholders should be removed.</param>
        /// <returns>
        /// <paramref name="escapedString"/> with all placeholders replaced with their corresponding characters.
        /// </returns>
        public static string ToNormalString(string escapedString)
        {
            return escapedString?.Replace(@"\t", "\t").Replace(@"\r", "\r")
                .Replace(@"\n", "\n").Replace(@"\slash", @"\");
        }

        /// <summary>
        /// Replaces special symbols (\t, \r, \n \\) with corresponding placeholders.
        /// </summary>
        /// <param name="normalString">The string whose special characters should be removed.</param>
        /// <returns>
        /// <paramref name="normalString"/> with all special characters replaced with their corresponding placeholders.
        /// </returns>
        public static string ToEscapedString(string normalString)
        {
            if (normalString == null)
            {
                return null;
            }

            var builder = new StringBuilder();

            foreach (var c in normalString)
            {
                switch (c)
                {
                    case '\t':
                        builder.Append(@"\t");
                        break;
                    case '\r':
                        builder.Append(@"\r");
                        break;
                    case '\n':
                        builder.Append(@"\n");
                        break;
                    case '\\':
                        //using @"\\" causes problems, when used with Replace(@"\\", @"\").Replace(@"\n", "\n")
                        //in situations like '\'+'n' -> "\\n" -> file -> "\\n" -> '\'+'n' -> '\n'.
                        builder.Append(@"\slash");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
