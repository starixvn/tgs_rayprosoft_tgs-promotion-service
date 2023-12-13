using Microsoft.EntityFrameworkCore.Query;

namespace Tgs.Promotion.Service.IntegrationTests;

internal class TestInMemoryQueryTranslationPreprocessorFactory(QueryTranslationPreprocessorDependencies dependencies) : IQueryTranslationPreprocessorFactory
{
	public QueryTranslationPreprocessorDependencies Dependencies { get; } = dependencies;

	public QueryTranslationPreprocessor Create(QueryCompilationContext queryCompilationContext)
		=> new OverrideDbFunctionsInMemoryQueryTranslationPreprocessor(Dependencies, queryCompilationContext);
}
