using System.Security.Claims;
using EmployeeManagementSystem.API.Models.DTOs.Auth;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates an Admin or Employee and returns a JWT token.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.LoginAsync(dto);
            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Registers a new Admin user. Restricted to authenticated Admins.
        /// </summary>
        [HttpPost("register-admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.RegisterAdminAsync(dto);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Public self-registration for Employee accounts.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterEmployeeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.RegisterEmployeeAsync(dto);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Gets the profile information of the currently authenticated user from the JWT claims.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User identifier claim not found in token." });
            }

            var user = await _authService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(user);
        }
    }
}
