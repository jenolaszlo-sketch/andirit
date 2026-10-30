public static class Worker
{
    public static void Execute()
    {
        Logger.Write("first");
        Logger.Write("second");
        Logger.Write("third");
    }
}
public static class Logger
{
    public static void Write(string value) { }
}
