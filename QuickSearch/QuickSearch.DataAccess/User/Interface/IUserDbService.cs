using QuickSearch.Model;

namespace QuickSearch.DataAccess
{
    public interface IUserDbService
    {
        Task<LoginResponse> AuthenticateAdminAsync(string username, string passwordHash);
        Task<IEnumerable<UserResponse>> GetAllUsersAsync();
        Task<bool> DeleteUserAsync(int userId);
    }
}
