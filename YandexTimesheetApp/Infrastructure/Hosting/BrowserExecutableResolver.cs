using Mugnum.YandexTimesheetApp.Application.Settings;

namespace Mugnum.YandexTimesheetApp.Infrastructure.Hosting;

public sealed class BrowserExecutableResolver
{
	public BrowserLaunchCommand? Resolve(
		ApplicationSettings settings,
		string applicationUrl)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentException.ThrowIfNullOrWhiteSpace(applicationUrl);

		return settings.Browser switch
		{
			BrowserPreference.SystemDefault => null,

			BrowserPreference.GoogleChrome => ResolveGoogleChrome(
				applicationUrl),

			BrowserPreference.MozillaFirefox => ResolveMozillaFirefox(
				applicationUrl),

			BrowserPreference.Safari => ResolveSafari(
				applicationUrl),

			BrowserPreference.Custom => ResolveCustom(
				settings.CustomBrowserPath,
				applicationUrl),

			_ => throw new ArgumentOutOfRangeException(
				nameof(settings.Browser),
				settings.Browser,
				"Неизвестный тип браузера.")
		};
	}

	private static BrowserLaunchCommand? ResolveGoogleChrome(
		string applicationUrl)
	{
		if (OperatingSystem.IsWindows())
		{
			var executablePath = FindGoogleChromeOnWindows();

			return executablePath is null
				? null
				: new BrowserLaunchCommand(
					executablePath,
					[applicationUrl],
					false);
		}

		if (OperatingSystem.IsMacOS())
		{
			return CreateMacOsApplicationCommand(
				"Google Chrome",
				applicationUrl);
		}

		return null;
	}

	private static BrowserLaunchCommand? ResolveMozillaFirefox(
		string applicationUrl)
	{
		if (OperatingSystem.IsWindows())
		{
			var executablePath = FindMozillaFirefoxOnWindows();

			return executablePath is null
				? null
				: new BrowserLaunchCommand(
					executablePath,
					[applicationUrl],
					false);
		}

		if (OperatingSystem.IsMacOS())
		{
			return CreateMacOsApplicationCommand(
				"Firefox",
				applicationUrl);
		}

		return null;
	}

	private static BrowserLaunchCommand ResolveSafari(
		string applicationUrl)
	{
		if (!OperatingSystem.IsMacOS())
		{
			throw new PlatformNotSupportedException(
				"Safari доступен только на macOS.");
		}

		return CreateMacOsApplicationCommand(
			"Safari",
			applicationUrl);
	}

	private static BrowserLaunchCommand ResolveCustom(
		string? path,
		string applicationUrl)
	{
		var executablePath = ResolveCustomPath(path);

		return new BrowserLaunchCommand(
			executablePath,
			[applicationUrl],
			false);
	}

	private static BrowserLaunchCommand CreateMacOsApplicationCommand(
		string applicationName,
		string applicationUrl)
	{
		return new BrowserLaunchCommand(
			"/usr/bin/open",
			[
				"-a",
				applicationName,
				applicationUrl
			],
			false);
	}

	private static string? FindGoogleChromeOnWindows()
	{
		var candidates = new[]
		{
			Path.Combine(
				Environment.GetFolderPath(
					Environment.SpecialFolder.ProgramFiles),
				"Google",
				"Chrome",
				"Application",
				"chrome.exe"),

			Path.Combine(
				Environment.GetFolderPath(
					Environment.SpecialFolder.ProgramFilesX86),
				"Google",
				"Chrome",
				"Application",
				"chrome.exe"),

			Path.Combine(
				Environment.GetFolderPath(
					Environment.SpecialFolder.LocalApplicationData),
				"Google",
				"Chrome",
				"Application",
				"chrome.exe")
		};

		return FindFirstExistingFile(candidates);
	}

	private static string? FindMozillaFirefoxOnWindows()
	{
		var candidates = new[]
		{
			Path.Combine(
				Environment.GetFolderPath(
					Environment.SpecialFolder.ProgramFiles),
				"Mozilla Firefox",
				"firefox.exe"),

			Path.Combine(
				Environment.GetFolderPath(
					Environment.SpecialFolder.ProgramFilesX86),
				"Mozilla Firefox",
				"firefox.exe")
		};

		return FindFirstExistingFile(candidates);
	}

	private static string ResolveCustomPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new InvalidOperationException(
				"Не указан путь к выбранному браузеру.");
		}

		if (!File.Exists(path))
		{
			throw new FileNotFoundException(
				"Исполняемый файл выбранного браузера не найден.",
				path);
		}

		return path;
	}

	private static string? FindFirstExistingFile(
		IEnumerable<string> candidates)
	{
		foreach (var candidate in candidates)
		{
			if (!string.IsNullOrWhiteSpace(candidate)
				&& File.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}
}