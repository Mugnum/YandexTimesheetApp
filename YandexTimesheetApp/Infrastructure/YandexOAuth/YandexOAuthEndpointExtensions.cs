namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexOAuth;

public static class YandexOAuthEndpointExtensions
{
	public static IEndpointRouteBuilder MapYandexOAuthEndpoints(
		this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet(
			"/oauth/login",
			async (
				YandexOAuthService oauthService,
				CancellationToken cancellationToken) =>
			{
				try
				{
					var authorizationUrl =
						await oauthService
							.CreateAuthorizationUrlAsync(
								cancellationToken);

					return Results.Redirect(
						authorizationUrl);
				}
				catch (InvalidOperationException exception)
				{
					var errorMessage =
						Uri.EscapeDataString(
							exception.Message);

					return Results.Redirect(
						$"/settings?oauthError={errorMessage}");
				}
				catch (Exception)
				{
					var errorMessage =
						Uri.EscapeDataString(
							"Не удалось начать авторизацию Яндекс OAuth.");

					return Results.Redirect(
						$"/settings?oauthError={errorMessage}");
				}
			});

		endpoints.MapGet(
			"/oauth/callback",
			async (
				string? code,
				string? state,
				string? error,
				string? error_description,
				YandexOAuthService oauthService,
				CancellationToken cancellationToken) =>
			{
				if (!string.IsNullOrWhiteSpace(error))
				{
					var message =
						error_description ?? error;

					return Results.Redirect(
						"/settings?oauthError="
						+ Uri.EscapeDataString(message));
				}

				if (string.IsNullOrWhiteSpace(code)
					|| string.IsNullOrWhiteSpace(state))
				{
					var message =
						"Яндекс OAuth не вернул code или state.";

					return Results.Redirect(
						"/settings?oauthError="
						+ Uri.EscapeDataString(message));
				}

				try
				{
					await oauthService
						.CompleteAuthorizationAsync(
							code,
							state,
							cancellationToken);

					return Results.Redirect(
						"/?oauthSuccess=true");
				}
				catch (Exception exception)
				{
					return Results.Redirect(
						"/settings?oauthError="
						+ Uri.EscapeDataString(
							exception.Message));
				}
			});

		return endpoints;
	}
}