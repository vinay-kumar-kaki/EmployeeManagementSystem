using Microsoft.AspNetCore.Identity;

namespace EmployeeManagementSystem.API.Models.Entities
{
    /// <summary>
    /// Extends ASP.NET Core Identity's default user class to include custom profile fields.
    /// IdentityUser provides Id, UserName, Email, PasswordHash, PhoneNumber, etc.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property linking to the Employee business profile (1-to-1)
        public virtual Employee? Employee { get; set; }
    }
}
