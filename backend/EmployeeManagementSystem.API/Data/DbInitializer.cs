using EmployeeManagementSystem.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            try
            {
                // Ensure database is created
                await context.Database.EnsureCreatedAsync();

                // 1. Seed Roles: Admin and Employee
                string[] roleNames = { "Admin", "Employee" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                        logger.LogInformation("Seeded Role: {RoleName}", roleName);
                    }
                }

                // 2. Seed Default Admin Account
                var adminEmail = configuration["AdminSeed:Email"] ?? "admin@company.com";
                var adminPassword = configuration["AdminSeed:Password"] ?? "admin@123";
                var adminFirstName = configuration["AdminSeed:FirstName"] ?? "System";
                var adminLastName = configuration["AdminSeed:LastName"] ?? "Admin";

                var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
                if (existingAdmin == null)
                {
                    var adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FirstName = adminFirstName,
                        LastName = adminLastName,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        logger.LogInformation("Seeded default Admin user: {Email}", adminEmail);
                    }
                    else
                    {
                        logger.LogError("Failed to seed Admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    await EnsureSeedPasswordAsync(userManager, existingAdmin, adminPassword, "Admin", logger);
                }

                // 3. Seed Default Departments if empty
                if (!await context.Departments.AnyAsync())
                {
                    var departments = new List<Department>
                    {
                        new Department { Name = "Information Technology", Description = "Software engineering, DevOps, and IT support", IsActive = true },
                        new Department { Name = "Human Resources", Description = "Recruitment, payroll, and employee relations", IsActive = true },
                        new Department { Name = "Finance & Accounts", Description = "Financial planning, accounting, and budgeting", IsActive = true }
                    };

                    await context.Departments.AddRangeAsync(departments);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded initial departments.");
                }

                // 3b. Seed a demo Employee account so the login screen quick-fill works out of the box
                var demoEmployeeEmail = "employee@company.com";
                var demoEmployeePassword = "employee@123";
                var existingDemoEmployee = await userManager.FindByEmailAsync(demoEmployeeEmail);
                if (existingDemoEmployee == null)
                {
                    var defaultDepartment = await context.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();
                    if (defaultDepartment != null)
                    {
                        var demoUser = new ApplicationUser
                        {
                            UserName = demoEmployeeEmail,
                            Email = demoEmployeeEmail,
                            FirstName = "Demo",
                            LastName = "Employee",
                            EmailConfirmed = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        var result = await userManager.CreateAsync(demoUser, demoEmployeePassword);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(demoUser, "Employee");

                            context.Employees.Add(new Employee
                            {
                                UserId = demoUser.Id,
                                DepartmentId = defaultDepartment.Id,
                                FirstName = demoUser.FirstName,
                                LastName = demoUser.LastName,
                                Email = demoEmployeeEmail,
                                Designation = "Software Engineer",
                                JoiningDate = DateTime.Today,
                                Salary = 60000m,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            });

                            await context.SaveChangesAsync();
                            logger.LogInformation("Seeded demo Employee user: {Email}", demoEmployeeEmail);
                        }
                        else
                        {
                            logger.LogError("Failed to seed demo Employee user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }
                else
                {
                    await EnsureSeedPasswordAsync(userManager, existingDemoEmployee, demoEmployeePassword, "demo Employee", logger);
                }

                // 4. Seed Default Leave Types if empty
                if (!await context.LeaveTypes.AnyAsync())
                {
                    var leaveTypes = new List<LeaveType>
                    {
                        new LeaveType { Name = "Casual Leave", DefaultDaysPerYear = 12, IsActive = true },
                        new LeaveType { Name = "Sick Leave", DefaultDaysPerYear = 10, IsActive = true },
                        new LeaveType { Name = "Annual Paid Leave", DefaultDaysPerYear = 15, IsActive = true }
                    };

                    await context.LeaveTypes.AddRangeAsync(leaveTypes);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded initial leave types.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        /// <summary>
        /// Keeps the documented demo credentials in sync with configuration, even when
        /// the database already exists from a previous deployment.
        /// </summary>
        private static async Task EnsureSeedPasswordAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            string password,
            string label,
            ILogger logger)
        {
            if (await userManager.CheckPasswordAsync(user, password)) return;

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, password);
            if (result.Succeeded)
            {
                logger.LogInformation("Updated the seeded {Label} password.", label);
            }
            else
            {
                logger.LogError("Failed to update the seeded {Label} password: {Errors}",
                    label, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
