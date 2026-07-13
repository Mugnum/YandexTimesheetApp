using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Mugnum.YandexTimesheetApp.Application.Settings;

namespace Mugnum.YandexTimesheetApp.Infrastructure.Hosting;

public sealed class BrowserLauncher(
	IServiceProvider serviceProvider,
	IApplicationSettingsStore settingsStore,
	BrowserExecutableResolver browserResolver,
	ILogger<BrowserLauncher> logger)
{
	public void Register(WebApplication application)
	{
		application.Lifetime.ApplicationStarted.Register(
			() => _ = OpenBrowserAsync());
	}

	private async Task OpenBrowserAsync()
	{
		try
		{
			var settings = await settingsStore.LoadAsync();

			if (!settings.OpenBrowserOnStart)
			{
				return;
			}

			var applicationUrl = GetApplicationUrl();

			if (applicationUrl is null)
			{
				logger.LogWarning(
					"Не удалось определить HTTP-адрес приложения.");

				return;
			}

			var launchCommand = browserResolver.Resolve(
				settings,
				applicationUrl);

			if (settings.Browser != BrowserPreference.SystemDefault
				&& launchCommand is null)
			{
				logger.LogWarning(
					"Выбранный браузер не найден. " +
					"Ссылка будет открыта в системном браузере.");

				OpenWithSystemBrowser(applicationUrl);

				return;
			}

			if (launchCommand is null)
			{
				OpenWithSystemBrowser(applicationUrl);

				return;
			}

			OpenWithSelectedBrowser(launchCommand);
		}
		catch (Exception exception)
		{
			logger.LogError(
				exception,
				"Не удалось автоматически открыть браузер.");
		}
	}

	private string? GetApplicationUrl()
	{
		var server =
			serviceProvider.GetRequiredService<IServer>();

		var addressesFeature =
			server.Features.Get<IServerAddressesFeature>();

		return addressesFeature?
			.Addresses
			.FirstOrDefault(IsHttpAddress);
	}

	private static void OpenWithSystemBrowser(string url)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true
		};

		if (Process.Start(startInfo) is null)
		{
			throw new InvalidOperationException(
				"Не удалось открыть системный браузер.");
		}
	}

	private static void OpenWithSelectedBrowser(
		BrowserLaunchCommand launchCommand)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = launchCommand.FileName,
			UseShellExecute = launchCommand.UseShellExecute
		};

		foreach (var argument in launchCommand.Arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		if (Process.Start(startInfo) is null)
		{
			throw new InvalidOperationException(
				"Не удалось открыть выбранный браузер.");
		}
	}

	private static bool IsHttpAddress(string address)
	{
		return address.StartsWith(
			"http://",
			StringComparison.OrdinalIgnoreCase);
	}
}