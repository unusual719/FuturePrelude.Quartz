namespace FuturePrelude.Quartz;

/// <summary> 系统内置 HTTP Job 种子定义 </summary>
internal sealed record SystemHttpJobSeed(
    string JobName,
    string JobDescription,
    string TriggerName,
    string TriggerDescription,
    string RelativeUrl,
    string CronExpression,
    string CronDescription,
    string TimeZoneId,
    int TimeoutSeconds = 30);

/// <summary> 系统内置 Job 种子定义集合 </summary>
internal static class SystemJobSeedDefinitions
{
    internal const string SystemGroupName = "System（系统内置任务）";
    internal const string SystemGroupKey = "system";
    internal const string SystemGroupDescription = "系统内置任务分组";
    internal const string SystemGroupIcon = "fas fa-cogs";

    internal static readonly SystemHttpJobSeed LogCleanup = new(
        JobName: "清理30天前日志数据",
        JobDescription: "系统内置任务：删除30天前的任务执行日志数据",
        TriggerName: "每日03点清理日志",
        TriggerDescription: "系统内置触发器：每天03:00执行日志清理",
        RelativeUrl: "/api/sys-job-execution-log/clean-up",
        CronExpression: "0 0 3 * * ?",
        CronDescription: "每天03:00清理30天前日志数据",
        TimeZoneId: TimeZoneDefaults.ChinaIanaTimeZone);
}
