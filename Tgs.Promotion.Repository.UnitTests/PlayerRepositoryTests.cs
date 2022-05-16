using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository.UnitTests;

[TestClass]
public class PlayerRepositoryTests
{
	[TestMethod]
	public async Task Set_Player_FirstDepositOn_If_Source_Null_Should_Success()
	{
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		using var dbContext = new TgsContext(dbContextOptions);

		var sut = new PlayerRepository(dbContext);

		var playerCode = "ThisIsPlayerCode";
		var depositOn = DateTime.UtcNow;

		await dbContext.Players.AddAsync(new Player
		{
			PlayerCode = playerCode,
			DefaultWalletCurrencyCode = "CNY",
			Password = Array.Empty<byte>(),
			Salt = Array.Empty<byte>(),
			WithdrawPassword = Array.Empty<byte>(),
			WithdrawSalt = Array.Empty<byte>(),
			Username = "Tester"
		});

		await dbContext.SaveChangesAsync();

		await sut.SetFirstDepositTimeAsync(playerCode, depositOn);

		var actual = await dbContext.Players.FirstAsync();

		Assert.AreEqual(depositOn, actual.FirstDepositOn);
	}

	[TestMethod]
	public async Task Set_Player_FirstDepositOn_If_Source_Set_Should_Ignore()
	{
		var dbContextOptions = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		using var dbContext = new TgsContext(dbContextOptions);

		var sut = new PlayerRepository(dbContext);

		var playerCode = "ThisIsPlayerCode";
		var depositOn = DateTimeOffset.UtcNow.UtcDateTime;

		await dbContext.Players.AddAsync(new Player
		{
			PlayerCode = playerCode,
			FirstDepositOn = DateTime.Parse("2021/1/1"),
			DefaultWalletCurrencyCode = "CNY",
			Password = Array.Empty<byte>(),
			Salt = Array.Empty<byte>(),
			WithdrawPassword = Array.Empty<byte>(),
			WithdrawSalt = Array.Empty<byte>(),
			Username = "Tester"
		});

		await dbContext.SaveChangesAsync();

		await sut.SetFirstDepositTimeAsync(playerCode, depositOn);

		var actual = await dbContext.Players.FirstAsync();

		Assert.IsNotNull(actual.FirstDepositOn);
		Assert.AreNotEqual(depositOn, actual.FirstDepositOn);
	}
}
