using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Services
{
	internal class ChangeOrderCommandService : ICommandService<ChangeOrderCommand>
	{
		private readonly TgsContext _tgsRepository;

		public ChangeOrderCommandService(TgsContext tgsRepository)
		{
			_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		}

		public async ValueTask ExecuteAsync(ChangeOrderCommand command, CancellationToken cancellationToken = default)
		{
			var ids = command.Orders.Select(q => q.Id);

			var promotions = await _tgsRepository.Promotions
				.AsQueryable()
				.Where(q => ids.Contains(q.Id))
				.ToListAsync(cancellationToken)
				.ConfigureAwait(false);

			promotions.ForEach(p =>
			{
				var item = command.Orders.FirstOrDefault(x => x.Id == p.Id);

				if (item != null)
				{
					p.DisplayOrder = item.DisplayOrder;
				}
			});

			_tgsRepository.Promotions.UpdateRange(promotions);
			_ = await _tgsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
