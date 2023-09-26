using Microsoft.EntityFrameworkCore.InMemory.Query.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace Tgs.Promotion.Service.IntegrationTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<暫止>")]
internal class TestInMemoryQueryTranslationPreprocessorFactory : InMemoryQueryTranslationPreprocessorFactory
{
	public TestInMemoryQueryTranslationPreprocessorFactory(QueryTranslationPreprocessorDependencies dependencies) : base(dependencies)
	{
	}

	public override QueryTranslationPreprocessor Create(QueryCompilationContext queryCompilationContext)
		=> new OverrideDbFunctionsInMemoryQueryTranslationPreprocessor(Dependencies, queryCompilationContext);
}
