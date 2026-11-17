using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Models.DTOs.Auth;
using EmployeeManagementSystem.API.Models.Entities;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _context = context;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            // Check if user is associated with an Employee profile
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee != null && !employee.IsActive)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Your account has been deactivated. Please contact HR."
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Employee";

            var (token, expiresAt) = await _tokenService.GenerateTokenAsync(user, employee?.Id);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                ExpiresAt = expiresAt,
                Role = primaryRole,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                EmployeeId = employee?.Id
            };
        }

        public async Task<AuthResponseDto> RegisterAdminAsync(RegisterAdminDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An account with this email already exists."
                };
            }

            // Ensure Admin role exists
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Registration failed: {errors}"
                };
            }

            await _userManager.AddToRoleAsync(user, "Admin");

            var (token, expiresAt) = await _tokenService.GenerateTokenAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Admin registered successfully.",
                Token = token,
                ExpiresAt = expiresAt,
                Role = "Admin",
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim()
            };
        }

        public async Task<CurrentUserDto?> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            return new CurrentUserDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Role = roles.FirstOrDefault() ?? "Employee",
                EmployeeId = employee?.Id
            };
        }
    }
}
