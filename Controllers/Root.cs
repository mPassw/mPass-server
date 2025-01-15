using Microsoft.AspNetCore.Mvc;

namespace mPass_server.Controllers;

[Route("/")]
[ApiController]
public class Root(IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Check server status
    /// </summary>
    /// <remarks>Returns x-mpass-instance header</remarks>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Tags("General")]
    public ActionResult Get()
    {
        Response.Headers.Append("x-mpass-instance", "true");

        return Ok();
    }
}