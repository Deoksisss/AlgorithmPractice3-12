namespace StackQueueLab.Services;

public sealed class ExpressionSyntaxException : Exception
{
    public ExpressionSyntaxException(string message)
        : base(message)
    {
    }
}

public sealed class ExpressionEvaluationException : Exception
{
    public ExpressionEvaluationException(string message)
        : base(message)
    {
    }
}
