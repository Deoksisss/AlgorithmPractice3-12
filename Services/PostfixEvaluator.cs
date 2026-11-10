using StackQueueLab.Core.Interfaces;
using StackQueueLab.Core.Structures;

namespace StackQueueLab.Services;

/// <summary>
/// Вычисление постфиксной записи на собственном стеке.
/// </summary>
public sealed class PostfixEvaluator : IPostfixEvaluator
{
    public double Evaluate(string postfixExpression) =>
        EvaluateCore(postfixExpression, null);

    /// <summary>
    /// Повторное вычисление с пошаговым выводом. Используется для стресс-файлов:
    /// вывод каждого токена делает полное время наглядным и ограничивает
    /// огромную строку в консоли.
    /// </summary>
    public double Trace(string postfixExpression, Action<int, string, int> step) =>
        EvaluateCore(postfixExpression, step);

    private static double EvaluateCore(
        string postfixExpression,
        Action<int, string, int>? step)
    {
        var tokens = ExpressionTokenizer.TokenizePostfix(postfixExpression);

        if (tokens.Count == 0)
        {
            throw new ExpressionEvaluationException("Выражение пусто.");
        }

        var stack = new LinkedStack<double>();
        var stepIndex = 0;

        foreach (var token in tokens)
        {
            if (token is "(" or ")")
            {
                continue;
            }

            if (NumberText.TryParse(token, out var number))
            {
                stack.Push(number);
                step?.Invoke(stepIndex++, token, stack.Count);
                continue;
            }

            if (MathExpressionRules.IsBinaryOperator(token))
            {
                if (stack.Count < 2)
                {
                    throw new ExpressionEvaluationException(
                        $"Оператору «{token}» не хватает двух операндов.");
                }

                var right = stack.Pop();
                var left = stack.Pop();
                stack.Push(ApplyBinary(token, left, right));
                step?.Invoke(stepIndex++, token, stack.Count);
                continue;
            }

            if (MathExpressionRules.IsUnaryFunction(token) || MathExpressionRules.IsUnaryMinus(token))
            {
                if (stack.Count < 1)
                {
                    throw new ExpressionEvaluationException(
                        $"Функции «{token}» не хватает операнда.");
                }

                var operand = stack.Pop();
                stack.Push(ApplyUnary(token.ToLowerInvariant(), operand));
                step?.Invoke(stepIndex++, token, stack.Count);
                continue;
            }

            throw new ExpressionEvaluationException($"Лишний или неизвестный токен «{token}».");
        }

        if (stack.Count != 1)
        {
            throw new ExpressionEvaluationException(
                stack.Count == 0
                    ? "В выражении нет результата."
                    : "В выражении остались лишние операнды.");
        }

        return stack.Pop();
    }

    private static double ApplyBinary(string operation, double left, double right)
    {
        double result;

        switch (operation)
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
            case ":":
                if (right == 0.0)
                {
                    throw new ExpressionEvaluationException("Деление на ноль.");
                }

                result = left / right;
                break;
            case "^":
                result = Math.Pow(left, right);
                break;
            default:
                throw new ExpressionEvaluationException($"Неизвестная операция «{operation}».");
        }

        EnsureFinite(result, operation);
        return result;
    }

    private static double ApplyUnary(string function, double operand)
    {
        double result;

        switch (function)
        {
            case "u-":
            case "neg":
                result = -operand;
                break;
            case "ln":
                if (operand <= 0.0)
                {
                    throw new ExpressionEvaluationException("ln определён только для положительных чисел.");
                }

                result = Math.Log(operand);
                break;
            case "cos":
                result = Math.Cos(operand);
                break;
            case "sin":
                result = Math.Sin(operand);
                break;
            case "sqrt":
                if (operand < 0.0)
                {
                    throw new ExpressionEvaluationException("sqrt определён только для неотрицательных чисел.");
                }

                result = Math.Sqrt(operand);
                break;
            default:
                throw new ExpressionEvaluationException($"Неизвестная функция «{function}».");
        }

        EnsureFinite(result, function);
        return result;
    }

    private static void EnsureFinite(double result, string operation)
    {
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new ExpressionEvaluationException(
                $"Операция «{operation}» дала недопустимый результат (NaN/∞).");
        }
    }
}
