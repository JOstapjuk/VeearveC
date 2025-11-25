using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using Veearve.Models;
using Veearve.Services;

namespace Veearve.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReadingsController : ControllerBase
    {
        private readonly IReadingService _readingService;
        private readonly IUserService _userService;

        public ReadingsController(IReadingService readingService, IUserService userService)
        {
            _readingService = readingService;
            _userService = userService;
        }

        [HttpGet]
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
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
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateReading([FromBody] CreateReadingDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst("userId")?.Value;
                var user = await _userService.GetUserByIdAsync(userId);

                var reading = await _readingService.CreateReadingAsync(dto, userId, user.Name);
                return StatusCode(201, reading);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
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
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
