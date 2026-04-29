using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

namespace FuturePrelude.Quartz;

internal static class PluginExecutionRuntime
{
    private static readonly ConcurrentDictionary<string, Assembly> AssemblyCache = new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, ResolvedPluginExecutionParameter> ResolveParameters(
        IEnumerable<PluginExecutionParameter>? parameters,
        Assembly assembly)
    {
        return parameters?
            .Select(parameter => new ResolvedPluginExecutionParameter(parameter, ResolveParameterType(parameter.ParamType, assembly)))
            .ToDictionary(parameter => parameter.Parameter.ParamName, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, ResolvedPluginExecutionParameter>(StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryScoreCandidate(
        MethodInfo candidate,
        IReadOnlyDictionary<string, ResolvedPluginExecutionParameter> provided,
        out int score)
    {
        score = 0;
        var methodParams = candidate.GetParameters();

        foreach (var providedParam in provided.Keys)
        {
            if (!methodParams.Any(methodParam => string.Equals(methodParam.Name, providedParam, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        foreach (var methodParam in methodParams)
        {
            if (!provided.TryGetValue(methodParam.Name ?? string.Empty, out var parameter))
            {
                if (methodParam.HasDefaultValue)
                {
                    score += 1;
                    continue;
                }

                return false;
            }

            if (!IsParameterCompatible(methodParam.ParameterType, parameter.ParameterType))
            {
                return false;
            }

            score += methodParam.ParameterType == parameter.ParameterType ? 4 : 2;
        }

        return true;
    }

    private static bool IsParameterCompatible(Type targetType, Type descriptorType)
    {
        return targetType == descriptorType || targetType.IsAssignableFrom(descriptorType);
    }

    private static Type ResolveParameterType(string typeName, Assembly assembly)
    {
        var targetType = assembly.GetType(typeName) ?? Type.GetType(typeName);
        if (targetType == null)
        {
            throw new InvalidOperationException($"无法解析参数类型 {typeName}。");
        }

        return targetType;
    }

    internal static Assembly LoadPluginAssembly(string assemblyPath, string? version = null)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new ArgumentException("插件程序集路径不能为空。", nameof(assemblyPath));
        }

        var fullPath = Path.GetFullPath(assemblyPath);
        var cacheKey = $"{fullPath}|{version ?? string.Empty}";

        return AssemblyCache.GetOrAdd(cacheKey, _ =>
        {
            var loadContext = new PluginAssemblyLoadContext(fullPath, version);
            return loadContext.LoadFromAssemblyPath(fullPath);
        });
    }

    internal static MethodInfo ResolveMethod(
        Type pluginType,
        string methodName,
        IEnumerable<PluginExecutionParameter>? parameters,
        Assembly assembly)
    {
        var resolvedParameters = ResolveParameters(parameters, assembly);

        var candidates = pluginType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.Name == methodName)
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new MissingMethodException($"在 {pluginType.FullName} 中未找到方法 {methodName}。");
        }

        var matches = candidates
            .Select(candidate => new
            {
                Method = candidate,
                Score = TryScoreCandidate(candidate, resolvedParameters, out var score) ? score : (int?)null
            })
            .Where(match => match.Score.HasValue)
            .OrderByDescending(match => match.Score)
            .ToArray();

        if (matches.Length == 1)
        {
            return matches[0].Method;
        }

        if (matches.Length > 1)
        {
            var bestScore = matches[0].Score;
            var bestMatches = matches
                .Where(match => match.Score == bestScore)
                .Select(match => match.Method)
                .ToArray();

            if (bestMatches.Length == 1)
            {
                return bestMatches[0];
            }
        }

        var signature = string.Join(", ", resolvedParameters.Values.Select(parameter => parameter.ParameterType.FullName));
        throw new MissingMethodException($"在 {pluginType.FullName} 中未找到可匹配签名 {methodName}({signature}) 的方法。");
    }

    internal static object?[] BuildParameters(
        MethodInfo method,
        IEnumerable<PluginExecutionParameter>? parameters,
        Assembly assembly)
    {
        var methodParams = method.GetParameters();
        if (methodParams.Length == 0)
        {
            return Array.Empty<object?>();
        }

        if (parameters == null || !parameters.Any())
        {
            if (methodParams.All(parameter => parameter.HasDefaultValue))
            {
                return methodParams.Select(parameter => parameter.DefaultValue).ToArray();
            }

            throw new InvalidOperationException("方法需要参数，但未提供参数配置。");
        }

        var provided = ResolveParameters(parameters, assembly);
        var result = new object?[methodParams.Length];
        var consumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < methodParams.Length; i++)
        {
            var methodParam = methodParams[i];
            if (!provided.TryGetValue(methodParam.Name ?? string.Empty, out var descriptor))
            {
                if (methodParam.HasDefaultValue)
                {
                    result[i] = methodParam.DefaultValue;
                    continue;
                }

                throw new InvalidOperationException($"缺少方法参数 {methodParam.Name} 的配置。");
            }

            if (!IsParameterCompatible(methodParam.ParameterType, descriptor.ParameterType))
            {
                throw new InvalidOperationException($"参数 {descriptor.Parameter.ParamName} 的类型 {descriptor.Parameter.ParamType} 与方法签名不匹配。");
            }

            result[i] = JsonConvert.DeserializeObject(descriptor.Parameter.ParamValueJson, descriptor.ParameterType);
            consumed.Add(descriptor.Parameter.ParamName);
        }

        if (provided.Count != consumed.Count)
        {
            var unexpectedParameters = provided.Keys
                .Except(consumed, StringComparer.OrdinalIgnoreCase);
            throw new InvalidOperationException($"存在未匹配到方法签名的参数：{string.Join(", ", unexpectedParameters)}。");
        }

        return result;
    }

    internal static async Task<object?> InvokeAsync(MethodInfo method, object? instance, object?[] parameters)
    {
        object? result;

        try
        {
            result = method.Invoke(instance, parameters);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }

        if (result is not Task task)
        {
            return result;
        }

        await task.ConfigureAwait(false);

        var taskType = task.GetType();
        if (taskType.IsGenericType)
        {
            return taskType.GetProperty(nameof(Task<object>.Result))?.GetValue(task);
        }

        return null;
    }

    private sealed class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        internal PluginAssemblyLoadContext(string pluginPath, string? version)
            : base($"plugin-{Path.GetFileNameWithoutExtension(pluginPath)}-{version ?? "current"}", isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath == null ? null : LoadFromAssemblyPath(assemblyPath);
        }
    }

    private sealed record ResolvedPluginExecutionParameter(
        PluginExecutionParameter Parameter,
        Type ParameterType);
}