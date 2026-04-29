using Microsoft.Extensions.Caching.Memory;

namespace FuturePrelude.Quartz.Services;

/// <summary> 仪表盘概览快照缓存 </summary>
internal static class DashboardOverviewCache
{
    private const string CacheKey = "dashboard:overview";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(1);
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    /// <summary> 获取缓存中的概览快照；不存在时创建并缓存 </summary>
    /// <param name="factory"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    internal static async Task<DashboardOverviewOutput> GetOrCreateAsync(
        Func<CancellationToken, Task<DashboardOverviewOutput>> factory,
        CancellationToken cancellationToken)
    {
        if (Cache.TryGetValue<DashboardOverviewOutput>(CacheKey, out var cached)
            && cached != null)
        {
            return cached;
        }

        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            if (Cache.TryGetValue<DashboardOverviewOutput>(CacheKey, out cached)
                && cached != null)
            {
                return cached;
            }

            var overview = await factory(cancellationToken);
            Cache.Set(CacheKey, overview, CacheDuration);
            return overview;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    /// <summary> 主动失效概览快照缓存 </summary>
    internal static void Invalidate() => Cache.Remove(CacheKey);
}
