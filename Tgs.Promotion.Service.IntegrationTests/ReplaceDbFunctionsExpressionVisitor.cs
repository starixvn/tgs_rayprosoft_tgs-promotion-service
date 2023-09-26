using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Tgs.Promotion.Service.IntegrationTests;

internal class ReplaceDbFunctionsExpressionVisitor : ExpressionVisitor
{
	protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
	{
		var method = methodCallExpression.Method;

		if (method.DeclaringType == typeof(NpgsqlDbFunctionsExtensions))
		{
			var mi = typeof(DbFunctionsForTest).GetMethod(
				nameof(DbFunctionsForTest.ILike),
				BindingFlags.Public | BindingFlags.Static,
				new[] { typeof(string), typeof(string) })!;

			var column = Visit(methodCallExpression.Arguments[1])!;
			var like = Visit(methodCallExpression.Arguments[2])!;

			var visitedMethodCall = Expression.Call(
				mi,
				column,
				like);

			return visitedMethodCall;
		}
		else
			return base.VisitMethodCall(methodCallExpression);
	}
}
