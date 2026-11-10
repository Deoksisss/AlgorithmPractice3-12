using StackQueueLab.Models;

namespace StackQueueLab.Services;

/// <summary>
/// Оценка сложности: для постфиксной записи O(n) по времени и O(n) по памяти,
/// где n — число токенов (операндов и операций).
/// </summary>
public sealed class ComplexityAnalyzer
{
    private readonly PostfixValidator _validator = new();

    public ComplexityInfo AnalyzePostfix(string expression)
    {
        var validation = _validator.Validate(expression);

        if (validation.IsValid)
        {
            return new ComplexityInfo(
                validation.OperationCount,
                validation.OperandCount,
                validation.OperatorCount);
        }

        var fallback = ExpressionTokenizer.TokenizePostfix(expression).Count;
        return new ComplexityInfo(fallback, fallback, 0);
    }

    public ComplexityInfo AnalyzeInfix(string expression)
    {
        try
        {
            var tokens = ExpressionTokenizer.TokenizeInfix(expression);
            var count = tokens.Count(token =>
                token.Kind is not InfixTokenKind.LeftParenthesis and not InfixTokenKind.RightParenthesis);

            return new ComplexityInfo(count, count, 0);
        }
        catch (ExpressionSyntaxException)
        {
            var fallback = string.IsNullOrWhiteSpace(expression)
                ? 0
                : expression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

            return new ComplexityInfo(fallback, fallback, 0);
        }
    }
}
