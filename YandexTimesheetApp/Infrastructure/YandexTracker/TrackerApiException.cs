using System.Net;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker;

public sealed class TrackerApiException : Exception
{
	public TrackerApiException(
		HttpStatusCode statusCode,
		string requestUri,
		string responseContent)
		: base(CreateMessage(statusCode, requestUri, responseContent))
	{
		StatusCode = statusCode;
		RequestUri = requestUri;
		ResponseContent = responseContent;
	}

	public HttpStatusCode StatusCode { get; }

	public string RequestUri { get; }

	public string ResponseContent { get; }

	private static string CreateMessage(
		HttpStatusCode statusCode,
		string requestUri,
		string responseContent)
	{
		var content = string.IsNullOrWhiteSpace(responseContent)
			? "Тело ответа отсутствует."
			: responseContent;

		return
			$"Tracker API вернул ошибку {(int)statusCode} " +
			$"({statusCode}) для запроса '{requestUri}'. " +
			$"Ответ: {content}";
	}
}
