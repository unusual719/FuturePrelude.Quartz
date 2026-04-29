namespace FuturePrelude.Quartz;

/// <summary> 任务运行时状态（基于 Quartz 运行时信息汇总） </summary>
public class JobRuntimeState
{
    /// <summary> 任务是否存在于调度器中 </summary>
    /// <remarks>
    /// 对应 Quartz 的 <c> CheckExists(JobKey) </c>
    /// <para> true：任务已注册；false：任务不存在或已被删除 </para>
    /// </remarks>
    public bool Exists { get; set; }

    /// <summary> 任务是否处于暂停状态 </summary>
    /// <remarks>
    /// 基于 TriggerState 推导（通常为 Paused）
    /// <para> 暂停后不会再触发执行，但不会影响当前正在执行的任务 </para>
    /// </remarks>
    public bool IsPaused { get; set; }

    /// <summary> 任务当前是否正在执行中 </summary>
    /// <remarks>
    /// 通过 <c> GetCurrentlyExecutingJobs() </c> 判断
    /// <para> true：当前有实例正在运行；false：当前没有执行中的实例 </para>
    /// </remarks>
    public bool IsRunning { get; set; }

    /// <summary> Trigger 的当前状态 </summary>
    /// <remarks>
    /// 常见状态：
    /// <list type="bullet">
    /// <item> Normal：正常调度 </item>
    /// <item> Paused：已暂停 </item>
    /// <item> Complete：已完成（不会再触发） </item>
    /// <item> Error：执行异常 </item>
    /// <item> Blocked：被阻塞（通常因禁止并发执行） </item>
    /// <item> None：不存在 </item>
    /// </list>
    /// </remarks>
    public TriggerState TriggerState { get; set; }
}