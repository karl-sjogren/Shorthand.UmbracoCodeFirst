// Code from the Visual Studio analyzer project

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new() {
    public class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> {
        public Test() {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
            CompilerDiagnostics = CompilerDiagnostics.None;
            SolutionTransforms.Add((solution, projectId) => {
                var compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
        }
    }
}
