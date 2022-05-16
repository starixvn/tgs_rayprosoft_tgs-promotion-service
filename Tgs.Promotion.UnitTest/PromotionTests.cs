using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFixture;
using ExpectedObjects;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.Promotion.Core;
using Tgs.Promotion.Core.Handlers;
using Tgs.Promotion.Core.Services;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.Promotion;
using Tgs.Promotion.Domain.shared;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.UnitTest
{
	[TestFixture]
	public class PromotionTests
	{
		[TestCase(true)]
		[TestCase(false)]
		public async Task Can_Get_Promotions(bool showAll)
		{
			var promotionServiceMock = new Mock<IPromotionRepository>();

			var promotionsQueryHandlerMock =
				new GetPromotionsQueryHandler(promotionServiceMock.Object);

			var request = new GetPromotionsRequest
			{
				BrandId = Guid.NewGuid(),
				Page = 1,
				Rows = 10,
				SortBy = "updatedOn",
				OrderBy = SortOrder.Descending,
				ShowAll = showAll
			};

			var list = CreatePromotionFixture()
				.CreateMany<Domain.Entities.Promotion>(20)
				.AsQueryable()
				.BuildMock();

			foreach (var promotion in list)
			{
				promotion.BrandId = request.BrandId;
			}

			promotionServiceMock.Setup(x =>
					x.GetPromotionQueries(
						It.IsAny<Func<IQueryable<Domain.Entities.Promotion>,
							IQueryable<Domain.Entities.Promotion>>>(),
						false))
				.Returns(list);

			var result = await promotionsQueryHandlerMock.GetAsync(request, new());

			promotionServiceMock.Verify(x => x.GetPromotionQueries(
				It.IsAny<Func<IQueryable<Domain.Entities.Promotion>,
					IQueryable<Domain.Entities.Promotion>>>(),
				false));

			Assert.AreEqual(result.Items.Length, showAll ? 20 : 10);
		}

		[Test]
		public async Task Can_Get_Promotions_Without_Archived()
		{
			var promotionServiceMock = new Mock<IPromotionRepository>();

			var promotionsQueryHandlerMock =
				new GetPromotionsQueryHandler(promotionServiceMock.Object);

			var request = new GetPromotionsRequest
			{
				BrandId = Guid.NewGuid(),
				Page = 1,
				Rows = 10,
				SortBy = "updatedOn",
				OrderBy = SortOrder.Descending,
				ShowAll = false
			};

			var list = CreatePromotionFixture()
				.CreateMany<Domain.Entities.Promotion>(10)
				.AsQueryable()
				.BuildMock();

			var index = 0;

			foreach (var promotion in list)
			{
				if (index == 0)
				{
					promotion.Status = (short)PromotionStatus.Archived;
				}

				promotion.BrandId = request.BrandId;

				index++;
			}

			promotionServiceMock.Setup(x =>
					x.GetPromotionQueries(
						It.IsAny<Func<IQueryable<Domain.Entities.Promotion>,
							IQueryable<Domain.Entities.Promotion>>>(),
						false))
				.Returns(list);

			var result = await promotionsQueryHandlerMock.GetAsync(request, new());

			promotionServiceMock.Verify(x => x.GetPromotionQueries(
				It.IsAny<Func<IQueryable<Domain.Entities.Promotion>,
					IQueryable<Domain.Entities.Promotion>>>(),
				false));

			Assert.AreEqual(result.Items.Length, 9);
		}

		[Test]
		public async Task Can_Revert_Promotion()
		{
			var fakePromotionRepository = Substitute.For<IPromotionRepository>();

			var sut = new RevertPromotionAmountCommandService(
				fakePromotionRepository,
				NullLogger<RevertPromotionAmountCommandService>.Instance);

			var command = new RevertPromotionAmountCommand(Guid.NewGuid(), 1000M);

			var percentage = 10M;
			var bonusAmountLimit = 500M;

			fakePromotionRepository.GetPromotionByIdAsync(command.PromotionId).Returns(
				new Domain.Entities.Promotion
				{
					Percentage = percentage,
					BonusAmountLimit = bonusAmountLimit
				});

			var amount = command.Amount * percentage / 100M;
			await sut.ExecuteAsync(command);

			_ = fakePromotionRepository
				.Received(1)
				.RevertPromotionAmountAsync(Arg.Is(command.PromotionId), Arg.Is(amount));
		}

		[Test]
		public async Task Check_Player_Can_Join_Promotion_Should_Be_True()
		{
			var fakePromotionRepository = Substitute.For<IPromotionRepository>();
			var fakeServiceProvider = Substitute.For<IServiceProvider>();

			var checkerFactory = new PromotionCheckerFactory(
				new Dictionary<string, Type>
				{
					["TestPromotionType"] = typeof(IPromotionChecker)
				},
				fakeServiceProvider);

			var sut = new CheckCanJoinPromotionQueryService(
				fakePromotionRepository,
				checkerFactory);

			var query = new CheckCanJoinPromotionQuery(
				Guid.NewGuid(),
				"ThisIsPlayerCode",
				Guid.NewGuid(),
				100);

			fakePromotionRepository
				.GetPromotionByIdAsync(query.PromotionId)
				.Returns(new Domain.Entities.Promotion
				{
					BrandId = query.BrandId,
					PromotionType = "TestPromotionType"
				});

			var fakeChecker = Substitute.For<IPromotionChecker>();

			fakeChecker
				.CheckAsync(Arg.Any<string>())
				.Returns(true);

			fakeServiceProvider
				.GetService(Arg.Any<Type>())
				.Returns(fakeChecker);

			fakePromotionRepository
				.UpdatePromotionAmountAsync(query.PromotionId, Arg.Any<decimal>())
				.Returns(true);

			var actual = await sut.GetAsync(query);

			Assert.IsTrue(actual);
		}

		[Test]
		public async Task Get_Promotions_By_Player_Should_Return_Match_Promotions()
		{
			var fakeValidPromotionQueryService =
				Substitute.For<IQueryService<GetValidPromotionsQuery, PromotionModel>>();
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();
			var fakePromotionChecker = Substitute.For<IPromotionChecker>();

			var sut = new GetValidPromotionsByPlayerQueryService(
				fakeValidPromotionQueryService,
				fakePlayerService,
				fakePromotionCheckerFactory);

			fakePromotionCheckerFactory.GetPromotionChecker(Arg.Any<string>())
				.Returns(fakePromotionChecker);

			fakePromotionChecker.CheckAsync(Arg.Any<string>())
				.Returns(true);

			var testData = new[]
			{
				new PromotionModel
				{
					Id = Guid.NewGuid(),
					VipLevels = Array.Empty<int>()
				},
				new PromotionModel
				{
					Id = Guid.NewGuid(),
					VipLevels = Array.Empty<int>()
				},
				new PromotionModel
				{
					Id = Guid.NewGuid(),
					VipLevels = Array.Empty<int>()
				}
			};

			var expected = testData.ToExpectedObject();

			var query = new GetValidPromotionsByPlayerQuery(Guid.NewGuid(), "ThisIsPlayerCode");

			fakePlayerService
				.GetPlayerAsync(
					Arg.Any<Expression<Func<Player, bool>>>(),
					Arg.Any<Func<IQueryable<Player>, IQueryable<Player>>>())
				.Returns(new Player
				{
					PlayerCode = query.PlayerCode,
					Vip = new VipLevel
					{
						Level = 1
					}
				});

			fakeValidPromotionQueryService.QueryAsync(Arg.Any<GetValidPromotionsQuery>())
				.Returns(testData.ToAsyncEnumerable());

			var actual = await sut.QueryAsync(query).ToArrayAsync();

			expected.ShouldMatch(actual);
		}

		[Test]
		public void Get_Promotions_Without_Player_Should_Return_All_Match_Promotions()
		{
			var fakePromotionRepository = new Mock<IPromotionRepository>();

			fakePromotionRepository
				.Setup(x => x.GetPromotionQueries(It.IsAny<Func<IQueryable<Domain.Entities.Promotion>, IQueryable<Domain.Entities.Promotion>>?>(), default))
				.Returns(new Domain.Entities.Promotion[] { }.AsQueryable().BuildMock());

			var sut = new GetValidPromotionsQueryService(fakePromotionRepository.Object);

			var query = new GetValidPromotionsQuery(Guid.NewGuid());

			sut.QueryAsync(query);

			fakePromotionRepository.Verify(x => x.GetPromotionQueries(It.IsAny<Func<
					IQueryable<Domain.Entities.Promotion>,
					IQueryable<Domain.Entities.Promotion>>>(),
				It.IsAny<bool>()), Times.Once());
		}

		private Fixture CreatePromotionFixture()
		{
			var fixture = new Fixture();

			fixture.Customize<Domain.Entities.Promotion>(u => u
				.With(s => s.PromotionGameProviders,
					new PromotionGameProvider[]
					{
					})
				.With(s => s.PromotionVipLevels,
					new PromotionVipLevel[]
					{
					})
				.With(s => s.PromotionParticipants,
					new PromotionParticipant[]
					{
					})
				.With(s => s.Localization, "{}")
				.With(s => s.Status, (short)PromotionStatus.Enabled)
				.With(s => s.PromotionBonus, new PromotionBonus[] { })
				.With(s => s.TransactionPromotions, new TransactionPromotion[] { })
				.With(s => s.VipLevel, new VipLevel { }));
			fixture.Behaviors.Add(new OmitOnRecursionBehavior());

			return fixture;
		}
	}
}
