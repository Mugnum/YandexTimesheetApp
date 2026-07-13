using Mugnum.YandexTimesheetApp.Domain.Worklogs;

namespace Mugnum.YandexTimesheetApp.Application.Worklogs;

public sealed class WorklogAggregationService
{
	public IReadOnlyList<WorklogDaySummary> BuildDailySummaries(
		IEnumerable<WorklogEntry> worklogs,
		DateOnly from,
		DateOnly to,
		TimeZoneInfo timeZone)
	{
		ArgumentNullException.ThrowIfNull(worklogs);
		ArgumentNullException.ThrowIfNull(timeZone);

		if (to < from)
		{
			throw new ArgumentException(
				"Конечная дата не может быть меньше начальной даты.",
				nameof(to));
		}

		var entriesByDate = worklogs
			.GroupBy(worklog => GetLocalDate(worklog.Start, timeZone))
			.ToDictionary(
				group => group.Key,
				group => group.ToList());

		var result = new List<WorklogDaySummary>();

		for (var date = from; date <= to; date = date.AddDays(1))
		{
			if (!entriesByDate.TryGetValue(date, out var entries))
			{
				entries = new List<WorklogEntry>();
			}

			var orderedEntries = entries
				.OrderBy(entry => TimeZoneInfo.ConvertTime(entry.Start, timeZone))
				.ToArray();

			var totalDuration = TimeSpan.FromTicks(
				orderedEntries.Sum(entry => entry.Duration.Ticks));

			result.Add(
				new WorklogDaySummary(
					date,
					totalDuration,
					orderedEntries));
		}

		return result;
	}

	private static DateOnly GetLocalDate(
		DateTimeOffset value,
		TimeZoneInfo timeZone)
	{
		var localValue = TimeZoneInfo.ConvertTime(value, timeZone);

		return DateOnly.FromDateTime(localValue.DateTime);
	}
}
