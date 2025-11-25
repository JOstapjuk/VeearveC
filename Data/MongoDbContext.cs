using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Veearve.Models;

namespace Veearve.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            // Ensure MongoDB is configured before creating collections
            MongoDbConfiguration.Configure();

            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
        public IMongoCollection<Reading> Readings => _database.GetCollection<Reading>("readings");
    }
}
