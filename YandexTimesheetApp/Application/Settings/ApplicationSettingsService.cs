namespace Mugnum.YandexTimesheetApp.Application.Settings;

public sealed class ApplicationSettingsService(IApplicationSettingsStore settingsStore)
{
	public async Task<ApplicationSettings> GetAsync(
		CancellationToken cancellationToken = default)
	{
		var settings = await settingsStore.LoadAsync(cancellationToken);
		var deviceId = settings.OAuthDeviceId;

		if (!string.IsNullOrWhiteSpace(deviceId))
		{
			return Normalize(settings);
		}

		deviceId = Guid.NewGuid().ToString("N");
		settings = settings with
		{
			OAuthDeviceId = deviceId
		};

		await settingsStore.SaveAsync(settings, cancellationToken);
		return Normalize(settings);
	}

	public async Task SaveAsync(ApplicationSettings settings,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(settings);
		Validate(settings);

		var currentSettings = await GetAsync(cancellationToken);
		var normalizedSettings = Normalize(settings with
		{
			OAuthDeviceId =
				string.IsNullOrWhiteSpace(
					settings.OAuthDeviceId)
					? currentSettings.OAuthDeviceId
					: settings.OAuthDeviceId
		});

		await settingsStore.SaveAsync(normalizedSettings, cancellationToken);
	}

	private static void Validate(ApplicationSettings settings)
	{
		if (string.IsNullOrWhiteSpace(settings.OrganizationId))
		{
			throw new InvalidOperationException("Укажите ID организации Яндекс 360.");
		}

		if (settings.WorkdayDurationHours is <= 0 or > 24)
		{
			throw new InvalidOperationException("Длительность рабочего дня должна находиться " +
				"в диапазоне от 0 до 24 часов.");
		}
	}

	private static ApplicationSettings Normalize(ApplicationSettings settings)
	{
		return settings with
		{
			OrganizationId = settings.OrganizationId.Trim(),

			CustomBrowserPath = string.IsNullOrWhiteSpace(settings.CustomBrowserPath)
				? null
				: settings.CustomBrowserPath.Trim(),

			ValidationExclusions = (settings.ValidationExclusions ?? [])
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Select(value => value.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList()
		};
	}
}
