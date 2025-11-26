using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using Veearve.Models;
using Veearve.Services;

namespace Veearve.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class ReadingsController : ControllerBase
    {
        private readonly IReadingService _readingService;
        private readonly IUserService _userService;

        public ReadingsController(IReadingService readingService, IUserService userService)
        {
            _readingService = readingService;
            _userService = userService;
        }

        /// <summary>
        /// Tagastab kõik lugemised sisselogitud kasutaja jaoks
        /// </summary>
        /// <remarks>
        /// Admin kasutajad näevad kõiki lugemisi, tavalised kasutajad ainult oma lugemisi.
        /// Lugemisi saab filtreerida kuupäevavahemiku järgi.
        /// 
        /// Sample request:
        /// 
        ///     GET /api/readings?startDate=2024-01-01&amp;endDate=2024-12-31
        /// </remarks>
        /// <param name="startDate">Alguskuupäev filtreerimiseks (valikuline, formaat: YYYY-MM-DD)</param>
        /// <param name="endDate">Lõppkuupäev filtreerimiseks (valikuline, formaat: YYYY-MM-DD)</param>
        /// <returns>Lugemiste nimekiri</returns>
        /// <response code="200">Lugemised leiti edukalt</response>
        /// <response code="400">Viga lugemiste päringul</response>
        /// <response code="401">Autentimine puudub</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Hangi lugemised",
            Description = "Tagastab kõik lugemised sisselogitud kasutaja jaoks. Saab filtreerida kuupäevade järgi.",
            Tags = new[] { "Readings" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetReadings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var readings = await _readingService.GetReadingsAsync(userId, role, startDate, endDate);
                return Ok(readings);
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to get readings",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Tagastab ühe lugemise ID järgi
        /// </summary>
        /// <remarks>
        /// Võimaldab vaadata konkreetse lugemise detaile. 
        /// Tavalised kasutajad saavad vaadata ainult oma lugemisi, admin kasutajad kõiki.
        /// 
        /// Sample request:
        /// 
        ///     GET /api/readings/507f1f77bcf86cd799439011
        /// </remarks>
        /// <param name="id">Lugemise unikaalne identifikaator (MongoDB ObjectId)</param>
        /// <returns>Lugemise detailid</returns>
        /// <response code="200">Lugemine leiti edukalt</response>
        /// <response code="401">Autentimine puudub</response>
        /// <response code="403">Kasutajal puudub ligipääs sellele lugemisele</response>
        /// <response code="404">Lugemist ei leitud</response>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Hangi üks lugemine",
            Description = "Tagastab ühe lugemise ID järgi. Kasutajad näevad ainult oma lugemisi, admin näeb kõiki.",
            Tags = new[] { "Readings" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReading(string id)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var reading = await _readingService.GetReadingByIdAsync(id, userId, role);
                return Ok(reading);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Reading not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
        }

        /// <summary>
        /// Loo uus lugemine
        /// </summary>
        /// <remarks>
        /// Võimaldab kasutajal lisada uue veemõõdiku lugemise. 
        /// Lugemine sisaldab kuupäeva ja veekulu näitu kuupmeetrites.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/readings
        ///     {
        ///        "readingDate": "2024-11-25T10:00:00Z",
        ///        "waterUsage": 125.5
        ///     }
        /// </remarks>
        /// <param name="dto">Lugemise loomise andmed (kuupäev ja veekulu)</param>
        /// <returns>Loodud lugemine</returns>
        /// <response code="201">Lugemine loodud edukalt</response>
        /// <response code="400">Vigased sisendandmed</response>
        /// <response code="401">Autentimine puudub</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Loo uus lugemine",
            Description = "Loo uus veemõõdiku lugemine sisselogitud kasutaja jaoks.",
            Tags = new[] { "Readings" }
        )]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateReading([FromBody] CreateReadingDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ValidationProblemDetails(ModelState));
                }

                var userId = User.FindFirst("userId")?.Value;
                var user = await _userService.GetUserByIdAsync(userId);

                var reading = await _readingService.CreateReadingAsync(dto, userId, user.Name);
                return StatusCode(201, reading);
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to create reading",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Uuenda olemasolevat lugemist
        /// </summary>
        /// <remarks>
        /// Võimaldab uuendada olemasolevat lugemist. 
        /// Tavalised kasutajad saavad uuendada ainult oma lugemisi, admin kasutajad kõiki.
        /// 
        /// Sample request:
        /// 
        ///     PUT /api/readings/507f1f77bcf86cd799439011
        ///     {
        ///        "readingDate": "2024-11-25T10:00:00Z",
        ///        "waterUsage": 130.0
        ///     }
        /// </remarks>
        /// <param name="id">Lugemise ID</param>
        /// <param name="dto">Uuendatavad andmed (kuupäev ja/või veekulu)</param>
        /// <returns>Uuendatud lugemine</returns>
        /// <response code="200">Lugemine uuendatud edukalt</response>
        /// <response code="400">Vigased sisendandmed</response>
        /// <response code="401">Autentimine puudub</response>
        /// <response code="403">Kasutajal puudub ligipääs selle lugemise muutmiseks</response>
        [HttpPut("{id}")]
        [SwaggerOperation(
            Summary = "Uuenda lugemist",
            Description = "Uuendab olemasolevat lugemist sisselogitud kasutaja või administraatori jaoks.",
            Tags = new[] { "Readings" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateReading(string id, [FromBody] UpdateReadingDto dto)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var reading = await _readingService.UpdateReadingAsync(id, dto, userId, role);
                return Ok(reading);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to update reading",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Kustuta lugemine
        /// </summary>
        /// <remarks>
        /// Kustutab lugemise püsivalt. Seda tegevust ei saa tagasi võtta.
        /// Tavalised kasutajad saavad kustutada ainult oma lugemisi, admin kasutajad kõiki.
        /// 
        /// Sample request:
        /// 
        ///     DELETE /api/readings/507f1f77bcf86cd799439011
        /// </remarks>
        /// <param name="id">Lugemise ID</param>
        /// <returns>Kinnitussõnum</returns>
        /// <response code="200">Lugemine kustutatud edukalt</response>
        /// <response code="400">Viga lugemise kustutamisel</response>
        /// <response code="401">Autentimine puudub</response>
        /// <response code="403">Kasutajal puudub ligipääs selle lugemise kustutamiseks</response>
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Kustuta lugemine",
            Description = "Kustutab lugemise sisselogitud kasutaja või administraatori jaoks.",
            Tags = new[] { "Readings" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteReading(string id)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                await _readingService.DeleteReadingAsync(id, userId, role);
                return Ok(new { message = "Reading deleted successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to delete reading",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
    }
}