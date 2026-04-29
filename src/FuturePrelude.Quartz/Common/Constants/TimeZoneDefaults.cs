namespace FuturePrelude.Quartz;

/// <summary> 时区默认值 </summary>
internal static class TimeZoneDefaults
{
    internal const string ChinaIanaTimeZone = "Asia/Shanghai";
    internal const string ChinaWindowsTimeZone = "China Standard Time";

    /// <summary> 获取中国时区，优先 IANA，失败回退 Windows </summary>
    internal static TimeZoneInfo ChinaTimeZone
    {
        get
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(ChinaIanaTimeZone);
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById(ChinaWindowsTimeZone);
            }
        }
    }

    /// <summary> 解析时区ID，空值回退中国时区 </summary>
    /// <param name="timeZoneId"> </param>
    /// <returns> </returns>
    internal static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return ChinaTimeZone;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch when (string.Equals(timeZoneId, ChinaIanaTimeZone, StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ChinaWindowsTimeZone);
        }
    }
}