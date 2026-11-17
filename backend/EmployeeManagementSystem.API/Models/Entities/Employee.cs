using System.Text.Json.Serialization;

namespace EmployeeManagementSystem.API.Models.Entities
{
    public class Employee
    {
        public int Id { get; set; }

        // Foreign Key to ASP.NET Core Identity ApplicationUser
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        // Foreign Key to Department
        public int DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Designation { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        public decimal Salary { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [JsonIgnore]
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

        [JsonIgnore]
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
