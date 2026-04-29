namespace FuturePrelude.Quartz;

/// <summary> Quartz.NET 选项配置 </summary>
public class QuartzStoreOptions
{
    /// <summary> SectionName Key </summary>
    public const string SectionName = "QuartzStoreOptions";

    /// <summary> 是否启用 Quartz 持久化存储 </summary>
    public bool UsePersistentStore { get; set; }

    /// <summary> 数据库类型 </summary>
    public DBType DBType { get; set; }

    /// <summary> 数据库提供程序名称 </summary>
    public string DbProviderName
    {
        get
        {
            InternalConstants.DBProviderNameDict.TryGetValue(DBType, out var providerName);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(providerName, $"无法获取数据库类型 '{DBType}-{DBType.GetDescription()}' 的提供程序名称。");
            return providerName;
        }
    }

    /// <summary> 数据库连接字符串 </summary>
    public string ConnectionString { get; set; } = string.Empty;

    public string TablePrefix { get; set; } = "QRTZ_";

    public int ThreadCount { get; set; } = 10;
}