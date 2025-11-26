using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Tagastab sisselogitud kasutaja andmed
        /// </summary>
        /// <remarks>
        /// Võimaldab kasutajal vaadata oma profiili andmeid, sealhulgas email, nimi, korteri number ja roll.
        /// 
        /// Sample request:
        /// 
        ///     GET /api/users/me
        ///     Authorization: Bearer {your_token}
        /// </remarks>
        /// <returns>Kasutaja profiili andmed</returns>
        /// <response code="200">Kasutaja andmed edukalt leitud</response>
        /// <response code="401">Autentimine puudub või token on kehtetu</response>
        /// <response code="404">Kasutajat ei leitud</response>
        [HttpGet("me")]
        [SwaggerOperation(
            Summary = "Hangi enda andmed",
            Description = "Tagastab sisselogitud kasutaja profiili andmed.",
            Tags = new[] { "Users" }
        )]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
                return NotFound(new ProblemDetails
                {
                    Title = "User not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
        }

        /// <summary>
        /// Uuendab sisselogitud kasutaja andmeid
        /// </summary>
        /// <remarks>
        /// Võimaldab kasutajal uuendada oma profiili andmeid (nimi, email, korteri number).
        /// 
        /// Sample request:
        /// 
        ///     PUT /api/users/me
        ///     {
        ///        "name": "Uus Nimi",
        ///        "email": "uus.email@example.com",
        ///        "apartmentNumber": "102"
        ///     }
        /// </remarks>
        /// <param name="updateDto">Uuendatavad kasutaja andmed</param>
        /// <returns>Uuendatud kasutaja andmed</returns>
        /// <response code="200">Kasutaja andmed uuendatud edukalt</response>
        /// <response code="400">Vigased sisendandmed või email on juba kasutusel</response>
        /// <response code="401">Autentimine puudub</response>
        [HttpPut("me")]
        [SwaggerOperation(
            Summary = "Uuenda enda andmeid",
            Description = "Uuendab sisselogitud kasutaja profiili andmeid.",
            Tags = new[] { "Users" }
        )]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to update user",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Muuda sisselogitud kasutaja parooli
        /// </summary>
        /// <remarks>
        /// Võimaldab kasutajal muuta oma parooli. Nõutav on vana parool kinnitamiseks.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/users/me/change-password
        ///     {
        ///        "oldPassword": "VanaParool123!",
        ///        "newPassword": "UusParool456!"
        ///     }
        /// </remarks>
        /// <param name="changePasswordDto">Parooli muutmise andmed (vana ja uus parool)</param>
        /// <returns>Kinnitussõnum</returns>
        /// <response code="200">Parool muudetud edukalt</response>
        /// <response code="400">Vana parool on vale või uus parool ei vasta nõuetele</response>
        /// <response code="401">Autentimine puudub</response>
        [HttpPost("me/change-password")]
        [SwaggerOperation(
            Summary = "Muuda parooli",
            Description = "Muudab sisselogitud kasutaja parooli. Nõuab vana parooli kinnitamiseks.",
            Tags = new[] { "Users" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to change password",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Kustutab sisselogitud kasutaja konto
        /// </summary>
        /// <remarks>
        /// Kustutab sisselogitud kasutaja konto püsivalt. Seda tegevust ei saa tagasi võtta.
        /// Kõik kasutaja lugemised ja arved kustutatakse samuti.
        /// 
        /// Sample request:
        /// 
        ///     DELETE /api/users/me
        ///     Authorization: Bearer {your_token}
        /// </remarks>
        /// <returns>Kinnitussõnum</returns>
        /// <response code="200">Konto kustutatud edukalt</response>
        /// <response code="400">Viga konto kustutamisel</response>
        /// <response code="401">Autentimine puudub</response>
        [HttpDelete("me")]
        [SwaggerOperation(
            Summary = "Kustuta konto",
            Description = "Kustutab sisselogitud kasutaja konto püsivalt. Seda tegevust ei saa tagasi võtta.",
            Tags = new[] { "Users" }
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
                return BadRequest(new ProblemDetails
                {
                    Title = "Failed to delete account",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
    }
}