namespace FuturePrelude.Quartz;

/// <summary> HTTP认证类型 </summary>
public enum AuthType
{
    [Description("无认证")]
    None = 0,

    [Description("基础认证")]
    Basic = 1,

    [Description("Bearer Token")]
    BearerToken = 2,

    [Description("API Key")]
    ApiKey = 3
}