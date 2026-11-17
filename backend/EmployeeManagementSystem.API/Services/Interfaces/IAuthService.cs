using EmployeeManagementSystem.API.Models.DTOs.Auth;

namespace EmployeeManagementSystem.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RegisterAdminAsync(RegisterAdminDto dto);
        Task<AuthResponseDto> RegisterEmployeeAsync(RegisterEmployeeDto dto);
        Task<CurrentUserDto?> GetCurrentUserAsync(string userId);
    }
}
