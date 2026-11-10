namespace StackQueueLab.Core.Interfaces;

/// <summary>
/// Вычисление выражения, записанного в постфиксной форме.
/// </summary>
public interface IPostfixEvaluator
{
    double Evaluate(string postfixExpression);
}
