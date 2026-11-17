using System.ComponentModel.DataAnnotations;
using EmployeeManagementSystem.API.Models.Entities;

namespace EmployeeManagementSystem.API.Models.DTOs.Leaves
{
    public class LeaveTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DefaultDaysPerYear { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateLeaveTypeDto
    {
        [Required(ErrorMessage = "Leave type name is required.")]
        [MaxLength(100, ErrorMessage = "Leave type name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 365, ErrorMessage = "Default days per year must be between 1 and 365.")]
        public int DefaultDaysPerYear { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateLeaveTypeDto : CreateLeaveTypeDto
    {
    }

    public class ApplyLeaveDto
    {
        [Required(ErrorMessage = "Leave type is required.")]
        public int LeaveTypeId { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [MinLength(5, ErrorMessage = "Reason must be at least 5 characters long.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewLeaveDto
    {
        [Required]
        [Range(1, 2, ErrorMessage = "Status must be 1 (Approved) or 2 (Rejected).")]
        public LeaveStatus Status { get; set; }

        [MaxLength(250)]
        public string? AdminRemarks { get; set; }
    }

    public class LeaveRequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public LeaveStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public string? AdminRemarks { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
