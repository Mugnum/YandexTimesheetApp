using System.Text.Json.Serialization;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexOAuth;

public sealed record YandexOAuthTokenResponse
{
	[JsonPropertyName("token_type")]
	public string TokenType { get; init; } =
		string.Empty;

	[JsonPropertyName("access_token")]
	public string AccessToken { get; init; } =
		string.Empty;

	[JsonPropertyName("expires_in")]
	public long ExpiresIn { get; init; }

	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; init; }

	[JsonPropertyName("scope")]
	public string? Scope { get; init; }
}