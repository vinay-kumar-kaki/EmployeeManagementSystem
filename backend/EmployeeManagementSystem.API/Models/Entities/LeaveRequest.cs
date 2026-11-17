namespace EmployeeManagementSystem.API.Models.Entities
{
    public enum LeaveStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }

        public int LeaveTypeId { get; set; }
        public virtual LeaveType? LeaveType { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public string Reason { get; set; } = string.Empty;

        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
        public string? AdminRemarks { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}
