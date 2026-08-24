using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;

namespace Auth.Tests.TestDoubles;

/// <summary>
/// An NSubstitute <see cref="IDistributedCache"/> backed by an in-memory dictionary so tests can
/// observe real store/revoke state transitions (issue -> verify -> logout -> reject) instead of
/// single-shot stubs, while still supporting <c>Received()</c> assertions.
/// </summary>
internal sealed class CacheSubstitute
{
    private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DistributedCacheEntryOptions> _options = new(StringComparer.Ordinal);
    private readonly List<string> _removedKeys = [];

    private CacheSubstitute()
    {
        Cache = Substitute.For<IDistributedCache>();

        Cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<byte[]?>(
                _entries.TryGetValue(call.ArgAt<string>(0), out var value) ? value : null));

        Cache.When(cache => cache.SetAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var key = call.ArgAt<string>(0);
                _entries[key] = call.ArgAt<byte[]>(1);
                _options[key] = call.ArgAt<DistributedCacheEntryOptions>(2);
            });

        Cache.When(cache => cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var key = call.ArgAt<string>(0);
                _entries.Remove(key);
                _options.Remove(key);
                _removedKeys.Add(key);
            });
    }

    public IDistributedCache Cache { get; }

    public IReadOnlyList<string> RemovedKeys => _removedKeys;

    public IReadOnlyCollection<string> Keys => _entries.Keys;

    public static CacheSubstitute Create() => new();

    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    public string? ReadString(string key) =>
        _entries.TryGetValue(key, out var value) ? Encoding.UTF8.GetString(value) : null;

    public void WriteString(string key, string value) =>
        _entries[key] = Encoding.UTF8.GetBytes(value);

    public DistributedCacheEntryOptions? OptionsFor(string key) =>
        _options.TryGetValue(key, out var options) ? options : null;

    public IReadOnlyList<string> KeysStartingWith(string prefix) =>
        [.. _entries.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal))];
}
