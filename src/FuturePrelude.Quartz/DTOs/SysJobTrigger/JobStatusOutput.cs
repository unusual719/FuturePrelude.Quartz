namespace FuturePrelude.Quartz;

/// <summary> 任务状态输出 </summary>
public class JobStatusOutput
{
    /// <summary> 任务ID </summary>
    public long Id { get; set; }

    /// <summary> 任务状态：运行中、暂停中、空闲中 </summary>
    public string Status { get; set; } = string.Empty;
}