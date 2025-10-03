using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System.CommandLine.Minimal.SourceGeneration
{


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
                sb.AppendLine("using System.CommandLine;");
                sb.AppendLine("using System.CommandLine.Invocation;");
                sb.AppendLine("using System.CommandLine.Minimal.Bindings;");
                sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
                sb.AppendLine();

                // namespace System.CommandLine.Minimal
                sb.AppendLine("namespace System.CommandLine.Minimal");
                sb.AppendLine("{");
                sb.AppendLine();
                #region namespace System.CommandLine.Minimal

                sb.AppendLine($"    // \"{binder.CommandName}\" command, handler: {binder.FullMethodName}");
                if(binder.CommandName is not null)
                {
                    // an extension onto builder that allows the developer to configure the options
                    #region public static ConfigureCommandExtensions
                    sb.AppendLine($"    public static class Configure{binder.CommandNameTitleCase}BuilderExtension");
                    sb.AppendLine("    {");
                    sb.AppendLine("        /// <summary>Configure any additional options on your root command.</summary>");
                    sb.AppendLine($"        public static MinimalCommandLineBuilder Configure{binder.CommandNameTitleCase}Command(this MinimalCommandLineBuilder builder, Action<{binder.CommandOptionsName}> options)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            {binder.CommandOptionsName} cliOptions = builder.TryRegisterCommandOptions<{binder.CommandOptionsName}>();");
                    sb.AppendLine("            // apply developer's configuration changes");
                    sb.AppendLine("            options(cliOptions);");
                    sb.AppendLine("            return builder;");
                    sb.AppendLine("        }");
                    sb.AppendLine("    }");
                    sb.AppendLine();
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
                            linkCommandToSymbolsSb.AppendLine($"            this.Command.Arguments.Add(this.{arg.NameTitleCase}Argument);");
                            linkCommandToSymbolsSb.AppendLine($"            bindings[{i}] = new ArgumentBinding(");
                            linkCommandToSymbolsSb.AppendLine($"                ParameterName: \"{arg.OriginalParameterName}\",");
                            linkCommandToSymbolsSb.AppendLine($"                ParameterType: typeof({arg.Type}),");
                            linkCommandToSymbolsSb.AppendLine($"                Argument: this.{arg.NameTitleCase}Argument");
                            linkCommandToSymbolsSb.AppendLine($"            );");
                            // instantiate argument parameter
                            createParametersSb.AppendLine($"                {arg.Type} {arg.OriginalParameterName} = parseResult.GetValue(this.{arg.NameTitleCase}Argument);");
                        }
                        else if(param is OptionBinding opt)
                        {
                            // public property
                            writePublicPropertiesSb.AppendLine($"        public Option<{opt.Type}> {opt.NameTitleCase}Option {{ get; }} = new Option<{opt.Type}>(\"{opt.OptionName}\");");
                            // link together the Command and the Options
                            linkCommandToSymbolsSb.AppendLine($"            this.Command.Options.Add(this.{opt.NameTitleCase}Option);");
                            linkCommandToSymbolsSb.AppendLine($"            bindings[{i}] = new OptionBinding(");
                            linkCommandToSymbolsSb.AppendLine($"                ParameterName: \"{opt.OriginalParameterName}\",");
                            linkCommandToSymbolsSb.AppendLine($"                ParameterType: typeof({opt.Type}),");
                            linkCommandToSymbolsSb.AppendLine($"                Option: this.{opt.NameTitleCase}Option");
                            linkCommandToSymbolsSb.AppendLine($"            );");
                            // instantiate options parameter
                            createParametersSb.AppendLine($"                {opt.Type} {opt.OriginalParameterName} = parseResult.GetValue(this.{opt.NameTitleCase}Option);");
                        }
                        else if(param is FromServicesBinding svcs)
                        {
                            // get FromServices parameter
                            createParametersSb.AppendLine($"                {svcs.Type} {svcs.OriginalParameterName} = services.GetRequiredService<{svcs.Type}>();");
                        }
                    }

                    #region public partial class CommandOptions
                    sb.AppendLine($"    public sealed class " + binder.CommandOptionsName + " : CommandOptions");
                    sb.AppendLine("    {");
                    // add property accessor for the actual command
                    sb.AppendLine($"        public override Command Command {{ get; }} = new Command(\"{binder.CommandNameTitleCase}\");");
                    sb.AppendLine();
                    sb.AppendLine(writePublicPropertiesSb.ToString());

                    //foreach (ParameterBinding param in binder.Bindings)
                    //{
                    //    if (param is ArgumentBinding arg)
                    //    {
                    //        sb.AppendLine($"        public Argument<{arg.Type}> {arg.NameTitleCase}Argument {{ get; }} = new Argument<{arg.Type}>(\"{arg.HelpName}\");");
                    //    }
                    //    else if (param is OptionBinding opt)
                    //    {
                    //        sb.AppendLine($"        public Option<{opt.Type}> {opt.NameTitleCase}Option {{ get; }} = new Option<{opt.Type}>(\"{opt.OptionName}\");");
                    //    }
                    //    //else if(param is FromServicesBinding svcs)
                    //    //{
                    //    //    sb.AppendLine($"            public {svcs.Type} Get");
                    //    //}
                    //}
                    sb.AppendLine();
                    sb.AppendLine("        public override ParameterBinding[] SetupCommandParameterBindings()");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            ParameterBinding[] bindings = new ParameterBinding[{binder.Bindings.Value.Length}];");
                    sb.AppendLine(linkCommandToSymbolsSb.ToString());
                    //for (int i = 0; i < binder.Bindings.Value.Length; i++)
                    //{
                    //    ParameterBinding param = binder.Bindings.Value[i];

                    //    sb.AppendLine();
                    //    if (param is ArgumentBinding arg)
                    //    {
                    //        sb.AppendLine($"            this.Command.Arguments.Add(this.{arg.NameTitleCase}Argument);");
                    //        sb.AppendLine($"            bindings[{i}] = new ArgumentBinding(");
                    //        sb.AppendLine($"                ParameterName: \"{arg.OriginalParameterName}\",");
                    //        sb.AppendLine($"                ParameterType: typeof({arg.Type}),");
                    //        sb.AppendLine($"                Argument: this.{arg.NameTitleCase}Argument");
                    //        sb.AppendLine($"            );");
                    //    }
                    //    else if(param is OptionBinding opt)
                    //    {
                    //        sb.AppendLine($"            this.Command.Options.Add(this.{opt.NameTitleCase}Option);");
                    //        sb.AppendLine($"            bindings[{i}] = new OptionBinding(");
                    //        sb.AppendLine($"                ParameterName: \"{opt.OriginalParameterName}\",");
                    //        sb.AppendLine($"                ParameterType: typeof({opt.Type}),");
                    //        sb.AppendLine($"                Option: this.{opt.NameTitleCase}Option");
                    //        sb.AppendLine($"            );");
                    //    }
                    //    else if (param is FromServicesBinding svcs)
                    //    {
                    //        // note: I don't think I need this
                    //        sb.AppendLine($"            bindings[{i}] = new DependencyInjectionBinding(");
                    //        sb.AppendLine($"                ParameterName: \"{svcs.OriginalParameterName}\",");
                    //        sb.AppendLine($"                ParameterType: typeof({svcs.Type})");
                    //        sb.AppendLine($"            );");
                    //    }
                    //}
                    sb.AppendLine("            return bindings;");
                    sb.AppendLine("        }");

                    // TODO: create a private static Handler that takes in a (ParseResult parseResult) here
                    sb.AppendLine();
                    sb.AppendLine("        public override Func<ParseResult, CancellationToken, Task<int>> Handler(IServiceProvider services)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            return {(binder.MethodReturnType.Contains("System.Threading.Task") ? "async " : "")}(ParseResult parseResult, CancellationToken cancellationToken) => {{ ");

                    // get service from dependency injections
                    sb.AppendLine($"                {binder.ClassNamespace}.{binder.ClassName} svc = services.GetRequiredService<{binder.ClassNamespace}.{binder.ClassName}>();");
                    sb.AppendLine(createParametersSb.ToString());

                    // funcParams collects a small stringbuilder of parameters to pass in
                    //StringBuilder functionCallParameters = new();
                    //for (int i = 0; i < binder.Bindings.Value.Length; i++)
                    //{
                    //    if(i > 0) functionCallParameters.Append(", ");
                    //    functionCallParameters.Append(binder.Bindings.Value[i].OriginalParameterName);

                    //    if (binder.Bindings.Value[i] is ArgumentBinding arg)
                    //    {
                    //        sb.AppendLine($"                {arg.Type} {arg.OriginalParameterName} = parseResult.GetValue(this.{arg.NameTitleCase}Argument);");
                    //    }
                    //    else if (binder.Bindings.Value[i] is OptionBinding opt)
                    //    {
                    //        sb.AppendLine($"                {opt.Type} {opt.OriginalParameterName} = parseResult.GetValue(this.{opt.NameTitleCase}Option);");
                    //    }
                    //    else if (binder.Bindings.Value[i] is FromServicesBinding svcs)
                    //    {
                    //        sb.AppendLine($"                {svcs.Type} {svcs.OriginalParameterName} = services.GetRequiredService<{svcs.Type}>();");
                    //    }
                    //}

                    string parameterList = string.Join(", ", parameterNames);
                    if (binder.MethodReturnType == "void")
                    {
                        sb.AppendLine($"                svc.{binder.MethodName}({parameterList});");
                        sb.AppendLine("                return Task.FromResult(0);");
                    }
                    else if (binder.MethodReturnType == "int")
                    {
                        sb.AppendLine("                return Task.FromResult(");
                        sb.AppendLine($"                    svc.{binder.MethodName}({parameterList})");
                        sb.AppendLine("                );");
                    }
                    else if (binder.MethodReturnType.EndsWith("System.Threading.Tasks.Task"))
                    {
                        sb.AppendLine($"                await svc.{binder.MethodName}({parameterList});");
                        sb.AppendLine("                return 0;");
                    }
                    else if (binder.MethodReturnType.EndsWith("System.Threading.Tasks.Task<int>"))
                    {
                        sb.AppendLine($"                return await svc.{binder.MethodName}({parameterList});");
                    }
                    sb.AppendLine("            };");
                    sb.AppendLine("        }");
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
}
