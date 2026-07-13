using Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker.Contracts;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

public interface ITrackerApiClient
{
	Task<TrackerCurrentUserResponse> GetCurrentUserAsync(
		CancellationToken cancellationToken = default);

	Task<TrackerPage<TrackerWorklogResponse>> GetWorklogsPageAsync(
		string createdBy,
		int page,
		int pageSize,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<TrackerWorklogResponse>> GetAllWorklogsAsync(
		string createdBy,
		CancellationToken cancellationToken = default);
}
