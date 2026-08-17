using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Dto;
using Shared.Extensions;
using StackExchange.Redis;

namespace API.Sessions;

/// <summary>
/// Revokes sessions by scanning Valkey for <c>session:*</c> entries and deleting the ones that
/// belong to a given login name. See <see cref="ISessionRevocationService"/>.
/// <para>
/// There is no user-to-token index in Valkey, so this has to scan. That is fine for the
/// administrative actions it serves (deactivating an account is rare and not on a hot path). If
/// your project deactivates users in bulk, add a <c>user-sessions:{userId}</c> set that the Auth
/// API appends to when it issues a session, and read that here instead.
/// </para>
/// </summary>
public class SessionRevocationService : ISessionRevocationService
{
    /// <summary>
    /// The logical key prefix the Auth API writes sessions under. The physical Redis key is this
    /// prefixed again by <c>Valkey:InstanceName</c>, which IDistributedCache adds transparently.
    /// </summary>
    private const string SessionKeyPrefix = "session:";

    /// <summary>How many keys SCAN pulls per round trip.</summary>
    private const int ScanPageSize = 250;

    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SessionRevocationService> _logger;
    private readonly string _instanceName;

    public SessionRevocationService(
        IConnectionMultiplexer multiplexer,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<SessionRevocationService> logger)
    {
        _multiplexer = multiplexer;
        _cache = cache;
        _logger = logger;
        _instanceName = configuration["Valkey:InstanceName"] ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<int?> RevokeUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return 0;

        try
        {
            var revoked = 0;
            var pattern = $"{_instanceName}{SessionKeyPrefix}*";

            foreach (var endpoint in _multiplexer.GetEndPoints())
            {
                var server = _multiplexer.GetServer(endpoint);

                // Replicas mirror the primary, so scanning them would only find the same keys
                // again - and DEL against a replica is refused anyway.
                if (!server.IsConnected || server.IsReplica)
                    continue;

                await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: ScanPageSize)
                                   .WithCancellation(cancellationToken))
                {
                    // Strip the instance prefix: IDistributedCache re-adds it on every call, so
                    // handing it the physical key would look for a double-prefixed one.
                    var logicalKey = StripInstancePrefix(key.ToString());
                    if (logicalKey is null)
                        continue;

                    var payload = await _cache.GetStringAsync(logicalKey, cancellationToken);
                    if (string.IsNullOrEmpty(payload))
                        continue;

                    AuthDto? session;
                    try
                    {
                        session = JsonExtensions.Deserialize<AuthDto>(payload);
                    }
                    catch (JsonException)
                    {
                        // An unreadable session cannot be attributed to a user; leave it to expire.
                        continue;
                    }

                    if (session is null
                        || !string.Equals(session.UserId, userId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    await _cache.RemoveAsync(logicalKey, cancellationToken);
                    revoked++;
                }
            }

            return revoked;
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Could not revoke sessions for {UserId}: Valkey is unreachable.", userId);
            return null;
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogError(ex, "Could not revoke sessions for {UserId}: Valkey timed out.", userId);
            return null;
        }
    }

    /// <summary>
    /// Turns a physical Redis key back into the logical key IDistributedCache understands.
    /// Returns null for keys outside this instance's namespace.
    /// </summary>
    private string? StripInstancePrefix(string physicalKey)
    {
        if (_instanceName.Length == 0)
            return physicalKey;

        return physicalKey.StartsWith(_instanceName, StringComparison.Ordinal)
            ? physicalKey[_instanceName.Length..]
            : null;
    }
}
