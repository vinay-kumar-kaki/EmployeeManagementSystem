namespace EmployeeManagementSystem.API.Models.Entities
{
    public class Attendance
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }

        // Date without time for daily indexing
        public DateTime Date { get; set; }

        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        // Calculated in minutes once checked out
        public int? WorkDurationMinutes { get; set; }

        public string Status { get; set; } = "Present"; // e.g., Present, Late, HalfDay
    }
}
