namespace Tgs.Promotion;
internal class DefaultSystemClock : ISystemClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
