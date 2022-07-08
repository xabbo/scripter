using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;

namespace Xabbo.Scripter.SourceGeneration
{
    [Generator]
    public class GlobalsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            StringBuilder sb = new StringBuilder();




            context.AddSource("G.Send.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}
