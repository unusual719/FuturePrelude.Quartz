namespace FuturePrelude.Quartz.Services;

/// <summary> 任务详情服务接口 </summary>
public interface ISysJobDetailService
{
    /// <summary> 分页获取任务详情列表 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<PageResponse<SysJobDetailOutput>> GetPageListAsync(SysJobDetailQueryInput query, CancellationToken cancellationToken = default);

    /// <summary> 根据ID获取任务详情 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<SysJobDetailOutput?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 创建任务详情 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<SysJobDetailOutput> CreateAsync(SysJobDetailCreateInput input, CancellationToken cancellationToken = default);

    /// <summary> 更新任务详情 </summary>
    /// <param name="input"> 更新入参 </param>
    /// <param name="cancellationToken"> 取消令牌 </param>
    /// <returns> </returns>
    Task<SysJobDetailOutput> UpdateAsync(SysJobDetailUpdateInput input, CancellationToken cancellationToken = default);

    /// <summary> 删除任务详情 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 启用任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> EnableAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 禁用任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> DisableAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 暂停任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> PauseJobAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 恢复任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> ResumeJobAsync(long id, CancellationToken cancellationToken = default);
}