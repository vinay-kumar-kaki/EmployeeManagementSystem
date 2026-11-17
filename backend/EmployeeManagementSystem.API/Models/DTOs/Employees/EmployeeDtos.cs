using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.API.Models.DTOs.Employees
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        public decimal Salary { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateEmployeeDto
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public int DepartmentId { get; set; }

        [Required, MaxLength(50)]
        public string Designation { get; set; } = string.Empty;

        [Required]
        public DateTime JoiningDate { get; set; } = DateTime.UtcNow.Date;

        [Range(0.01, 10000000, ErrorMessage = "Salary must be greater than 0.")]
        public decimal Salary { get; set; }
    }

    public class UpdateEmployeeDto
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required, MaxLength(50)]
        public string Designation { get; set; } = string.Empty;

        [Range(0.01, 10000000)]
        public decimal Salary { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class EmployeeProfileDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        public decimal Salary { get; set; }
        public bool IsActive { get; set; }
    }
}
