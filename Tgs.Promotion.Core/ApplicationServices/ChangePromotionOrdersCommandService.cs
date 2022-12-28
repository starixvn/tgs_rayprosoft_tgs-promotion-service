using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class ChangePromotionOrdersCommandService : ICommandService<ChangePromotionOrdersCommand>
{
	private readonly TgsContext _tgsRepository;

	public ChangePromotionOrdersCommandService(TgsContext tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public async ValueTask ExecuteAsync(ChangePromotionOrdersCommand command, CancellationToken cancellationToken = default)
	{
		var ids = command.Orders.Select(p => p.PromotionId);

		var promotions = await _tgsRepository.PromotionRows
			.AsQueryable()
			.Where(q => ids.Contains(q.Id))
			.ToListAsync(cancellationToken)
			.ConfigureAwait(false);

		promotions.ForEach(p =>
		{
			var item = command.Orders.FirstOrDefault(x => x.PromotionId == p.Id);

			if (item != null)
			{
				p.DisplayOrder = item.Order;
			}
		});

		_tgsRepository.PromotionRows.UpdateRange(promotions);
		_ = await _tgsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}
