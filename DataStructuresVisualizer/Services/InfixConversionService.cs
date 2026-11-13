namespace DataStructuresVisualizer.Services;

public sealed record InfixConversionStep(
    int CurrentToken,
    IReadOnlyList<string> Stack,
    IReadOnlyList<string> Output,
    string Message);

public sealed record InfixConversionResult(
    IReadOnlyList<string> Tokens,
    IReadOnlyList<InfixConversionStep> Steps,
    string Postfix);

public static class InfixConversionService
{
    private sealed record Token(string Text, bool IsNumber);

    public static InfixConversionResult ConvertWithSteps(string expression)
    {
        var tokens = Tokenize(expression);
        var stack = new Stack<string>();
        var output = new List<string>();
        var steps = new List<InfixConversionStep>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (token.IsNumber)
            {
                output.Add(token.Text);
            }
            else if (token.Text == "(")
            {
                stack.Push(token.Text);
            }
            else if (token.Text == ")")
            {
                var opened = false;

                while (stack.Count > 0)
                {
                    var top = stack.Pop();

                    if (top == "(")
                    {
                        opened = true;
                        break;
                    }

                    output.Add(top);
                }

                if (!opened)
                {
                    throw new ExpressionConversionException("Не найдена открывающая скобка.");
                }
            }
            else
            {
                while (stack.Count > 0 &&
                       stack.Peek() != "(" &&
                       Precedence(stack.Peek()) >= Precedence(token.Text))
                {
                    output.Add(stack.Pop());
                }

                stack.Push(token.Text);
            }

            steps.Add(CreateStep(i, stack, output, Describe(token.Text)));
        }

        while (stack.Count > 0)
        {
            var top = stack.Pop();

            if (top == "(")
            {
                throw new ExpressionConversionException("Не закрыта скобка.");
            }

            output.Add(top);
        }

        steps.Add(new InfixConversionStep(
            tokens.Count,
            Array.Empty<string>(),
            output.ToArray(),
            "Готово: все операторы перенесены в выходную запись."));

        return new InfixConversionResult(
            tokens.Select(token => token.Text).ToArray(),
            steps,
            string.Join(" ", output));
    }

    private static InfixConversionStep CreateStep(
        int currentToken,
        Stack<string> stack,
        List<string> output,
        string message)
    {
        return new InfixConversionStep(
            currentToken,
            stack.ToArray(),
            output.ToArray(),
            message);
    }

    private static string Describe(string token) => token switch
    {
        _ when token == "(" => "Открывающая скобка: помещаем в стек.",
        _ when token == ")" => "Закрывающая скобка: выталкиваем операторы до (.",
        _ => $"Читаем «{token}»."
    };

    private static int Precedence(string operation) => operation switch
    {
        "+" or "-" => 1,
        "*" or "/" => 2,
        _ => 0
    };

    private static List<Token> Tokenize(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ExpressionConversionException("Выражение пустое.");
        }

        var result = new List<Token>();
        var index = 0;

        while (index < expression.Length)
        {
            var current = expression[index];

            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (char.IsDigit(current) || current == '.')
            {
                var start = index;

                while (index < expression.Length &&
                       (char.IsDigit(expression[index]) || expression[index] == '.'))
                {
                    index++;
                }

                var text = expression[start..index];

                if (!double.TryParse(
                        text,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out _))
                {
                    throw new ExpressionConversionException($"Некорректное число «{text}».");
                }

                result.Add(new Token(text, true));
                continue;
            }

            if (current is '+' or '-' or '*' or '/' or '(' or ')')
            {
                result.Add(new Token(current.ToString(), false));
                index++;
                continue;
            }

            throw new ExpressionConversionException($"Недопустимый символ «{current}».");
        }

        if (result.Count == 0)
        {
            throw new ExpressionConversionException("Выражение не содержит токенов.");
        }

        return result;
    }
}
