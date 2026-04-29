namespace FuturePrelude.Quartz.Services;

/// <summary> 任务分组服务实现 </summary>
public class SysJobGroupService(IFreeSql freeSql) : ISysJobGroupService
{
    private readonly IFreeSql _freeSql = freeSql;

    /// <inheritdoc />
    public async Task<List<SysJobGroupOutput>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _freeSql.Select<SysJobGroup>()
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.SortOrder)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync<SysJobGroupOutput>(cancellationToken);

        return list;
    }

    /// <inheritdoc />
    public async Task<SysJobGroupOutput?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _freeSql.Select<SysJobGroup>()
            .Where(a => a.Id == id && !a.IsDeleted)
            .FirstAsync<SysJobGroupOutput>(cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public async Task<SysJobGroupOutput> CreateAsync(SysJobGroupCreateInput input, CancellationToken cancellationToken = default)
    {
        // 检查名称是否已存在
        var exists = await _freeSql.Select<SysJobGroup>()
            .Where(a => a.Name == input.Name && !a.IsDeleted)
            .AnyAsync(cancellationToken);

        if (exists)
            throw new ArgumentException($"分组名称 '{input.Name}' 已存在");

        var entity = new SysJobGroup
        {
            Name = input.Name.Trim(),
            ICon = input.Icon?.Trim(),
            EnableStatus = EnableStatus.Enabled,
            Description = input.Description,
            CreateBy = "system",
            CreateTime = ChinaTimeZoneConverter.Now(),
            IsDeleted = false
        };

        SchedulerIdentityHelper.NormalizeGroupIdentity(entity);
        entity.Id = (long)await _freeSql.Insert(entity).ExecuteIdentityAsync(cancellationToken);

        return entity.Adapt<SysJobGroupOutput>();
    }

    /// <inheritdoc />
    public async Task<SysJobGroupOutput> UpdateAsync(SysJobGroupUpdateInput input, CancellationToken cancellationToken = default)
    {
        // 检查名称是否已被其他记录使用
        var exists = await _freeSql.Select<SysJobGroup>()
            .Where(a => a.Name == input.Name && a.Id != input.Id && !a.IsDeleted)
            .AnyAsync(cancellationToken);

        if (exists)
            throw new ArgumentException($"分组名称 '{input.Name}' 已存在");

        var entity = await _freeSql.Select<SysJobGroup>()
            .Where(a => a.Id == input.Id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (entity == null)
            throw new KeyNotFoundException($"分组 ID '{input.Id}' 不存在");

        if (entity.Name == InternalConstants.DEFAULT_GROUP_NAME)
            throw new KeyNotFoundException($"默认分组不允许修改");

        entity.Name = input.Name.Trim();
        entity.ICon = input.Icon?.Trim();
        entity.EnableStatus = EnableStatus.Enabled;
        entity.Description = input.Description;
        entity.UpdateBy = "system";
        entity.UpdateTime = ChinaTimeZoneConverter.Now();
        SchedulerIdentityHelper.NormalizeGroupIdentity(entity);

        await _freeSql.Update<SysJobGroup>()
            .SetSource(entity)
            .ExecuteAffrowsAsync(cancellationToken);

        return entity.Adapt<SysJobGroupOutput>();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _freeSql.Select<SysJobGroup>().Where(c => c.Id == id).FirstAsync();
        if (entity?.Name == InternalConstants.DEFAULT_GROUP_NAME)
            throw new KeyNotFoundException($"默认分组不允许修改");

        // 检查是否存在关联的任务
        var hasJobs = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.GroupId == id && !a.IsDeleted)
            .AnyAsync(cancellationToken);
        if (hasJobs) throw new InvalidOperationException($"分组下存在关联任务，无法删除");

        // 真删除不做假删
        var rows = await _freeSql.Delete<SysJobGroup>()
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        return rows > 0;
    }
}