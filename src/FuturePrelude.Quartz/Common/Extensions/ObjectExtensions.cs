namespace FuturePrelude.Quartz;

/// <summary> ObjectExtensions 拓展类 </summary>
public static class ObjectExtensions
{
    /// <summary> 设置默认 JsonSerializerSettings </summary>
    /// <param name="jsonSerializerSettings"> </param>
    public static JsonSerializerSettings SetJsonSerializerSettings(this JsonSerializerSettings jsonSerializerSettings)
    {
        jsonSerializerSettings.Converters.Add(new NewtonsoftJsonDateTimeJsonConverter());
        jsonSerializerSettings.Converters.Add(new NewtonsoftNullableJsonDateTimeJsonConverter());
        jsonSerializerSettings.Converters.Add(new NewtonsoftJsonLongToStringJsonConverter());
        jsonSerializerSettings.Converters.Add(new NewtonsoftJsonNullableLongToStringJsonConverter());
        // 解决循环引用问题
        jsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

        // 解决 DateTimeOffset 序列化/反序列化问题
        jsonSerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
        jsonSerializerSettings.DateParseHandling = DateParseHandling.None;
        // 小驼峰 jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        jsonSerializerSettings.ContractResolver = null;
        jsonSerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
        jsonSerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";

        return jsonSerializerSettings;
    }

    /// <summary> 获取枚举的描述信息 </summary>
    /// <param name="value"> </param>
    /// <returns> </returns>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString())!;
        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))!;
        return attribute == null ? value.ToString() : attribute.Description;
    }

    /// <summary> 字符串转 Base64 </summary>
    /// <param name="value"> </param>
    /// <returns> </returns>
    public static string ToBase64(this string value)
    {
        byte[] base64 = System.Text.Encoding.Default.GetBytes(value);
        return Convert.ToBase64String(base64);
    }

    /// <summary> 获取触发器类型 </summary>
    public static TriggerType GetTriggerType(this ITrigger trigger)
    {
        if (trigger is ICronTrigger)
            return TriggerType.Cron;
        if (trigger is ISimpleTrigger)
            return TriggerType.Simple;
        if (trigger is IDailyTimeIntervalTrigger)
            return TriggerType.Daily;

        throw new Exception($"ITrigger 解析失败");
    }

    /// <summary> MD5 </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    public static string ToMd5(this string input)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(bytes);

        return Convert.ToHexString(hash).ToLower();
    }

    /// <summary> 将字符串转换为带有 Redis 前缀的 Key </summary>
    /// <param name="key"> </param>
    /// <returns> </returns>
    public static string BuildRedisKey(this string key)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        var options = App.GetOptions<RedisOptions>();
        var prefix = options.InstanceName?.Trim(':');
        return string.IsNullOrWhiteSpace(prefix)
            ? key : $"{prefix}:{key.Trim(':')}";
    }
}
