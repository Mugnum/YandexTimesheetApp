namespace Mugnum.YandexTimesheetApp.Domain.Worklogs;

public sealed record WorklogEntry(
	long Id,
	long Version,
	string IssueKey,
	string IssueTitle,
	string? Comment,
	long AuthorUid,
	string AuthorDisplayName,
	DateTimeOffset Start,
	TimeSpan Duration,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt);