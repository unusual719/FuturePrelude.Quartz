namespace FuturePrelude.Quartz;

/// <summary> HTTP 请求方式枚举 </summary>
public enum HttpRequestMethod
{
    /// <summary> GET </summary>
    [Description("GET")]
    GET,

    /// <summary> POST </summary>
    [Description("POST")]
    POST,

    /// <summary> PUT </summary>
    [Description("PUT")]
    PUT,

    /// <summary> DELETE </summary>
    [Description("DELETE")]
    DELETE,

    /// <summary> PATCH </summary>
    [Description("PATCH")]
    PATCH
}