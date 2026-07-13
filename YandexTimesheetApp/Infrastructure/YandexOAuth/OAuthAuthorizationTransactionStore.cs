using System.Collections.Concurrent;

namespace Mugnum.YandexTimesheetApp.Infrastructure.YandexOAuth;

public sealed class OAuthAuthorizationTransactionStore
{
	private readonly ConcurrentDictionary<
		string,
		OAuthAuthorizationTransaction> _transactions = new();

	public void Add(
		OAuthAuthorizationTransaction transaction)
	{
		RemoveExpired();

		_transactions[transaction.State] = transaction;
	}

	public bool TryTake(
		string state,
		out OAuthAuthorizationTransaction transaction)
	{
		RemoveExpired();

		if (_transactions.TryRemove(
			state,
			out var value))
		{
			transaction = value;

			return true;
		}

		transaction = null!;

		return false;
	}

	private void RemoveExpired()
	{
		var now = DateTimeOffset.UtcNow;

		foreach (var pair in _transactions)
		{
			if (pair.Value.ExpiresAtUtc <= now)
			{
				_transactions.TryRemove(
					pair.Key,
					out _);
			}
		}
	}
}