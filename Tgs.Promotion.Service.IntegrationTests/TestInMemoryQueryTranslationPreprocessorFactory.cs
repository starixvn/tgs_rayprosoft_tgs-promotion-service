using Microsoft.EntityFrameworkCore.Query;

namespace Tgs.Promotion.Service.IntegrationTests;

internal class TestInMemoryQueryTranslationPreprocessorFactory : IQueryTranslationPreprocessorFactory
{
	public QueryTranslationPreprocessorDependencies Dependencies { get; }

	public TestInMemoryQueryTranslationPreprocessorFactory(QueryTranslationPreprocessorDependencies dependencies)
	{
		Dependencies = dependencies;
	}

	public QueryTranslationPreprocessor Create(QueryCompilationContext queryCompilationContext)
		=> new OverrideDbFunctionsInMemoryQueryTranslationPreprocessor(Dependencies, queryCompilationContext);
}
