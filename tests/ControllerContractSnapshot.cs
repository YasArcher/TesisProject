using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

internal static class ControllerContractSnapshot
{
    private const string FileName = "tests/controller-contracts-before-cleanup.json";
    public static string Capture(Type type)
    {
        string Attributes(MemberInfo member) => string.Join(";", member.GetCustomAttributesData()
            .Where(a => a.AttributeType.Namespace?.StartsWith("Microsoft.AspNetCore") == true && a.AttributeType != typeof(NonControllerAttribute))
            .Select(a => a.ToString()).Order());
        return JsonSerializer.Serialize(new
        {
            attributes = Attributes(type),
            actions = type.GetMethods().Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any()).OrderBy(m => m.Name)
                .Select(m => new { m.Name, returns = m.ReturnType.ToString(), attributes = Attributes(m),
                    parameters = m.GetParameters().Select(p => new { p.Name, type = p.ParameterType.ToString(), defaultValue = p.DefaultValue?.ToString(),
                        attributes = string.Join(";",p.GetCustomAttributesData().Where(a => a.AttributeType.Namespace?.StartsWith("Microsoft.AspNetCore") == true).Select(a => a.ToString()).Order()) }) })
        });
    }
    public static void Export(Assembly assembly) => File.WriteAllText(FileName, JsonSerializer.Serialize(
        assembly.GetTypes().Where(t => t.Namespace == "tesisproject.backend.Controllers.Unified" && !t.IsAbstract && t.Name.EndsWith("Controller"))
        .ToDictionary(t => t.Name, Capture), new JsonSerializerOptions { WriteIndented = true }));
    public static bool Matches(Type type) => JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(FileName))![type.Name] == Capture(type);
}
