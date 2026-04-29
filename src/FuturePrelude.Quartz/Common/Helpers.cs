namespace FuturePrelude.Quartz;

/// <summary> 帮助类 </summary>
internal class Helpers
{
    /// <summary> 检查给定的 cron 表达式是否有效 </summary>
    /// <param name="cronExpression"> </param>
    /// <returns> </returns>
    internal static bool IsValidExpression(string cronExpression) => CronExpression.IsValidExpression(cronExpression);
}
