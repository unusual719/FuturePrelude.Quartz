namespace FuturePrelude.Quartz.Core;

/// <summary> JWT 配置选项 </summary>
public class JWTOptions
{
    /// <summary> SectionName Key </summary>
    public const string SectionName = "JWTSettings";

    /// <summary> 密钥 </summary>
    public string SecretKey { get; set; }

    /// <summary> 签发者 </summary>
    public string Issuer { get; set; }

    /// <summary> 签收者 </summary>
    public string Audience { get; set; }

    /// <summary> 过期时间（分钟） </summary>
    public long Expiration { get; set; }

    /// <summary> 验证过期时间，设置 false 永不过期 </summary>
    public bool RequireExpirationTime { get; set; } = true;
}