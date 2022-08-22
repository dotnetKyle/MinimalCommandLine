using System.CommandLine.Minimal;

namespace DemoApp.Handlers;

public static class RepeatHandler
{
    public static async Task HandleAsync(string message, int repeatNumber, int waitTimesBetweenMessages = 150)
    {
        for (int i = 0; i < repeatNumber; i++)
        {
            Console.WriteLine(message);
            await Task.Delay(waitTimesBetweenMessages);
        }
    }
}
public static class RepeatHandler2
{
    public static Task HandleAsync(
        [Argument] string message,
        [Option] int repeatNumber,
        [Option] int waitTimesBetweenMessagesInMs = 200
        )
    {
        throw new NotImplementedException();
    }
}