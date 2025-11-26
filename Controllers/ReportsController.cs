using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using Veearve.Services;

namespace Veearve.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class ReportsController : ControllerBase
    {
        private readonly IBillService _billService;

        public ReportsController(IBillService billService)
        {
            _billService = billService;
        }

        /// <summary>
        /// Tagastab aastaaruande maksetest
        /// </summary>
        /// <remarks>
        /// Admin saab näha kõigi kasutajate aastaaruannet, tavalised kasutajad näevad ainult oma andmeid.
        /// Kui aastat ei määrata, kasutatakse käesolevat aastat.
        /// 
        /// Sample request:
        /// 
        ///     GET /api/reports/annual?year=2024
        /// </remarks>
        /// <param name="year">Aasta (valikuline, vaikimisi käesolev aasta)</param>
        /// <returns>Aastaaruanne maksetest</returns>
        /// <response code="200">Aastaaruanne edukalt loodud</response>
        /// <response code="400">Viga aruande koostamisel</response>
        /// <response code="401">Autentimine puudub</response>
        [HttpGet("annual")]
        [SwaggerOperation(
            Summary = "Aastaaruanne",
            Description = "Tagastab aastaaruande maksetest. Admin saab näha kõiki, kasutaja ainult oma arveid.",
            Tags = new[] { "Reports" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAnnualReport([FromQuery] int? year)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var result = await _billService.GetAnnualReportAsync(year, userId, role);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to generate annual report",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
    }
}