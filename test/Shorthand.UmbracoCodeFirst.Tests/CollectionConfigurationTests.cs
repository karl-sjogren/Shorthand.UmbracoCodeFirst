using System.Text.Json;
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class CollectionConfigurationTests {
    [Fact]
    public void FromType_CustomListView_UsesConfiguredAlias() {
        // Act
        var settings = CodeFirstCollectionConfiguration.FromType(typeof(CustomListView), CodeFirstDefinitionKind.DocumentType);
        using var json = JsonDocument.Parse(settings.Json);

        // Assert
        settings.Error.ShouldBeNull();
        json.RootElement.GetProperty("layouts")[0].GetProperty("collectionView").GetString().ShouldBe("Example.CollectionView");
    }

    [Fact]
    public void FromType_EmptyCustomListView_ReportsError() {
        // Act
        var settings = CodeFirstCollectionConfiguration.FromType(typeof(EmptyListView), CodeFirstDefinitionKind.DocumentType);

        // Assert
        settings.Error.ShouldNotBeNull().ShouldContain("alias cannot be empty");
    }

    [Theory]
    [InlineData(CodeFirstDefinitionKind.DocumentType, "Document")]
    [InlineData(CodeFirstDefinitionKind.MediaType, "Media")]
    public void FromType_MapsColumnsAndLayouts(CodeFirstDefinitionKind kind, string entity) {
        // Arrange
        // Act
        var settings = CodeFirstCollectionConfiguration.FromType(typeof(CustomCollection), kind);
        using var json = JsonDocument.Parse(settings.Json);

        // Assert
        var root = json.RootElement;
        root.GetProperty("pageSize").GetInt32().ShouldBe(25);
        root.GetProperty("orderBy").GetString().ShouldBe("summary");
        root.GetProperty("orderDirection").GetString().ShouldBe("asc");
        root.GetProperty("layouts")[0].GetProperty("collectionView").GetString()
            .ShouldBe($"Umb.CollectionView.{entity}.Grid");
        var column = root.GetProperty("includeProperties")[0];
        column.GetProperty("alias").GetString().ShouldBe("summary");
        column.GetProperty("header").GetString().ShouldBe("Summary");
        column.GetProperty("isSystem").GetInt32().ShouldBe(0);
        column.GetProperty("nameTemplate").GetString().ShouldBe("{=value}");
    }

    [Fact]
    public void FromType_UsesTypedAliasesWithoutChangingSerializedConfiguration() {
        // Arrange
        var settings = CodeFirstCollectionConfiguration.FromType(typeof(TypedCollection), CodeFirstDefinitionKind.DocumentType);

        // Act
        using var json = JsonDocument.Parse(settings.Json);
        var root = json.RootElement;

        // Assert
        settings.Error.ShouldBeNull();
        root.GetProperty("orderBy").GetString().ShouldBe("explicitSummary");
        root.GetProperty("includeProperties")[0].GetProperty("alias").GetString().ShouldBe("explicitSummary");
        root.GetProperty("includeProperties")[0].GetProperty("isSystem").GetInt32().ShouldBe(0);
        root.GetProperty("includeProperties")[1].GetProperty("alias").GetString().ShouldBe("updateDate");
        root.GetProperty("includeProperties")[1].GetProperty("isSystem").GetInt32().ShouldBe(1);
    }

    [Theory]
    [InlineData(CodeFirstCollectionSystemFields.Name, true)]
    [InlineData(CodeFirstCollectionSystemFields.ContentTypeAlias, true)]
    [InlineData(CodeFirstCollectionSystemFields.CreateDate, true)]
    [InlineData(CodeFirstCollectionSystemFields.SortOrder, true)]
    [InlineData(CodeFirstCollectionSystemFields.UpdateDate, true)]
    [InlineData(CodeFirstCollectionSystemFields.Creator, true)]
    [InlineData(CodeFirstCollectionSystemFields.Updater, true)]
    [InlineData(CodeFirstCollectionSystemFields.Published, true)]
    [InlineData("summary", false)]
    [InlineData("UpdateDate", false)]
    public void Column_DerivesSystemStatusFromAlias(string alias, bool expected) {
        // Arrange
        var column = new CodeFirstCollectionColumn(alias, "Column");

        // Act
        var isSystem = column.IsSystem;

        // Assert
        isSystem.ShouldBe(expected);
    }

    [Fact]
    public void Column_ChangingAliasRecalculatesSystemStatus() {
        // Arrange
        var column = new CodeFirstCollectionColumn("summary", "Summary");

        // Act
        var updated = column with { Alias = CodeFirstCollectionSystemFields.UpdateDate };

        // Assert
        column.IsSystem.ShouldBeFalse();
        updated.IsSystem.ShouldBeTrue();
    }

    [Fact]
    public void FromType_RejectsMissingLayouts() {
        // Arrange
        // Act
        var result = CodeFirstCollectionConfiguration.FromType(typeof(NoLayouts), CodeFirstDefinitionKind.DocumentType);

        // Assert
        result.Error.ShouldNotBeNull().ShouldContain("at least one layout");
    }

    [Fact]
    public void FromType_RejectsOrderingOutsideColumns() {
        // Arrange
        // Act
        var result = CodeFirstCollectionConfiguration.FromType(typeof(InvalidOrder), CodeFirstDefinitionKind.MediaType);

        // Assert
        result.Error.ShouldNotBeNull().ShouldContain("include the order field");
    }

    [Fact]
    public void FromType_ReportsInvalidTypedAliasSelector() {
        // Arrange
        // Act
        var result = CodeFirstCollectionConfiguration.FromType(typeof(InvalidTypedCollection), CodeFirstDefinitionKind.DocumentType);

        // Assert
        result.Error.ShouldNotBeNull().ShouldContain("Select a direct property marked with UCFProperty");
    }

    private sealed class CustomListView : CodeFirstCollectionConfiguration {
        public CustomListView() {
            ListViewAlias = "Example.CollectionView";
        }
    }

    private sealed class EmptyListView : CodeFirstCollectionConfiguration {
        public EmptyListView() {
            ListViewAlias = " ";
        }
    }

    private sealed class CustomCollection : CodeFirstCollectionConfiguration {
        public CustomCollection() {
            List = false;
            PageSize = 25;
            OrderBy = "summary";
            OrderDirection = "asc";
            Columns = [new("summary", "Summary", NameTemplate: "{=value}")];
        }
    }

    private sealed class NoLayouts : CodeFirstCollectionConfiguration {
        public NoLayouts() {
            List = false;
            Grid = false;
        }
    }

    private sealed class InvalidOrder : CodeFirstCollectionConfiguration {
        public InvalidOrder() {
            OrderBy = "missing";
        }
    }

    private sealed class TypedCollection : CodeFirstCollectionConfiguration<TypedModel> {
        public TypedCollection() {
            OrderBy = Alias(model => model.Summary);
            Columns = [
                new(Alias(model => model.Summary), "Summary"),
                new(CodeFirstCollectionSystemFields.UpdateDate, "Updated"),
            ];
        }
    }

    private sealed class InvalidTypedCollection : CodeFirstCollectionConfiguration<TypedModel> {
        public InvalidTypedCollection() {
            OrderBy = Alias(model => model.Summary.ToUpperInvariant());
        }
    }

    private sealed class TypedModel {
        [UCFProperty("explicitSummary", "Summary", "text")]
        public string Summary { get; set; } = string.Empty;

        public static class PropertyAliases {
            public const string Summary = "explicitSummary";
        }
    }
}
