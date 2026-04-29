using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(FuturePrelude.Quartz.HostingStartup))]

namespace FuturePrelude.Quartz;

/// <summary> 配置程序启动时自动注入 </summary>
public sealed class HostingStartup : IHostingStartup
{
    /// <summary> 配置应用启动 </summary>
    /// <param name="builder"> </param>
    public void Configure(IWebHostBuilder builder)
    {
        App.ConfigureApplication(builder);
    }
}
