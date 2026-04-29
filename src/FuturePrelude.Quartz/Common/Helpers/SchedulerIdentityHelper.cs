using System.Text.RegularExpressions;

namespace FuturePrelude.Quartz;

/// <summary> 调度 identity 生成与修正辅助方法 </summary>
internal static class SchedulerIdentityHelper
{
    private static readonly Regex Md5CodeRegex = new("^[0-9a-f]{32}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AsciiKeyRegex = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    internal static void NormalizeGroupIdentity(SysJobGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.Name = group.Name?.Trim();
        group.Key = ResolveGroupKey(group.Key, group.Name);
    }

    internal static void NormalizeJobIdentity(SysJobDetail job, string groupKey)
    {
        ArgumentNullException.ThrowIfNull(job);

        job.JobName = job.JobName?.Trim();
        job.JobKey = BuildCompositeKey(groupKey, ResolveNameCode(job.JobKey, job.JobName));
    }

    internal static void NormalizeTriggerIdentity(SysJobTrigger trigger, string groupKey)
    {
        ArgumentNullException.ThrowIfNull(trigger);

        trigger.TriggerName = trigger.TriggerName?.Trim();
        trigger.TriggerGroup = groupKey.Trim();
        trigger.TriggerKey = BuildCompositeKey(trigger.TriggerGroup, ResolveNameCode(trigger.TriggerKey, trigger.TriggerName));
    }

    internal static string ResolveGroupKey(string? existingKey, string groupName)
    {
        var normalized = (existingKey ?? string.Empty).Trim();
        if (IsAsciiKey(normalized))
        {
            return normalized.ToLowerInvariant();
        }

        return (groupName ?? string.Empty).Trim().ToMd5();
    }

    internal static string ResolveNameCode(string? compositeKey, string displayName)
    {
        if (TryParseCompositeKey(compositeKey, out _, out var nameCode) && IsMd5Code(nameCode))
        {
            return nameCode.ToLowerInvariant();
        }

        return (displayName ?? string.Empty).Trim().ToMd5();
    }

    internal static string BuildCompositeKey(string groupKey, string nameCode)
        => $"{groupKey.Trim()}:{nameCode.Trim().ToLowerInvariant()}";

    internal static bool TryParseCompositeKey(string? compositeKey, out string groupKey, out string nameCode)
    {
        groupKey = string.Empty;
        nameCode = string.Empty;

        if (string.IsNullOrWhiteSpace(compositeKey) || !compositeKey.Contains(':'))
        {
            return false;
        }

        var parts = compositeKey.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        groupKey = parts[0];
        nameCode = parts[1];
        return !string.IsNullOrWhiteSpace(groupKey) && !string.IsNullOrWhiteSpace(nameCode);
    }

    internal static bool IsMd5Code(string? value) => !string.IsNullOrWhiteSpace(value) && Md5CodeRegex.IsMatch(value);

    internal static bool IsAsciiKey(string? value) => !string.IsNullOrWhiteSpace(value) && AsciiKeyRegex.IsMatch(value);
}