using System.Text;

namespace System.CommandLine.Minimal.SourceGeneration;

internal static class CommandOptionsWriter
{
    /// <summary>
    /// Generates the <c>CommandOptions</c> class.
    /// </summary>
    internal static string? GenerateOptions(GeneratingCommandBinder binder)
    {
        if (binder.Bindings is not null)
        {
            StringBuilder sb = new();
            sb.AppendLine(
                """
                using System.CommandLine;
                using System.CommandLine.Invocation;
                using System.CommandLine.Minimal.Bindings;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.DependencyInjection.Extensions;

                namespace System.CommandLine.Minimal
                {
                """
            );
            sb.AppendLine();
            #region namespace System.CommandLine.Minimal

            sb.AppendLine($"    // \"{binder.CommandName}\" command, handler: {binder.FullMethodName}");
            if(binder.CommandName is not null)
            {
                // an extension onto builder that allows the developer to configure the options

                #region public static ConfigureCommandExtensions
                sb.AppendLine(
                    $$"""
                        public static class Configure{{binder.CommandNameTitleCase}}BuilderExtensions
                        {
                            /// <summary>Configure any additional options on your root command.</summary>
                            public static MinimalCommandLineBuilder Map{{binder.CommandNameTitleCase}}Command(this MinimalCommandLineBuilder builder, Action<{{binder.CommandOptionsName}}> configure)
                            {
                                // register '{{binder.CommandNameTitleCase}}' Command
                    """
                );
                // do not add the class to services if it's static
                if (!binder.MethodIsStatic)
                    sb.AppendLine($"            builder.Services.TryAddTransient<{binder.CommandOptionsName}>();");
                sb.AppendLine(
                    $$"""
                                {{binder.CommandOptionsName}} cliOptions = builder.TryRegisterCommandOptions<{{binder.CommandOptionsName}}>();

                                // apply developer's configuration changes
                                configure(cliOptions);

                                return builder;
                            }
                        }
                    """
                );
                #endregion

                // Use this generated class to add the conventional Command, Arguments, and Options for the class

                // iterate once through the bindings and create 3 stringbuilders
                StringBuilder writePublicPropertiesSb = new();
                StringBuilder linkCommandToSymbolsSb = new();
                StringBuilder createParametersSb = new();
                string[] parameterNames = new string[binder.Bindings.Value.Length];
                for (int i = 0; i < binder.Bindings.Value.Length; i++)
                {
                    ParameterBinding param = binder.Bindings.Value[i];
                    parameterNames[i] = param.OriginalParameterName;
                    if(param is ArgumentBinding arg)
                    {
                        // public property for Argument
                        writePublicPropertiesSb.AppendLine($"        public Argument<{arg.Type}> {arg.NameTitleCase}Argument {{ get; }} = new Argument<{arg.Type}>(\"{arg.HelpName}\");");
                        // link together the Command and the Arguments
                        linkCommandToSymbolsSb.AppendLine(
                            $$"""
                                        this.Command.Arguments.Add(this.{{arg.NameTitleCase}}Argument);
                                        bindings[{{i}}] = new ArgumentBinding(
                                            ParameterName: "{{arg.OriginalParameterName}}",
                                            ParameterType: typeof({{arg.Type}}),
                                            Argument: this.{{arg.NameTitleCase}}Argument
                                        );
                            """
                        );
                        // instantiate argument parameter
                        createParametersSb.AppendLine($"                {arg.Type} {arg.OriginalParameterName} = parseResult.GetValue(this.{arg.NameTitleCase}Argument);");
                    }
                    else if(param is OptionBinding opt)
                    {
                        // public property
                        writePublicPropertiesSb.AppendLine($"        public Option<{opt.Type}> {opt.NameTitleCase}Option {{ get; }} = new Option<{opt.Type}>(\"{opt.OptionName}\");");
                        // link together the Command and the Options
                        linkCommandToSymbolsSb.AppendLine(
                            $$"""
                                        this.Command.Options.Add(this.{{opt.NameTitleCase}}Option);
                                        bindings[{{i}}] = new OptionBinding(
                                            ParameterName: "{{opt.OriginalParameterName}}",
                                            ParameterType: typeof({{opt.Type}}),
                                            Option: this.{{opt.NameTitleCase}}Option
                                        );
                            """
                        );
                        // instantiate options parameter
                        createParametersSb.AppendLine($"                {opt.Type} {opt.OriginalParameterName} = parseResult.GetValue(this.{opt.NameTitleCase}Option);");
                    }
                    else if(param is FromServicesBinding svcs)
                    {
                        // get FromServices parameter
                        createParametersSb.AppendLine($"                {svcs.Type} {svcs.OriginalParameterName} = services.GetRequiredService<{svcs.Type}>();");
                    }
                }

                #region implement CommandOptions class
                sb.AppendLine($"    public sealed class " + binder.CommandOptionsName + " : CommandOptions");
                sb.AppendLine("    {");
                // add property accessor for the actual command
                sb.AppendLine($"        public override Command Command {{ get; }} = new Command(\"{binder.CommandNameTitleCase}\");");
                sb.AppendLine();
                // public Command, Argument, and Option properties
                sb.AppendLine(writePublicPropertiesSb.ToString());
                sb.AppendLine();
                sb.AppendLine("        public override ParameterBinding[] SetupCommandParameterBindings()");
                sb.AppendLine("        {");
                sb.AppendLine($"            ParameterBinding[] bindings = new ParameterBinding[{binder.Bindings.Value.Length}];");
                // join all of the bindings together
                sb.AppendLine(linkCommandToSymbolsSb.ToString());
                sb.AppendLine("            return bindings;");
                sb.AppendLine("        }");

                sb.AppendLine();

                // override Handler
                {
                    // create a Handler that takes in a (ParseResult parseResult) here
                    sb.AppendLine("        public override Func<ParseResult, CancellationToken, Task<int>> Handler(IServiceProvider services)");
                    sb.AppendLine("        {");
                    // return function
                    {
                        sb.AppendLine($"            return {(binder.MethodReturnType.Contains("System.Threading.Task") ? "async " : "")}(ParseResult parseResult, CancellationToken cancellationToken) => ");
                        sb.AppendLine("            {");
                        // if method is a static method
                        string methodQualifier;
                        if(binder.MethodIsStatic)
                        {
                            // if static qualify with class name
                            methodQualifier = binder.FullClassName;
                        }
                        else
                        {
                            // if instance method, qualify with 'svc'
                            methodQualifier = "svc";
                            // get service from dependency injections
                            sb.AppendLine($"                var svc = services.GetRequiredService<{binder.ClassNamespace}.{binder.ClassName}>();");
                        }

                        sb.AppendLine(createParametersSb.ToString());

                        string parameterList = string.Join(", ", parameterNames);
                        if (binder.MethodReturnType == "void")
                        {
                            sb.AppendLine($"                {methodQualifier}.{binder.MethodName}({parameterList});");
                            sb.AppendLine("                return Task.FromResult(0);");
                        }
                        else if (binder.MethodReturnType == "int")
                        {
                            sb.AppendLine("                return Task.FromResult(");
                            sb.AppendLine($"                    {methodQualifier}.{binder.MethodName}({parameterList})");
                            sb.AppendLine("                );");
                        }
                        else if (binder.MethodReturnType.EndsWith("System.Threading.Tasks.Task"))
                        {
                            sb.AppendLine($"                await {methodQualifier}.{binder.MethodName}({parameterList});");
                            sb.AppendLine("                return 0;");
                        }
                        else if (binder.MethodReturnType.EndsWith("System.Threading.Tasks.Task<int>"))
                        {
                            sb.AppendLine($"                return await {methodQualifier}.{binder.MethodName}({parameterList});");
                        }
                        // end return function
                        sb.AppendLine("            };");
                    }

                    // end override Handler
                    sb.AppendLine("        }");
                }

                sb.AppendLine();
                // end CommandOptions class
                sb.AppendLine("    }");
                sb.AppendLine();
                #endregion

            }

            #endregion
            // end namespace System.CommandLine.Minimal
            sb.AppendLine("}");

            return sb.ToString();
        }
        else
        {
            return null;
        }
    }
}
