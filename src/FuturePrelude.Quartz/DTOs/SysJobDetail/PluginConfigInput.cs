namespace FuturePrelude.Quartz;

/// <summary> 插件配置入参（使用 DTOs/JOBs/PluginInput.cs 中的 PluginParamInput） </summary>
public class PluginConfigInput
{
    /// <summary> 包名 </summary>
    public string PackageName { get; set; }

    /// <summary> 版本 </summary>
    public string Version { get; set; }

    /// <summary> 存储路径 </summary>
    public string StoragePath { get; set; }

    /// <summary> 哈希（SHA256） </summary>
    public string Hash { get; set; }

    /// <summary> 程序集路径 </summary>
    public string AssemblyPath { get; set; }

    /// <summary> 类型全名 </summary>
    public string TypeFullName { get; set; }

    /// <summary> 方法名 </summary>
    public string MethodName { get; set; }

    /// <summary> 状态 </summary>
    public int Status { get; set; }

    /// <summary> 备注 </summary>
    public string Remark { get; set; }

    /// <summary> 参数列表（使用 DTOs/JOBs/PluginInput.cs 中的 PluginParamInput） </summary>
    public List<PluginParamInput> Params { get; set; }
}