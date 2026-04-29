namespace FuturePrelude.Quartz;

/// <summary> Trigger 类型配置 JSON 辅助方法 </summary>
internal static class TriggerConfigJsonHelper
{
    /// <summary> 序列化 CronTrigger 配置 </summary>
    /// <param name="config"> </param>
    /// <returns> </returns>
    internal static string SerializeCron(CronTriggerConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return JsonConvert.SerializeObject(config);
    }

    /// <summary> 反序列化 CronTrigger 配置 </summary>
    /// <param name="json"> </param>
    /// <returns> </returns>
    /// <exception cref="InvalidOperationException"> </exception>
    internal static CronTriggerConfig DeserializeCron(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("CronTrigger 配置缺失。");
        }

        return JsonConvert.DeserializeObject<CronTriggerConfig>(json)
            ?? throw new InvalidOperationException("CronTrigger 配置无效。");
    }

    /// <summary> 保存前标准化 CronTrigger 配置 </summary>
    /// <param name="trigger"> </param>
    /// <param name="cronExpression"> </param>
    /// <param name="cronDescription"> </param>
    /// <param name="timeZoneId"> </param>
    /// <exception cref="InvalidOperationException"> </exception>
    /// <exception cref="ArgumentException"> </exception>
    internal static void NormalizeForSave(
        SysJobTrigger trigger,
        string? cronExpression,
        string? cronDescription,
        string? timeZoneId)
    {
        ArgumentNullException.ThrowIfNull(trigger);

        if (trigger.TriggerType != TriggerType.Cron)
        {
            throw new InvalidOperationException("当前版本仅支持 CronTrigger");
        }

        if (string.IsNullOrWhiteSpace(trigger.TriggerGroup) || string.IsNullOrWhiteSpace(trigger.TriggerName))
        {
            throw new ArgumentException("触发器分组和名称不能为空");
        }

        trigger.TriggerName = trigger.TriggerName?.Trim();
        trigger.TriggerGroup = trigger.TriggerGroup?.Trim();

        if (string.IsNullOrWhiteSpace(cronExpression) || !Helpers.IsValidExpression(cronExpression))
        {
            throw new ArgumentException("Cron 表达式无效");
        }

        if (trigger.StartTimeUtc.HasValue
            && trigger.EndTimeUtc.HasValue
            && trigger.StartTimeUtc.Value > trigger.EndTimeUtc.Value)
        {
            throw new ArgumentException("开始时间不能晚于结束时间");
        }

        SchedulerIdentityHelper.NormalizeTriggerIdentity(trigger, trigger.TriggerGroup);
        trigger.TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId)
            ? TimeZoneDefaults.ChinaIanaTimeZone
            : timeZoneId.Trim();
        trigger.TypeConfigJson = SerializeCron(new CronTriggerConfig
        {
            CronExpression = cronExpression.Trim(),
            CronDescription = cronDescription
        });
    }
}