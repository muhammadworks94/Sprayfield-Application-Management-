namespace SAM.Infrastructure.Helpers;

/// <summary>
/// Helper class for building dynamic LINQ predicates.
/// </summary>
public static class PredicateBuilder
{
    public static System.Linq.Expressions.Expression<Func<T, bool>> True<T>() => _ => true;
    public static System.Linq.Expressions.Expression<Func<T, bool>> False<T>() => _ => false;

    public static System.Linq.Expressions.Expression<Func<T, bool>> Or<T>(
        this System.Linq.Expressions.Expression<Func<T, bool>> expr1,
        System.Linq.Expressions.Expression<Func<T, bool>> expr2)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T));
        var body = System.Linq.Expressions.Expression.OrElse(
            ReplaceParameter(expr1.Body, expr1.Parameters[0], parameter),
            ReplaceParameter(expr2.Body, expr2.Parameters[0], parameter));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static System.Linq.Expressions.Expression<Func<T, bool>> And<T>(
        this System.Linq.Expressions.Expression<Func<T, bool>> expr1,
        System.Linq.Expressions.Expression<Func<T, bool>> expr2)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T));
        var body = System.Linq.Expressions.Expression.AndAlso(
            ReplaceParameter(expr1.Body, expr1.Parameters[0], parameter),
            ReplaceParameter(expr2.Body, expr2.Parameters[0], parameter));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static System.Linq.Expressions.Expression ReplaceParameter(
        System.Linq.Expressions.Expression expression,
        System.Linq.Expressions.ParameterExpression oldParameter,
        System.Linq.Expressions.ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    private class ParameterReplacer : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly System.Linq.Expressions.ParameterExpression _oldParameter;
        private readonly System.Linq.Expressions.ParameterExpression _newParameter;

        public ParameterReplacer(
            System.Linq.Expressions.ParameterExpression oldParameter,
            System.Linq.Expressions.ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}

