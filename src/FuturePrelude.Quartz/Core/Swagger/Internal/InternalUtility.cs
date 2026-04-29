using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FuturePrelude.Quartz.Core;

/// <summary> 内部帮助工具类 </summary>
internal partial class InternalUtility
{
    /// <summary> 生成 Inheritdoc cref 属性 </summary>
    /// <param name="xmlDoc"> </param>
    /// <param name="memberName"> </param>
    /// <param name="className"> </param>
    /// <returns> </returns>
    private static string GenerateInheritdocCref(XDocument xmlDoc, string memberName, string className)
    {
        var classElement = xmlDoc
            .XPathSelectElements($"/doc/members/member[@name='{"T" + className}' and @_ref_]")
            .FirstOrDefault();
        if (classElement == null)
            return default;

        var _ref_value = classElement.Attribute("_ref_")?.Value;
        if (_ref_value == null)
            return default;

        var classCrefValue = _ref_value[_ref_value.IndexOf(":")..];
        return memberName.Replace(className, classCrefValue);
    }

    /// <summary> 转换 </summary>
    /// <param name="reader"> </param>
    /// <returns> </returns>
    internal static DateTime ConvertToDateTime(ref JsonReader reader)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            return ConvertToDateTime(JValue.ReadFrom(reader).Value<long>());
        }

        var stringValue = JValue.ReadFrom(reader).Value<string>();

        // 处理时间戳自动转换
        if (long.TryParse(stringValue, out var longValue2))
        {
            return ConvertToDateTime(longValue2);
        }

        return Convert.ToDateTime(stringValue);
    }

    /// <summary> 加载注释描述文件 </summary>
    /// <param name="swaggerGenOptions"> Swagger 生成器配置 </param>
    internal static void LoadXmlComments(SwaggerGenOptions swaggerGenOptions)
    {
        var xmlComments = Array.Empty<string>();
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        xmlComments = dir.EnumerateFiles("*.xml").Select(c => c.FullName).ToArray();

        var members = new Dictionary<string, XElement>();

        // 显式继承的注释
        var regex = new Regex(@"[A-Z]:[a-zA-Z0-9_@\.]+");
        // 隐式继承的注释
        var regex2 = new Regex(@"[A-Z]:[a-zA-Z0-9_@\.]+\.");

        // 支持注释完整特性，包括 inheritdoc 注释语法
        foreach (var xmlComment in xmlComments)
        {
            var assemblyXmlName = xmlComment.EndsWith(".xml") ? xmlComment : $"{xmlComment}.xml";
            var assemblyXmlPath = Path.Combine(AppContext.BaseDirectory, assemblyXmlName);

            if (File.Exists(assemblyXmlPath))
            {
                var xmlDoc = XDocument.Load(assemblyXmlPath);

                // 查找所有 member[name] 节点，且不包含
                // <inheritdoc />
                // 节点的注释
                var memberNotInheritdocElementList = xmlDoc.XPathSelectElements(
                    "/doc/members/member[@name and not(inheritdoc)]"
                );

                foreach (var memberElement in memberNotInheritdocElementList)
                {
                    members.TryAdd(memberElement.Attribute("name").Value, memberElement);
                }

                // 查找所有 member[name] 含有
                // <inheritdoc />
                // 节点的注释
                var memberElementList = xmlDoc.XPathSelectElements(
                    "/doc/members/member[inheritdoc]"
                );
                foreach (var memberElement in memberElementList)
                {
                    var inheritdocElement = memberElement.Element("inheritdoc");
                    var cref = inheritdocElement.Attribute("cref");
                    var value = cref?.Value;

                    // 处理不带 cref 的 inheritdoc 注释
                    if (value == null)
                    {
                        var memberName = inheritdocElement.Parent.Attribute("name").Value;

                        // 处理隐式实现接口的注释 注释格式：M:Furion.Application.TestInheritdoc.Furion#Application#ITestInheritdoc#Abc(System.String) 匹配格式：[A-Z]:[a-zA-Z0-9_@\.]+\. 处理逻辑：直接替换匹配为空，然后讲 # 替换为 . 查找即可
                        if (memberName.Contains('#'))
                        {
                            value =
                                $"{memberName[..2]}{regex2.Replace(memberName, "").Replace('#', '.')}";
                        }
                        // 处理带参数的注释 注释格式：M:Furion.Application.TestInheritdoc.WithParams(System.String) 匹配格式：[A-Z]:[a-zA-Z0-9_@\.]+ 处理逻辑：匹配出不带参数的部分，然后获取类型命名空间，最后调用 GenerateInheritdocCref 进行生成
                        else if (memberName.Contains('('))
                        {
                            var noParamsClassName = regex.Match(memberName).Value;
                            var className = noParamsClassName[
                                noParamsClassName.IndexOf(":")..noParamsClassName.LastIndexOf(".")
                            ];
                            value = GenerateInheritdocCref(xmlDoc, memberName, className);
                        }
                        // 处理不带参数的注释 注释格式：M:Furion.Application.TestInheritdoc.WithParams 匹配格式：无 处理逻辑：获取类型命名空间，最后调用 GenerateInheritdocCref 进行生成
                        else
                        {
                            var className = memberName[
                                memberName.IndexOf(":")..memberName.LastIndexOf(".")
                            ];
                            value = GenerateInheritdocCref(xmlDoc, memberName, className);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    // 处理带 cref 的 inheritdoc 注释
                    if (members.TryGetValue(value, out var realDocMember))
                    {
                        memberElement.SetAttributeValue("_ref_", value);
                        inheritdocElement.Parent.ReplaceNodes(realDocMember.Nodes());
                    }
                }

                swaggerGenOptions.IncludeXmlComments(
                    () => new XPathDocument(xmlDoc.CreateReader()),
                    true
                );
            }
        }
    }

    /// <summary> 将时间戳转换为 DateTime </summary>
    /// <param name="timestamp"> </param>
    /// <returns> </returns>
    public static DateTime ConvertToDateTime(long timestamp)
    {
        var timeStampDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var digitCount = (int)Math.Floor(Math.Log10(timestamp) + 1);

        if (digitCount != 13 && digitCount != 10)
        {
            throw new ArgumentException("Data is not a valid timestamp format.");
        }

        return (digitCount == 13
            ? timeStampDateTime.AddMilliseconds(timestamp)  // 13 位时间戳
            : timeStampDateTime.AddSeconds(timestamp)).ToLocalTime();   // 10 位时间戳
    }
}
