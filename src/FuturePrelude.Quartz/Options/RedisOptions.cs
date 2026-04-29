namespace FuturePrelude.Quartz;

/// <summary> Redis 配置选项 </summary>
public class RedisOptions
{
    /// <summary> 配置节点名称 </summary>
    public const string SectionName = "RedisOptions";

    /// <summary> Redis 实例名称 </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary> Redis 连接字符串 </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary> Redis 数据库编号，默认为 0 </summary>
    public int Database { get; set; }

    /// <summary> 配置描述说明 </summary>
    public string Desc { get; set; } = string.Empty;
}
