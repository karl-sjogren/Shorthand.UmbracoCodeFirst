using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators;

public sealed partial class CodeFirstRegistryGenerator {
    private static readonly DiagnosticDescriptor _invalidTemplate = new(
        "UCF120",
        "Invalid template declaration",
        "Template declaration in '{0}' is invalid: {1}",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor _duplicateTemplate = new(
        "UCF121",
        "Duplicate template alias",
        "Multiple code-first templates have the alias '{0}'",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor _templateIdentifierCollision = new(
        "UCF122",
        "Duplicate template constant",
        "Template aliases generate the same UmbracoTemplates constant '{0}'",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor _multipleTemplateRegistries = new(
        "UCF124",
        "Multiple code-first registries",
        "This project and a referenced project both generate code-first registries; move all code-first definitions to the models project and set UmbracoCodeFirstGenerateTemplates to false in this project",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static bool IsRootView(string path, string projectDirectory, string viewsDirectory) {
        var fullPath = Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(projectDirectory, path));
        viewsDirectory = Path.GetFullPath(Path.IsPathRooted(viewsDirectory)
            ? viewsDirectory
            : Path.Combine(projectDirectory, viewsDirectory));
        if(viewsDirectory.Length > (Path.GetPathRoot(viewsDirectory)?.Length ?? 0)) {
            viewsDirectory = viewsDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetDirectoryName(fullPath), viewsDirectory, comparison);
    }

    private static IReadOnlyList<TemplateDefinition> ReadTemplates(
        SourceProductionContext context,
        ImmutableArray<AdditionalText> files,
        string? projectDirectory,
        string? viewsDirectory,
        bool enabled) {
        var definitions = new List<TemplateDefinition>();
        if(!enabled || projectDirectory is null || projectDirectory.Length == 0) {
            return definitions;
        }

        var templateDirectory = viewsDirectory is { Length: > 0 } ? viewsDirectory : Path.Combine(projectDirectory, "Views");
        foreach(var file in files.GroupBy(file => file.Path, StringComparer.Ordinal).Select(group => group.First())) {
            if(!IsRootView(file.Path, projectDirectory, templateDirectory)) {
                continue;
            }

            var text = file.GetText(context.CancellationToken);
            if(text is null) {
                continue;
            }

            var masked = MaskComments(text.ToString());
            var maskedText = SourceText.From(masked);
            var declarations = new List<(string Source, TextSpan Span)>();
            foreach(var line in maskedText.Lines) {
                var lineText = line.ToString();
                var match = Regex.Match(lineText,
                    @"^\s*@attribute\s+\[\s*(?:global::)?(?:Shorthand\.UmbracoCodeFirst\.Attributes\.)?UCFTemplate(?:Attribute)?\b");
                if(match.Success) {
                    var attributeIndex = lineText.IndexOf('[', match.Index);
                    declarations.Add((lineText.Substring(attributeIndex), line.Span));
                }
            }

            if(declarations.Count == 0) {
                continue;
            }

            var location = Location.Create(file.Path, declarations[0].Span, text.Lines.GetLinePositionSpan(declarations[0].Span));
            if(declarations.Count != 1) {
                context.ReportDiagnostic(Diagnostic.Create(_invalidTemplate, location, file.Path, "use exactly one UCFTemplate attribute"));
                continue;
            }

            var syntax = SyntaxFactory.ParseCompilationUnit($"{declarations[0].Source}\nclass TemplateView {{ }}");
            var attributes = syntax.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault()?
                .AttributeLists.SelectMany(list => list.Attributes).ToArray();
            var attribute = attributes?.FirstOrDefault();
            if(syntax.ContainsDiagnostics || attributes?.Length != 1 || attribute?.ArgumentList?.Arguments.Count != 1
                || attribute.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal
                || !literal.IsKind(SyntaxKind.StringLiteralExpression)
                || string.IsNullOrWhiteSpace(literal.Token.ValueText)) {
                context.ReportDiagnostic(Diagnostic.Create(_invalidTemplate, location, file.Path,
                    "specify one non-empty string literal as the template name on a single line"));
                continue;
            }

            var alias = Path.GetFileNameWithoutExtension(file.Path);
            if(alias.Length > 100 || !Regex.IsMatch(alias, @"^[A-Za-z][A-Za-z0-9_]*$")) {
                context.ReportDiagnostic(Diagnostic.Create(_invalidTemplate, location, file.Path,
                    "the filename must start with a letter and contain only letters, digits or underscores (maximum 100 characters)"));
                continue;
            }

            using var sha256 = SHA256.Create();
            var contentHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
            definitions.Add(new TemplateDefinition(alias, literal.Token.ValueText, Path.GetFileName(file.Path), contentHash, location, text.ToString()));
        }

        var duplicates = definitions.GroupBy(definition => definition.Alias, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToArray();
        foreach(var duplicate in duplicates) {
            foreach(var definition in duplicate) {
                context.ReportDiagnostic(Diagnostic.Create(_duplicateTemplate, definition.Location, definition.Alias));
            }
        }

        var duplicateAliases = new HashSet<string>(duplicates.Select(group => group.Key), StringComparer.OrdinalIgnoreCase);
        var uniqueDefinitions = definitions.Where(definition => !duplicateAliases.Contains(definition.Alias))
            .OrderBy(definition => definition.Alias, StringComparer.Ordinal)
            .ToArray();
        var collisions = uniqueDefinitions.GroupBy(definition => TemplateIdentifier(definition.Alias), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToArray();
        foreach(var collision in collisions) {
            foreach(var definition in collision) {
                context.ReportDiagnostic(Diagnostic.Create(_templateIdentifierCollision, definition.Location, collision.Key));
            }
        }

        var conflictingIdentifiers = new HashSet<string>(collisions.Select(group => group.Key), StringComparer.Ordinal);
        return uniqueDefinitions.Where(definition => !conflictingIdentifiers.Contains(TemplateIdentifier(definition.Alias))).ToArray();
    }

    private static string TemplateIdentifier(string alias) {
        var identifier = char.ToUpperInvariant(alias[0]) + alias.Substring(1);
        return identifier == "UmbracoTemplates" ? "TemplateUmbracoTemplates" : identifier;
    }

    private static string BuildTemplateAliases(IReadOnlyList<TemplateDefinition> templates) {
        var builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace Shorthand.UmbracoCodeFirst;");
        builder.AppendLine();
        AppendGeneratedCodeAttribute(builder, fullyQualified: true);
        builder.AppendLine("public static class UmbracoTemplates {");
        builder.Indent++;
        foreach(var template in templates) {
            builder.Append("/// <summary>Template alias generated from <c>Views\\");
            builder.Append(template.FileName);
            builder.AppendLine("</c>.</summary>");
            builder.Append("public const string ");
            builder.Append(TemplateIdentifier(template.Alias));
            builder.Append(" = ");
            builder.Append(SymbolDisplay.FormatLiteral(template.Alias, true));
            builder.AppendLine(";");
        }

        builder.Indent--;
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string MaskComments(string source) {
        var result = new StringBuilder(source.Length);
        var razorComment = false;
        var htmlComment = false;
        for(var index = 0; index < source.Length; index++) {
            if(!razorComment && !htmlComment && index + 1 < source.Length && source[index] == '@' && source[index + 1] == '*') {
                razorComment = true;
            } else if(!razorComment && !htmlComment && index + 3 < source.Length && source.Substring(index, 4) == "<!--") {
                htmlComment = true;
            }

            var endRazor = razorComment && index > 0 && source[index - 1] == '*' && source[index] == '@';
            var endHtml = htmlComment && index > 1 && source[index - 2] == '-' && source[index - 1] == '-' && source[index] == '>';
            result.Append((razorComment || htmlComment) && source[index] is not '\r' and not '\n' ? ' ' : source[index]);
            if(endRazor) {
                razorComment = false;
            } else if(endHtml) {
                htmlComment = false;
            }
        }

        return result.ToString();
    }

    private sealed class TemplateDefinition(string alias, string name, string fileName, string contentHash, Location location, string content) {
        public string Alias { get; } = alias;

        public string Name { get; } = name;

        public string FileName { get; } = fileName;

        public string ContentHash { get; } = contentHash;

        public Location Location { get; } = location;

        public string Content { get; } = content;
    }
}
