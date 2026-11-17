using EmployeeManagementSystem.API.Models.DTOs.Leaves;

namespace EmployeeManagementSystem.API.Services.Interfaces
{
    public interface ILeaveService
    {
        Task<IEnumerable<LeaveTypeDto>> GetLeaveTypesAsync();
        Task<IEnumerable<LeaveTypeDto>> GetAllLeaveTypesAsync();
        Task<LeaveTypeDto> CreateLeaveTypeAsync(CreateLeaveTypeDto dto);
        Task<LeaveTypeDto?> UpdateLeaveTypeAsync(int id, UpdateLeaveTypeDto dto);
        Task<bool> DeactivateLeaveTypeAsync(int id);
        Task<LeaveRequestDto> ApplyLeaveAsync(int employeeId, ApplyLeaveDto dto);
        Task<IEnumerable<LeaveRequestDto>> GetEmployeeLeavesAsync(int employeeId);
        Task<IEnumerable<LeaveRequestDto>> GetPendingLeavesAsync();
        Task<IEnumerable<LeaveRequestDto>> GetAllLeavesAsync();
        Task<LeaveRequestDto?> ReviewLeaveAsync(int leaveId, ReviewLeaveDto dto);
    }
}
