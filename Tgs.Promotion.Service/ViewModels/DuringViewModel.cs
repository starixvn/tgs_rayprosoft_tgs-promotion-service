namespace Tgs.Promotion.Service.ViewModels;

public record struct DuringViewModel(DateTime From, DateTime To)
{
	public void Deconstruct(out DateTime from, out DateTime to)
	{
		from = From;
		to = To;
	}

	public static implicit operator DuringViewModel((DateTime From, DateTime To) value)
	{
		return new DuringViewModel(value.From, value.To);
	}
}
