namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexTracker.Exceptions;

public sealed class TrackerAuthenticationRequiredException(string message) :
	InvalidOperationException(message);