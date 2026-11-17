using System.Text.Json.Serialization;

namespace EmployeeManagementSystem.API.Models.Entities
{
    public class LeaveType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., Sick, Casual, Annual
        public int DefaultDaysPerYear { get; set; } = 12;
        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
