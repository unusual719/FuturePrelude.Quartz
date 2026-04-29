namespace FuturePrelude.Quartz;

/// <summary> 登录服务定义 </summary>
public interface IAuthService
{
    /// <summary> Auth 授权 </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    Task<AuthOutput> AuthAsync(AuthInput input);

    /// <summary> 刷新 Token </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    Task<AuthOutput> RefreshTokenAsync(RefreshTokenInput input);
}
