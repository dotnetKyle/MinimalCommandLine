namespace DemoApp.Handlers;

public static class EchoHandler
{
    public static void Handle(string message)
    {
        Console.WriteLine(message);
    }
}
