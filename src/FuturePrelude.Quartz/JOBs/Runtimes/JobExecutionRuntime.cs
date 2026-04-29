namespace FuturePrelude.Quartz;

internal static class JobExecutionRuntime
{
    private const string LegacyLocalDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    private static bool TryParseExpiration(string? value, out DateTimeOffset expiration)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            expiration = default;
            return false;
        }

        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out expiration))
        {
            return true;
        }

        if (DateTime.TryParseExact(
            value,
            LegacyLocalDateTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var localDateTime))
        {
            expiration = ChinaTimeZoneConverter.ToChinaOffset(localDateTime);
            return true;
        }

        if (DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var chinaDateTime))
        {
            expiration = ChinaTimeZoneConverter.ToChinaOffset(chinaDateTime);
            return true;
        }

        return false;
    }

    internal static SysJobTrigger? ResolveCurrentTrigger(SysJobDetail jobInfo, string triggerName, string triggerGroup)
    {
        var triggerKey = BuildTriggerKey(triggerName, triggerGroup);
        return jobInfo.JobTriggers?
            .FirstOrDefault(t => (t.TriggerName == triggerName && t.TriggerGroup == triggerGroup)
                || t.TriggerKey == triggerKey);
    }

    internal static string BuildTriggerKey(string triggerName, string triggerGroup)
    {
        return $"{triggerGroup}:{triggerName}";
    }

    internal static DateTimeOffset? ResolveExpiration(JobDataMap jobDataMap, SysJobTrigger? currentTrigger)
    {
        var endTime = jobDataMap.ContainsKey(JobDataMapKeys.ExpirationEndTime)
            ? jobDataMap.GetString(JobDataMapKeys.ExpirationEndTime)
            : null;
        if (TryParseExpiration(endTime, out var expiration))
        {
            return expiration;
        }

        if (currentTrigger?.EndTimeUtc is DateTime endTimeUtc)
        {
            return ChinaTimeZoneConverter.ToUtcOffset(endTimeUtc);
        }

        return null;
    }

    internal static bool IsExpired(JobDataMap jobDataMap, SysJobTrigger? currentTrigger, DateTimeOffset now)
    {
        var expiration = ResolveExpiration(jobDataMap, currentTrigger);
        return expiration.HasValue && expiration.Value.ToUniversalTime() <= now.ToUniversalTime();
    }
}