using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Api.Management.Routing;
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
    public CodeFirstStatusResponse Status(
        [FromServices] ICodeFirstDefinitionRegistry registry,
        [FromServices] ISynchronizationReportStore reportStore) =>
        CodeFirstStatusMapping.Map(registry, reportStore);
}
