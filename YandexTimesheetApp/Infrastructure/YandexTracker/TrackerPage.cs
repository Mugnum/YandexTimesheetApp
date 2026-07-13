namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

public sealed record TrackerPage<T>(
	IReadOnlyList<T> Items,
	int Page,
	int TotalPages,
	int TotalCount);