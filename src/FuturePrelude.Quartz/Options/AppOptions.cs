namespace FuturePrelude.Quartz;

/// <summary> 应用配置信息 </summary>
public class AppOptions
{
    /// <summary> SectionName Key </summary>
    public const string SectionName = "AppSettings";

    /// <summary> 登录 Token </summary>
    public string AuthToken { get; set; }
}
