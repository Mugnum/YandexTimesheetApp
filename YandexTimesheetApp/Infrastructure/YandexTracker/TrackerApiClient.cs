using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mugnum.YandexTimesheetApp.Application.Settings;
using Mugnum.YandexTimesheetApp.Infrastructure.Settings;
using Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker.Contracts;
using Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker.Exceptions;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

public sealed class TrackerApiClient : ITrackerApiClient
{
	private const string OrganizationHeaderName = "X-Org-ID";
	private const string AuthorizationScheme = "OAuth";
	private const int WorklogPageSize = 100;

	private static readonly JsonSerializerOptions SerializerOptions =
		new(JsonSerializerDefaults.Web)
		{
			PropertyNameCaseInsensitive = true
		};

	private readonly HttpClient _httpClient;
	private readonly IApplicationSettingsStore _settingsStore;
	private readonly ISecretStore _secretStore;
	private readonly ILogger<TrackerApiClient> _logger;

	public TrackerApiClient(
		HttpClient httpClient,
		IApplicationSettingsStore settingsStore,
		ISecretStore secretStore,
		ILogger<TrackerApiClient> logger)
	{
		_httpClient = httpClient;
		_settingsStore = settingsStore;
		_secretStore = secretStore;
		_logger = logger;
	}

	public async Task<TrackerCurrentUserResponse> GetCurrentUserAsync(
		CancellationToken cancellationToken = default)
	{
		using var request = await CreateRequestAsync(
			HttpMethod.Get,
			"v3/myself",
			cancellationToken);

		using var response = await _httpClient.SendAsync(
			request,
			HttpCompletionOption.ResponseHeadersRead,
			cancellationToken);

		await EnsureSuccessAsync(
			response,
			request.RequestUri,
			cancellationToken);

		var currentUser =
			await response.Content.ReadFromJsonAsync<
				TrackerCurrentUserResponse>(
				SerializerOptions,
				cancellationToken);

		if (currentUser is null)
		{
			throw new InvalidOperationException(
				"Tracker API вернул пустой ответ " +
				"для текущего пользователя.");
		}

		return currentUser;
	}

	public async Task<TrackerPage<TrackerWorklogResponse>>
		GetWorklogsPageAsync(
			string createdBy,
			int page,
			int pageSize,
			CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(createdBy))
		{
			throw new ArgumentException(
				"Логин или идентификатор автора обязателен.",
				nameof(createdBy));
		}

		if (page < 1)
		{
			throw new ArgumentOutOfRangeException(
				nameof(page),
				page,
				"Номер страницы должен быть больше нуля.");
		}

		var requestUri =
			$"v3/worklog/_search" +
			$"?perPage={pageSize.ToString(
				CultureInfo.InvariantCulture)}" +
			$"&page={page.ToString(
				CultureInfo.InvariantCulture)}";

		using var request = await CreateRequestAsync(
			HttpMethod.Post,
			requestUri,
			cancellationToken);

		request.Content = JsonContent.Create(
			new WorklogSearchRequest(createdBy),
			options: SerializerOptions);

		using var response = await _httpClient.SendAsync(
			request,
			HttpCompletionOption.ResponseHeadersRead,
			cancellationToken);

		await EnsureSuccessAsync(
			response,
			request.RequestUri,
			cancellationToken);

		var items =
			await response.Content.ReadFromJsonAsync<
				List<TrackerWorklogResponse>>(
				SerializerOptions,
				cancellationToken);

		var totalCount = ReadRequiredIntegerHeader(
			response,
			"X-Total-Count");

		var totalPages = ReadRequiredIntegerHeader(
			response,
			"X-Total-Pages");

		return new TrackerPage<TrackerWorklogResponse>(
			items ?? [],
			page,
			totalPages,
			totalCount);
	}

	public async Task<IReadOnlyList<TrackerWorklogResponse>> GetAllWorklogsAsync(
		string createdBy, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(createdBy))
		{
			throw new ArgumentException(
				"Логин или идентификатор автора обязателен.",
				nameof(createdBy));
		}

		var firstPage = await GetWorklogsPageAsync(
			createdBy,
			page: 1,
			WorklogPageSize,
			cancellationToken);

		if (firstPage.TotalPages <= 1)
		{
			return firstPage.Items;
		}

		var result = new List<TrackerWorklogResponse>(
			firstPage.TotalCount);

		result.AddRange(firstPage.Items);

		for (var page = 2;
			page <= firstPage.TotalPages;
			page++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			_logger.LogInformation(
				"Загрузка страницы worklog {Page} из {TotalPages}.",
				page,
				firstPage.TotalPages);

			var currentPage = await GetWorklogsPageAsync(
				createdBy,
				page,
				WorklogPageSize,
				cancellationToken);

			result.AddRange(currentPage.Items);
		}

		if (result.Count != firstPage.TotalCount)
		{
			_logger.LogWarning(
				"Количество загруженных worklog отличается " +
				"от X-Total-Count. Загружено: {LoadedCount}, " +
				"ожидалось: {ExpectedCount}.",
				result.Count,
				firstPage.TotalCount);
		}

		return result;
	}

	private async Task<HttpRequestMessage> CreateRequestAsync(
		HttpMethod method,
		string requestUri,
		CancellationToken cancellationToken)
	{
		var settings = await _settingsStore.LoadAsync(
			cancellationToken);

		if (string.IsNullOrWhiteSpace(
			settings.OrganizationId))
		{
			throw new InvalidOperationException(
				"Не указан идентификатор организации " +
				"Яндекс 360. Откройте страницу настроек.");
		}

		var oauthToken = await _secretStore.GetAsync(
			TrackerSecretNames.OAuthAccessToken,
			cancellationToken);

		if (string.IsNullOrWhiteSpace(oauthToken))
		{
			throw new TrackerAuthenticationRequiredException(
				"OAuth-токен Яндекс Трекера отсутствует.");
		}

		var request = new HttpRequestMessage(
			method,
			requestUri);

		request.Headers.TryAddWithoutValidation(
			OrganizationHeaderName,
			settings.OrganizationId.Trim());

		request.Headers.TryAddWithoutValidation(
			"Authorization",
			$"{AuthorizationScheme} {oauthToken.Trim()}");

		request.Headers.Accept.ParseAdd(
			"application/json");

		request.Headers.AcceptLanguage.ParseAdd("ru");

		return request;
	}

	private async Task EnsureSuccessAsync(
		HttpResponseMessage response,
		Uri? requestUri,
		CancellationToken cancellationToken)
	{
		if (response.IsSuccessStatusCode)
		{
			return;
		}

		var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			try
			{
				await _secretStore.RemoveAsync(
					TrackerSecretNames.OAuthAccessToken,
					cancellationToken);

				_logger.LogInformation(
					"Недействительный OAuth access token " +
					"удалён из защищённого хранилища.");
			}
			catch (Exception exception)
			{
				_logger.LogWarning(
					exception,
					"Не удалось удалить недействительный " +
					"OAuth access token.");
			}
		}

		throw new TrackerApiException(
			response.StatusCode,
			requestUri?.ToString() ?? string.Empty,
			responseContent);
	}

	private static int ReadRequiredIntegerHeader(
		HttpResponseMessage response,
		string headerName)
	{
		if (!response.Headers.TryGetValues(
			headerName,
			out var values))
		{
			throw new InvalidOperationException(
				$"Tracker API не вернул обязательный " +
				$"заголовок '{headerName}'.");
		}

		var headerValue = values.FirstOrDefault();

		if (!int.TryParse(
			headerValue,
			NumberStyles.Integer,
			CultureInfo.InvariantCulture,
			out var result))
		{
			throw new InvalidOperationException(
				$"Заголовок '{headerName}' содержит " +
				$"некорректное значение '{headerValue}'.");
		}

		return result;
	}

	private sealed record WorklogSearchRequest(
		string CreatedBy);
}