using EmployeeManagementSystem.API.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<Attendance> Attendances => Set<Attendance>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // CRITICAL: Call base.OnModelCreating first to configure Identity tables
            base.OnModelCreating(builder);

            // Configure Employee
            builder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Designation).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Salary).HasPrecision(18, 2);

                // 1-to-1 relationship with ApplicationUser
                entity.HasOne(e => e.User)
                      .WithOne(u => u.Employee)
                      .HasForeignKey<Employee>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Many-to-1 relationship with Department
                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Department
            builder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasIndex(d => d.Name).IsUnique();
                entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
                entity.Property(d => d.Description).HasMaxLength(250);
            });

            // Configure LeaveType
            builder.Entity<LeaveType>(entity =>
            {
                entity.HasKey(lt => lt.Id);
                entity.HasIndex(lt => lt.Name).IsUnique();
                entity.Property(lt => lt.Name).IsRequired().HasMaxLength(50);
            });

            // Configure LeaveRequest
            builder.Entity<LeaveRequest>(entity =>
            {
                entity.HasKey(lr => lr.Id);
                entity.Property(lr => lr.Reason).IsRequired().HasMaxLength(500);
                entity.Property(lr => lr.AdminRemarks).HasMaxLength(250);

                entity.HasOne(lr => lr.Employee)
                      .WithMany(e => e.LeaveRequests)
                      .HasForeignKey(lr => lr.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(lr => lr.LeaveType)
                      .WithMany(lt => lt.LeaveRequests)
                      .HasForeignKey(lr => lr.LeaveTypeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Attendance
            builder.Entity<Attendance>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Status).IsRequired().HasMaxLength(20);

                // Ensure an employee can only have one attendance record per calendar date
                entity.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();

                entity.HasOne(a => a.Employee)
                      .WithMany(e => e.Attendances)
                      .HasForeignKey(a => a.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
