using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace System.CommandLine.Minimal.SourceGeneration
{
    internal static class SymbolFormattingExtensions
    {
        /// <summary>
        /// Not implemented yet, this should split the symbol into sections, add spaces and capitalize words.
        /// </summary>
        /// <param name="textInfo"></param>
        /// <param name="symbolString"></param>
        /// <returns></returns>
        public static string ToHelpName(this TextInfo textInfo, string symbolString)
        {
            return textInfo.ToTitleCase(symbolString);
        }

        public static string ToSymbolName(this string symbolString)
        {
            char c  = char.ToUpper(symbolString[0]);
            return c + symbolString.Substring(1);
        }
    }
}
