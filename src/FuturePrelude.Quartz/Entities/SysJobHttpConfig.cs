using System.ComponentModel.DataAnnotations;

namespace FuturePrelude.Quartz;

/// <summary> 任务详情Http配置信息 </summary>
[Table(Name = "SysJobHttpConfig")]
[Index("idx_sysjobhttpconfig_jobid", nameof(JobId), true)]
[Index("idx_sysjobhttpconfig_isdeleted", nameof(IsDeleted))]
[Index("idx_sysjobhttpconfig_jobid_deleted", nameof(JobId) + "," + nameof(IsDeleted))]
[Index("idx_sysjobhttpconfig_authtype", nameof(AuthType))]
[Index("idx_sysjobhttpconfig_createtime", nameof(CreateTime) + " desc")]
public class SysJobHttpConfig
{
    /// <summary> Id 主键 </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary> 任务ID（SysJobDetail.Id） </summary>
    [Column(IsNullable = false)]
    public long JobId { get; set; }

    /// <summary> Http 请求方式（GET, POST, PUT, DELETE, PATCH） </summary>
    [Column(IsNullable = false)]
    public string RequestMethod { get; set; }

    /// <summary> Content-Type </summary>
    [Column(IsNullable = false)]
    public string ContentType { get; set; } = System.Net.Mime.MediaTypeNames.Application.Json;

    /// <summary> Http 请求地址 </summary>
    [Column(IsNullable = false)]
    public string RequestUrl { get; set; }

    /// <summary> Http 请求头 </summary>
    [Column(IsNullable = true)]
    public string RequestHeaders { get; set; }

    /// <summary> Http 请求参数 </summary>
    [Column(IsNullable = true)]
    public string RequestBody { get; set; }

    /// <summary> 超时时间(秒) </summary>
    [Column(IsNullable = false)]
    public int TimeoutSeconds { get; set; } = (int)TimeSpan.FromHours(1).TotalSeconds;

    /// <summary> 认证类型（None/Basic/BearerToken） </summary>
    [Column(IsNullable = false)]
    public AuthType AuthType { get; set; } = AuthType.None;

    /// <summary> 认证凭证（JSON存储） </summary>
    [Column(IsNullable = true)]
    [MaxLength(-1)]
    public string? AuthCredentials { get; set; }

    /// <summary> 任务创建人 </summary>
    [Column(IsNullable = true)]
    public string CreateBy { get; set; }

    /// <summary> 创建时间 </summary>
    [Column(IsNullable = false)]
    public DateTime CreateTime { get; set; } = ChinaTimeZoneConverter.Now();

    /// <summary> 软删除标记 </summary>
    [Column(IsNullable = false)]
    public bool IsDeleted { get; set; } = false;

    /// <summary> 任务Job信息 </summary>
    [Navigate(nameof(JobId))]
    public SysJobDetail JobDetail { get; set; }
}
