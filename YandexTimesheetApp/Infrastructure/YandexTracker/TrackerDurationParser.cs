using System.Globalization;
using System.Text.RegularExpressions;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

public static partial class TrackerDurationParser
{
	public static TimeSpan Parse(string value, decimal workdayDurationHours)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Длительность не может быть пустой.", nameof(value));
		}

		if (workdayDurationHours <= 0m)
		{
			throw new ArgumentOutOfRangeException(
				nameof(workdayDurationHours),
				"Длительность рабочего дня должна быть больше нуля.");
		}

		var match = DurationRegex().Match(value);

		if (!match.Success)
		{
			throw new FormatException($"Длительность Tracker имеет неподдерживаемый формат: '{value}'.");
		}

		var days = ParseNumber(match.Groups["days"].Value);
		var hours = ParseNumber(match.Groups["hours"].Value);
		var minutes = ParseNumber(match.Groups["minutes"].Value);
		var seconds = ParseNumber(match.Groups["seconds"].Value);
		var totalHours = days * workdayDurationHours + hours + minutes / 60m + seconds / 3600m;

		return TimeSpan.FromHours((double)totalHours);
	}

	public static decimal ParseHours(string value, decimal workdayDurationHours)
	{
		return (decimal)Parse(value, workdayDurationHours).TotalHours;
	}

	private static decimal ParseNumber(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return 0m;
		}

		return decimal.Parse(
			value.Replace(',', '.'),
			NumberStyles.AllowDecimalPoint,
			CultureInfo.InvariantCulture);
	}

	[GeneratedRegex(
		@"^P(?:(?<days>\d+(?:[.,]\d+)?)D)?(?:T(?:(?<hours>\d+(?:[.,]\d+)?)H)?(?:(?<minutes>\d+(?:[.,]\d+)?)M)?(?:(?<seconds>\d+(?:[.,]\d+)?)S)?)?$",
		RegexOptions.CultureInvariant)]
	private static partial Regex DurationRegex();
}
