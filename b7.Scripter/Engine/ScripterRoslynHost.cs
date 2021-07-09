using b7.Scripter.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynPad.Roslyn;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace b7.Scripter.Engine
{
    public class ScripterRoslynHost : RoslynHost
    {
        public ScripterRoslynHost(IEnumerable<Assembly>? additionalAssemblies = null,
            RoslynHostReferences? references = null, ImmutableArray<string>? disabledDiagnostics = null)
            : base(additionalAssemblies, references, disabledDiagnostics)
        { }

        protected override Project CreateProject(Solution solution, DocumentCreationArgs args,
            CompilationOptions compilationOptions, Project? previousProject = null)
        {
            string name = args.Name ?? "Program";
            ProjectId id = ProjectId.CreateNewId(name);

            CSharpParseOptions parseOptions = new(
                kind: SourceCodeKind.Script,
                languageVersion: LanguageVersion.CSharp8
            );
            

            compilationOptions = compilationOptions
                .WithScriptClassName(name);

            if (compilationOptions is CSharpCompilationOptions csharpCompilationOptions)
            {
                compilationOptions = csharpCompilationOptions
                    .WithNullableContextOptions(NullableContextOptions.Disable);
            }

            solution = solution.AddProject(ProjectInfo.Create(
                id,
                VersionStamp.Create(),
                name,
                name,
                LanguageNames.CSharp,
                isSubmission: true,
                parseOptions: parseOptions,
                hostObjectType: typeof(G),
                compilationOptions: compilationOptions,
                metadataReferences: previousProject != null ? ImmutableArray<MetadataReference>.Empty : DefaultReferences,
                projectReferences: previousProject != null ? new[] { new ProjectReference(previousProject.Id) } : null
            ));

            Project project = solution.GetProject(id)
                ?? throw new InvalidOperationException("Failed to get project.");

            return project;
        }
    }
}
