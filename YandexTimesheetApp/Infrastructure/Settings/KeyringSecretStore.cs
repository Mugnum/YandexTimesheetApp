using ktsu.CredentialCache;
using ktsu.CredentialCache.Storage;
using ktsu.Semantics.Strings;

namespace Mugnum.YandexTimesheetApp.Infrastructure.Settings;

public sealed class KeyringSecretStore : ISecretStore, IDisposable
{
	private const string ServiceName = "Mugnum.YandexTimesheetApp";

	private readonly CredentialCache _credentialCache;

	public KeyringSecretStore()
	{
		var credentialStore = CredentialStoreFactory.CreateDefault(ServiceName);
		_credentialCache = new CredentialCache(credentialStore);
	}

	public Task<string?> GetAsync(string key,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var persona = CreatePersona(key);

		if (!_credentialCache.TryGet(persona, out var credential))
		{
			return Task.FromResult<string?>(null);
		}

		if (credential is not CredentialWithToken tokenCredential)
		{
			throw new InvalidOperationException($"Секрет '{key}' сохранён в неожиданном формате.");
		}

		var token = tokenCredential.Token.ToString();

		return Task.FromResult<string?>(token);
	}

	public Task SetAsync(
		string key,
		string value,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		ValidateKey(key);

		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException(
				"Значение секрета не может быть пустым.",
				nameof(value));
		}

		var persona = CreatePersona(key);

		var credential = new CredentialWithToken
		{
			Token = SemanticString<CredentialToken>.Create(
				value)
		};

		_credentialCache.AddOrReplace(
			persona,
			credential);

		return Task.CompletedTask;
	}

	public Task RemoveAsync(
		string key,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var persona = CreatePersona(key);

		_credentialCache.Remove(persona);

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_credentialCache.Dispose();
	}

	private static PersonaGUID CreatePersona(
		string key)
	{
		ValidateKey(key);

		/*
		 * PersonaGUID должен быть стабильным между запусками.
		 * Поэтому нельзя использовать CreatePersonaGUID(),
		 * который создаёт случайное значение.
		 */
		var stableGuid = CreateStableGuid(
			$"{ServiceName}/{key}");

		return SemanticString<PersonaGUID>.Create(
			stableGuid.ToString("D"));
	}

	private static Guid CreateStableGuid(
		string value)
	{
		var namespaceId = new Guid(
			"FD2AE536-4B4C-4F10-BB47-B82FA6AE9706");

		var namespaceBytes =
			namespaceId.ToByteArray();

		var valueBytes =
			System.Text.Encoding.UTF8.GetBytes(value);

		var combinedBytes =
			new byte[
				namespaceBytes.Length
				+ valueBytes.Length];

		Buffer.BlockCopy(
			namespaceBytes,
			0,
			combinedBytes,
			0,
			namespaceBytes.Length);

		Buffer.BlockCopy(
			valueBytes,
			0,
			combinedBytes,
			namespaceBytes.Length,
			valueBytes.Length);

		var hash =
			System.Security.Cryptography
				.SHA256.HashData(
					combinedBytes);

		Span<byte> guidBytes =
			stackalloc byte[16];

		hash.AsSpan(0, 16).CopyTo(
			guidBytes);

		guidBytes[6] =
			(byte)((guidBytes[6] & 0x0F) | 0x50);

		guidBytes[8] =
			(byte)((guidBytes[8] & 0x3F) | 0x80);

		return new Guid(guidBytes);
	}

	private static void ValidateKey(
		string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException(
				"Ключ секрета не может быть пустым.",
				nameof(key));
		}
	}
}