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
    public class BillsController : ControllerBase
    {
        private readonly IBillService _billService;
        private readonly IEmailService _emailService;

        public BillsController(IBillService billService, IEmailService emailService)
        {
            _billService = billService;
            _emailService = emailService;
        }

        /// <summary>
        /// Tagastab kõik maksmata arved sisselogitud kasutaja jaoks
        /// </summary>
        /// <remarks>
        /// Admin kasutajad näevad kõiki maksmata arveid süsteemis.
        /// Tavalised kasutajad näevad ainult oma maksmata arveid.
        /// 
        /// Sample request:
        /// 
        ///     GET /api/bills/unpaid
        ///     Authorization: Bearer {your_token}
        /// </remarks>
        /// <returns>Maksmata arvete nimekiri</returns>
        /// <response code="200">Maksmata arved leiti edukalt</response>
        /// <response code="400">Viga päringu töötlemisel</response>
        /// <response code="401">Autentimine puudub</response>
        [HttpGet("unpaid")]
        [SwaggerOperation(
            Summary = "Maksmata arvete päring",
            Description = "Tagastab kõik maksmata arved sisselogitud kasutaja jaoks. Admin näeb kõiki, kasutaja ainult oma arveid.",
            Tags = new[] { "Bills" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUnpaidBills()
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var result = await _billService.GetUnpaidBillsAsync(userId, role);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to get unpaid bills",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Märkida arve tasutuks (admin ainult)
        /// </summary>
        /// <remarks>
        /// Võimaldab administraatoril märkida arve tasutuks. 
        /// Seda funktsiooni saavad kasutada ainult admin rolliga kasutajad.
        /// 
        /// Sample request:
        /// 
        ///     PATCH /api/bills/507f1f77bcf86cd799439011/pay
        ///     Authorization: Bearer {admin_token}
        /// </remarks>
        /// <param name="id">Arve ID (lugemise ID)</param>
        /// <returns>Uuendatud lugemine koos arve staatusega</returns>
        /// <response code="200">Arve märgitud tasutuks edukalt</response>
        /// <response code="400">Viga arve töötlemisel</response>
        /// <response code="401">Autentimine puudub</response>
        /// <response code="403">Ainult admin saab arveid tasutud märkida</response>
        [HttpPatch("{id}/pay")]
        [Authorize(Roles = "admin")]
        [SwaggerOperation(
            Summary = "Märgi arve tasutuks",
            Description = "Admin saab arve tasutuks märkida. Nõuab admin õiguseid.",
            Tags = new[] { "Bills" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkAsPaid(string id)
        {
            try
            {
                var reading = await _billService.MarkAsPaidAsync(id);
                return Ok(new { message = "Bill marked as paid", reading });
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to mark bill as paid",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Märkida arve mittemakstud (admin ainult)
        /// </summary>
        /// <remarks>
        /// Võimaldab administraatoril märkida arve tagasi mittemakstud olekusse.
        /// Kasulik, kui arve oli ekslikult tasutuks märgitud.
        /// 
        /// Sample request:
        /// 
        ///     PATCH /api/bills/507f1f77bcf86cd799439011/unpay
        ///     Authorization: Bearer {admin_token}
        /// </remarks>
        /// <param name="id">Arve ID (lugemise ID)</param>
        /// <returns>Uuendatud lugemine koos arve staatusega</returns>
        /// <response code="200">Arve märgitud mittemakstud edukalt</response>
        /// <response code="400">Viga arve töötlemisel</response>
        /// <response code="401">Autentimine puudub</response>
        /// <response code="403">Ainult admin saab arveid mittemakstud märkida</response>
        [HttpPatch("{id}/unpay")]
        [Authorize(Roles = "admin")]
        [SwaggerOperation(
            Summary = "Märgi arve mittemakstud",
            Description = "Admin saab arve mittemakstud märkida. Nõuab admin õiguseid.",
            Tags = new[] { "Bills" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkAsUnpaid(string id)
        {
            try
            {
                var reading = await _billService.MarkAsUnpaidAsync(id);
                return Ok(new { message = "Bill marked as unpaid", reading });
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to mark bill as unpaid",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Saada meeldetuletus arve tasumiseks (admin ainult)
        /// </summary>
        /// <remarks>
        /// Saadab emaili teel meeldetuletuse konkreetse maksmata arve tasumiseks.
        /// Email saadetakse arve omanikule.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/bills/507f1f77bcf86cd799439011/send-reminder
        ///     Authorization: Bearer {admin_token}
        /// </remarks>
        /// <param name="id">Arve ID (lugemise ID)</param>
        /// <returns>Kinnitussõnum emaili saatmise kohta</returns>
        /// <response code="200">Meeldetuletus saadetud edukalt</response>
        /// <response code="400">Viga meeldetuletuse saatmisel või arve on juba tasutud</response>
        /// <response code="401">Autentimine puudub</response>
        /// <response code="403">Ainult admin saab meeldetuletusi saata</response>
        [HttpPost("{id}/send-reminder")]
        [Authorize(Roles = "admin")]
        [SwaggerOperation(
            Summary = "Saada meeldetuletus arve tasumiseks",
            Description = "Admin saab saata arve tasumise meeldetuletuse emaili teel. Nõuab admin õiguseid.",
            Tags = new[] { "Bills" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SendReminder(string id)
        {
            try
            {
                var result = await _emailService.SendReminderAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to send reminder",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Saada meeldetuletused kõigile maksmata arvetele (admin ainult)
        /// </summary>
        /// <remarks>
        /// Saadab emaili teel meeldetuletused kõigile kasutajatele, kellel on maksmata arveid.
        /// Igale kasutajale saadetakse üks email tema kõigi maksmata arvete kohta.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/bills/send-all-reminders
        ///     Authorization: Bearer {admin_token}
        /// </remarks>
        /// <returns>Statistika saadetud meeldetuletuste kohta</returns>
        /// <response code="200">Meeldetuletused saadetud edukalt</response>
        /// <response code="400">Viga meeldetuletuste saatmisel</response>
        /// <response code="401">Autentimine puudub</response>
        /// <response code="403">Ainult admin saab massilisi meeldetuletusi saata</response>
        [HttpPost("send-all-reminders")]
        [Authorize(Roles = "admin")]
        [SwaggerOperation(
            Summary = "Saada meeldetuletused kõigile maksmata arvetele",
            Description = "Admin saab saata meeldetuletused kõigile kasutajatele, kellel on maksmata arveid. Nõuab admin õiguseid.",
            Tags = new[] { "Bills" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SendAllReminders()
        {
            try
            {
                var result = await _emailService.SendAllRemindersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to send all reminders",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
    }
}