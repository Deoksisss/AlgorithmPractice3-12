namespace DataStructuresVisualizer.Models;

public sealed class ExpressionNode
{
    public required string Value { get; init; }

    public bool IsOperator { get; init; }

    public ExpressionNode? Left { get; set; }

    public ExpressionNode? Right { get; set; }

    public double Result { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    /// <summary>
    /// Канонический ключ узла в постфиксной форме. Используется для LRU-кэша.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public ExpressionNode? Parent { get; set; }
}

public sealed record EvaluationStep(
    ExpressionNode Node,
    double Value,
    string Description,
    bool FromCache);
