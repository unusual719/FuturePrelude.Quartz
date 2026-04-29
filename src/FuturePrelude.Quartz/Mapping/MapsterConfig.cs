#pragma warning disable CS8603 // 可能返回 null 引用。

namespace FuturePrelude.Quartz;

/// <summary> Mapster 映射配置 </summary>
public static class MapsterConfig
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<JobGroupInput, SysJobGroup>();
        config.NewConfig<SysJobGroup, JobGroupOutput>();

        config.NewConfig<JobDetailInput, SysJobDetail>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreateTime)
            .Ignore(dest => dest.UpdateTime)
            .Ignore(dest => dest.IsDeleted);

        config.NewConfig<PluginInput, SysJobPlugin>()
            .Ignore(dest => dest.JobId)
            .Ignore(dest => dest.JobDetail)
            .Map(dest => dest.ParamsJson, src => PluginParameterJsonHelper.Serialize(src.Param));

        config.NewConfig<TriggerInput, SysJobTrigger>()
            .Ignore(dest => dest.JobId)
            .Ignore(dest => dest.CreateTime)
            .Ignore(dest => dest.UpdateTime)
            .Ignore(dest => dest.TriggerKey)
            .Map(dest => dest.TimeZoneId, src => string.IsNullOrWhiteSpace(src.TimeZoneId)
                ? TimeZoneDefaults.ChinaIanaTimeZone
                : src.TimeZoneId)
            .Map(dest => dest.TypeConfigJson, src => TriggerConfigJsonHelper.SerializeCron(new CronTriggerConfig
            {
                CronExpression = src.CronExpression,
                CronDescription = src.CronDescription
            }));

        config.NewConfig<PluginJobInput, SchedulePluginJobRequest>()
            .Map(dest => dest.JobDetail, src => src.Job)
            .Map(dest => dest.Plugin, src => src.Plugin)
            .Map(dest => dest.Trigger, src => src.Trigger);

        // SysJobDetail DTOs
        config.NewConfig<SysJobDetailCreateInput, SysJobDetail>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreateTime)
            .Ignore(dest => dest.UpdateTime)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.JobKey);

        config.NewConfig<SysJobDetailUpdateInput, SysJobDetail>()
            .Ignore(dest => dest.CreateTime)
            .Ignore(dest => dest.UpdateTime)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.JobKey);

        config.NewConfig<SysJobDetail, SysJobDetailOutput>()
            .Map(dest => dest.JobStatus, src => src.JobStatus);

        config.NewConfig<HttpConfigInput, SysJobHttpConfig>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.JobId)
            .Ignore(dest => dest.CreateBy)
            .Ignore(dest => dest.CreateTime)
            .Ignore(dest => dest.JobDetail);

        config.NewConfig<SysJobHttpConfig, HttpConfigOutput>()
            .Ignore(dest => dest.AuthCredentials);

        config.NewConfig<PluginConfigInput, SysJobPlugin>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.JobId)
            .Ignore(dest => dest.CreateBy)
            .Ignore(dest => dest.CreateTime)
            .Ignore(dest => dest.UpdateBy)
            .Ignore(dest => dest.UpdateTime)
            .Ignore(dest => dest.JobDetail)
            .Map(dest => dest.ParamsJson, src => PluginParameterJsonHelper.Serialize(src.Params));

        config.NewConfig<SysJobPlugin, PluginConfigOutput>()
            .Map(dest => dest.Params, src => PluginParameterJsonHelper.DeserializeOutputParameters(src.ParamsJson));

        config.NewConfig<SysJobTrigger, TriggerOutput>()
            .Map(dest => dest.CronExpression, src => TriggerConfigJsonHelper.DeserializeCron(src.TypeConfigJson).CronExpression)
            .Map(dest => dest.CronDescription, src => TriggerConfigJsonHelper.DeserializeCron(src.TypeConfigJson).CronDescription);
    }
}
