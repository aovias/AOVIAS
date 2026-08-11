using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/token")]
    [Authorize]
    public class TokenController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }
    }
}
