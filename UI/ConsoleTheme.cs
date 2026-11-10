using System.Text;

namespace StackQueueLab.UI;

/// <summary>
/// Спокойная консольная тема: тёмно-серые линии, серый текст,
/// приглушённый голубой акцент и статусные цвета.
/// </summary>
public static class ConsoleTheme
{
    private const int Width = 78;
    private static readonly object Sync = new();

    public static void Initialize()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Перенаправленный вывод: кодировку менять не нужно.
        }

        try
        {
            Console.Title = "Лабораторная работа №3 — стек, очередь, выражения";
            Console.CursorVisible = true;
        }
        catch (IOException)
        {
        }
    }

    public static void BeginScreen(string breadcrumb)
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            Console.WriteLine();
        }

        DrawHeader(breadcrumb);
    }

    public static void DrawHeader(string breadcrumb)
    {
        lock (Sync)
        {
            Color(ConsoleColor.DarkGray);
            Console.WriteLine($"  ╭{new string('─', Width)}╮");
            Console.Write("  │  ");
            Color(ConsoleColor.Cyan);
            Console.Write(Pad("ЛАБОРАТОРНАЯ РАБОТА №3", Width - 13));
            Color(ConsoleColor.DarkGray);
            Console.WriteLine("C# / .NET  │");

            Console.Write("  │  ");
            Color(ConsoleColor.Gray);
            Console.Write(Pad("Стек · очередь · выражения", Width - 2));
            Color(ConsoleColor.DarkGray);
            Console.WriteLine("│");
            Console.WriteLine($"  ╰{new string('─', Width)}╯");

            Console.WriteLine();
            Color(ConsoleColor.DarkGray);
            Console.Write("  ");
            Console.Write(breadcrumb);
            Console.WriteLine();
            Console.WriteLine();
            Color(ConsoleColor.Gray);
        }
    }

    public static void Title(string title, string? subtitle = null)
    {
        Color(ConsoleColor.Cyan);
        Console.WriteLine($"  {title}");

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            Muted($"  {subtitle}");
        }

        Console.WriteLine();
    }

    public static void Section(string title)
    {
        Console.WriteLine();
        Color(ConsoleColor.DarkGray);
        Console.WriteLine($"  ── {title} {new string('─', Math.Max(2, Width - title.Length - 5))}");
        Color(ConsoleColor.Gray);
    }

    public static void Rule()
    {
        Color(ConsoleColor.DarkGray);
        Console.WriteLine($"  {new string('─', Width)}");
        Color(ConsoleColor.Gray);
    }

    public static void MenuOption(string key, string text, string? hint = null)
    {
        Console.Write("  ");
        Color(ConsoleColor.DarkCyan);
        Console.Write($"[{key}]");
        Color(ConsoleColor.Gray);
        Console.Write($"  {text}");

        if (!string.IsNullOrWhiteSpace(hint))
        {
            Color(ConsoleColor.DarkGray);
            Console.Write($"   {hint}");
        }

        Console.WriteLine();
        Color(ConsoleColor.Gray);
    }

    public static void Text(string text)
    {
        Color(ConsoleColor.Gray);
        Console.WriteLine($"  {text}");
    }

    public static void Muted(string text)
    {
        Color(ConsoleColor.DarkGray);
        Console.WriteLine(text);
        Color(ConsoleColor.Gray);
    }

    public static void Accent(string text)
    {
        Color(ConsoleColor.Cyan);
        Console.WriteLine(text);
        Color(ConsoleColor.Gray);
    }

    public static void Success(string text)
    {
        Color(ConsoleColor.Green);
        Console.WriteLine($"  ✔ {text}");
        Color(ConsoleColor.Gray);
    }

    public static void Info(string text)
    {
        Color(ConsoleColor.DarkCyan);
        Console.WriteLine($"  • {text}");
        Color(ConsoleColor.Gray);
    }

    public static void Warning(string text)
    {
        Color(ConsoleColor.Yellow);
        Console.WriteLine($"  ! {text}");
        Color(ConsoleColor.Gray);
    }

    public static void Error(string text)
    {
        Color(ConsoleColor.Red);
        Console.WriteLine($"  × {text}");
        Color(ConsoleColor.Gray);
    }

    public static void WriteLogLine(string line)
    {
        Color(ConsoleColor.Gray);
        Console.WriteLine(line);
    }

    public static void KeyValue(string key, string value)
    {
        Console.Write("  ");
        Color(ConsoleColor.DarkGray);
        Console.Write($"{key}: ");
        Color(ConsoleColor.Gray);
        Console.WriteLine(value);
    }

    public static void Pause(string message = "Enter — продолжить")
    {
        Console.WriteLine();
        Color(ConsoleColor.DarkGray);
        Console.Write($"  {message} ");
        Console.ResetColor();

        try
        {
            Console.ReadKey(intercept: true);
        }
        catch (InvalidOperationException)
        {
            Console.ReadLine();
        }

        Console.WriteLine();
    }

    public static void Separator(string caption)
    {
        Console.WriteLine();
        Color(ConsoleColor.DarkGray);
        var side = Math.Max(2, (Width - caption.Length - 6) / 2);
        Console.WriteLine($"  {new string('─', side)}  {caption}  {new string('─', side)}");
        Console.WriteLine();
        Color(ConsoleColor.Gray);
    }

    public static string Pad(string text, int width)
    {
        if (text.Length >= width)
        {
            return text[..width];
        }

        return text.PadRight(width);
    }

    public static void Color(ConsoleColor color) => Console.ForegroundColor = color;

    public static void Reset() => Console.ResetColor();
}
