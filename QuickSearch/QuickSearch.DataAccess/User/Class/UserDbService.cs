using Microsoft.EntityFrameworkCore;
using QuickSearch.Data.Entities;
using QuickSearch.Data.Repositories;
using QuickSearch.Model;

namespace QuickSearch.DataAccess
{
    public class UserDbService : IUserDbService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserRole> _userRoleRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<UserLog> _userLogRepository;

        public UserDbService(
            IRepository<User> userRepository,
            IRepository<UserRole> userRoleRepository,
            IRepository<Role> roleRepository,
            IRepository<UserLog> userLogRepository)
        {
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
            _userLogRepository = userLogRepository;
        }

        public async Task<LoginResponse> AuthenticateAdminAsync(string username, string passwordHash)
        {
            // Join Users -> UserRoles -> Roles using LINQ IQueryable
            var adminQuery = from u in _userRepository.Query()
                             join ur in _userRoleRepository.Query() on u.Id equals ur.UserId
                             join r in _roleRepository.Query() on ur.RoleId equals r.Id
                             where u.Username == username 
                                && u.PasswordHash == passwordHash 
                                && r.Name == "Admin"
                             select u;

            var adminUser = await adminQuery.FirstOrDefaultAsync();

            if (adminUser == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username, password, or insufficient permissions."
                };
            }

            // Write to user_logs using EF Core UserLog repository
            var newLog = new UserLog
            {
                UserId = adminUser.Id,
                LoginTime = DateTime.Now
            };

            await _userLogRepository.AddAsync(newLog);
            await _userLogRepository.SaveChangesAsync();

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful.",
                Username = adminUser.Username,
                Email = adminUser.Email ?? string.Empty
            };
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
        {
            var usersWithRoles = await (from u in _userRepository.Query()
                                       join ur in _userRoleRepository.Query() on u.Id equals ur.UserId
                                       join r in _roleRepository.Query() on ur.RoleId equals r.Id
                                       select new UserResponse
                                       {
                                           Id = u.Id,
                                           Username = u.Username,
                                           Email = u.Email,
                                           CreatedAt = u.CreatedAt,
                                           Role = r.Name
                                       }).ToListAsync();

            return usersWithRoles;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // Delete associated user roles first
            var userRoles = await _userRoleRepository.Query()
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            foreach (var ur in userRoles)
            {
                _userRoleRepository.Delete(ur);
            }
            await _userRoleRepository.SaveChangesAsync();

            // Delete user logs
            var userLogs = await _userLogRepository.Query()
                .Where(ul => ul.UserId == userId)
                .ToListAsync();

            foreach (var ul in userLogs)
            {
                _userLogRepository.Delete(ul);
            }
            await _userLogRepository.SaveChangesAsync();

            // Delete the user
            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }
    }
}
