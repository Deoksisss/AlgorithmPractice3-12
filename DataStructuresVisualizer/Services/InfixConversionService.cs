namespace DataStructuresVisualizer.Services;

public sealed record InfixConversionStep(
    int CurrentToken,
    IReadOnlyList<string> Stack,
    IReadOnlyList<string> Output,
    string Message,
    IReadOnlyList<int>? StackIndices = null,
    IReadOnlyList<int>? OutputIndices = null);

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
        // Validate grammar using the same parser as the expression tree before producing frames.
        ExpressionService.BuildTree(ExpressionService.ToPostfix(expression));
        var stack = new Stack<(string Text, int Index)>();
        var output = new List<(string Text, int Index)>();
        var steps = new List<InfixConversionStep>();
        void Record(int index, string message) => steps.Add(new InfixConversionStep(index,
            stack.Select(t => t.Text).ToArray(), output.Select(t => t.Text).ToArray(), message,
            stack.Select(t => t.Index).ToArray(), output.Select(t => t.Index).ToArray()));
        void Pop(int index)
        {
            var moved = stack.Pop();
            output.Add(moved);
            Record(index, $"Извлекаем «{moved.Text}» с вершины стека в выход.");
        }
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            Record(i, $"Читаем «{token.Text}».");
            if (token.IsNumber)
            {
                output.Add((token.Text, i));
                Record(i, $"Число «{token.Text}» переносим в выход.");
            }
            else if (token.Text == "(")
            {
                stack.Push((token.Text, i));
                Record(i, "Открывающую скобку помещаем в стек.");
            }
            else if (token.Text == ")")
            {
                while (stack.Count > 0 && stack.Peek().Text != "(") Pop(i);
                if (stack.Count == 0) throw new ExpressionConversionException("Не найдена открывающая скобка.");
                stack.Pop();
                Record(i, "Парные скобки удаляем; в постфиксную запись они не входят.");
            }
            else
            {
                while (stack.Count > 0 && stack.Peek().Text != "(" &&
                       Precedence(stack.Peek().Text) >= Precedence(token.Text)) Pop(i);
                stack.Push((token.Text, i));
                Record(i, $"Оператор «{token.Text}» (приоритет {Precedence(token.Text)}) помещаем на вершину.");
            }
        }
        while (stack.Count > 0)
        {
            if (stack.Peek().Text == "(") throw new ExpressionConversionException("Не закрыта скобка.");
            Pop(tokens.Count);
        }
        Record(tokens.Count, "Готово: все операторы перенесены в выходную запись.");
        return new InfixConversionResult(tokens.Select(t => t.Text).ToArray(), steps,
            string.Join(" ", output.Select(t => t.Text)));
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
