namespace DynamicProxyGenerator;

public class CustomConsoleWriter
{
    public static void WriteLine(string content, int count)
    {
        for (var i = 0; i < count; i++)
        {
            Console.WriteLine(content);
        }
    }
}
