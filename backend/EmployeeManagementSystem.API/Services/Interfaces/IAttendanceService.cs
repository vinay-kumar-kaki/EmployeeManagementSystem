using EmployeeManagementSystem.API.Models.DTOs.Attendance;

namespace EmployeeManagementSystem.API.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task<AttendanceStatusDto> GetTodayStatusAsync(int employeeId);
        Task<AttendanceStatusDto> CheckInAsync(int employeeId);
        Task<AttendanceStatusDto> CheckOutAsync(int employeeId);
        Task<IEnumerable<AttendanceRecordDto>> GetMyAttendanceHistoryAsync(int employeeId);
        Task<IEnumerable<AttendanceRecordDto>> GetAllAttendanceAsync(DateTime? date = null);
    }
}
