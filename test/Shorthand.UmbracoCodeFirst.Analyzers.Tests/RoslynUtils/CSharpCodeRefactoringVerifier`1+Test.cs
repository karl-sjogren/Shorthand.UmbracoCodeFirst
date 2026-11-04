// Code from the Visual Studio analyzer project

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils;

public static partial class CSharpCodeRefactoringVerifier<TCodeRefactoring>
    where TCodeRefactoring : CodeRefactoringProvider, new() {
    public class Test : CSharpCodeRefactoringTest<TCodeRefactoring, DefaultVerifier> {
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
