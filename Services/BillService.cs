using MongoDB.Driver;
using Veearve.Data;
using Veearve.Models;

namespace Veearve.Services
{
    public interface IBillService
    {
        Task<UnpaidBillsDto> GetUnpaidBillsAsync(string userId, string role);
        Task<AnnualReportDto> GetAnnualReportAsync(int? year, string userId, string role);
        Task<Reading> MarkAsPaidAsync(string id);
        Task<Reading> MarkAsUnpaidAsync(string id);
    }

    public class BillService : IBillService
    {
        private readonly MongoDbContext _context;

        public BillService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<UnpaidBillsDto> GetUnpaidBillsAsync(string userId, string role)
        {
            var filterBuilder = Builders<Reading>.Filter;
            var filter = filterBuilder.Eq(r => r.IsPaid, false);

            // Non-admin users only see their own unpaid bills
            if (role != "admin")
            {
                filter = filterBuilder.And(
                    filter,
                    filterBuilder.Eq(r => r.UserId, userId)
                );
            }

            var unpaidBills = await _context.Readings
                .Find(filter)
                .SortByDescending(r => r.Date)
                .ToListAsync();

            var totalAmount = unpaidBills.Sum(b => b.Amount);

            return new UnpaidBillsDto
            {
                Count = unpaidBills.Count,
                TotalAmount = totalAmount.ToString("F2"),
                Bills = unpaidBills
            };
        }

        public async Task<AnnualReportDto> GetAnnualReportAsync(int? year, string userId, string role)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var startDate = new DateTime(targetYear, 1, 1);
            var endDate = new DateTime(targetYear, 12, 31, 23, 59, 59);

            var filterBuilder = Builders<Reading>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Gte(r => r.Date, startDate),
                filterBuilder.Lte(r => r.Date, endDate)
            );

            // Non-admin users only see their own data
            if (role != "admin")
            {
                filter = filterBuilder.And(
                    filter,
                    filterBuilder.Eq(r => r.UserId, userId)
                );
            }

            var readings = await _context.Readings
                .Find(filter)
                .SortBy(r => r.Date)
                .ToListAsync();

            var totalColdWater = readings.Sum(r => r.ColdWater);
            var totalHotWater = readings.Sum(r => r.HotWater);
            var totalAmount = readings.Sum(r => r.Amount);
            var paidAmount = readings.Where(r => r.IsPaid).Sum(r => r.Amount);

            return new AnnualReportDto
            {
                Year = targetYear,
                Summary = new AnnualSummaryDto
                {
                    TotalReadings = readings.Count,
                    TotalColdWater = totalColdWater.ToString("F2"),
                    TotalHotWater = totalHotWater.ToString("F2"),
                    TotalAmount = totalAmount.ToString("F2"),
                    PaidAmount = paidAmount.ToString("F2"),
                    UnpaidAmount = (totalAmount - paidAmount).ToString("F2")
                },
                Readings = readings
            };
        }

        public async Task<Reading> MarkAsPaidAsync(string id)
        {
            var reading = await _context.Readings
                .Find(r => r.Id == id)
                .FirstOrDefaultAsync();

            if (reading == null)
            {
                throw new Exception("Bill not found");
            }

            var update = Builders<Reading>.Update.Set(r => r.IsPaid, true);
            await _context.Readings.UpdateOneAsync(r => r.Id == id, update);

            return await _context.Readings.Find(r => r.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Reading> MarkAsUnpaidAsync(string id)
        {
            var reading = await _context.Readings
                .Find(r => r.Id == id)
                .FirstOrDefaultAsync();

            if (reading == null)
            {
                throw new Exception("Bill not found");
            }

            var update = Builders<Reading>.Update.Set(r => r.IsPaid, false);
            await _context.Readings.UpdateOneAsync(r => r.Id == id, update);

            return await _context.Readings.Find(r => r.Id == id).FirstOrDefaultAsync();
        }
    }
}
