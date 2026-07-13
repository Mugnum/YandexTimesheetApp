using Mugnum.YandexTimesheetApp.Application.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mugnum.YandexTimesheetApp.Infrastructure.Settings;

public sealed class JsonApplicationSettingsStore : IApplicationSettingsStore
{
	private const string CompanyDirectoryName = "Mugnum";

	private const string ApplicationDirectoryName = "YandexTimesheetApp";

	private const string SettingsFileName = "settings.json";

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = true,
		Converters =
		{
			new JsonStringEnumConverter()
		}
	};

	private readonly string _settingsFilePath;

	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public JsonApplicationSettingsStore()
	{
		var localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var applicationDirectoryPath = Path.Combine(localApplicationDataPath,
			CompanyDirectoryName, ApplicationDirectoryName);

		_settingsFilePath = Path.Combine(applicationDirectoryPath, SettingsFileName);
	}

	public async Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
	{
		await _semaphore.WaitAsync(cancellationToken);

		try
		{
			if (!File.Exists(_settingsFilePath))
			{
				return new ApplicationSettings();
			}

			await using var stream = File.OpenRead(_settingsFilePath);

			var settings = await JsonSerializer.DeserializeAsync<ApplicationSettings>(
				stream, SerializerOptions, cancellationToken);

			return settings ?? new ApplicationSettings();
		}
		catch (JsonException)
		{
			return new ApplicationSettings();
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async Task SaveAsync(ApplicationSettings settings,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(settings);
		await _semaphore.WaitAsync(cancellationToken);

		try
		{
			var directoryPath = Path.GetDirectoryName(_settingsFilePath);

			if (string.IsNullOrWhiteSpace(directoryPath))
			{
				throw new InvalidOperationException("Не удалось определить каталог настроек.");
			}

			Directory.CreateDirectory(directoryPath);
			var temporaryFilePath = $"{_settingsFilePath}.tmp";

			await using (var stream = File.Create(temporaryFilePath))
			{
				await JsonSerializer.SerializeAsync(stream,
					settings, SerializerOptions, cancellationToken);
			}

			File.Move(temporaryFilePath, _settingsFilePath, overwrite: true);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
