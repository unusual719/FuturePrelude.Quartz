namespace FuturePrelude.Quartz;

/// <summary> 内部常量类 </summary>
internal class InternalConstants
{
    /// <summary> 默认的分组名称 </summary>
    internal const string DEFAULT_GROUP_NAME = "Default（系统默认）";

    /// <summary> HttpClient 忽略 SSL 证书验证的配置键 </summary>
    internal const string HttpClientIgnoreVerifySsl = "IgnoreSsl";

    /// <summary> dbProviderName - 数据库提供者名称字典 </summary>
    internal static readonly Dictionary<DBType, string> DBProviderNameDict = new()
    {
        { DBType.SQLite, "SQLite-Microsoft" },
        { DBType.PostgreSQL, "Npgsql" },
        { DBType.MySql, "MySql" },
        { DBType.SqlServer, "SqlServer" }
    };
}
