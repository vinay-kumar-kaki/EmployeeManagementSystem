namespace EmployeeManagementSystem.API.Models.DTOs.Dashboard
{
    public class AdminDashboardStatsDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveDepartments { get; set; }
        public int PendingLeaves { get; set; }
        public int PresentTodayCount { get; set; }
        public string TodayAttendanceRate { get; set; } = "0%";
    }

    public class EmployeeDashboardStatsDto
    {
        public int TotalLeavesApplied { get; set; }
        public int ApprovedLeaves { get; set; }
        public int PendingLeaves { get; set; }
        public int RejectedLeaves { get; set; }
        public bool HasCheckedInToday { get; set; }
        public string FormattedTodayHours { get; set; } = "--";
    }
}
