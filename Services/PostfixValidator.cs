namespace StackQueueLab.Services;

public sealed record PostfixValidationResult(
    bool IsValid,
    string? Error,
    int OperationCount,
    int OperandCount,
    int OperatorCount);

/// <summary>
/// Проверяет структуру постфиксной записи до запуска: словарь токенов,
/// достаточность операндов и ровно один результат в конце.
/// </summary>
public sealed class PostfixValidator
{
    public PostfixValidationResult Validate(string expression)
    {
        var tokens = ExpressionTokenizer.TokenizePostfix(expression);

        if (tokens.Count == 0)
        {
            return new PostfixValidationResult(false, "Выражение пусто.", 0, 0, 0);
        }

        var depth = 0;
        var operands = 0;
        var operators = 0;

        foreach (var token in tokens)
        {
            if (token is "(" or ")")
            {
                // Скобки в постфиксной записи не нужны, но допускаются как
                // нейтральные разделители: это делает чтение файлов терпимее.
                continue;
            }

            if (NumberText.TryParse(token, out _))
            {
                depth++;
                operands++;
                continue;
            }

            if (MathExpressionRules.IsBinaryOperator(token))
            {
                if (depth < 2)
                {
                    return new PostfixValidationResult(
                        false,
                        $"Оператору «{token}» не хватает двух операндов.",
                        operands + operators + 1,
                        operands,
                        operators + 1);
                }

                depth--;
                operators++;
                continue;
            }

            if (MathExpressionRules.IsUnaryFunction(token) || MathExpressionRules.IsUnaryMinus(token))
            {
                if (depth < 1)
                {
                    return new PostfixValidationResult(
                        false,
                        $"Функции «{token}» не хватает операнда.",
                        operands + operators + 1,
                        operands,
                        operators + 1);
                }

                operators++;
                continue;
            }

            return new PostfixValidationResult(
                false,
                $"Лишний или неизвестный токен «{token}».",
                operands + operators + 1,
                operands,
                operators);
        }

        if (depth == 0)
        {
            return new PostfixValidationResult(false, "В выражении нет результата.", operands + operators, operands, operators);
        }

        if (depth > 1)
        {
            return new PostfixValidationResult(
                false,
                "В выражении остались лишние операнды (в конце должно быть ровно одно значение).",
                operands + operators,
                operands,
                operators);
        }

        return new PostfixValidationResult(true, null, operands + operators, operands, operators);
    }
}
