namespace FuturePrelude.Quartz.Controllers;

/// <summary> Development 开发测试服务 </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DevelopmentController : ControllerBase
{
    private readonly ILogger<DevelopmentController> _logger;

    public DevelopmentController(ILogger<DevelopmentController> logger)
    {
        this._logger = logger;
    }

    /// <summary> 开发测试数据 </summary>
    /// <returns> </returns>
    [HttpPost("SyncData")]
    public async Task<ActionResult> SyncDataAsync()
    {
        await Task.Delay(1000 * 30);
        _logger.LogInformation($"【DevelopmentController】开发测试，数据同步成功...");
        return Ok(new {
            Success = true,
            Message = "操作成功"
        });
    }
}
