using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QuickSearch.LoggerUtility
{
    public class LoggerRequestModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
    }
}