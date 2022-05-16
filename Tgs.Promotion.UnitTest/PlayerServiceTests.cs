using System;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using Tgs.Promotion.Core;
using Tgs.Promotion.Domain;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.UnitTest
{
	[TestFixture]
	public class PlayerServiceTests
	{
		[Test]
		public async Task Set_Player_FirstDepositOn_If_Source_Null_Should_Success()
		{
			var fakePlayerService = new Mock<ITgsService<Player>>();

			var sut = new PlayerRepository(fakePlayerService.Object);

			var playerCode = "ThisIsPlayerCode";
			var depositOn = DateTime.UtcNow;

			var fakePlayers = new Player[]
			{
				new Player
				{
					PlayerCode = playerCode,
					DefaultWalletCurrencyCode = "CNY",
					Password = Array.Empty<byte>(),
					Salt = Array.Empty<byte>(),
					WithdrawPassword = Array.Empty<byte>(),
					WithdrawSalt = Array.Empty<byte>(),
					Username = "Tester"
				}
			}.AsQueryable().BuildMock();

			fakePlayerService
				.Setup(x => x.GetQueries(It.IsAny<Func<IQueryable<Player>, IQueryable<Player>>?>(), default))
				.Returns(fakePlayers);

			await sut.SetFirstDepositTimeAsync(playerCode, depositOn);

			var actual = fakePlayerService.Object.GetQueries().First(x => x.PlayerCode == playerCode);

			Assert.AreEqual(depositOn, actual.FirstDepositOn);
		}

		[Test]
		public async Task Set_Player_FirstDepositOn_If_Source_Set_Should_Ignore()
		{
			var fakePlayerService = new Mock<ITgsService<Player>>();

			var sut = new PlayerRepository(fakePlayerService.Object);

			var playerCode = "ThisIsPlayerCode";
			var depositOn = DateTimeOffset.UtcNow.UtcDateTime;

			var fakePlayers = new Player[]
			{
				new Player
				{
					PlayerCode = playerCode,
					FirstDepositOn = DateTime.Parse("2021/1/1"),
					DefaultWalletCurrencyCode = "CNY",
					Password = Array.Empty<byte>(),
					Salt = Array.Empty<byte>(),
					WithdrawPassword = Array.Empty<byte>(),
					WithdrawSalt = Array.Empty<byte>(),
					Username = "Tester"
				}
			}.AsQueryable().BuildMock();

			fakePlayerService
				.Setup(x => x.GetQueries(It.IsAny<Func<IQueryable<Player>, IQueryable<Player>>?>(), default))
				.Returns(fakePlayers);

			await sut.SetFirstDepositTimeAsync(playerCode, depositOn);

			var actual = fakePlayerService.Object.GetQueries().First(x => x.PlayerCode == playerCode);

			Assert.NotNull(actual.FirstDepositOn);
			Assert.AreNotEqual(depositOn, actual.FirstDepositOn);
		}
	}
}
