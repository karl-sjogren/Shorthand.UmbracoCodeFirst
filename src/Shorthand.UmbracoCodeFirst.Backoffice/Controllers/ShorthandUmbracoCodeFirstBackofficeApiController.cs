using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Shorthand.UmbracoCodeFirst.Backoffice")]
public class ShorthandUmbracoCodeFirstBackofficeApiController : ShorthandUmbracoCodeFirstBackofficeApiControllerBase {
    [HttpGet("ping")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public string Ping() => "Pong";
}
