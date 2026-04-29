namespace FuturePrelude.Quartz;

/// <summary> 插件 Job 创建入参 </summary>
public class PluginJobInput
{
    public JobDetailInput Job { get; set; }
    public PluginInput Plugin { get; set; }
    public TriggerInput Trigger { get; set; }
}
