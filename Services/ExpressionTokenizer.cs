namespace StackQueueLab.Services;

public enum InfixTokenKind
{
    Number,
    BinaryOperator,
    Function,
    LeftParenthesis,
    RightParenthesis
}

public sealed record InfixToken(string Text, InfixTokenKind Kind, double Number);

/// <summary>
/// Постфиксная запись режется по пробелам; инфиксная — посимвольно, чтобы
/// корректно распознавать функции, числа с десятичной запятой/точкой и скобки.
/// </summary>
public static class ExpressionTokenizer
{
    public static IReadOnlyList<InfixToken> TokenizeInfix(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ExpressionSyntaxException("Выражение пусто.");
        }

        var tokens = new List<InfixToken>();
        var index = 0;

        while (index < expression.Length)
        {
            var current = expression[index];

            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (char.IsDigit(current) || current is '.' or ',')
            {
                var start = index;

                while (index < expression.Length &&
                       (char.IsDigit(expression[index]) || expression[index] is '.' or ','))
                {
                    index++;
                }

                var text = expression[start..index];

                if (!NumberText.TryParse(text, out var number))
                {
                    throw new ExpressionSyntaxException($"Некорректное число «{text}».");
                }

                tokens.Add(new InfixToken(text, InfixTokenKind.Number, number));
                continue;
            }

            if (char.IsLetter(current))
            {
                var start = index;

                while (index < expression.Length && char.IsLetter(expression[index]))
                {
                    index++;
                }

                var name = expression[start..index].ToLowerInvariant();

                if (!MathExpressionRules.IsFunction(name))
                {
                    throw new ExpressionSyntaxException(
                        $"Неизвестная функция «{name}». Поддерживаются: ln, cos, sin, sqrt.");
                }

                tokens.Add(new InfixToken(name, InfixTokenKind.Function, 0));
                continue;
            }

            if (current == '(')
            {
                tokens.Add(new InfixToken("(", InfixTokenKind.LeftParenthesis, 0));
                index++;
                continue;
            }

            if (current == ')')
            {
                tokens.Add(new InfixToken(")", InfixTokenKind.RightParenthesis, 0));
                index++;
                continue;
            }

            if (current is '+' or '-' or '*' or '/' or ':' or '^')
            {
                tokens.Add(new InfixToken(current.ToString(), InfixTokenKind.BinaryOperator, 0));
                index++;
                continue;
            }

            throw new ExpressionSyntaxException($"Недопустимый символ «{current}».");
        }

        if (tokens.Count == 0)
        {
            throw new ExpressionSyntaxException("Выражение не содержит токенов.");
        }

        return tokens;
    }

    public static IReadOnlyList<string> TokenizePostfix(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Array.Empty<string>();
        }

        return expression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }
}
