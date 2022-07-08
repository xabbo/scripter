using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;

using RoslynPad.Roslyn;

using Xabbo.Scripter.Scripting;

namespace Xabbo.Scripter.Engine
{
    public class ScripterRoslynHost : RoslynHost
    {
        private readonly ScriptOptions _scriptOptions;

        public ScripterRoslynHost(ScriptOptions scriptOptions,
            IEnumerable<Assembly>? additionalAssemblies = null,
            RoslynHostReferences? references = null,
            ImmutableArray<string>? disabledDiagnostics = null)
            : base(additionalAssemblies, references, disabledDiagnostics)
        {
            _scriptOptions = scriptOptions;
        }

        protected override Project CreateProject(Solution solution, DocumentCreationArgs args,
            CompilationOptions compilationOptions, Project? previousProject = null)
        {
            string name = args.Name ?? "Program";
            ProjectId id = ProjectId.CreateNewId(name);
            CSharpParseOptions parseOptions = new(
                kind: SourceCodeKind.Script,
                languageVersion: LanguageVersion.Latest
            );
            
            compilationOptions = compilationOptions
                .WithScriptClassName(name)
                .WithSourceReferenceResolver(_scriptOptions.SourceResolver);

            if (compilationOptions is CSharpCompilationOptions csharpCompilationOptions)
            {
                compilationOptions = csharpCompilationOptions
                    .WithNullableContextOptions(NullableContextOptions.Disable)
                    .WithSourceReferenceResolver(_scriptOptions.SourceResolver);
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
