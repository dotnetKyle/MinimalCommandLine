using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MinimalCli.SourceGeneration.Conventions;

internal static class SymbolFormattingExtensions
{
    static readonly char[] unallowedChars = [ 
        '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
        '-', '+', '=', '{', '}', '[', ']', '|', '\\', ':', ';',
        '\'', '"', '<', '>', ',', '.', '?', '/', '~', '`',
        ' ', '\t', '\n', '\r' ];

    /// <summary>
    /// Conventionally convert a parameter name from a function into an help name using title case with spaces.
    /// </summary>
    /// <param name="symbolString">The parameter name or symbol string to convert</param>
    /// <returns></returns>
    public static string ToHelpName(this string symbolString)
    {
        // convert "myArgument" to "my Argument"
        string spaced = Regex.Replace(symbolString, "([a-z])([A-Z])", "$1 $2");
        // convert "my Argument" to "My Argument"
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced);
    }

    /// <summary>
    /// Capitalize the first character of a string. e.g. paramName becomes ParamName
    /// </summary>
    /// <param name="symbolString"></param>
    /// <returns></returns>
    public static string ToSymbolName(this string symbolString)
    {
        if (string.IsNullOrWhiteSpace(symbolString))
            return string.Empty;

        bool capitalizeNext = true;
        int j = 0;
        char[] characters = new char[symbolString.Length];

        for (int i = 0; i < symbolString.Length; i++)
        {
            char c = symbolString[i];
            if(unallowedChars.Contains(c))
            {
                capitalizeNext = true;
                continue;
            }
            else if(capitalizeNext)
            {
                characters[j++] = char.ToUpperInvariant(c);
                capitalizeNext = false;
            }
            else
            {
                characters[j++] = c;
            }
        }

        return new(characters, 0, j);
    }
}
