namespace StackQueueLab.UI;

/// <summary>
/// Небольшие текстовые помощники для аккуратных русских окончаний.
/// </summary>
public static class TextFormat
{
    public static string Plural(int number, string one, string few, string many)
    {
        var lastTwo = number % 100;
        var last = number % 10;

        if (lastTwo is >= 11 and <= 14)
        {
            return many;
        }

        return last switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many
        };
    }

    public static string Count(int number, string one, string few, string many) =>
        $"{number} {Plural(number, one, few, many)}";
}
