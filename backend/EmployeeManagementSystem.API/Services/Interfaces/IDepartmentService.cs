using EmployeeManagementSystem.API.Models.DTOs.Departments;

namespace EmployeeManagementSystem.API.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetAllAsync(bool activeOnly = false);
        Task<DepartmentDto?> GetByIdAsync(int id);
        Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);
        Task<DepartmentDto?> UpdateAsync(int id, UpdateDepartmentDto dto);
        Task<bool> DeleteAsync(int id); // soft delete / toggle IsActive
    }
}
