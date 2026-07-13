namespace Mugnum.YandexTimesheetApp.Application.Settings;

public sealed record ApplicationSettings
{
	public const string YandexOAuthClientId = "4b20513412c347a5a6ec966a246de5ec";

	public const string YandexOAuthRedirectUri = "http://localhost:7200/oauth/callback";

	public string OrganizationId { get; init; } = string.Empty;

	public string OAuthDeviceId { get; init; } = string.Empty;

	public bool OpenBrowserOnStart { get; init; } = true;

	public BrowserPreference Browser { get; init; } = BrowserPreference.SystemDefault;

	public string? CustomBrowserPath { get; init; }

	public decimal WorkdayDurationHours { get; init; } = 8.0m;

	public List<string> ValidationExclusions { get; init; } =
	[
		"(овертайм)"
	];
}
