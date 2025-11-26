using MongoDB.Driver;
using Veearve.Data;
using Veearve.Models;

namespace Veearve.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(string userId);
        Task<UserDto> UpdateUserAsync(string userId, UpdateUserDto updateDto);
        Task DeleteUserAsync(string userId);
        Task ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    }

    public class UserService : IUserService
    {
        private readonly MongoDbContext _context;

        public UserService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            var user = await _context.Users
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new Exception("User not found");
            }

            return user;
        }

        public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserDto updateDto)
        {
            var user = await GetUserByIdAsync(userId);

            var updateDefinition = Builders<User>.Update;
            var updates = new List<UpdateDefinition<User>>();

            if (!string.IsNullOrEmpty(updateDto.Name))
                updates.Add(updateDefinition.Set("name", updateDto.Name));

            if (!string.IsNullOrEmpty(updateDto.ApartmentNumber))
                updates.Add(updateDefinition.Set("apartmentNumber", updateDto.ApartmentNumber));

            if (!string.IsNullOrEmpty(updateDto.Email))
                updates.Add(updateDefinition.Set("email", updateDto.Email));

            if (updates.Count > 0)
            {
                var combinedUpdate = updateDefinition.Combine(updates);
                await _context.Users.UpdateOneAsync(u => u.Id == userId, combinedUpdate);
                user = await GetUserByIdAsync(userId);
            }

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                ApartmentNumber = user.ApartmentNumber,
                Role = user.Role
            };
        }

        public async Task DeleteUserAsync(string userId)
        {
            // Kustuta kasutaja
            await _context.Users.DeleteOneAsync(u => u.Id == userId);

            // Kustuta kõik selle kasutaja lugemid
            await _context.Readings.DeleteManyAsync(r => r.UserId == userId);
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            var user = await GetUserByIdAsync(userId);

            // Kinnita praegune parool
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.Password))
            {
                throw new Exception("Current password is incorrect");
            }

            // Uue parooli hash
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);

            // Parooli uuendamine
            var updateDefinition = Builders<User>.Update.Set("password", hashedPassword);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, updateDefinition);
        }
    }
}