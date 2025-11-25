using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Veearve.Data;
using Veearve.Models;

namespace Veearve.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task CreateAdminUserAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly MongoDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(MongoDbContext context, IOptions<JwtSettings> jwtSettings, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<LoginResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .Find(u => u.Email == registerDto.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                throw new Exception("User already exists");
            }

            // Hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            // Create user
            var user = new User
            {
                Email = registerDto.Email,
                Password = hashedPassword,
                Name = registerDto.Name,
                ApartmentNumber = registerDto.ApartmentNumber,
                Role = "user"
            };

            await _context.Users.InsertOneAsync(user);

            // Generate token
            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    ApartmentNumber = user.ApartmentNumber,
                    Role = user.Role
                }
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            _logger.LogInformation($"Login attempt for: {loginDto.Email}");

            var user = await _context.Users
                .Find(u => u.Email == loginDto.Email)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User not found");
                throw new Exception("Invalid credentials");
            }

            _logger.LogInformation("User found, verifying password");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                _logger.LogWarning("Password verification failed");
                throw new Exception("Invalid credentials");
            }

            _logger.LogInformation("Password verified, generating token");
            _logger.LogInformation($"SecretKey is null: {_jwtSettings.SecretKey == null}");
            _logger.LogInformation($"Issuer is null: {_jwtSettings.Issuer == null}");

            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    ApartmentNumber = user.ApartmentNumber,
                    Role = user.Role
                }
            };
        }

        public async Task CreateAdminUserAsync()
        {
            try
            {
                var adminEmail = "admin@gmail.com";
                var adminExists = await _context.Users
                    .Find(u => u.Email == adminEmail)
                    .FirstOrDefaultAsync();

                if (adminExists == null)
                {
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword("admin123");
                    var admin = new User
                    {
                        Email = adminEmail,
                        Password = hashedPassword,
                        Name = "Administrator",
                        Role = "admin"
                    };

                    await _context.Users.InsertOneAsync(admin);
                    _logger.LogInformation("Admin user created successfully");
                    _logger.LogInformation("Email: admin@gmail.com");
                    _logger.LogInformation("Password: admin123");
                }
                else
                {
                    _logger.LogInformation("Admin user already exists");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating admin user: {ex.Message}");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim("userId", user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
