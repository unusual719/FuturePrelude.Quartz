namespace FuturePrelude.Quartz;

/// <summary> 插件 Job 创建返回 </summary>
public class PluginJobOutput
{
    public string JobKey { get; set; }
    public long JobId { get; set; }
    public long TriggerId { get; set; }
    public long PluginId { get; set; }
}