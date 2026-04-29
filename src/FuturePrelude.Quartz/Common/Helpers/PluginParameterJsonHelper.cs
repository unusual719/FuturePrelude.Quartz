namespace FuturePrelude.Quartz;

/// <summary> 插件参数 JSON 序列化辅助方法 </summary>
internal static class PluginParameterJsonHelper
{
    internal static string Serialize(IEnumerable<PluginParamInput>? parameters)
    {
        return Serialize(parameters?.Select(parameter => new PluginExecutionParameter
        {
            ParamName = parameter.ParamName,
            ParamType = parameter.ParamType,
            ParamValueJson = parameter.ParamValueJson
        }));
    }

    internal static string Serialize(PluginParamInput? parameter)
    {
        if (parameter == null)
        {
            return Serialize(parameters: Array.Empty<PluginExecutionParameter>());
        }

        return Serialize([parameter]);
    }

    internal static string Serialize(IEnumerable<PluginExecutionParameter>? parameters)
    {
        return JsonConvert.SerializeObject(parameters?.ToList() ?? []);
    }

    internal static List<PluginExecutionParameter> DeserializeExecutionParameters(string? paramsJson)
    {
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            return [];
        }

        return JsonConvert.DeserializeObject<List<PluginExecutionParameter>>(paramsJson) ?? [];
    }

    internal static List<PluginParamOutput> DeserializeOutputParameters(string? paramsJson)
    {
        return DeserializeExecutionParameters(paramsJson)
            .Select(parameter => new PluginParamOutput
            {
                ParamName = parameter.ParamName,
                ParamType = parameter.ParamType,
                ParamValueJson = parameter.ParamValueJson
            }).ToList();
    }
}