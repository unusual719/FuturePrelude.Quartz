namespace FuturePrelude.Quartz;

/// <summary> 插件元数据 </summary>
[Table(Name = "SysJobPlugin")]
public class SysJobPlugin
{
    /// <summary> Id 主键 </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary> 任务ID（SysJobDetail.Id） </summary>
    [Column(IsNullable = false)]
    public long JobId { get; set; }

    /// <summary> 包名 </summary>
    [Column(IsNullable = false)]
    public string PackageName { get; set; }

    /// <summary> 版本 </summary>
    [Column(IsNullable = false)]
    public string Version { get; set; }

    /// <summary> 存储路径 </summary>
    [Column(IsNullable = false)]
    public string StoragePath { get; set; }

    /// <summary> 哈希（SHA256） </summary>
    [Column(IsNullable = false)]
    public string Hash { get; set; }

    /// <summary> 程序集路径 </summary>
    [Column(IsNullable = false)]
    public string AssemblyPath { get; set; }

    /// <summary> 类型全名 </summary>
    [Column(IsNullable = false)]
    public string TypeFullName { get; set; }

    /// <summary> 方法名 </summary>
    [Column(IsNullable = false)]
    public string MethodName { get; set; }

    /// <summary> 状态（待审核/可用/禁用/加载失败） </summary>
    [Column(IsNullable = false)]
    public int Status { get; set; }

    /// <summary> 备注 </summary>
    [Column(IsNullable = true)]
    public string Remark { get; set; }

    /// <summary> 更新人 </summary>
    [Column(IsNullable = true)]
    public string UpdateBy { get; set; }

    /// <summary> 更新时间 </summary>
    [Column(IsNullable = true)]
    public DateTime? UpdateTime { get; set; }

    /// <summary> 创建人 </summary>
    [Column(IsNullable = true)]
    public string CreateBy { get; set; }

    /// <summary> 创建时间 </summary>
    [Column(IsNullable = false)]
    public DateTime CreateTime { get; set; } = ChinaTimeZoneConverter.Now();

    /// <summary> 插件执行参数 JSON（数组） </summary>
    [Column(IsNullable = false)]
    public string ParamsJson { get; set; } = "[]";

    /// <summary> 任务Job信息 </summary>
    [Navigate(nameof(JobId))]
    public SysJobDetail JobDetail { get; set; }
}
