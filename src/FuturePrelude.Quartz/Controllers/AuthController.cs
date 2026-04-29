namespace FuturePrelude.Quartz.Controllers;

/// <summary> 登录服务相关接口 </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary> Auth 授权 </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    [HttpPost]
    [AllowAnonymous]
    [Description("登录")]
    public async Task<AuthOutput> AuthAsync(AuthInput input)
        => await _authService.AuthAsync(input);

    /// <summary> 刷新 Token </summary>
    /// <param name="input"> 包含过期 Access Token 和 Refresh Token 的输入 </param>
    /// <returns> 新的 Access Token 和 Refresh Token </returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [Description("刷新 Token")]
    public async Task<AuthOutput> RefreshTokenAsync(RefreshTokenInput input)
        => await _authService.RefreshTokenAsync(input);
}
