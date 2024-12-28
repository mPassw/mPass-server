using Microsoft.AspNetCore.Mvc;

namespace mPass_server.Controllers;

[Route("/")]
[ApiController]
public class Root : ControllerBase
{
    /// <summary>
    /// Check server status
    /// </summary>
    /// <remarks>Returns X-Mpass-Instance header</remarks>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Tags("General")]
    public ActionResult Get()
    {
        Response.Headers.Append("X-Mpass-Instance", "true");
        return Ok();
    }
}