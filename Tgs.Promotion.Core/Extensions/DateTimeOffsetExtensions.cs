namespace Tgs.Promotion.Extensions;

public static class DateTimeOffsetExtensions
{
	public static string ToIso8601String(this DateTimeOffset dateTimeOffset, bool containMillisecond = false)
	{
		// Returns string format like as below:
		// contain millisecond: 2020-09-25T07:07:31.329+00:00
		// no millisecond: 2020-09-25T07:07:31+00:00

		var format = containMillisecond ? @"yyyy-MM-dd\THH:mm:ss.fffK" : @"yyyy-MM-dd\THH:mm:ssK";
		return dateTimeOffset.ToString(format);
	}

	public static string ToDateTimeFormat(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.ToString(@"yyyy-MM-dd HH:mm:ss");

	public static string ToUtcDateTimeFormat(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.ToString(@"yyyy-MM-dd HH:mmK");

	public static DateTimeOffset GetMonday(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.Subtract(new TimeSpan((int)dateTimeOffset.DayOfWeek - 1, 0, 0, 0));

	public static DateTimeOffset GetSunday(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.Subtract(new TimeSpan((int)dateTimeOffset.DayOfWeek - 7, 0, 0, 0));

	public static DateTimeOffset GetDayOfWeek(this DateTimeOffset dateTimeOffset, int day) =>
		dateTimeOffset.Subtract(new TimeSpan((int)dateTimeOffset.DayOfWeek - day, 0, 0, 0));

	public static DateTimeOffset GetFirstDayOfMonth(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.AddDays(1 - dateTimeOffset.Day);

	public static DateTimeOffset GetLastDayOfMonth(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.GetFirstDayOfMonth().AddMonths(1).AddDays(-1);
}
