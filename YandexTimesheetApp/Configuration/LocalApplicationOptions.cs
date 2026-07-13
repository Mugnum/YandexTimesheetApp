using System.ComponentModel.DataAnnotations;

namespace Mugnum.YandexTimesheetApp.Configuration;

public sealed class LocalApplicationOptions
{
	public const string SectionName = "Application";
	public const string DefaultUrl = "http://localhost:7200";

	[Required]
	[Url]
	public string Url { get; init; } = DefaultUrl;

	public bool OpenBrowserOnStart { get; init; } = true;
}