namespace StackQueueLab.Core.Interfaces;

/// <summary>
/// Перевод инфиксной записи в постфиксную.
/// </summary>
public interface IExpressionConverter
{
    string Convert(string infixExpression);
}
