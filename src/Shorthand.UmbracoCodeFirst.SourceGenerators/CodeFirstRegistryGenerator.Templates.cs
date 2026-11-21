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

    private static bool TryGetRelativeViewPath(
        string path,
        string projectDirectory,
        string viewsDirectory,
        out string relativePath,
        out string alias) {
        relativePath = string.Empty;
        alias = string.Empty;
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
        var prefix = viewsDirectory + Path.DirectorySeparatorChar;
        if(!fullPath.StartsWith(prefix, comparison)) {
            return false;
        }

        relativePath = fullPath.Substring(prefix.Length)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace('\\', '/');
        var stem = relativePath.Substring(0, relativePath.Length - Path.GetExtension(relativePath).Length);
        alias = stem;
        return true;
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
            if(!TryGetRelativeViewPath(file.Path, projectDirectory, templateDirectory, out var relativePath, out var alias)) {
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

            var aliasSegments = alias.Split('/');
            if(alias.Length > 100 || aliasSegments.Any(segment =>
                segment.Length == 0 || !Regex.IsMatch(segment, @"^[A-Za-z][A-Za-z0-9_]*$"))) {
                context.ReportDiagnostic(Diagnostic.Create(_invalidTemplate, location, file.Path,
                    "every relative path segment must start with a letter and contain only letters, digits or underscores (maximum 100 characters for the full alias)"));
                continue;
            }

            alias = string.Join("_", aliasSegments);
            if(alias.Length > 100) {
                context.ReportDiagnostic(Diagnostic.Create(_invalidTemplate, location, file.Path,
                    "the flattened template alias must contain at most 100 characters"));
                continue;
            }

            using var sha256 = SHA256.Create();
            var contentHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
            definitions.Add(new TemplateDefinition(alias, literal.Token.ValueText, relativePath, relativePath, contentHash, location, text.ToString()));
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
        var hierarchicalConflicts = FindHierarchyConflicts(context, uniqueDefinitions);
        return uniqueDefinitions
            .Where(definition => !hierarchicalConflicts.Contains(definition))
            .ToArray();
    }

    private static HashSet<TemplateDefinition> FindHierarchyConflicts(
        SourceProductionContext context,
        IReadOnlyList<TemplateDefinition> templates) {
        var conflicts = new HashSet<TemplateDefinition>();
        static string RelativeAlias(TemplateDefinition template) =>
            template.RelativePath.Substring(0, template.RelativePath.Length - Path.GetExtension(template.RelativePath).Length);

        var prefixes = new List<string[]>();
        void CollectPrefixes(TemplateDefinition template) {
            var segments = RelativeAlias(template).Split('/');
            for(var index = 0; index < segments.Length; index++) {
                var prefix = segments.Take(index).ToArray();
                if(!prefixes.Any(existing => existing.SequenceEqual(prefix, StringComparer.Ordinal))) {
                    prefixes.Add(prefix);
                }
            }
        }

        foreach(var template in templates) {
            CollectPrefixes(template);
        }

        foreach(var prefix in prefixes) {
            var enclosingIdentifier = "UmbracoTemplates";
            foreach(var segment in prefix) {
                enclosingIdentifier = TemplateMemberIdentifier(segment, enclosingIdentifier);
            }

            var leafAndChildNames = templates
                .Select(template => RelativeAlias(template).Split('/'))
                .Where(segments => segments.Length > prefix.Length
                    && segments.Take(prefix.Length).SequenceEqual(prefix, StringComparer.Ordinal))
                .Select(segments => new {
                    Segment = segments[prefix.Length],
                    IsLeaf = segments.Length == prefix.Length + 1,
                })
                .Distinct()
                .ToArray();

            foreach(var group in leafAndChildNames
                .GroupBy(item => TemplateMemberIdentifier(item.Segment, enclosingIdentifier), StringComparer.Ordinal)
                .Where(group => group.Count() > 1)) {
                foreach(var template in templates.Where(template => {
                    var segments = RelativeAlias(template).Split('/');
                    return segments.Length > prefix.Length
                        && segments.Take(prefix.Length).SequenceEqual(prefix, StringComparer.Ordinal)
                        && group.Any(item => item.Segment == segments[prefix.Length]
                            && item.IsLeaf == (segments.Length == prefix.Length + 1));
                })) {
                    context.ReportDiagnostic(Diagnostic.Create(_templateIdentifierCollision, template.Location, group.Key));
                    conflicts.Add(template);
                }
            }
        }

        return conflicts;
    }

    private static string TemplateIdentifier(string alias) {
        var leaf = alias.Substring(alias.LastIndexOf('/') + 1);
        var identifier = char.ToUpperInvariant(leaf[0]) + leaf.Substring(1);
        return identifier == "UmbracoTemplates" ? "TemplateUmbracoTemplates" : identifier;
    }

    private static string TemplateMemberIdentifier(string segment, string enclosingIdentifier) {
        var identifier = TemplateIdentifier(segment);
        return identifier == enclosingIdentifier ? "Template" + identifier : identifier;
    }

    private static string GetViewsDirectoryName(string? viewsDirectory) {
        if(string.IsNullOrWhiteSpace(viewsDirectory)) {
            return "Views";
        }

        var normalized = viewsDirectory!.Replace('\\', '/').TrimEnd('/');
        var separatorIndex = normalized.LastIndexOf('/');
        var name = separatorIndex >= 0 ? normalized.Substring(separatorIndex + 1) : normalized;
        return string.IsNullOrWhiteSpace(name) ? "Views" : name;
    }

    private static string BuildTemplateAliases(
        IReadOnlyList<TemplateDefinition> templates,
        string? viewsDirectory) {
        var builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace Shorthand.UmbracoCodeFirst;");
        builder.AppendLine();
        AppendGeneratedCodeAttribute(builder, fullyQualified: true);
        builder.AppendLine("public static class UmbracoTemplates {");
        builder.Indent++;
        AppendTemplateHierarchy(builder, templates, [], viewsDirectory, "UmbracoTemplates");

        builder.Indent--;
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendTemplateHierarchy(
        IndentedStringBuilder builder,
        IReadOnlyList<TemplateDefinition> templates,
        IReadOnlyList<string> prefix,
        string? viewsDirectory,
        string enclosingIdentifier) {
        static string RelativeAlias(TemplateDefinition template) =>
            template.RelativePath.Substring(0, template.RelativePath.Length - Path.GetExtension(template.RelativePath).Length);
        var atPrefix = templates
            .Where(template => RelativeAlias(template).Split('/').Take(prefix.Count).SequenceEqual(prefix, StringComparer.Ordinal))
            .ToArray();
        var leafs = atPrefix
            .Where(template => RelativeAlias(template).Split('/').Length == prefix.Count + 1)
            .OrderBy(template => template.Alias, StringComparer.Ordinal)
            .ToArray();
        var children = atPrefix
            .Where(template => RelativeAlias(template).Split('/').Length > prefix.Count + 1)
            .Select(template => RelativeAlias(template).Split('/')[prefix.Count])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(segment => segment, StringComparer.Ordinal)
            .ToArray();

        foreach(var template in leafs) {
            var segments = RelativeAlias(template).Split('/');
            var viewsDirectoryName = GetViewsDirectoryName(viewsDirectory);
            builder.Append("/// <summary>Template alias generated from <c>");
            builder.Append(viewsDirectoryName);
            builder.Append("\\");
            builder.Append(template.RelativePath.Replace('/', '\\'));
            builder.AppendLine("</c>.</summary>");
            builder.Append("public const string ");
            builder.Append(TemplateMemberIdentifier(segments[segments.Length - 1], enclosingIdentifier));
            builder.Append(" = ");
            builder.Append(SymbolDisplay.FormatLiteral(template.Alias, true));
            builder.AppendLine(";");
        }

        foreach(var child in children) {
            var childIdentifier = TemplateMemberIdentifier(child, enclosingIdentifier);
            builder.Append("public static class ");
            builder.Append(childIdentifier);
            builder.AppendLine(" {");
            builder.Indent++;
            AppendTemplateHierarchy(builder, atPrefix, prefix.Concat([child]).ToArray(), viewsDirectory, childIdentifier);
            builder.Indent--;
            builder.AppendLine("}");
        }
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

    private sealed class TemplateDefinition(string alias, string name, string fileName, string relativePath, string contentHash, Location location, string content) {
        public string Alias { get; } = alias;

        public string Name { get; } = name;

        public string FileName { get; } = fileName;

        public string RelativePath { get; } = relativePath;

        public string ContentHash { get; } = contentHash;

        public Location Location { get; } = location;

        public string Content { get; } = content;
    }
}
