namespace Mugnum.YandexTimesheetApp.Domain.Worklogs;

public sealed record WorklogDaySummary(
	DateOnly Date,
	TimeSpan TotalDuration,
	IReadOnlyList<WorklogEntry> Entries);