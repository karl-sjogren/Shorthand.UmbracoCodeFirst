using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Models;
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
        [FromServices] IContentTypeService contentTypeService) {
        IReadOnlyList<IDataType> dataTypes = (await dataTypeService.GetAllAsync()).ToArray();
        Dictionary<(CodeFirstDefinitionKind Kind, string Alias), string> urls = [];

        foreach(var definition in registry.Definitions) {
            var key = definition.Kind switch {
                CodeFirstDefinitionKind.DataType => dataTypes
                    .FirstOrDefault(dataType =>
                        dataType.ConfigurationData.TryGetValue(
                            "shorthand.umbraco-codefirst",
                            out object? value) &&
                        string.Equals(value as string, definition.Alias, StringComparison.Ordinal))
                    ?.Key,
                CodeFirstDefinitionKind.DocumentType => contentTypeService.Get(definition.Alias)?.Key,
                _ => null,
            };

            if(key is not null) {
                string path = definition.Kind == CodeFirstDefinitionKind.DataType
                    ? "data-type"
                    : "document-type";
                urls[(definition.Kind, definition.Alias)] =
                    $"/umbraco/section/settings/workspace/{path}/edit/{key.Value:D}";
            }
        }

        return CodeFirstStatusMapping.Map(registry, reportStore, urls);
    }
}
