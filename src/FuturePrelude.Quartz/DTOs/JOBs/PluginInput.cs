namespace FuturePrelude.Quartz;

/// <summary> 插件元数据入参 </summary>
public class PluginInput
{
    public long Id { get; set; }
    public string PackageName { get; set; }
    public string Version { get; set; }
    public string StoragePath { get; set; }
    public string Hash { get; set; }
    public string AssemblyPath { get; set; }
    public string TypeFullName { get; set; }
    public string MethodName { get; set; }
    public int Status { get; set; }
    public string Remark { get; set; }
    public PluginParamInput Param { get; set; }
}

/// <summary> 插件参数入参 </summary>
public class PluginParamInput
{
    public long Id { get; set; }
    public string ParamName { get; set; }
    public string ParamType { get; set; }
    public string ParamValueJson { get; set; }
}