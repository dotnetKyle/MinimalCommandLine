using System.Collections.Generic;

namespace System.CommandLine.Minimal.Bindings
{
    public class CommandBindingFactory
    {
        readonly Dictionary<string, CommandOptions> options;

        public CommandBindingFactory()
        {
            this.options = [];
        }

        public void AddCommandOptions(string commandName, CommandOptions options)
        {
            this.options.Add(commandName, options);
        }

        public CommandOptions? TryGetOptionsFor(string commandName)
        {
            if(this.options.TryGetValue(commandName, out CommandOptions? binding))
                return binding;

            return null;
        }
    }
}
