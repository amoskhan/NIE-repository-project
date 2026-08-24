using Domain.Identifiers;

namespace Domain.Tests;

/// <summary>
/// <see cref="Uuid7"/> is the only piece of real logic in the Domain assembly: it mints
/// every primary key. The value must be a genuine RFC 9562 version 7 UUID, because the
/// database relies on the embedded timestamp for index locality, and
/// <see cref="Uuid7.IsValid"/> is the guard that stops a v4 key (or an all-zero one)
/// from being accepted as an application identity.
/// </summary>
public sealed class Uuid7Tests
{
    private const int TimestampByteCount = 6;

    [Fact]
    public void New_mints_a_non_empty_rfc_9562_version_7_identifier()
    {
        var id = Uuid7.New();
        var bytes = id.ToByteArray(bigEndian: true);

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(7, id.Version);
        Assert.Equal(7, bytes[6] >> 4);
        Assert.Equal(0x80, bytes[8] & 0xC0);
        Assert.True(Uuid7.IsValid(id));
    }

    [Fact]
    public void New_never_repeats_a_value_within_a_tight_loop()
    {
        var seen = new HashSet<Guid>();

        for (var i = 0; i < 5_000; i++)
        {
            Assert.True(seen.Add(Uuid7.New()));
        }

        var mintedCount = seen.Count;
        Assert.Equal(5_000, mintedCount);
    }

    [Fact]
    public void New_embeds_a_current_and_non_decreasing_unix_millisecond_timestamp()
    {
        var lowerBound = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeMilliseconds();
        var timestamps = new List<long>();

        for (var i = 0; i < 200; i++)
        {
            timestamps.Add(ExtractUnixMilliseconds(Uuid7.New()));
        }

        var upperBound = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds();
        var outOfOrder = timestamps
            .Zip(timestamps.Skip(1), (earlier, later) => later - earlier)
            .Where(delta => delta < 0)
            .ToList();
        var outOfRange = timestamps
            .Where(timestamp => timestamp < lowerBound || timestamp > upperBound)
            .ToList();

        Assert.Empty(outOfOrder);
        Assert.Empty(outOfRange);
    }

    [Fact]
    public void IsValid_rejects_the_empty_guid_and_the_random_v4_guids_produced_elsewhere()
    {
        Assert.False(Uuid7.IsValid(Guid.Empty));
        Assert.False(Uuid7.IsValid(Guid.NewGuid()));
        Assert.True(Uuid7.IsValid(Uuid7.New()));
    }

    /// <summary>
    /// The guard is a version check, not a shape check: only the version nibble decides.
    /// </summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void IsValid_accepts_version_7_only(int version, bool expected)
    {
        var candidate = WithVersion(Uuid7.New(), version);

        Assert.Equal(version, candidate.Version);
        Assert.Equal(expected, Uuid7.IsValid(candidate));
    }

    private static Guid WithVersion(Guid source, int version)
    {
        var bytes = source.ToByteArray(bigEndian: true);
        bytes[6] = (byte)((version << 4) | (bytes[6] & 0x0F));
        return new Guid(bytes, bigEndian: true);
    }

    private static long ExtractUnixMilliseconds(Guid value)
    {
        var bytes = value.ToByteArray(bigEndian: true);
        long milliseconds = 0;

        for (var i = 0; i < TimestampByteCount; i++)
        {
            milliseconds = (milliseconds << 8) | bytes[i];
        }

        return milliseconds;
    }
}
