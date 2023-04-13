namespace Tgs.Promotion;

public record During(DateTime From, DateTime To)
{
	public static implicit operator During((DateTime From, DateTime To) during)
		=> new(during.From, during.To);
}
