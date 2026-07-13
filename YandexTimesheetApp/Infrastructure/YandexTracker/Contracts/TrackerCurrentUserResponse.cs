using System.Text.Json.Serialization;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker.Contracts;

public sealed class TrackerCurrentUserResponse
{
	[JsonPropertyName("uid")]
	public long Uid { get; init; }

	[JsonPropertyName("login")]
	public string Login { get; init; } = string.Empty;

	[JsonPropertyName("firstName")]
	public string FirstName { get; init; } = string.Empty;

	[JsonPropertyName("lastName")]
	public string LastName { get; init; } = string.Empty;

	[JsonPropertyName("display")]
	public string Display { get; init; } = string.Empty;

	[JsonPropertyName("email")]
	public string Email { get; init; } = string.Empty;

	[JsonPropertyName("hasLicense")]
	public bool HasLicense { get; init; }

	[JsonPropertyName("dismissed")]
	public bool Dismissed { get; init; }
}
