namespace FuturePrelude.Quartz;

public class JobDetailModel
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = InternalConstants.DEFAULT_GROUP_NAME;
    public string? Description { get; set; }
    public Type? JobClass { get; set; }
    public IDictionary<string, object> JobDataMap { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary> 标记是否即使没有分配给作业的触发器，作业仍应保留在调度器中 </summary>
    public bool IsDurable { get; set; }
}