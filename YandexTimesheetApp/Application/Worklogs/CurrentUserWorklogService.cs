using Mugnum.YandexTimesheetApp.Application.Settings;
using Mugnum.YandexTimesheetApp.Domain.Users;
using Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

namespace Mugnum.YandexTimesheetApp.Application.Worklogs;

public sealed class CurrentUserWorklogService : IDisposable
{
	private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

	private readonly ITrackerApiClient _trackerApiClient;
	private readonly ApplicationSettingsService _settingsService;
	private readonly ILogger<CurrentUserWorklogService> _logger;
	private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

	private CurrentUserWorklogSnapshot? _cachedSnapshot;

	public CurrentUserWorklogService(
		ITrackerApiClient trackerApiClient,
		ApplicationSettingsService settingsService,
		ILogger<CurrentUserWorklogService> logger)
	{
		_trackerApiClient = trackerApiClient;
		_settingsService = settingsService;
		_logger = logger;
	}

	public async Task<CurrentUserWorklogSnapshot> GetSnapshotAsync(
		bool forceRefresh = false,
		CancellationToken cancellationToken = default)
	{
		var cachedSnapshot = _cachedSnapshot;

		if (!forceRefresh && IsFresh(cachedSnapshot))
		{
			return cachedSnapshot!;
		}

		await _loadSemaphore.WaitAsync(cancellationToken);

		try
		{
			cachedSnapshot = _cachedSnapshot;

			if (!forceRefresh && IsFresh(cachedSnapshot))
			{
				return cachedSnapshot!;
			}

			var settings = await _settingsService.GetAsync(cancellationToken);
			var userResponse = await _trackerApiClient.GetCurrentUserAsync(cancellationToken);
			var worklogResponses = await _trackerApiClient.GetAllWorklogsAsync(userResponse.Login, cancellationToken);

			var worklogs = worklogResponses
				.Select(response => TrackerWorklogMapper.Map(response, settings.WorkdayDurationHours))
				.Where(worklog => worklog.AuthorUid == userResponse.Uid)
				.GroupBy(worklog => worklog.Id)
				.Select(group => group.OrderByDescending(worklog => worklog.Version).First())
				.OrderByDescending(worklog => worklog.Start)
				.ToArray();

			var currentUser = new CurrentUser(
				userResponse.Uid,
				userResponse.Login,
				userResponse.Display,
				userResponse.Email);

			var snapshot = new CurrentUserWorklogSnapshot(currentUser, worklogs, DateTimeOffset.UtcNow);
			_cachedSnapshot = snapshot;

			_logger.LogInformation(
				"Загружено {WorklogCount} списаний пользователя {Login}.",
				worklogs.Length,
				currentUser.Login);

			return snapshot;
		}
		finally
		{
			_loadSemaphore.Release();
		}
	}

	public void ClearCache()
	{
		_cachedSnapshot = null;
	}

	public void Dispose()
	{
		_loadSemaphore.Dispose();
	}

	private static bool IsFresh(CurrentUserWorklogSnapshot? snapshot)
	{
		return snapshot is not null && DateTimeOffset.UtcNow - snapshot.LoadedAtUtc <= CacheLifetime;
	}
}
