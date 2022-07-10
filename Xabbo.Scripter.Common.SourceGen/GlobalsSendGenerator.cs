using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Xabbo.Scripter.SourceGeneration
{
    /// <summary>
    /// Generates generic Send methods up to a specified number of parameters.
    /// </summary>
    [Generator]
    public class GlobalsSendGenerator : ISourceGenerator
    {
        const int MaxParams = 20;

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            string[] genericParamNames = Enumerable.Range(1, MaxParams).Select(i => $"T{i}").ToArray();
            string[] paramNames = Enumerable.Range(1, MaxParams).Select(i => $"value{i}").ToArray();

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using Xabbo.Messages;");
            sb.AppendLine("using Xabbo.Interceptor;");

            sb.AppendLine("namespace Xabbo.Scripter.Scripting;");
            sb.AppendLine();

            sb.AppendLine("public partial class G");
            sb.AppendLine("{");

            for (int n = 0; n <= MaxParams; n++)
            {
                if (n > 0)
                    sb.AppendLine();

                sb.Append($"\t/// <summary>Sends a message with the specified header");
                if (n > 0)
                    sb.Append("and values");
                sb.AppendLine(".</summary>");
                sb.Append("\tpublic void Send");

                if (n > 0)
                {
                    sb.Append('<');
                    sb.Append(string.Join(", ", genericParamNames.Take(n)));
                    sb.Append('>');
                }

                sb.Append("(Header header");
                if (n > 0)
                {
                    sb.Append(", ");
                    sb.Append(string.Join(", ", genericParamNames.Take(n).Zip(paramNames, (a, b) => $"{a} {b}")));
                }

                sb.AppendLine(")");
                sb.Append("\t\t=> Interceptor.Send(header");
                
                if (n > 0)
                {
                    sb.Append(", ");
                    sb.Append(string.Join(", ", paramNames.Take(n)));
                }

                sb.AppendLine(");");
            }

            sb.AppendLine("}");

            context.AddSource("G.Send.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}
