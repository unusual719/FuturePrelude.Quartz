namespace FuturePrelude.Quartz;

/// <summary> 数据存储方式 </summary>
public enum DBType
{
    /// <summary> SQLite </summary>
    [Description("SQLite")]
    SQLite = 0,

    /// <summary> PostgreSQL </summary>
    [Description("PostgreSQL")]
    PostgreSQL = 1,

    /// <summary> MySql </summary>
    [Description("MySql")]
    MySql = 2,

    /// <summary> SQL Server </summary>
    [Description("SQL Server")]
    SqlServer = 3
}
