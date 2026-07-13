namespace Mugnum.YandexTimesheetApp.Domain.Users;

public sealed record CurrentUser(
	long Uid,
	string Login,
	string DisplayName,
	string Email);