namespace FuturePrelude.Quartz;

internal static class ChinaTimeZoneConverter
{
    internal static DateTime Now()
        => FromUtc(DateTime.UtcNow);

    internal static DateTime FromUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneDefaults.ChinaTimeZone);
    }

    internal static DateTime FromOffset(DateTimeOffset value)
        => TimeZoneInfo.ConvertTime(value, TimeZoneDefaults.ChinaTimeZone).DateTime;

    internal static DateTime? NormalizeToChinaTime(DateTime? value)
        => value.HasValue ? FromUtc(value.Value) : null;

    internal static DateTime ToUtc(DateTime chinaTime)
    {
        if (chinaTime.Kind == DateTimeKind.Utc)
            return chinaTime;

        var tz = TimeZoneDefaults.ChinaTimeZone;

        return chinaTime.Kind switch
        {
            DateTimeKind.Local => chinaTime.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(chinaTime, DateTimeKind.Unspecified), tz)
        };
    }

    internal static DateTimeOffset ToUtcOffset(DateTime chinaTime)
        => new(ToUtc(chinaTime), TimeSpan.Zero);

    internal static DateTimeOffset ToChinaOffset(DateTime chinaTime)
    {
        var tz = TimeZoneDefaults.ChinaTimeZone;
        var offset = tz.GetUtcOffset(chinaTime);

        return new DateTimeOffset(
            DateTime.SpecifyKind(chinaTime, DateTimeKind.Unspecified),
            offset);
    }
}