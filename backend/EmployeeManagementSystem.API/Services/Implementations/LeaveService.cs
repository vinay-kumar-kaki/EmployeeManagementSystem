using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Models.DTOs.Leaves;
using EmployeeManagementSystem.API.Models.Entities;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Services.Implementations
{
    public class LeaveService : ILeaveService
    {
        private readonly ApplicationDbContext _context;

        public LeaveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LeaveTypeDto>> GetLeaveTypesAsync()
        {
            return await _context.LeaveTypes
                .Where(lt => lt.IsActive)
                .Select(lt => new LeaveTypeDto
                {
                    Id = lt.Id,
                    Name = lt.Name,
                    DefaultDaysPerYear = lt.DefaultDaysPerYear,
                    IsActive = lt.IsActive
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<LeaveTypeDto>> GetAllLeaveTypesAsync()
        {
            return await _context.LeaveTypes
                .OrderBy(lt => lt.Name)
                .Select(lt => new LeaveTypeDto
                {
                    Id = lt.Id,
                    Name = lt.Name,
                    DefaultDaysPerYear = lt.DefaultDaysPerYear,
                    IsActive = lt.IsActive
                })
                .ToListAsync();
        }

        public async Task<LeaveTypeDto> CreateLeaveTypeAsync(CreateLeaveTypeDto dto)
        {
            await EnsureLeaveTypeNameIsUniqueAsync(dto.Name);

            var leaveType = new LeaveType
            {
                Name = dto.Name.Trim(),
                DefaultDaysPerYear = dto.DefaultDaysPerYear,
                IsActive = dto.IsActive
            };

            _context.LeaveTypes.Add(leaveType);
            await _context.SaveChangesAsync();
            return ToDto(leaveType);
        }

        public async Task<LeaveTypeDto?> UpdateLeaveTypeAsync(int id, UpdateLeaveTypeDto dto)
        {
            var leaveType = await _context.LeaveTypes.FindAsync(id);
            if (leaveType == null) return null;

            await EnsureLeaveTypeNameIsUniqueAsync(dto.Name, id);

            leaveType.Name = dto.Name.Trim();
            leaveType.DefaultDaysPerYear = dto.DefaultDaysPerYear;
            leaveType.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return ToDto(leaveType);
        }

        public async Task<bool> DeactivateLeaveTypeAsync(int id)
        {
            var leaveType = await _context.LeaveTypes.FindAsync(id);
            if (leaveType == null) return false;

            leaveType.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task EnsureLeaveTypeNameIsUniqueAsync(string name, int? ignoreId = null)
        {
            var normalizedName = name.Trim().ToLower();
            var alreadyExists = await _context.LeaveTypes.AnyAsync(lt =>
                lt.Name.ToLower() == normalizedName &&
                (!ignoreId.HasValue || lt.Id != ignoreId.Value));

            if (alreadyExists)
            {
                throw new InvalidOperationException("A leave type with this name already exists.");
            }
        }

        private static LeaveTypeDto ToDto(LeaveType leaveType) => new LeaveTypeDto
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            DefaultDaysPerYear = leaveType.DefaultDaysPerYear,
            IsActive = leaveType.IsActive
        };

        public async Task<LeaveRequestDto> ApplyLeaveAsync(int employeeId, ApplyLeaveDto dto)
        {
            var startDate = dto.StartDate.Date;
            var endDate = dto.EndDate.Date;

            if (endDate < startDate)
            {
                throw new InvalidOperationException("End date cannot be earlier than start date.");
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                throw new InvalidOperationException("Employee record not found.");
            }

            var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
            if (leaveType == null || !leaveType.IsActive)
            {
                throw new InvalidOperationException("Invalid or inactive leave type selected.");
            }

            // Check for date overlap with any pending or approved leaves
            var hasOverlap = await _context.LeaveRequests.AnyAsync(l =>
                l.EmployeeId == employeeId &&
                l.Status != LeaveStatus.Rejected &&
                l.StartDate <= endDate &&
                l.EndDate >= startDate);

            if (hasOverlap)
            {
                throw new InvalidOperationException("You already have an active leave request covering these dates.");
            }

            var totalDays = (endDate - startDate).Days + 1;

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employeeId,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = startDate,
                EndDate = endDate,
                TotalDays = totalDays,
                Reason = dto.Reason.Trim(),
                Status = LeaveStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return new LeaveRequestDto
            {
                Id = leaveRequest.Id,
                EmployeeId = employee.Id,
                EmployeeName = $"{employee.FirstName} {employee.LastName}".Trim(),
                DepartmentName = employee.Department?.Name ?? "None",
                LeaveTypeId = leaveType.Id,
                LeaveTypeName = leaveType.Name,
                StartDate = leaveRequest.StartDate,
                EndDate = leaveRequest.EndDate,
                TotalDays = leaveRequest.TotalDays,
                Reason = leaveRequest.Reason,
                Status = leaveRequest.Status,
                AdminRemarks = leaveRequest.AdminRemarks,
                AppliedAt = leaveRequest.AppliedAt
            };
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetEmployeeLeavesAsync(int employeeId)
        {
            return await _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department)
                .Include(l => l.LeaveType)
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.AppliedAt)
                .Select(l => new LeaveRequestDto
                {
                    Id = l.Id,
                    EmployeeId = l.EmployeeId,
                    EmployeeName = $"{l.Employee!.FirstName} {l.Employee.LastName}".Trim(),
                    DepartmentName = l.Employee!.Department != null ? l.Employee!.Department.Name : "None",
                    LeaveTypeId = l.LeaveTypeId,
                    LeaveTypeName = l.LeaveType != null ? l.LeaveType.Name : "None",
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    TotalDays = l.TotalDays,
                    Reason = l.Reason,
                    Status = l.Status,
                    AdminRemarks = l.AdminRemarks,
                    AppliedAt = l.AppliedAt,
                    ReviewedAt = l.ReviewedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetPendingLeavesAsync()
        {
            return await _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department)
                .Include(l => l.LeaveType)
                .Where(l => l.Status == LeaveStatus.Pending)
                .OrderBy(l => l.AppliedAt)
                .Select(l => new LeaveRequestDto
                {
                    Id = l.Id,
                    EmployeeId = l.EmployeeId,
                    EmployeeName = $"{l.Employee!.FirstName} {l.Employee.LastName}".Trim(),
                    DepartmentName = l.Employee!.Department != null ? l.Employee!.Department.Name : "None",
                    LeaveTypeId = l.LeaveTypeId,
                    LeaveTypeName = l.LeaveType != null ? l.LeaveType.Name : "None",
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    TotalDays = l.TotalDays,
                    Reason = l.Reason,
                    Status = l.Status,
                    AdminRemarks = l.AdminRemarks,
                    AppliedAt = l.AppliedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetAllLeavesAsync()
        {
            return await _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department)
                .Include(l => l.LeaveType)
                .OrderByDescending(l => l.AppliedAt)
                .Select(l => new LeaveRequestDto
                {
                    Id = l.Id,
                    EmployeeId = l.EmployeeId,
                    EmployeeName = $"{l.Employee!.FirstName} {l.Employee.LastName}".Trim(),
                    DepartmentName = l.Employee!.Department != null ? l.Employee!.Department.Name : "None",
                    LeaveTypeId = l.LeaveTypeId,
                    LeaveTypeName = l.LeaveType != null ? l.LeaveType.Name : "None",
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    TotalDays = l.TotalDays,
                    Reason = l.Reason,
                    Status = l.Status,
                    AdminRemarks = l.AdminRemarks,
                    AppliedAt = l.AppliedAt,
                    ReviewedAt = l.ReviewedAt
                })
                .ToListAsync();
        }

        public async Task<LeaveRequestDto?> ReviewLeaveAsync(int leaveId, ReviewLeaveDto dto)
        {
            var leave = await _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department)
                .Include(l => l.LeaveType)
                .FirstOrDefaultAsync(l => l.Id == leaveId);

            if (leave == null) return null;

            leave.Status = dto.Status;
            leave.AdminRemarks = dto.AdminRemarks?.Trim();
            leave.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new LeaveRequestDto
            {
                Id = leave.Id,
                EmployeeId = leave.EmployeeId,
                EmployeeName = $"{leave.Employee!.FirstName} {leave.Employee.LastName}".Trim(),
                DepartmentName = leave.Employee!.Department != null ? leave.Employee!.Department.Name : "None",
                LeaveTypeId = leave.LeaveTypeId,
                LeaveTypeName = leave.LeaveType != null ? leave.LeaveType.Name : "None",
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                TotalDays = leave.TotalDays,
                Reason = leave.Reason,
                Status = leave.Status,
                AdminRemarks = leave.AdminRemarks,
                AppliedAt = leave.AppliedAt,
                ReviewedAt = leave.ReviewedAt
            };
        }
    }
}
