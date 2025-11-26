using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Veearve.Models;
using Veearve.Services;

namespace Veearve.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registreeri uus elanik (ainult korteriomanikud)
        /// </summary>
        /// <remarks>
        /// Registreerib uue kasutaja rolliga "user". Süsteemis on juba olemas administraator.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/register
        ///     {
        ///        "email": "user@example.com",
        ///        "password": "SecurePassword123!",
        ///        "name": "John Doe",
        ///        "apartmentNumber": "101"
        ///     }
        /// </remarks>
        /// <param name="registerDto">Kasutaja registreerimise andmed</param>
        /// <returns>Loodud kasutaja andmed</returns>
        /// <response code="201">Kasutaja registreeriti edukalt</response>
        /// <response code="400">Vigased sisendandmed või kasutaja juba eksisteerib</response>
        [HttpPost("register")]
        [SwaggerOperation(
            Summary = "Registreeri uus elanik",
            Description = "Registreerib uue kasutaja rolliga 'user'. Igal korteriomanikel on võimalus luua konto.",
            Tags = new[] { "Authentication" }
        )]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                return StatusCode(201, new { message = "User registered successfully", user = result.User });
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Registration failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Kasutaja sisselogimine
        /// </summary>
        /// <remarks>
        /// Sisselogimine tagastab JWT tokeni, mida kasutatakse autentimiseks teiste API päringute jaoks.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/login
        ///     {
        ///        "email": "user@example.com",
        ///        "password": "SecurePassword123!"
        ///     }
        /// </remarks>
        /// <param name="loginDto">Kasutaja sisselogimise andmed</param>
        /// <returns>JWT token ja kasutaja andmed</returns>
        /// <response code="200">Sisselogimine õnnestus</response>
        /// <response code="401">Kehtetud autentimisandmed</response>
        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Kasutaja sisselogimine",
            Description = "Tagastab JWT tokeni ja kasutaja andmed. Token tuleb lisada Authorization headerisse: 'Bearer {token}'",
            Tags = new[] { "Authentication" }
        )]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            try
            {
                var result = await _authService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Login failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }
        }
    }
}