using System.Text.RegularExpressions;

namespace Tgs.Promotion.Service.IntegrationTests;

internal static class DbFunctionsForTest
{
	public static bool ILike(string matchExpression, string pattern)
	{
		var regex = new Regex(
			pattern.Replace("%", ".*").Replace("_", "."),
			RegexOptions.IgnoreCase);

		return regex.IsMatch(matchExpression);
	}
}
