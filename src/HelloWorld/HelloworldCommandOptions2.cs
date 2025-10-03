using System.CommandLine;
using System.CommandLine.Minimal.Bindings;

public sealed class HelloworldCommandOptions2 : CommandOptions
{

    public override Command Command { get; } = new Command("Helloworld");
    public Argument<string> MessageArgument { get; } = new Argument<string>("Message");
    public Option<string> Option1Option { get; } = new Option<string>("--option1");
    public Option<string> Option2Option { get; } = new Option<string>("--option2");

    public override ParameterBinding[] SetupCommandParameterBindings()
    {
        ParameterBinding[] bindings = new ParameterBinding[3];

        this.Command.Arguments.Add(this.MessageArgument);
        bindings[0] = new ArgumentBinding(
            ParameterName: "message",
            ParameterType: typeof(string),
            Argument: this.MessageArgument
        );

        this.Command.Options.Add(this.Option1Option);
        bindings[1] = new OptionBinding(
            ParameterName: "option1",
            ParameterType: typeof(string),
            Option: this.Option1Option
        );

        this.Command.Options.Add(this.Option2Option);
        bindings[2] = new OptionBinding(
            ParameterName: "option2",
            ParameterType: typeof(string),
            Option: this.Option2Option
        );
        return bindings;
    }
    void callHandler(IServiceProvider services, ParseResult parseResult)
    {
        // get all services that need to be injected here
        Command.SetAction((ParseResult parseResult) => { 

        });
    }
    public override Func<ParseResult, CancellationToken, Task<int>> Handler(IServiceProvider services)
    {
        return (ParseResult parseResult, CancellationToken cancellationToken) => { 
            string? message = parseResult.GetValue(this.MessageArgument);
            string? option1 = parseResult.GetValue(this.Option1Option);
            string? option2 = parseResult.GetValue(this.Option2Option);
            return Task.FromResult(0);
        };
    }
}