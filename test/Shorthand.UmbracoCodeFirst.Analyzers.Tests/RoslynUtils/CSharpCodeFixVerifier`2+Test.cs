// Code from the Visual Studio analyzer project

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new() {
    public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> {
        public Test() {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck;
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
