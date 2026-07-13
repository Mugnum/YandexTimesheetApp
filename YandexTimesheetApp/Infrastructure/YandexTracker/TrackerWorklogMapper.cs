using System.Globalization;
using Mugnum.YandexTimesheetApp.Domain.Worklogs;
using Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker.Contracts;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

public static class TrackerWorklogMapper
{
	public static WorklogEntry Map(TrackerWorklogResponse response, decimal workdayDurationHours)
	{
		ArgumentNullException.ThrowIfNull(response);

		if (!long.TryParse(
			response.CreatedBy.Id,
			NumberStyles.Integer,
			CultureInfo.InvariantCulture,
			out var authorUid))
		{
			throw new FormatException($"Некорректный идентификатор автора '{response.CreatedBy.Id}'.");
		}

		return new WorklogEntry(
			Id: response.Id,
			Version: response.Version,
			IssueKey: response.Issue.Key,
			IssueTitle: response.Issue.Display,
			Comment: response.Comment,
			AuthorUid: authorUid,
			AuthorDisplayName: response.CreatedBy.Display,
			Start: TrackerValueParser.ParseDateTimeOffset(response.Start),
			Duration: TrackerDurationParser.Parse(response.Duration, workdayDurationHours),
			CreatedAt: TrackerValueParser.ParseDateTimeOffset(response.CreatedAt),
			UpdatedAt: TrackerValueParser.ParseDateTimeOffset(response.UpdatedAt));
	}
}
