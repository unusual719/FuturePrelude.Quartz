using Microsoft.Extensions.Options;

namespace FuturePrelude.Quartz;

/// <summary> 登录服务定义实现 </summary>
public class AuthService : IAuthService
{
    private readonly AppOptions _options;

    public AuthService(IOptions<AppOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<AuthOutput> AuthAsync(AuthInput input)
    {
        var jwtOptions = App.GetOptions<JWTOptions>();
        if (input.Token.Trim() == _options.AuthToken.Trim())
        {
            var token = JWTEncryption.Encrypt(new Dictionary<string, object>
            {
                { "sub", "quartz" },
                { "role", "admin" },
                { "token", input.Token.ToMd5() }
            }, expiredTime: jwtOptions.Expiration);

            var refreshToken = JWTEncryption.GenerateRefreshToken(token, 43200);

            return Task.FromResult(new AuthOutput
            {
                AccessToken = token,
                ExpiresIn = jwtOptions.Expiration,
                RefreshToken = refreshToken,
                TokenType = "Bearer"
            });
        }

        throw new Exception("登录失败，无效访问令牌");
    }

    /// <inheritdoc />
    public async Task<AuthOutput> RefreshTokenAsync(RefreshTokenInput input)
    {
        var jwtOptions = App.GetOptions<JWTOptions>();

        // 使用过期的 Token 和刷新 Token 换取新的 Token
        var newAccessToken = await JWTEncryption.Exchange(input.AccessToken, input.RefreshToken, jwtOptions.Expiration);
        if (string.IsNullOrEmpty(newAccessToken))
        {
            throw new Exception("Token 刷新失败，请重新登录");
        }

        // 移除 "Bearer " 前缀
        newAccessToken = newAccessToken.Replace("Bearer ", "");

        // 生成新的刷新 Token
        var newRefreshToken = JWTEncryption.GenerateRefreshToken(newAccessToken);

        return new AuthOutput
        {
            AccessToken = newAccessToken,
            ExpiresIn = jwtOptions.Expiration,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer"
        };
    }
}
