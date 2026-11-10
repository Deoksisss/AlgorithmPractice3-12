using StackQueueLab.Core.Interfaces;
using StackQueueLab.Core.Structures;

namespace StackQueueLab.Services;

/// <summary>
/// Перевод инфиксной записи в постфиксную алгоритмом shunting-yard.
/// Приоритеты операторов хранятся и учитываются через стек.
/// </summary>
public sealed class InfixToPostfixConverter : IExpressionConverter
{
    public string Convert(string infixExpression)
    {
        var tokens = ExpressionTokenizer.TokenizeInfix(infixExpression);
        var output = new List<string>();
        var stack = new LinkedStack<string>();
        InfixToken? previous = null;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            switch (token.Kind)
            {
                case InfixTokenKind.Number:
                    EnsureNoImplicitMultiplication(previous, token);
                    output.Add(NumberText.Format(token.Number));
                    break;

                case InfixTokenKind.Function:
                    EnsureNoImplicitMultiplication(previous, token);

                    if (i + 1 >= tokens.Count || tokens[i + 1].Kind != InfixTokenKind.LeftParenthesis)
                    {
                        throw new ExpressionSyntaxException(
                            $"После функции «{token.Text}» должны стоять скобки, например {token.Text}(0.5).");
                    }

                    stack.Push(token.Text);
                    break;

                case InfixTokenKind.LeftParenthesis:
                    EnsureNoImplicitMultiplication(previous, token);
                    stack.Push("(");
                    break;

                case InfixTokenKind.RightParenthesis:
                    if (previous is null ||
                        previous.Kind is not (InfixTokenKind.Number or InfixTokenKind.RightParenthesis))
                    {
                        throw new ExpressionSyntaxException("Пустые или некорректные скобки.");
                    }

                    var opened = false;

                    while (!stack.IsEmpty)
                    {
                        var top = stack.Peek();

                        if (top == "(")
                        {
                            stack.Pop();
                            opened = true;
                            break;
                        }

                        output.Add(stack.Pop());
                    }

                    if (!opened)
                    {
                        throw new ExpressionSyntaxException("Не найдена открывающая скобка.");
                    }

                    if (!stack.IsEmpty && MathExpressionRules.IsFunction(stack.Peek()))
                    {
                        output.Add(stack.Pop());
                    }

                    break;

                case InfixTokenKind.BinaryOperator:
                    var isUnary = previous is null ||
                                  previous.Kind is InfixTokenKind.BinaryOperator or InfixTokenKind.LeftParenthesis;

                    if (isUnary)
                    {
                        if (token.Text == "-")
                        {
                            stack.Push("u-");
                        }
                        else if (token.Text != "+")
                        {
                            throw new ExpressionSyntaxException(
                                $"Оператор «{token.Text}» не может быть унарным.");
                        }
                    }
                    else
                    {
                        if (previous is null ||
                            previous.Kind is not (InfixTokenKind.Number or InfixTokenKind.RightParenthesis))
                        {
                            throw new ExpressionSyntaxException(
                                $"Перед оператором «{token.Text}» не хватает операнда.");
                        }

                        while (!stack.IsEmpty && ShouldPop(stack.Peek(), token.Text))
                        {
                            output.Add(stack.Pop());
                        }

                        stack.Push(token.Text);
                    }

                    break;
            }

            previous = token;
        }

        if (previous is null ||
            previous.Kind is not (InfixTokenKind.Number or InfixTokenKind.RightParenthesis))
        {
            throw new ExpressionSyntaxException("Выражение заканчивается оператором или неполной конструкцией.");
        }

        while (!stack.IsEmpty)
        {
            var top = stack.Pop();

            if (top == "(")
            {
                throw new ExpressionSyntaxException("Не закрыта скобка.");
            }

            output.Add(top);
        }

        if (output.Count == 0)
        {
            throw new ExpressionSyntaxException("Не удалось построить постфиксную запись.");
        }

        return string.Join(" ", output);
    }

    private static void EnsureNoImplicitMultiplication(InfixToken? previous, InfixToken current)
    {
        if (previous is null)
        {
            return;
        }

        if (previous.Kind is InfixTokenKind.Number or InfixTokenKind.RightParenthesis)
        {
            throw new ExpressionSyntaxException(
                $"Пропущен оператор перед «{current.Text}». Неявное умножение не поддерживается.");
        }
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
