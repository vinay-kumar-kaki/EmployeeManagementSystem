using EmployeeManagementSystem.API.Models.DTOs.Dashboard;

namespace EmployeeManagementSystem.API.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<AdminDashboardStatsDto> GetAdminStatsAsync();
        Task<EmployeeDashboardStatsDto> GetEmployeeStatsAsync(int employeeId);
    }
}
