using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Models.DTOs.Attendance;
using EmployeeManagementSystem.API.Models.Entities;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Services.Implementations
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AttendanceStatusDto> GetTodayStatusAsync(int employeeId)
        {
            var today = DateTime.Today;
            var record = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today);

            if (record == null)
            {
                return new AttendanceStatusDto
                {
                    HasCheckedInToday = false,
                    HasCheckedOutToday = false,
                    Status = "Absent"
                };
            }

            return new AttendanceStatusDto
            {
                HasCheckedInToday = true,
                HasCheckedOutToday = record.CheckOutTime.HasValue,
                CheckInTime = record.CheckInTime,
                CheckOutTime = record.CheckOutTime,
                WorkDurationMinutes = record.WorkDurationMinutes,
                Status = record.Status
            };
        }

        public async Task<AttendanceStatusDto> CheckInAsync(int employeeId)
        {
            var today = DateTime.Today;
            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today);

            if (existing != null)
            {
                throw new InvalidOperationException("You have already checked in for today.");
            }

            var now = DateTime.UtcNow;
            var attendance = new Attendance
            {
                EmployeeId = employeeId,
                Date = today,
                CheckInTime = now,
                Status = "Present"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return new AttendanceStatusDto
            {
                HasCheckedInToday = true,
                HasCheckedOutToday = false,
                CheckInTime = attendance.CheckInTime,
                Status = attendance.Status
            };
        }

        public async Task<AttendanceStatusDto> CheckOutAsync(int employeeId)
        {
            var today = DateTime.Today;
            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today);

            if (existing == null)
            {
                throw new InvalidOperationException("You must check in before checking out.");
            }

            if (existing.CheckOutTime.HasValue)
            {
                throw new InvalidOperationException("You have already checked out for today.");
            }

            var now = DateTime.UtcNow;
            existing.CheckOutTime = now;
            var duration = (int)(now - existing.CheckInTime).TotalMinutes;
            existing.WorkDurationMinutes = Math.Max(1, duration);

            await _context.SaveChangesAsync();

            return new AttendanceStatusDto
            {
                HasCheckedInToday = true,
                HasCheckedOutToday = true,
                CheckInTime = existing.CheckInTime,
                CheckOutTime = existing.CheckOutTime,
                WorkDurationMinutes = existing.WorkDurationMinutes,
                Status = existing.Status
            };
        }

        public async Task<IEnumerable<AttendanceRecordDto>> GetMyAttendanceHistoryAsync(int employeeId)
        {
            return await _context.Attendances
                .Include(a => a.Employee).ThenInclude(e => e!.Department)
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.Date)
                .Take(30)
                .Select(a => new AttendanceRecordDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = $"{a.Employee!.FirstName} {a.Employee.LastName}".Trim(),
                    DepartmentName = a.Employee!.Department != null ? a.Employee!.Department.Name : "None",
                    Date = a.Date,
                    CheckInTime = a.CheckInTime,
                    CheckOutTime = a.CheckOutTime,
                    WorkDurationMinutes = a.WorkDurationMinutes,
                    Status = a.Status
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<AttendanceRecordDto>> GetAllAttendanceAsync(DateTime? date = null)
        {
            var targetDate = (date ?? DateTime.UtcNow).Date;

            return await _context.Attendances
                .Include(a => a.Employee).ThenInclude(e => e!.Department)
                .Where(a => a.Date == targetDate)
                .OrderBy(a => a.CheckInTime)
                .Select(a => new AttendanceRecordDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = $"{a.Employee!.FirstName} {a.Employee.LastName}".Trim(),
                    DepartmentName = a.Employee!.Department != null ? a.Employee!.Department.Name : "None",
                    Date = a.Date,
                    CheckInTime = a.CheckInTime,
                    CheckOutTime = a.CheckOutTime,
                    WorkDurationMinutes = a.WorkDurationMinutes,
                    Status = a.Status
                })
                .ToListAsync();
        }
    }
}
