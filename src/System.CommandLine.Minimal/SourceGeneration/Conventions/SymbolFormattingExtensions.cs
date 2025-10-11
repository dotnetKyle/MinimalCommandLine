using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace System.CommandLine.Minimal.SourceGeneration.Conventions
{
    internal static class SymbolFormattingExtensions
    {
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
            char c  = char.ToUpper(symbolString[0]);
            return c + symbolString.Substring(1);
        }
    }
}
