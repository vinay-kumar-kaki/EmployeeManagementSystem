using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.API.Models.DTOs.Departments
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int EmployeeCount { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required(ErrorMessage = "Department name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }
    }

    public class UpdateDepartmentDto
    {
        [Required(ErrorMessage = "Department name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
