using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using QuickSearch.LoggerUtility;
using QuickSearch.Model;
using QuickSearch.DataAccess;

namespace QuickSearch.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserDbService _userDbService;
        private readonly ILogger _logger;

        public AuthController(IUserDbService userDbService, ILogger logger)
        {
            _userDbService = userDbService;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Username and password are required."
                    });
                }

                // Compute SHA256 of the input password
                string passwordHash = ComputeSha256Hash(request.Password);
                Console.WriteLine($"[AUTH DEBUG] Username: '{request.Username}', Raw Password Length: {request.Password.Length}, Computed Hash: '{passwordHash}'");

                var loginResponse = await _userDbService.AuthenticateAdminAsync(request.Username, passwordHash);

                if (loginResponse.Success)
                {
                    await _logger.LogAsync(new LoggerRequestModel
                    {
                        Message = $"Admin user {loginResponse.Username} logged in successfully.",
                        Timestamp = DateTime.UtcNow,
                        Level = "Information",
                        Source = "AuthController"
                    });

                    return Ok(loginResponse);
                }

                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Failed login attempt for username: {request.Username}.",
                    Timestamp = DateTime.UtcNow,
                    Level = "Warning",
                    Source = "AuthController"
                });

                return Unauthorized(loginResponse);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(new LoggerRequestModel
                {
                    Message = $"Error in login endpoint: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Source = "AuthController"
                });

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = $"An unexpected error occurred during login: {ex.Message}"
                });
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
