using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shorthand.UmbracoCodeFirst.Backoffice.Contracts;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public class UmbracoCodeFirstDefinitionStoreLoggingTests {
    private static readonly CodeFirstDefinition _definition =
        new(CodeFirstDefinitionKind.DocumentType, "article", "Article", null, []);

    [Fact]
    public async Task ApplyAsync_UnownedDefinition_LogsWarningAndDoesNotUpdate_Async() {
        var contentTypeService = A.Fake<IContentTypeService>();
        var contentType = CreateContentType();
        A.CallTo(() => contentTypeService.Get("article")).Returns(contentType);
        var ownershipStore = A.Fake<ICodeFirstOwnershipStore>();
        A.CallTo(() => ownershipStore.Get("article", contentType.Key)).Returns(null);
        var logger = new FakeLogger<UmbracoCodeFirstDefinitionStore>();
        var store = CreateStore(contentTypeService, ownershipStore, logger);

        var status = await store.ApplyAsync(_definition, CancellationToken.None);

        status.ShouldBe(SynchronizationStatus.Skipped);
        var warning = logger.Collector.GetSnapshot().Single(entry => entry.Level == LogLevel.Warning);
        warning.Message.ShouldContain("not owned");
        warning.StructuredState!.Single(item => item.Key == "DefinitionKind").Value.ShouldBe("DocumentType");
        warning.StructuredState!.Single(item => item.Key == "DefinitionAlias").Value.ShouldBe("article");
        A.CallTo(() => contentTypeService.UpdateAsync(A<IContentType>._, A<Guid>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ApplyAsync_UnchangedDefinition_LogsOutcome_Async() {
        var contentTypeService = A.Fake<IContentTypeService>();
        var contentType = CreateContentType();
        A.CallTo(() => contentTypeService.Get("article")).Returns(contentType);
        var ownershipStore = A.Fake<ICodeFirstOwnershipStore>();
        A.CallTo(() => ownershipStore.Get("article", contentType.Key))
            .Returns(new CodeFirstDocumentTypeOwnership(contentType.Key, "article", []));
        var logger = new FakeLogger<UmbracoCodeFirstDefinitionStore>();
        var store = CreateStore(contentTypeService, ownershipStore, logger);

        var status = await store.ApplyAsync(_definition, CancellationToken.None);

        status.ShouldBe(SynchronizationStatus.Unchanged);
        var outcome = logger.Collector.GetSnapshot()
            .Single(entry => entry.StructuredState!.Any(item => item.Key == "Status"));
        outcome.Level.ShouldBe(LogLevel.Debug);
        outcome.StructuredState!.Single(item => item.Key == "DefinitionAlias").Value.ShouldBe("article");
        outcome.StructuredState!.Single(item => item.Key == "Status").Value.ShouldBe("Unchanged");
        A.CallTo(() => contentTypeService.UpdateAsync(A<IContentType>._, A<Guid>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ApplyAsync_ChangedOwnedDefinition_LogsUpdatedOutcome_Async() {
        var contentTypeService = A.Fake<IContentTypeService>();
        var contentType = CreateContentType();
        contentType.Name = "Old name";
        A.CallTo(() => contentTypeService.Get("article")).Returns(contentType);
        A.CallTo(() => contentTypeService.UpdateAsync(contentType, A<Guid>._))
            .Returns(Attempt.Succeed(ContentTypeOperationStatus.Success));
        var ownershipStore = A.Fake<ICodeFirstOwnershipStore>();
        A.CallTo(() => ownershipStore.Get("article", contentType.Key))
            .Returns(new CodeFirstDocumentTypeOwnership(contentType.Key, "article", []));
        var logger = new FakeLogger<UmbracoCodeFirstDefinitionStore>();
        var store = CreateStore(contentTypeService, ownershipStore, logger);

        var status = await store.ApplyAsync(_definition, CancellationToken.None);

        status.ShouldBe(SynchronizationStatus.Updated);
        var outcome = logger.Collector.GetSnapshot()
            .Single(entry => entry.StructuredState!.Any(item => item.Key == "Status"));
        outcome.Level.ShouldBe(LogLevel.Debug);
        outcome.StructuredState!.Single(item => item.Key == "Status").Value.ShouldBe("Updated");
        A.CallTo(() => contentTypeService.UpdateAsync(contentType, A<Guid>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ApplyAsync_ServiceFailure_LogsExceptionAndRethrows_Async() {
        var contentTypeService = A.Fake<IContentTypeService>();
        var failure = new InvalidOperationException("lookup failed");
        A.CallTo(() => contentTypeService.Get("article")).Throws(failure);
        var logger = new FakeLogger<UmbracoCodeFirstDefinitionStore>();
        var store = CreateStore(contentTypeService, A.Fake<ICodeFirstOwnershipStore>(), logger);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => store.ApplyAsync(_definition, CancellationToken.None));

        exception.ShouldBeSameAs(failure);
        var error = logger.Collector.GetSnapshot().Single(entry => entry.Level == LogLevel.Error);
        error.Exception.ShouldBeSameAs(failure);
        error.StructuredState!.Single(item => item.Key == "DefinitionKind").Value.ShouldBe("DocumentType");
        error.StructuredState!.Single(item => item.Key == "DefinitionAlias").Value.ShouldBe("article");
    }

    [Fact]
    public async Task ApplyAsync_FailedDocumentUpdate_DoesNotReportSuccess_Async() {
        var contentTypeService = A.Fake<IContentTypeService>();
        var contentType = CreateContentType();
        contentType.Name = "Old name";
        A.CallTo(() => contentTypeService.Get("article")).Returns(contentType);
        A.CallTo(() => contentTypeService.UpdateAsync(contentType, A<Guid>._))
            .Returns(Attempt.Fail(ContentTypeOperationStatus.InvalidAlias));
        var ownershipStore = A.Fake<ICodeFirstOwnershipStore>();
        A.CallTo(() => ownershipStore.Get("article", contentType.Key))
            .Returns(new CodeFirstDocumentTypeOwnership(contentType.Key, "article", []));
        var store = CreateStore(contentTypeService, ownershipStore, new FakeLogger<UmbracoCodeFirstDefinitionStore>());

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => store.ApplyAsync(_definition, CancellationToken.None));

        exception.Message.ShouldContain("InvalidAlias");
        A.CallTo(() => ownershipStore.Set(A<string>._, A<Guid>._, A<IEnumerable<string>>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ApplyAsync_FailedDocumentCreate_DoesNotClaimOwnership_Async() {
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => contentTypeService.Get("article")).Returns(null);
        A.CallTo(() => contentTypeService.CreateAsync(A<IContentType>._, A<Guid>._))
            .Returns(Attempt.Fail(ContentTypeOperationStatus.DuplicateAlias));
        var ownershipStore = A.Fake<ICodeFirstOwnershipStore>();
        var helper = A.Fake<IShortStringHelper>();
        A.CallTo(() => helper.CleanStringForSafeAlias(A<string>._))
            .ReturnsLazily((string value) => value);
        A.CallTo(() => helper.CleanString(A<string>._, A<CleanStringType>._))
            .ReturnsLazily((string value, CleanStringType _) => value);
        var store = new UmbracoCodeFirstDefinitionStore(
            null!, null!, contentTypeService, null!, null!, null!, null!, null!, null!,
            null!, null!, helper, ownershipStore, new FakeLogger<UmbracoCodeFirstDefinitionStore>());

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => store.ApplyAsync(_definition, CancellationToken.None));

        exception.Message.ShouldContain("DuplicateAlias");
        A.CallTo(() => ownershipStore.Set(A<string>._, A<Guid>._, A<IEnumerable<string>>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ApplyAsync_Cancelled_DoesNotLogAnError_Async() {
        var contentTypeService = A.Fake<IContentTypeService>();
        using var cancellation = new CancellationTokenSource();
        var logger = new FakeLogger<UmbracoCodeFirstDefinitionStore>();
        var store = CreateStore(contentTypeService, A.Fake<ICodeFirstOwnershipStore>(), logger);
        A.CallTo(() => contentTypeService.Get("article"))
            .Invokes(() => cancellation.Cancel())
            .Throws(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(
            () => store.ApplyAsync(_definition, cancellation.Token));

        logger.Collector.GetSnapshot().ShouldNotContain(entry => entry.Level == LogLevel.Error);
    }

    private static ContentType CreateContentType() {
        var helper = A.Fake<IShortStringHelper>();
        A.CallTo(() => helper.CleanStringForSafeAlias(A<string>._))
            .ReturnsLazily((string value) => value);
        A.CallTo(() => helper.CleanString(A<string>._, A<CleanStringType>._))
            .ReturnsLazily((string value, CleanStringType _) => value);
        return new ContentType(helper, -1) { Alias = "article", Name = "Article" };
    }

    private static UmbracoCodeFirstDefinitionStore CreateStore(
        IContentTypeService contentTypeService,
        ICodeFirstOwnershipStore ownershipStore,
        ILogger<UmbracoCodeFirstDefinitionStore> logger) =>
        new(null!, null!, contentTypeService, null!, null!, null!, null!, null!, null!,
            null!, null!, null!, ownershipStore, logger);
}
