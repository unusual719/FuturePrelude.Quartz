namespace FuturePrelude.Quartz;

/// <summary> 返回参数据 </summary>
public class AuthOutput
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string TokenType { get; set; }
    public long ExpiresIn { get; set; }
}