using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickSearch.DataAccess;
using QuickSearch.LoggerUtility;
using QuickSearch.Model;

namespace QuickSearch.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserDbService _userDbService;
        private readonly ILogger _logger;

        public UsersController(IUserDbService userDbService, ILogger logger)
        {
            _userDbService = userDbService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userDbService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in GetAllUsers: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "UsersController"
                });
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }

        [HttpDelete("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var success = await _userDbService.DeleteUserAsync(userId);
                if (!success)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Status = 404,
                        Error = "Not Found",
                        Message = $"User with ID {userId} does not exist."
                    });
                }

                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"User with ID {userId} deleted successfully.",
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    Source = "UsersController"
                });

                return Ok(new { Message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in DeleteUser: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "UsersController"
                });
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }
    }
}
