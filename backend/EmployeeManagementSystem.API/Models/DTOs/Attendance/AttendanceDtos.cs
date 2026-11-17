namespace EmployeeManagementSystem.API.Models.DTOs.Attendance
{
    public class AttendanceStatusDto
    {
        public bool HasCheckedInToday { get; set; }
        public bool HasCheckedOutToday { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? WorkDurationMinutes { get; set; }
        public string Status { get; set; } = "Absent";
        public string FormattedDuration => WorkDurationMinutes.HasValue 
            ? $"{WorkDurationMinutes.Value / 60}h {WorkDurationMinutes.Value % 60}m" 
            : "--";
    }

    public class AttendanceRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? WorkDurationMinutes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FormattedDuration => WorkDurationMinutes.HasValue 
            ? $"{WorkDurationMinutes.Value / 60}h {WorkDurationMinutes.Value % 60}m" 
            : "--";
    }
}
