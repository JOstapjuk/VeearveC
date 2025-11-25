using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Veearve.Services;

namespace Veearve.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BillsController : ControllerBase
    {
        private readonly IBillService _billService;
        private readonly IEmailService _emailService;

        public BillsController(IBillService billService, IEmailService emailService)
        {
            _billService = billService;
            _emailService = emailService;
        }

        [HttpGet("unpaid")]
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/pay")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> MarkAsPaid(string id)
        {
            try
            {
                var reading = await _billService.MarkAsPaidAsync(id);
                return Ok(new { message = "Bill marked as paid", reading });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/unpay")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> MarkAsUnpaid(string id)
        {
            try
            {
                var reading = await _billService.MarkAsUnpaidAsync(id);
                return Ok(new { message = "Bill marked as unpaid", reading });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/send-reminder")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendReminder(string id)
        {
            try
            {
                var result = await _emailService.SendReminderAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("send-all-reminders")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendAllReminders()
        {
            try
            {
                var result = await _emailService.SendAllRemindersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
