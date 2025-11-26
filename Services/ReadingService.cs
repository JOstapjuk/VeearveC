using MongoDB.Driver;
using Veearve.Data;
using Veearve.Models;

namespace Veearve.Services
{
    public interface IReadingService
    {
        Task<List<Reading>> GetReadingsAsync(string userId, string role, DateTime? startDate, DateTime? endDate);
        Task<Reading> GetReadingByIdAsync(string id, string userId, string role);
        Task<Reading> CreateReadingAsync(CreateReadingDto dto, string userId, string userName);
        Task<Reading> UpdateReadingAsync(string id, UpdateReadingDto dto, string userId, string role);
        Task DeleteReadingAsync(string id, string userId, string role);
    }

    public class ReadingService : IReadingService
    {
        private readonly MongoDbContext _context;
        private readonly IConfiguration _configuration;
        private const double COLD_WATER_PRICE = 2.5;
        private const double HOT_WATER_PRICE = 4.5;

        public ReadingService(MongoDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<List<Reading>> GetReadingsAsync(string userId, string role, DateTime? startDate, DateTime? endDate)
        {
            var filterBuilder = Builders<Reading>.Filter;
            var filters = new List<FilterDefinition<Reading>>();

            // Mitteadministraatorid saavad näha ainult oma näiteid.
            if (role != "admin")
            {
                filters.Add(filterBuilder.Eq(r => r.UserId, userId));
            }

            // Kuupäevade vahemiku filter
            if (startDate.HasValue)
            {
                filters.Add(filterBuilder.Gte(r => r.Date, startDate.Value));
            }

            if (endDate.HasValue)
            {
                filters.Add(filterBuilder.Lte(r => r.Date, endDate.Value));
            }

            var filter = filters.Count > 0
                ? filterBuilder.And(filters)
                : filterBuilder.Empty;

            return await _context.Readings
                .Find(filter)
                .SortByDescending(r => r.Date)
                .ToListAsync();
        }

        public async Task<Reading> GetReadingByIdAsync(string id, string userId, string role)
        {
            var reading = await _context.Readings
                .Find(r => r.Id == id)
                .FirstOrDefaultAsync();

            if (reading == null)
            {
                throw new Exception("Reading not found");
            }

            // Kontrolli juurdepääsuõigusi
            if (role != "admin" && reading.UserId != userId)
            {
                throw new UnauthorizedAccessException("Access denied");
            }

            return reading;
        }

        public async Task<Reading> CreateReadingAsync(CreateReadingDto dto, string userId, string userName)
        {
            var amount = (dto.ColdWater * COLD_WATER_PRICE) + (dto.HotWater * HOT_WATER_PRICE);

            var reading = new Reading
            {
                ApartmentNumber = dto.ApartmentNumber,
                UserName = userName,
                UserId = userId,
                Date = dto.Date,
                ColdWater = dto.ColdWater,
                HotWater = dto.HotWater,
                Amount = amount,
                IsPaid = false
            };

            await _context.Readings.InsertOneAsync(reading);
            return reading;
        }

        public async Task<Reading> UpdateReadingAsync(string id, UpdateReadingDto dto, string userId, string role)
        {
            var reading = await GetReadingByIdAsync(id, userId, role);

            var updateDefinition = Builders<Reading>.Update;
            var updates = new List<UpdateDefinition<Reading>>();

            if (dto.ColdWater.HasValue)
                updates.Add(updateDefinition.Set(r => r.ColdWater, dto.ColdWater.Value));

            if (dto.HotWater.HasValue)
                updates.Add(updateDefinition.Set(r => r.HotWater, dto.HotWater.Value));

            if (dto.IsPaid.HasValue)
                updates.Add(updateDefinition.Set(r => r.IsPaid, dto.IsPaid.Value));

            // Kui vee väärtused on muutunud, arvuta summa uuesti
            if (dto.ColdWater.HasValue || dto.HotWater.HasValue)
            {
                var coldWater = dto.ColdWater ?? reading.ColdWater;
                var hotWater = dto.HotWater ?? reading.HotWater;
                var newAmount = (coldWater * COLD_WATER_PRICE) + (hotWater * HOT_WATER_PRICE);
                updates.Add(updateDefinition.Set(r => r.Amount, newAmount));
            }

            if (updates.Count > 0)
            {
                var combinedUpdate = updateDefinition.Combine(updates);
                await _context.Readings.UpdateOneAsync(r => r.Id == id, combinedUpdate);
                reading = await _context.Readings.Find(r => r.Id == id).FirstOrDefaultAsync();
            }

            return reading;
        }

        public async Task DeleteReadingAsync(string id, string userId, string role)
        {
            var reading = await GetReadingByIdAsync(id, userId, role);
            await _context.Readings.DeleteOneAsync(r => r.Id == id);
        }
    }
}
