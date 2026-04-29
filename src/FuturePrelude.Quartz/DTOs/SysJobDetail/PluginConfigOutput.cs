namespace FuturePrelude.Quartz;

/// <summary> 插件配置出参 </summary>
public class PluginConfigOutput
{
    /// <summary> 主键ID </summary>
    public long Id { get; set; }

    /// <summary> 任务ID </summary>
    public long JobId { get; set; }

    /// <summary> 包名 </summary>
    public string PackageName { get; set; }

    /// <summary> 版本 </summary>
    public string Version { get; set; }

    /// <summary> 存储路径 </summary>
    public string StoragePath { get; set; }

    /// <summary> 哈希 </summary>
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

    /// <summary> 更新时间 </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary> 创建时间 </summary>
    public DateTime CreateTime { get; set; }

    /// <summary> 参数列表 </summary>
    public List<PluginParamOutput> Params { get; set; }
}

/// <summary> 插件参数出参 </summary>
public class PluginParamOutput
{
    /// <summary> 主键ID </summary>
    public long Id { get; set; }

    /// <summary> 参数名称 </summary>
    public string ParamName { get; set; }

    /// <summary> 参数类型 </summary>
    public string ParamType { get; set; }

    /// <summary> 参数值（JSON） </summary>
    public string ParamValueJson { get; set; }
}