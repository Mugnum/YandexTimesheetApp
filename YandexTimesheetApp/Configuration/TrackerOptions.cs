using System.ComponentModel.DataAnnotations;

namespace Mugnum.YandexTimesheetApp.Configuration;

public sealed class TrackerOptions
{
	public const string SectionName = "Tracker";

	[Required]
	public string BaseUrl { get; init; } = "https://api.tracker.yandex.net";

	[Required]
	public string OrganizationId { get; init; } = string.Empty;

	public int PageSize { get; init; } = 100;
}
