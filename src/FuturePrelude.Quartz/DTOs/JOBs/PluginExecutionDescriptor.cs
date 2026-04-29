using System.Text.Json.Serialization;

namespace FuturePrelude.Quartz;

/// <summary> 插件执行描述 </summary>
public class PluginExecutionDescriptor
{
    public string AssemblyPath { get; set; }

    public string TypeFullName { get; set; }

    public string MethodName { get; set; }

    public string PackageName { get; set; }

    public string Version { get; set; }

    [JsonPropertyName("parameters")]
    public List<PluginExecutionParameter>? Parameters { get; set; } = new();
}

/// <summary> 插件方法参数描述 </summary>
public class PluginExecutionParameter
{
    public string ParamName { get; set; }

    public string ParamType { get; set; }

    public string ParamValueJson { get; set; }
}