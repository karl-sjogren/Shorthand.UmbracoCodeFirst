using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;
using Shorthand.UmbracoCodeFirst.Backoffice.Contracts;
using Shorthand.UmbracoCodeFirst.Backoffice.Models;
using Shorthand.UmbracoCodeFirst.Backoffice.Reconciliation;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Controllers;

[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("shorthandumbracocodefirstbackoffice")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[ApiExplorerSettings(GroupName = "Shorthand.UmbracoCodeFirst.Backoffice")]
public class ShorthandUmbracoCodeFirstBackofficeApiController : ShorthandUmbracoCodeFirstBackofficeApiControllerBase {
    [HttpGet("ping")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public string Ping() => "Pong";

    [HttpGet("status")]
    [ProducesResponseType<CodeFirstStatusResponse>(StatusCodes.Status200OK)]
    public async Task<CodeFirstStatusResponse> StatusAsync(
            [FromServices] ICodeFirstDefinitionRegistry registry,
            [FromServices] ISynchronizationReportStore reportStore,
            [FromServices] IDataTypeService dataTypeService,
            [FromServices] IContentTypeService contentTypeService,
            [FromServices] IMediaTypeService mediaTypeService,
            [FromServices] IMemberTypeService memberTypeService) {
        var dataTypes = (await dataTypeService.GetAllAsync()).ToArray();
        var urls = new Dictionary<(CodeFirstDefinitionKind Kind, string Alias), string>();

        foreach(var definition in registry.Definitions) {
            var key = definition.Kind switch {
                CodeFirstDefinitionKind.DataType => dataTypes
                    .FirstOrDefault(dataType =>
                        dataType.ConfigurationData.TryGetValue("shorthand.umbraco-codefirst", out var value) &&
                        string.Equals(value as string, definition.Alias, StringComparison.Ordinal))
                    ?.Key,
                CodeFirstDefinitionKind.DocumentType => contentTypeService.Get(definition.Alias)?.Key,
                CodeFirstDefinitionKind.MediaType => mediaTypeService.Get(definition.Alias)?.Key,
                CodeFirstDefinitionKind.MemberType => memberTypeService.Get(definition.Alias)?.Key,
                _ => null,
            };

            if(key is not null) {
                var path = definition.Kind switch {
                    CodeFirstDefinitionKind.DataType => "data-type",
                    CodeFirstDefinitionKind.MediaType => "media-type",
                    CodeFirstDefinitionKind.MemberType => "member-type",
                    _ => "document-type",
                };
                urls[(definition.Kind, definition.Alias)] =
                    $"/umbraco/section/settings/workspace/{path}/edit/{key.Value:D}";
            }
        }

        return CodeFirstStatusMapping.Map(registry, reportStore, urls);
    }

    [HttpGet("cleanup-preview")]
    [ProducesResponseType<CodeFirstCleanupResponse>(StatusCodes.Status200OK)]
    public Task<CodeFirstCleanupResponse> CleanupPreviewAsync(
        [FromServices] ICodeFirstCleanupService cleanupService) =>
        cleanupService.PreviewAsync();

    [HttpGet("reconciliation-preview")]
    [ProducesResponseType<CodeFirstReconciliationPreview>(StatusCodes.Status200OK)]
    public async Task<CodeFirstReconciliationPreview> ReconciliationPreviewAsync(
        [FromServices] ISynchronizationReportStore reportStore,
        [FromServices] ICodeFirstCleanupService cleanupService) =>
        new(reportStore.Current, await cleanupService.PreviewAsync());

    [HttpPost("cleanup")]
    [ProducesResponseType<CodeFirstCleanupResult>(StatusCodes.Status200OK)]
    public Task<CodeFirstCleanupResult> CleanupAsync(
        [FromServices] ICodeFirstCleanupService cleanupService) =>
        cleanupService.ApplyAsync();
}
