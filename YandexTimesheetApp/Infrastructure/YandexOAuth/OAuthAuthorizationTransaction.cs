namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexOAuth;

public sealed record OAuthAuthorizationTransaction(
	string State,
	string CodeVerifier,
	DateTimeOffset ExpiresAtUtc);