using System.Globalization;
using StackQueueLab.Services;

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
    private sealed record StackEntry(string Text, int Index);

    public static InfixConversionResult ConvertWithSteps(string expression)
    {
        var tokens = ExpressionTokenizer.TokenizeInfix(expression);
        var texts = tokens
            .Select(token => token.Kind == InfixTokenKind.Number
                ? NumberText.Format(token.Number)
                : token.Text)
            .ToArray();

        // The console converter validates the grammar and produces the final postfix form.
        var postfix = new InfixToPostfixConverter().Convert(expression);

        var stack = new Stack<StackEntry>();
        var output = new List<StackEntry>();
        var steps = new List<InfixConversionStep>();

        void Record(int index, string message) => steps.Add(new InfixConversionStep(
            index,
            stack.Select(entry => entry.Text).ToArray(),
            output.Select(entry => entry.Text).ToArray(),
            message,
            stack.Select(entry => entry.Index).ToArray(),
            output.Select(entry => entry.Index).ToArray()));

        void PopToOutput(int index)
        {
            var moved = stack.Pop();
            output.Add(moved);
            Record(index, $"Извлекаем «{moved.Text}» с вершины стека в выход.");
        }

        InfixToken? previous = null;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var text = texts[i];
            Record(i, $"Читаем «{text}».");

            switch (token.Kind)
            {
                case InfixTokenKind.Number:
                    output.Add(new StackEntry(text, i));
                    Record(i, $"Число «{text}» переносим в выход.");
                    break;

                case InfixTokenKind.Function:
                    if (i + 1 >= tokens.Count || tokens[i + 1].Kind != InfixTokenKind.LeftParenthesis)
                    {
                        throw new ExpressionSyntaxException(
                            $"После функции «{token.Text}» нужна скобка, например {token.Text}(0.5).");
                    }

                    stack.Push(new StackEntry(token.Text, i));
                    Record(i, $"Функцию «{token.Text}» помещаем в стек: у неё самый высокий приоритет.");
                    break;

                case InfixTokenKind.LeftParenthesis:
                    stack.Push(new StackEntry(token.Text, i));
                    Record(i, "Открывающую скобку помещаем в стек.");
                    break;

                case InfixTokenKind.RightParenthesis:
                    while (stack.Count > 0 && stack.Peek().Text != "(")
                    {
                        PopToOutput(i);
                    }

                    if (stack.Count == 0)
                    {
                        throw new ExpressionSyntaxException("Не найдена открывающая скобка.");
                    }

                    stack.Pop();
                    Record(i, "Парные скобки удаляем; в постфиксную запись они не входят.");

                    if (stack.Count > 0 && MathExpressionRules.IsFunction(stack.Peek().Text))
                    {
                        var function = stack.Pop();
                        output.Add(function);
                        Record(i, $"Функцию «{function.Text}» переносим в выход после её аргумента.");
                    }

                    break;

                case InfixTokenKind.BinaryOperator:
                    var isUnary = previous is null ||
                                  previous.Kind is InfixTokenKind.BinaryOperator or InfixTokenKind.LeftParenthesis;

                    if (isUnary)
                    {
                        if (token.Text == "-")
                        {
                            stack.Push(new StackEntry("u-", i));
                            Record(i, "Унарный минус помещаем в стек.");
                        }
                        else if (token.Text == "+")
                        {
                            Record(i, "Унарный плюс не меняет значение выражения.");
                        }
                        else
                        {
                            throw new ExpressionSyntaxException($"Оператор «{token.Text}» не может быть унарным.");
                        }
                    }
                    else
                    {
                        while (stack.Count > 0 && ShouldPop(stack.Peek().Text, token.Text))
                        {
                            PopToOutput(i);
                        }

                        stack.Push(new StackEntry(token.Text, i));
                        Record(i,
                            $"Оператор «{token.Text}» (приоритет {MathExpressionRules.Precedence(token.Text)}) помещаем на вершину.");
                    }

                    break;
            }

            previous = token;
        }

        while (stack.Count > 0)
        {
            if (stack.Peek().Text == "(")
            {
                throw new ExpressionSyntaxException("Не закрыта скобка.");
            }

            PopToOutput(tokens.Count);
        }

        Record(tokens.Count, "Готово: все операторы перенесены в выходную запись.");

        return new InfixConversionResult(texts, steps, postfix);
    }

    public static double EvaluatePostfix(string postfix) => new PostfixEvaluator().Evaluate(postfix);

    public static string GenerateRandomExpression(Random? random = null, int maxDepth = 2)
    {
        random ??= Random.Shared;

        string Build(int depth)
        {
            if (depth <= 0)
            {
                return random.Next(1, 10).ToString(CultureInfo.InvariantCulture);
            }

            if (random.Next(6) == 0)
            {
                var function = new[] { "ln", "cos", "sin", "sqrt" }[random.Next(4)];
                var argument = random.Next(1, 10).ToString(CultureInfo.InvariantCulture);
                return $"{function}({argument})";
            }

            var left = Build(depth - 1);
            var operation = new[] { "+", "-", "*", ":", "^" }[random.Next(5)];
            var right = operation switch
            {
                ":" => random.Next(1, 10).ToString(CultureInfo.InvariantCulture),
                "^" => random.Next(1, 4).ToString(CultureInfo.InvariantCulture),
                _ => Build(depth - 1)
            };

            return $"({left} {operation} {right})";
        }

        return Build(random.Next(1, maxDepth + 1));
    }

    private static bool ShouldPop(string top, string incoming)
    {
        if (!MathExpressionRules.IsBinaryOperator(top) && !MathExpressionRules.IsUnaryMinus(top))
        {
            return false;
        }

        var topPrecedence = MathExpressionRules.Precedence(top);
        var incomingPrecedence = MathExpressionRules.Precedence(incoming);

        if (topPrecedence > incomingPrecedence)
        {
            return true;
        }

        if (topPrecedence < incomingPrecedence)
        {
            return false;
        }

        return !MathExpressionRules.IsRightAssociative(incoming);
    }
}
