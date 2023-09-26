using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Service.IntegrationTests;

internal class TgsContextForTest : TgsContext
{
	public TgsContextForTest(DbContextOptions<TgsContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
	}
}
