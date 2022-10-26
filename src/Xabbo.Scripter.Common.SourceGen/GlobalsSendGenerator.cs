using System.IO;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Scriban;

namespace Xabbo.Scripter.SourceGeneration;

/// <summary>
/// Generates generic Send methods up to a specified number of parameters.
/// </summary>
[Generator]
public class GlobalsSendGenerator : ISourceGenerator
{
    const int MaxParams = 20;

    private static string GetTemplate(string resourceName)
    {
        resourceName = $"Xabbo.Scripter.Common.SourceGen.Templates.{resourceName}";
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        using StreamReader sr = new(stream);
        return sr.ReadToEnd();
    }

    private static SourceText RenderTemplate(string resourceName, object? model = null)
    {
        string renderedTemplate = Template.Parse(GetTemplate(resourceName)).Render(model);
        return SourceText.From(renderedTemplate, Encoding.UTF8);
    }

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        context.AddSource("G.Send.g.cs", RenderTemplate("G.Send.sbncs", new { MaxParams }));
    }
}
