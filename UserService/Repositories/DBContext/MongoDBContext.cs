using MongoDB.Driver;
using UserService.Models;
using VaultSharp.V1.Commons;

namespace UserService.Repositories.DBContext
{
    public class MongoDBContext
    {
        private IMongoDatabase _database;
        private IMongoClient _client;
        IConfiguration _configuration;

        public MongoDBContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _client = new MongoClient(Environment.GetEnvironmentVariable("ConnectionString"));
            _database = _client.GetDatabase(Environment.GetEnvironmentVariable("DatabaseName"));
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }
    }
}