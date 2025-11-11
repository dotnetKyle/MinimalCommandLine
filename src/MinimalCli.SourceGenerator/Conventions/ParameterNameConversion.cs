using System.Globalization;
using System.Text.RegularExpressions;

namespace MinimalCli.SourceGeneration.Conventions;

internal static class ParameterNameConversion
{
    /// <summary>
    /// Conventionally convert a parameter name from a function into an Argument name using title case. 
    /// <para>
    /// For example convert <c>"myArgument"</c> into <c>"My Argument"</c>.
    /// </para>
    /// </summary>
    /// <param name="parameterName">The parameter name to convert.</param>
    /// <returns>A title case argument name. e.g. <c>"My Argument Name"</c>.</returns>
    internal static string ToArgumentName(string parameterName)
    {
        // convert "myArgument" to "my Argument"
        string spaced = Regex.Replace(parameterName, "([a-z])([A-Z])", "$1 $2");
        // convert "my Argument" to "My Argument"
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced);
    }

    /// <summary>
    /// Conventionally convert a parameter name from a function into a fully lowercase option name using kebab case. 
    /// <para>
    /// For example convert <c>"myOption"</c> into <c>"--my-option"</c>.
    /// </para>
    /// </summary>
    /// <param name="parameterName">The parameter name to convert.</param>
    /// <returns>A kebab case option name. e.g. <c>"--my-option-name"</c>.</returns>
    internal static string ToOptionName(string parameterName)
    {
        string kebab = Regex.Replace(parameterName, "([a-z])([A-Z])", "$1-$2")
            .Replace('_', '-')
            .ToLower();
        return "--" + kebab;
    }
}
