using System.Globalization;

namespace StackQueueLab.Services;

public static class MathExpressionRules
{
    private static readonly HashSet<string> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ln",
        "cos",
        "sin",
        "sqrt"
    };

    public static bool IsFunction(string token) => Functions.Contains(token);

    public static bool IsBinaryOperator(string token) =>
        token is "+" or "-" or "*" or "/" or ":" or "^";

    public static bool IsUnaryFunction(string token) => IsFunction(token);

    public static bool IsUnaryMinus(string token) => token is "u-" or "neg";

    public static int Precedence(string token) => token switch
    {
        "+" or "-" => 1,
        "*" or "/" or ":" => 2,
        "^" or "u-" or "neg" => 3,
        _ => 0
    };

    public static bool IsRightAssociative(string token) => token == "^";
}

public static class NumberText
{
    public static bool TryParse(string text, out double value)
    {
        var normalized = text.Trim();

        if (normalized.Contains(',') && !normalized.Contains('.'))
        {
            normalized = normalized.Replace(',', '.');
        }

        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    public static string Format(double value)
    {
        if (value == 0.0)
        {
            value = 0.0;
        }

        return value.ToString("0.###############", CultureInfo.InvariantCulture);
    }
}
