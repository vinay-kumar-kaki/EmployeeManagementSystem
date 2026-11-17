using EmployeeManagementSystem.API.Models.DTOs.Employees;

namespace EmployeeManagementSystem.API.Services.Interfaces
{
    public interface IEmployeeService
    {
        Task<IEnumerable<EmployeeDto>> GetAllAsync(string? search = null, int? departmentId = null);
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<EmployeeProfileDto?> GetProfileByUserIdAsync(string userId);
        Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
        Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
