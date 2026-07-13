using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Mugnum.YandexTimesheetApp.Application.Settings;
using Mugnum.YandexTimesheetApp.Infrastructure.Settings;
using Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexOAuth;

public sealed class YandexOAuthService
{
	private static readonly JsonSerializerOptions SerializerOptions =
		new(JsonSerializerDefaults.Web);

	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ApplicationSettingsService _settingsService;
	private readonly ISecretStore _secretStore;
	private readonly OAuthAuthorizationTransactionStore
		_transactionStore;

	public YandexOAuthService(
		IHttpClientFactory httpClientFactory,
		ApplicationSettingsService settingsService,
		ISecretStore secretStore,
		OAuthAuthorizationTransactionStore transactionStore)
	{
		_httpClientFactory = httpClientFactory;
		_settingsService = settingsService;
		_secretStore = secretStore;
		_transactionStore = transactionStore;
	}

	public async Task<string> CreateAuthorizationUrlAsync(
		CancellationToken cancellationToken = default)
	{
		var settings = await _settingsService.GetAsync(cancellationToken);
		var state = CreateRandomValue(32);
		var codeVerifier = CreateRandomValue(64);
		var codeChallenge = CreateCodeChallenge(codeVerifier);

		_transactionStore.Add(new OAuthAuthorizationTransaction(
			state, codeVerifier, DateTimeOffset.UtcNow.AddMinutes(10)));

		var query = new Dictionary<string, string?>
		{
			["response_type"] = "code",
			["client_id"] = ApplicationSettings.YandexOAuthClientId,
			["redirect_uri"] = ApplicationSettings.YandexOAuthRedirectUri,
			["state"] = state,
			["code_challenge"] = codeChallenge,
			["code_challenge_method"] = "S256",
			["device_id"] = settings.OAuthDeviceId,
			["device_name"] = Environment.MachineName
		};

		return QueryHelpers.AddQueryString(
			YandexOAuthConstants.AuthorizationEndpoint, query);
	}

	public async Task CompleteAuthorizationAsync(string code, string state,
		CancellationToken cancellationToken = default)
	{
		if (!_transactionStore.TryTake(state, out var transaction))
		{
			throw new InvalidOperationException("OAuth-состояние отсутствует или истекло.");
		}

		if (transaction.ExpiresAtUtc <= DateTimeOffset.UtcNow)
		{
			throw new InvalidOperationException("Время OAuth-авторизации истекло.");
		}

		var settings = await _settingsService.GetAsync(cancellationToken);
		using var request = new HttpRequestMessage(HttpMethod.Post, YandexOAuthConstants.TokenEndpoint);

		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "authorization_code",
			["code"] = code,
			["client_id"] = ApplicationSettings.YandexOAuthClientId,
			["redirect_uri"] = ApplicationSettings.YandexOAuthRedirectUri,
			["code_verifier"] = transaction.CodeVerifier,
			["device_id"] = settings.OAuthDeviceId,
			["device_name"] = Environment.MachineName
		});

		var client = _httpClientFactory.CreateClient();
		using var response = await client.SendAsync(
			request, cancellationToken);

		var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"Яндекс OAuth не выдал токен. {responseContent}");
		}

		var tokenResponse = JsonSerializer.Deserialize<YandexOAuthTokenResponse>(responseContent, SerializerOptions)
			?? throw new InvalidOperationException("Яндекс OAuth вернул пустой ответ.");

		if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
		{
			throw new InvalidOperationException("Ответ Яндекс OAuth не содержит access_token.");
		}

		await _secretStore.SetAsync(TrackerSecretNames.OAuthAccessToken,
			tokenResponse.AccessToken, cancellationToken);

		if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
		{
			await _secretStore.SetAsync(TrackerSecretNames.OAuthRefreshToken,
				tokenResponse.RefreshToken, cancellationToken);
		}
	}

	public async Task ClearTokensAsync(CancellationToken cancellationToken = default)
	{
		await _secretStore.RemoveAsync(TrackerSecretNames.OAuthAccessToken, cancellationToken);
		await _secretStore.RemoveAsync(TrackerSecretNames.OAuthRefreshToken, cancellationToken);
	}

	private static string CreateRandomValue(int byteCount)
	{
		return WebEncoders.Base64UrlEncode(
			RandomNumberGenerator.GetBytes(byteCount));
	}

	private static string CreateCodeChallenge(string codeVerifier)
	{
		var bytes = Encoding.ASCII.GetBytes(codeVerifier);
		var hash = SHA256.HashData(bytes);
		return WebEncoders.Base64UrlEncode(hash);
	}
}
