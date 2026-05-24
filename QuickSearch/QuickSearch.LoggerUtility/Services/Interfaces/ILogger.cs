
namespace QuickSearch.LoggerUtility
{
    public interface ILogger
    {
        Task LogAsync(LoggerRequestModel log);
    }
}