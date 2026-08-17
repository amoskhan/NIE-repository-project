namespace Shared.Helpers;

/// <summary>
/// Centralised clock for the whole application. Use <see cref="Now"/> instead of
/// <c>DateTime.Now</c> or <c>DateTime.UtcNow</c> everywhere, so every timestamp
/// agrees on one wall clock.
///
/// <para>
/// The application time zone is a <b>setting</b>, not a hard-coded constant. It is read
/// from <c>Application:TimeZone</c> and defaults to <see cref="DefaultTimeZoneId"/>
/// ("Asia/Singapore") when the setting is missing or blank. Change it in
/// <c>appsettings.json</c> for your own project — nothing else needs to change.
/// </para>
///
/// <para>
/// Call <see cref="Configure(string?)"/> once during start-up (see <c>Program.cs</c>)
/// before anything reads the clock. Until then the default zone is used, so the helper
/// is always usable — for example from unit tests or design-time EF Core tooling.
/// </para>
///
/// <para>
/// All dates are stored as plain <see cref="DateTime"/> values in application local time
/// with <see cref="DateTimeKind.Unspecified"/> (PostgreSQL <c>timestamp without time zone</c>).
/// </para>
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Time zone used when <c>Application:TimeZone</c> is not set. Students can change
    /// the setting; this constant is only the shipped fallback.
    /// </summary>
    public const string DefaultTimeZoneId = "Asia/Singapore";

    // volatile: Configure() runs once at start-up, but readers may already be on
    // other threads (background jobs), so publish the new reference safely.
    private static volatile TimeZoneInfo _timeZone = ResolveTimeZone(DefaultTimeZoneId);

    /// <summary>
    /// The configured application time zone.
    /// </summary>
    public static TimeZoneInfo TimeZone => _timeZone;

    /// <summary>
    /// Id of the configured time zone, exactly as the platform reports it
    /// (may be a Windows id such as "Singapore Standard Time").
    /// </summary>
    public static string TimeZoneId => _timeZone.Id;

    /// <summary>
    /// The configured zone expressed as an IANA id (e.g. "Asia/Singapore") for tools that
    /// only accept IANA names — Sentry cron monitors, cron schedulers, JS clients.
    /// Falls back to <see cref="TimeZoneId"/> if no IANA mapping exists.
    /// </summary>
    public static string IanaTimeZoneId =>
        TimeZoneInfo.TryConvertWindowsIdToIanaId(_timeZone.Id, out var ianaId) ? ianaId : _timeZone.Id;

    /// <summary>
    /// One-time initialiser, called from <c>Program.cs</c> with the
    /// <c>Application:TimeZone</c> setting. Passing null/blank keeps
    /// <see cref="DefaultTimeZoneId"/>.
    /// </summary>
    /// <param name="timeZoneId">An IANA id ("Asia/Singapore", "Europe/London") or a
    /// Windows id ("Singapore Standard Time", "GMT Standard Time").</param>
    /// <exception cref="InvalidOperationException">The id is not a time zone this machine knows.</exception>
    public static void Configure(string? timeZoneId)
    {
        var requested = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId.Trim();
        _timeZone = ResolveTimeZone(requested);
    }

    /// <summary>
    /// Gets the current date and time in the configured application time zone.
    /// Use this instead of DateTime.Now or DateTime.UtcNow.
    /// </summary>
    public static DateTime Now => AsUnspecified(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone));

    /// <summary>
    /// Gets today's date in the configured application time zone.
    /// </summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// Current instant as a UTC <see cref="DateTimeOffset"/> (JWT <c>iat</c>/<c>exp</c>, OAuth,
    /// anything that must be unambiguous across zones). Same moment as
    /// <see cref="DateTimeOffset.UtcNow"/>; derived from <see cref="Now"/>.
    /// </summary>
    public static DateTimeOffset UtcOffsetNow => new(ToUtc(Now), TimeSpan.Zero);

    /// <summary>
    /// Converts a UTC DateTime to application local time.
    /// </summary>
    public static DateTime FromUtc(DateTime utcDateTime) =>
        AsUnspecified(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), _timeZone));

    /// <summary>
    /// Converts an application-local DateTime to UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime localDateTime) =>
        TimeZoneInfo.ConvertTimeToUtc(AsUnspecified(localDateTime), _timeZone);

    /// <summary>
    /// Normalizes a DateTime for storage in PostgreSQL timestamp without time zone columns.
    /// </summary>
    public static DateTime AsUnspecified(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    /// <summary>
    /// Nullable overload for storage normalization.
    /// </summary>
    public static DateTime? AsUnspecified(DateTime? value) =>
        value.HasValue ? AsUnspecified(value.Value) : null;

    /// <summary>
    /// Resolves a time zone id, tolerating the Windows/IANA split: Windows hosts
    /// historically want "Singapore Standard Time" while Linux/macOS want
    /// "Asia/Singapore", so try the id as given and then its counterpart.
    /// </summary>
    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        foreach (var candidate in CandidateIds(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next candidate id.
            }
            catch (InvalidTimeZoneException)
            {
                // Corrupt entry in the platform time zone database — try the next id.
            }
        }

        // Last resort for the shipped default only, so the app still starts on a
        // container image with a stripped-down time zone database.
        if (string.Equals(timeZoneId, DefaultTimeZoneId, StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                id: DefaultTimeZoneId,
                baseUtcOffset: TimeSpan.FromHours(8),
                displayName: "Singapore Time",
                standardDisplayName: "Singapore Time");
        }

        // An explicitly configured zone that cannot be resolved is a configuration
        // error — fail loudly rather than silently logging timestamps in the wrong zone.
        throw new InvalidOperationException(
            $"Application:TimeZone '{timeZoneId}' is not a time zone this machine recognises. " +
            "Use an IANA id (e.g. 'Asia/Singapore') or a Windows id (e.g. 'Singapore Standard Time').");
    }

    private static IEnumerable<string> CandidateIds(string timeZoneId)
    {
        yield return timeZoneId;

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId))
        {
            yield return windowsId;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId))
        {
            yield return ianaId;
        }
    }
}
