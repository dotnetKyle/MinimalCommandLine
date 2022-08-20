namespace DemoApp.Handlers;

public static class RepeatHandler
{
    public static void Handle(string message, int repeatNumber)
    {
        for (int i = 0; i < repeatNumber; i++)
        {
            Console.WriteLine(message);
        }
    }
}
