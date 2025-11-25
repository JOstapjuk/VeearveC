using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Veearve.Models;
using Veearve.Services;

namespace Veearve.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var user = await _userService.GetUserByIdAsync(userId);

                return Ok(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    ApartmentNumber = user.ApartmentNumber,
                    Role = user.Role
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto updateDto)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var updatedUser = await _userService.UpdateUserAsync(userId, updateDto);
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                await _userService.ChangePasswordAsync(userId, changePasswordDto);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMe()
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                await _userService.DeleteUserAsync(userId);
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}