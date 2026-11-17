using EmployeeManagementSystem.API.Models.Entities;

namespace EmployeeManagementSystem.API.Services.Interfaces
{
    public interface ITokenService
    {
        Task<(string token, DateTime expiresAt)> GenerateTokenAsync(ApplicationUser user, int? employeeId = null);
    }
}
