using System.Text.Json.Serialization;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker.Contracts;

public sealed class TrackerUserReferenceResponse
{
	[JsonPropertyName("id")]
	public string Id { get; init; } = string.Empty;

	[JsonPropertyName("display")]
	public string Display { get; init; } = string.Empty;
}