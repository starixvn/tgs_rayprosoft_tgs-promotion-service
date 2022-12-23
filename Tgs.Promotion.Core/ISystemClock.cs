namespace Tgs.Promotion;

internal interface ISystemClock
{
	DateTimeOffset UtcNow { get; }
}
