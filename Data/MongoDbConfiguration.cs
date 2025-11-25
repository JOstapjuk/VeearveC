using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Veearve.Models;

namespace Veearve.Data
{
    public static class MongoDbConfiguration
    {
        private static bool _isConfigured = false;
        private static readonly object _lock = new object();

        public static void Configure()
        {
            lock (_lock)
            {
                if (_isConfigured)
                    return;

                // Register conventions
                var conventionPack = new ConventionPack
                {
                    new IgnoreExtraElementsConvention(true)
                };
                ConventionRegistry.Register("CustomConventions", conventionPack, t => true);

                // Explicitly register User class map if not already registered
                if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
                {
                    BsonClassMap.RegisterClassMap<User>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }

                // Register Reading class map if it exists
                if (!BsonClassMap.IsClassMapRegistered(typeof(Reading)))
                {
                    BsonClassMap.RegisterClassMap<Reading>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }

                _isConfigured = true;
            }
        }
    }
}