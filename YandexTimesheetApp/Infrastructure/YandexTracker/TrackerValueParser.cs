using System.Globalization;
using System.Text.RegularExpressions;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

public static partial class TrackerValueParser
{
	public static DateTimeOffset ParseDateTimeOffset(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new FormatException("Дата Яндекс Трекера не может быть пустой.");
		}

		var normalizedValue = NormalizeOffset(value);

		return DateTimeOffset.Parse(
			normalizedValue,
			CultureInfo.InvariantCulture,
			DateTimeStyles.AllowWhiteSpaces);
	}

	private static string NormalizeOffset(string value)
	{
		var offsetMatch = OffsetWithoutColonRegex().Match(value);
		return offsetMatch.Success ? value.Insert(value.Length - 2, ":") : value;
	}

	[GeneratedRegex(@"[+-]\d{4}$", RegexOptions.CultureInvariant)]
	private static partial Regex OffsetWithoutColonRegex();
}
