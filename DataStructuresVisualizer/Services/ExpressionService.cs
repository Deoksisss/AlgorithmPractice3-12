using System.Globalization;
using DataStructuresVisualizer.Models;

namespace DataStructuresVisualizer.Services;

public sealed class ExpressionConversionException : Exception
{
    public ExpressionConversionException(string message)
        : base(message)
    {
    }
}

public static class ExpressionService
{
    private enum TokenType
    {
        Number,
        Operator,
        LeftParenthesis,
        RightParenthesis
    }

    private sealed record Token(string Text, TokenType Type);

    public static string ToPostfix(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ExpressionConversionException("Выражение пустое.");
        }

        var tokens = Tokenize(expression);
        var output = new List<string>();
        var stack = new Stack<Token>();

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    output.Add(token.Text);
                    break;

                case TokenType.Operator:
                    while (stack.Count > 0 &&
                           stack.Peek().Type == TokenType.Operator &&
                           Precedence(stack.Peek().Text) >= Precedence(token.Text))
                    {
                        output.Add(stack.Pop().Text);
                    }

                    stack.Push(token);
                    break;

                case TokenType.LeftParenthesis:
                    stack.Push(token);
                    break;

                case TokenType.RightParenthesis:
                    var opened = false;

                    while (stack.Count > 0)
                    {
                        var top = stack.Pop();

                        if (top.Type == TokenType.LeftParenthesis)
                        {
                            opened = true;
                            break;
                        }

                        output.Add(top.Text);
                    }

                    if (!opened)
                    {
                        throw new ExpressionConversionException("Не найдена открывающая скобка.");
                    }

                    break;
            }
        }

        while (stack.Count > 0)
        {
            var top = stack.Pop();

            if (top.Type == TokenType.LeftParenthesis)
            {
                throw new ExpressionConversionException("Не закрыта скобка.");
            }

            output.Add(top.Text);
        }

        return string.Join(" ", output);
    }

    public static ExpressionNode BuildTree(string postfixExpression)
    {
        var tokens = postfixExpression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<ExpressionNode>();

        if (tokens.Length == 0)
        {
            throw new ExpressionConversionException("Постфиксная запись пуста.");
        }

        foreach (var token in tokens)
        {
            if (IsOperator(token))
            {
                if (stack.Count < 2)
                {
                    throw new ExpressionConversionException($"Оператору «{token}» не хватает двух операндов.");
                }

                var right = stack.Pop();
                var left = stack.Pop();
                var node = new ExpressionNode
                {
                    Value = token,
                    IsOperator = true,
                    Left = left,
                    Right = right
                };

                left.Parent = node;
                right.Parent = node;
                stack.Push(node);
            }
            else
            {
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    throw new ExpressionConversionException($"Неизвестный токен «{token}».");
                }

                stack.Push(new ExpressionNode
                {
                    Value = token,
                    IsOperator = false,
                    Result = value
                });
            }
        }

        if (stack.Count != 1)
        {
            throw new ExpressionConversionException("В конце должен остаться один корень дерева.");
        }

        return stack.Pop();
    }

    public static double Evaluate(ExpressionNode root, List<EvaluationStep> steps)
    {
        return EvaluateRecursive(root, steps);
    }

    public static double Layout(ExpressionNode root)
    {
        var leafIndex = 0;
        AssignLayout(root, 0, ref leafIndex);

        var minX = GetMinimumX(root);
        var maxX = GetMaximumX(root);
        return maxX - minX;
    }

    public static string GenerateRandomExpression(Random? random = null, int maxDepth = 3)
    {
        random ??= Random.Shared;

        string Build(int depth)
        {
            if (depth == 0)
            {
                return random.Next(1, 10).ToString(CultureInfo.InvariantCulture);
            }

            var left = Build(depth - 1);
            var right = Build(depth - 1);
            var operation = new[] { "+", "-", "*", "/" }[random.Next(4)];

            if (operation == "/")
            {
                right = random.Next(1, 10).ToString(CultureInfo.InvariantCulture);
            }

            return $"({left} {operation} {right})";
        }

        return Build(random.Next(2, maxDepth + 1));
    }

    private static double EvaluateRecursive(ExpressionNode node, List<EvaluationStep> steps)
    {
        if (!node.IsOperator)
        {
            node.Result = double.Parse(node.Value, CultureInfo.InvariantCulture);
            steps.Add(new EvaluationStep(node, node.Result, $"Лист: {node.Value} = {Format(node.Result)}"));
            return node.Result;
        }

        var left = EvaluateRecursive(node.Left!, steps);
        var right = EvaluateRecursive(node.Right!, steps);

        double result;

        switch (node.Value)
        {
            case "+":
                result = left + right;
                break;
            case "-":
                result = left - right;
                break;
            case "*":
                result = left * right;
                break;
            case "/":
                if (right == 0)
                {
                    throw new ExpressionConversionException("Деление на ноль.");
                }

                result = left / right;
                break;
            default:
                throw new ExpressionConversionException($"Неизвестный оператор «{node.Value}».");
        }

        node.Result = result;
        steps.Add(new EvaluationStep(
            node,
            result,
            $"{node.Left!.Value} {node.Value} {node.Right!.Value} = {Format(result)}"));

        return result;
    }

    private static void AssignLayout(ExpressionNode node, int depth, ref int leafIndex)
    {
        node.Y = depth * 118;

        if (!node.IsOperator)
        {
            node.X = leafIndex * 110;
            leafIndex++;
            return;
        }

        AssignLayout(node.Left!, depth + 1, ref leafIndex);
        AssignLayout(node.Right!, depth + 1, ref leafIndex);

        node.X = (node.Left!.X + node.Right!.X) / 2.0;
    }

    private static double GetMinimumX(ExpressionNode node)
    {
        if (!node.IsOperator)
        {
            return node.X;
        }

        return Math.Min(node.X, Math.Min(GetMinimumX(node.Left!), GetMinimumX(node.Right!)));
    }

    private static double GetMaximumX(ExpressionNode node)
    {
        if (!node.IsOperator)
        {
            return node.X;
        }

        return Math.Max(node.X, Math.Max(GetMaximumX(node.Left!), GetMaximumX(node.Right!)));
    }

    private static List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < expression.Length)
        {
            var c = expression[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                var start = i;

                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                {
                    i++;
                }

                tokens.Add(new Token(expression[start..i], TokenType.Number));
                continue;
            }

            if (c is '+' or '-' or '*' or '/')
            {
                tokens.Add(new Token(c.ToString(), TokenType.Operator));
                i++;
                continue;
            }

            if (c == '(')
            {
                tokens.Add(new Token(c.ToString(), TokenType.LeftParenthesis));
                i++;
                continue;
            }

            if (c == ')')
            {
                tokens.Add(new Token(c.ToString(), TokenType.RightParenthesis));
                i++;
                continue;
            }

            throw new ExpressionConversionException($"Недопустимый символ «{c}».");
        }

        return tokens;
    }

    private static int Precedence(string operation) => operation switch
    {
        "+" or "-" => 1,
        "*" or "/" => 2,
        _ => 0
    };

    private static bool IsOperator(string token) => token is "+" or "-" or "*" or "/";

    private static string Format(double value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);
}
