using System.Text;

namespace PackSF;

public class LogUtils
{
    public static void ConsoleWriteLine(ConsoleColor color, params object[] objects)
    {
        var sb = new StringBuilder();
        var now = DateTime.Now;
        var timeStr = now.ToString("HH:mm:ss.fff");
        sb.Append('[').Append(timeStr).Append("] ");
        var len = objects.Length;
        for (var n = 0; n < len; n++)
        {
            sb.Append(objects[n]);
            if (n != len - 1)
                sb.Append(' ');
        }

        Console.ForegroundColor = color;
        Console.WriteLine(sb);
    }

    public static void Info(params object[] objects)
    {
        ConsoleWriteLine(ConsoleColor.White, objects);
    }

    public static void Warning(params object[] objects)
    {
        ConsoleWriteLine(ConsoleColor.Yellow, objects);
    }

    public static void Error(params object[] objects)
    {
        ConsoleWriteLine(ConsoleColor.Red, objects);
    }
}