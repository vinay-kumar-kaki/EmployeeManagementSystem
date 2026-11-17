using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Models.DTOs.Dashboard;
using EmployeeManagementSystem.API.Models.Entities;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardStatsDto> GetAdminStatsAsync()
        {
            var today = DateTime.UtcNow.Date;

            var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive);
            var activeDepartments = await _context.Departments.CountAsync(d => d.IsActive);
            var pendingLeaves = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending);
            var presentToday = await _context.Attendances.CountAsync(a => a.Date == today);

            var rate = totalEmployees > 0 
                ? $"{(int)Math.Round((double)presentToday / totalEmployees * 100)}%" 
                : "0%";

            return new AdminDashboardStatsDto
            {
                TotalEmployees = totalEmployees,
                ActiveDepartments = activeDepartments,
                PendingLeaves = pendingLeaves,
                PresentTodayCount = presentToday,
                TodayAttendanceRate = rate
            };
        }

        public async Task<EmployeeDashboardStatsDto> GetEmployeeStatsAsync(int employeeId)
        {
            var today = DateTime.UtcNow.Date;

            var leaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == employeeId)
                .ToListAsync();

            var todayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today);

            return new EmployeeDashboardStatsDto
            {
                TotalLeavesApplied = leaves.Count,
                ApprovedLeaves = leaves.Count(l => l.Status == LeaveStatus.Approved),
                PendingLeaves = leaves.Count(l => l.Status == LeaveStatus.Pending),
                RejectedLeaves = leaves.Count(l => l.Status == LeaveStatus.Rejected),
                HasCheckedInToday = todayAttendance != null,
                FormattedTodayHours = todayAttendance?.WorkDurationMinutes.HasValue == true
                    ? $"{todayAttendance.WorkDurationMinutes.Value / 60}h {todayAttendance.WorkDurationMinutes.Value % 60}m"
                    : (todayAttendance != null ? "Active" : "Not Checked In")
            };
        }
    }
}
