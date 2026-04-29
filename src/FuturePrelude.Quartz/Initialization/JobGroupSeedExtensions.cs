namespace FuturePrelude.Quartz;

/// <summary> 默认分组种子初始化扩展 </summary>
public static class JobGroupSeedExtensions
{
    private const string SeedOperator = "system";
    private const string DefaultGroupDescription = "系统默认分组";
    private const int DefaultGroupSortOrder = 1;

    /// <summary> 在应用启动时确保默认任务分组存在 </summary>
    /// <param name="app"> </param>
    /// <param name="cancellationToken"> </param>
    public static Task SeedDefaultJobGroupAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Services.SeedDefaultJobGroupAsync(app.Logger, cancellationToken);
    }

    /// <summary> 确保默认任务分组存在 </summary>
    /// <param name="services"> </param>
    /// <param name="logger"> </param>
    /// <param name="cancellationToken"> </param>
    public static async Task SeedDefaultJobGroupAsync(this IServiceProvider services, Microsoft.Extensions.Logging.ILogger? logger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var freeSql = services.GetService<IFreeSql>();
        if (freeSql == null)
        {
            logger?.LogDebug("未注册 FreeSql，跳过默认分组种子初始化");
            return;
        }

        var defaultGroupName = InternalConstants.DEFAULT_GROUP_NAME;
        var existingGroup = await freeSql.Select<SysJobGroup>()
            .Where(x => x.Name == defaultGroupName)
            .OrderBy(x => x.Id)
            .FirstAsync(cancellationToken);

        if (existingGroup == null)
        {
            await freeSql.Insert(new SysJobGroup
            {
                Name = defaultGroupName,
                Key = defaultGroupName.ToMd5(),
                EnableStatus = EnableStatus.Enabled,
                CreateBy = SeedOperator,
                CreateTime = ChinaTimeZoneConverter.Now(),
                ICon = "fas fa-file-alt",
                Description = DefaultGroupDescription,
                SortOrder = JobGroupSeedExtensions.DefaultGroupSortOrder
            }).ExecuteAffrowsAsync(cancellationToken);

            logger?.LogInformation("已初始化默认分组：{GroupName}", defaultGroupName);
            return;
        }

        if (!existingGroup.IsDeleted)
        {
            logger?.LogDebug("默认分组已存在，跳过初始化：{GroupName}", defaultGroupName);
            return;
        }

        existingGroup.ICon = "fas fa-file-alt";
        existingGroup.IsDeleted = false;
        existingGroup.EnableStatus = EnableStatus.Enabled;
        existingGroup.UpdateBy = SeedOperator;
        existingGroup.UpdateTime = ChinaTimeZoneConverter.Now();
        existingGroup.Description ??= DefaultGroupDescription;
        existingGroup.SortOrder = JobGroupSeedExtensions.DefaultGroupSortOrder;

        await freeSql.Update<SysJobGroup>()
            .SetSource(existingGroup)
            .UpdateColumns(x => new { x.IsDeleted, x.EnableStatus, x.UpdateBy, x.UpdateTime, x.Description })
            .ExecuteAffrowsAsync(cancellationToken);

        logger?.LogInformation("已恢复默认分组：{GroupName}", defaultGroupName);
    }
}