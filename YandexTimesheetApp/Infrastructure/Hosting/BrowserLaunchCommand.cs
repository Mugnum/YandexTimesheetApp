namespace Mugnum.YandexTimesheetApp.Infrastructure.Hosting;

public sealed record BrowserLaunchCommand(
	string FileName,
	IReadOnlyList<string> Arguments,
	bool UseShellExecute);