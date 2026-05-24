using MongoDB.Driver;

namespace QuickSearch.LoggerUtility
{
    public class Logger : ILogger
    {
        #region Fields
        private readonly IMongoCollection<LoggerRequestModel> _logs;
        #endregion

        #region Constructors
        public Logger(string connectionString, string databaseName, string collectionName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _logs = database.GetCollection<LoggerRequestModel>(collectionName);
        }
        #endregion

        #region Public Methods
        public async Task LogAsync(LoggerRequestModel log)
        {
            await _logs.InsertOneAsync(log);
        }
        #endregion
    }
}
