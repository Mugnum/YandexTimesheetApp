using Mugnum.YandexTimesheetApp.Domain.Users;
using Mugnum.YandexTimesheetApp.Domain.Worklogs;

namespace Mugnum.YandexTimesheetApp.Application.Worklogs;

public sealed record CurrentUserWorklogSnapshot(
	CurrentUser User,
	IReadOnlyList<WorklogEntry> Worklogs,
	DateTimeOffset LoadedAtUtc);