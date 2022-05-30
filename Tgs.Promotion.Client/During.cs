namespace Tgs.Promotion.Clients;
public record struct During(DateTime From, DateTime To)
{
	public void Deconstruct(out DateTime from, out DateTime to)
	{
		from = From;
		to = To;
	}

	public static implicit operator During((DateTime From, DateTime To) during)
		=> new(during.From, during.To);
}
