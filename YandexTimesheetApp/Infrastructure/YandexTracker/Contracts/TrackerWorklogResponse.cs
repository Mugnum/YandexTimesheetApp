using System.Text.Json.Serialization;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker.Contracts;

public sealed class TrackerWorklogResponse
{
	[JsonPropertyName("id")]
	public long Id { get; init; }

	[JsonPropertyName("version")]
	public long Version { get; init; }

	[JsonPropertyName("issue")]
	public TrackerIssueReferenceResponse Issue { get; init; } = new();

	[JsonPropertyName("comment")]
	public string? Comment { get; init; }

	[JsonPropertyName("createdBy")]
	public TrackerUserReferenceResponse CreatedBy { get; init; } = new();

	[JsonPropertyName("createdAt")]
	public string CreatedAt { get; init; } = string.Empty;

	[JsonPropertyName("updatedAt")]
	public string UpdatedAt { get; init; } = string.Empty;

	[JsonPropertyName("start")]
	public string Start { get; init; } = string.Empty;

	[JsonPropertyName("duration")]
	public string Duration { get; init; } = string.Empty;
}
