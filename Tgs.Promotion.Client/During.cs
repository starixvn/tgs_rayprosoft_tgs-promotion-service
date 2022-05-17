namespace Tgs.Promotion.Client;
public record struct During(DateTime From, DateTime To)
{
	public void Deconstruct(out DateTime from, out DateTime to)
	{
		from = From;
		to = To;
	}
}
