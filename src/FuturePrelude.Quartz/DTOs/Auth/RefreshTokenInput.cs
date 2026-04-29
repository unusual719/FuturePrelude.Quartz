namespace FuturePrelude.Quartz;

/// <summary> 刷新 Token 请求输入 </summary>
public class RefreshTokenInput
{
    /// <summary> 过期的 Access Token </summary>
    public string AccessToken { get; set; }

    /// <summary> 刷新 Token </summary>
    public string RefreshToken { get; set; }
}