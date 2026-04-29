using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace FuturePrelude.Quartz.Core;

/// <summary> JWT 加解密 </summary>
public class JWTEncryption
{
    /// <summary> 日期类型的 Claim 类型 </summary>
    private static readonly string[] DateTypeClaimTypes = [JwtRegisteredClaimNames.Iat, JwtRegisteredClaimNames.Nbf, JwtRegisteredClaimNames.Exp];

    /// <summary> 组合 Claims 负荷 </summary>
    /// <param name="payload"> </param>
    /// <param name="expiredTime"> 过期时间，单位：分钟 </param>
    /// <returns> </returns>
    private static (IDictionary<string, object> Payload, JWTOptions JWTSettings) CombinePayload(IDictionary<string, object> payload, long? expiredTime = null)
    {
        var jwtSettings = App.GetOptions<JWTOptions>();
        var datetimeOffset = DateTimeOffset.UtcNow;

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Iat))
        {
            payload.Add(JwtRegisteredClaimNames.Iat, datetimeOffset.ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Nbf))
        {
            payload.Add(JwtRegisteredClaimNames.Nbf, datetimeOffset.ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Exp))
        {
            var minute = expiredTime ?? jwtSettings?.Expiration ?? 20;
            payload.Add(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(minute).ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Iss))
        {
            payload.Add(JwtRegisteredClaimNames.Iss, jwtSettings?.Issuer);
        }

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Aud))
        {
            payload.Add(JwtRegisteredClaimNames.Aud, jwtSettings?.Audience);
        }

        return (payload, jwtSettings);
    }

    /// <summary> 生成刷新 Token </summary>
    /// <param name="accessToken"> </param>
    /// <param name="expiredTime"> 刷新 Token 有效期（分钟），最大支持 13 年 </param>
    /// <returns> </returns>
    public static string GenerateRefreshToken(string accessToken, int expiredTime = 43200)
    {
        // 分割Token
        var tokenParagraphs = accessToken.Split('.', StringSplitOptions.RemoveEmptyEntries);

        var s = RandomNumberGenerator.GetInt32(10, tokenParagraphs[1].Length / 2 + 2);
        var l = RandomNumberGenerator.GetInt32(3, 13);

        var payload = new Dictionary<string, object>
            {
                { "f",tokenParagraphs[0] },
                { "e",tokenParagraphs[2] },
                { "s",s },
                { "l",l },
                { "k",tokenParagraphs[1].Substring(s,l) }
            };

        return Encrypt(payload, expiredTime);
    }

    /// <summary> 生成验证参数 </summary>
    /// <param name="jwtSettings"> </param>
    /// <returns> </returns>
    public static TokenValidationParameters CreateValidationParameters(JWTOptions jwtSettings)
    {
        return new TokenValidationParameters
        {
            // 验证签发方密钥
            ValidateIssuerSigningKey = true,
            // 签发方密钥
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

            // 验证签发方
            ValidateIssuer = true,
            // 设置签发方
            ValidIssuer = jwtSettings.Issuer,

            // 验证签收方
            ValidateAudience = true,
            // 设置接收方
            ValidAudience = jwtSettings.Audience,

            // 验证生存期
            ValidateLifetime = true,
            // 过期时间容错值
            ClockSkew = TimeSpan.FromSeconds(10),
            // 验证过期时间，设置 false 永不过期
            RequireExpirationTime = jwtSettings.RequireExpirationTime
        };
    }

    /// <summary> 验证 Token </summary>
    /// <param name="accessToken"> </param>
    /// <returns> </returns>
    public static async Task<(bool IsValid, JsonWebToken Token, TokenValidationResult validationResult)> Validate(string accessToken)
    {
        if (accessToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            accessToken = accessToken.Substring("Bearer ".Length).Trim();
        }

        var jwtSettings = App.GetOptions<JWTOptions>();
        if (jwtSettings == null) return (false, null, null);

        // 创建验证参数
        var tokenValidationParameters = CreateValidationParameters(jwtSettings);

        // 验证 Token
        var tokenHandler = new JsonWebTokenHandler();
        try
        {
            var tokenValidationResult = await tokenHandler.ValidateTokenAsync(accessToken, tokenValidationParameters);
            if (!tokenValidationResult.IsValid)
            {
                return (false, null, tokenValidationResult);
            }

            var jsonWebToken = tokenValidationResult.SecurityToken as JsonWebToken;
            return (true, jsonWebToken, tokenValidationResult);
        }
        catch
        {
            return (false, null, null);
        }
    }

    /// <summary> 生成 Token </summary>
    /// <param name="payload"> </param>
    /// <param name="expiredTime"> 过期时间（分钟），最大支持 13 年 </param>
    /// <returns> </returns>
    public static string Encrypt(IDictionary<string, object> payload, long? expiredTime = null)
    {
        var (Payload, JWTSettings) = CombinePayload(payload, expiredTime);

        var stringPayload = payload is JwtPayload jwtPayload
            ? jwtPayload.SerializeToJson()
            : JsonConvert.SerializeObject(payload);

        SigningCredentials credentials = null;

        if (!string.IsNullOrWhiteSpace(JWTSettings.SecretKey))
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTSettings.SecretKey));
            credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        }

        var tokenHandler = new JsonWebTokenHandler();
        return $"Bearer {(credentials == null
            ? tokenHandler.CreateToken(stringPayload)
            : tokenHandler.CreateToken(stringPayload, credentials))}";
    }

    /// <summary> 读取 Token，不含验证 </summary>
    /// <param name="accessToken"> </param>
    /// <returns> </returns>
    public static JwtSecurityToken SecurityReadJwtToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || !accessToken.Contains('.'))
        {
            return default;
        }

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        if (!jwtSecurityTokenHandler.CanReadToken(accessToken))
        {
            return default;
        }

        return jwtSecurityTokenHandler.ReadJwtToken(accessToken);
    }

    /// <summary> 通过过期Token 和 刷新Token 换取新的 Token </summary>
    /// <param name="expiredToken"> </param>
    /// <param name="refreshToken"> </param>
    /// <param name="expiredTime"> 过期时间（分钟），最大支持 13 年 </param>
    /// <param name="clockSkew"> 刷新token容差值，秒做单位 </param>
    /// <returns> </returns>
    public static async Task<string> Exchange(string expiredToken
        , string refreshToken
        , long? expiredTime = null
        , long clockSkew = 5)
    {
        // 交换刷新Token 必须原Token 已过期
        var (_isValid, _, _) = await Validate(expiredToken);
        if (_isValid) return default;

        // 判断刷新Token 是否过期
        var (isValid, refreshTokenObj, _) = await Validate(refreshToken);
        if (!isValid) return default;

        // 解析 HttpContext
        using var scope = App.ServiceProvider.CreateScope();

        // 判断这个刷新Token 是否已刷新过
        var blacklistRefreshKey = "BLACKLIST_REFRESH_TOKEN:" + refreshToken;
        var distributedCache = scope.ServiceProvider.GetService<IDatabase>();

        // 处理token并发容错问题
        var nowTime = DateTimeOffset.UtcNow;
        var cachedValue = await distributedCache?.StringGetAsync(blacklistRefreshKey);
        var isRefresh = !string.IsNullOrWhiteSpace(cachedValue);    // 判断是否刷新过
        if (isRefresh)
        {
            var refreshTime = new DateTimeOffset(long.Parse(cachedValue), TimeSpan.Zero);
            // 处理并发时容差值
            if ((nowTime - refreshTime).TotalSeconds > clockSkew) return default;
        }

        // 分割过期Token
        var tokenParagraphs = expiredToken.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (tokenParagraphs.Length < 3) return default;

        // 判断各个部分是否匹配
        var token = refreshTokenObj.GetPayloadValue<string>("f");
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring("Bearer ".Length).Trim();
        }

        if (!token.Equals(tokenParagraphs[0])) return default;
        if (!refreshTokenObj.GetPayloadValue<string>("e").Equals(tokenParagraphs[2])) return default;
        if (!tokenParagraphs[1].Substring(refreshTokenObj.GetPayloadValue<int>("s"), refreshTokenObj.GetPayloadValue<int>("l")).Equals(refreshTokenObj.GetPayloadValue<string>("k"))) return default;

        // 获取过期 Token 的存储信息
        var jwtSecurityToken = SecurityReadJwtToken(expiredToken);
        if (jwtSecurityToken is null)
        {
            return default;
        }
        var payload = jwtSecurityToken.Payload;

        // 移除 Iat，Nbf，Exp
        foreach (var innerKey in DateTypeClaimTypes)
        {
            if (!payload.ContainsKey(innerKey)) continue;

            payload.Remove(innerKey);
        }

        // 交换成功后登记刷新Token，标记失效
        if (!isRefresh)
        {
            // 解析 exp（秒）
            var exp = refreshTokenObj.GetPayloadValue<long>(JwtRegisteredClaimNames.Exp);

            // 过期时间点
            var expireAt = DateTimeOffset.FromUnixTimeSeconds(exp);

            // 剩余时间（TTL）
            var ttl = expireAt - DateTimeOffset.UtcNow;

            // 防止负数（token 已过期）
            if (ttl <= TimeSpan.Zero)
            {
                ttl = TimeSpan.FromSeconds(1);
            }

            await distributedCache?.StringSetAsync(
                blacklistRefreshKey,
                nowTime.Ticks.ToString(),
                ttl);
        }

        return Encrypt(payload, expiredTime);
    }
}