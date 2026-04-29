namespace FuturePrelude.Quartz.Services;

/// <summary> 任务分组服务接口 </summary>
public interface ISysJobGroupService
{
    /// <summary> 获取所有任务分组列表 </summary>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<List<SysJobGroupOutput>> GetListAsync(CancellationToken cancellationToken = default);

    /// <summary> 根据ID获取任务分组 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<SysJobGroupOutput?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 创建任务分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<SysJobGroupOutput> CreateAsync(SysJobGroupCreateInput input, CancellationToken cancellationToken = default);

    /// <summary> 更新任务分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<SysJobGroupOutput> UpdateAsync(SysJobGroupUpdateInput input, CancellationToken cancellationToken = default);

    /// <summary> 删除任务分组 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}