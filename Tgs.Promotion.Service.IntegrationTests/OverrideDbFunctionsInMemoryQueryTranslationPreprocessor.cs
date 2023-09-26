using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.InMemory.Query.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace Tgs.Promotion.Service.IntegrationTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<暫止>")]
internal class OverrideDbFunctionsInMemoryQueryTranslationPreprocessor : InMemoryQueryTranslationPreprocessor
{
	public OverrideDbFunctionsInMemoryQueryTranslationPreprocessor(
		QueryTranslationPreprocessorDependencies dependencies,
		QueryCompilationContext queryCompilationContext)
		: base(dependencies, queryCompilationContext)
	{
	}

	public override Expression Process(Expression query)
	{
		var visitor = new ReplaceDbFunctionsExpressionVisitor();

		return base.Process(visitor.Visit(query));
	}
}
