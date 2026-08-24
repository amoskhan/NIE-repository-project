namespace BuildingBlocks.Helpers;

/// <summary>
/// Centralized DateTime helper that provides Singapore local time (SGT, UTC+8).
/// Use DateTimeHelper.Now instead of DateTime.Now or DateTime.UtcNow everywhere.
/// All dates in the application are stored as plain DateTime in Singapore local time
/// without timezone information.
/// </summary>
public static class DateTimeHelper
{
    public static readonly TimeZoneInfo SingaporeTimeZone = ResolveSingaporeTimeZone();

    /// <summary>
    /// Gets the current date and time in Singapore timezone (SGT, UTC+8).
    /// Use this instead of DateTime.Now or DateTime.UtcNow.
    /// </summary>
    public static DateTime Now => AsUnspecified(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SingaporeTimeZone));

    /// <summary>
    /// Gets today's date in Singapore timezone.
    /// </summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// Current instant as a UTC <see cref="DateTimeOffset"/> (JWT <c>iat</c>/<c>exp</c>, OAuth, MyInfo).
    /// Same moment as <see cref="DateTimeOffset.UtcNow"/>; derived from <see cref="Now"/> / Singapore clock.
    /// </summary>
    public static DateTimeOffset UtcOffsetNow => new(ToUtc(Now), TimeSpan.Zero);

    /// <summary>
    /// Converts a UTC DateTime to Singapore local time.
    /// </summary>
    public static DateTime FromUtc(DateTime utcDateTime) =>
        AsUnspecified(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), SingaporeTimeZone));

    /// <summary>
    /// Converts a Singapore local DateTime to UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime sgtDateTime) =>
        TimeZoneInfo.ConvertTimeToUtc(AsUnspecified(sgtDateTime), SingaporeTimeZone);

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

    private static TimeZoneInfo ResolveSingaporeTimeZone()
    {
        foreach (var timeZoneId in new[] { "Singapore Standard Time", "Asia/Singapore" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            id: "Singapore",
            baseUtcOffset: TimeSpan.FromHours(8),
            displayName: "Singapore Time",
            standardDisplayName: "Singapore Time");
    }
}
