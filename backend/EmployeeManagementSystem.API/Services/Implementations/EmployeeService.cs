using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Models.DTOs.Employees;
using EmployeeManagementSystem.API.Models.Entities;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Services.Implementations
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeeService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IEnumerable<EmployeeDto>> GetAllAsync(string? search = null, int? departmentId = null)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e =>
                    e.FirstName.ToLower().Contains(term) ||
                    e.LastName.ToLower().Contains(term) ||
                    e.Email.ToLower().Contains(term) ||
                    e.Designation.ToLower().Contains(term));
            }

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            }

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Email = e.Email,
                    PhoneNumber = e.PhoneNumber,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : "None",
                    Designation = e.Designation,
                    JoiningDate = e.JoiningDate,
                    Salary = e.Salary,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id)
        {
            var e = await _context.Employees
                .Include(emp => emp.Department)
                .FirstOrDefaultAsync(emp => emp.Id == id);

            if (e == null) return null;

            return new EmployeeDto
            {
                Id = e.Id,
                UserId = e.UserId,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department?.Name ?? "None",
                Designation = e.Designation,
                JoiningDate = e.JoiningDate,
                Salary = e.Salary,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt
            };
        }

        public async Task<EmployeeProfileDto?> GetProfileByUserIdAsync(string userId)
        {
            var e = await _context.Employees
                .Include(emp => emp.Department)
                .FirstOrDefaultAsync(emp => emp.UserId == userId);

            if (e == null) return null;

            return new EmployeeProfileDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                DepartmentName = e.Department?.Name ?? "None",
                Designation = e.Designation,
                JoiningDate = e.JoiningDate,
                Salary = e.Salary,
                IsActive = e.IsActive
            };
        }

        public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
        {
            // 1. Verify email uniqueness
            if (await _context.Employees.AnyAsync(e => e.Email.ToLower() == dto.Email.ToLower()))
            {
                throw new InvalidOperationException($"An employee with email '{dto.Email}' already exists.");
            }

            // 2. Verify Department exists
            var department = await _context.Departments.FindAsync(dto.DepartmentId);
            if (department == null)
            {
                throw new InvalidOperationException("Selected department does not exist.");
            }

            // 3. Create Identity User login account
            var user = new ApplicationUser
            {
                UserName = dto.Email.Trim(),
                Email = dto.Email.Trim(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var userResult = await _userManager.CreateAsync(user, dto.Password);
            if (!userResult.Succeeded)
            {
                var errors = string.Join("; ", userResult.Errors.Select(err => err.Description));
                throw new InvalidOperationException($"Failed to create user account: {errors}");
            }

            // 4. Ensure Employee role exists and assign
            if (!await _roleManager.RoleExistsAsync("Employee"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Employee"));
            }
            await _userManager.AddToRoleAsync(user, "Employee");

            // 5. Create Employee profile record
            var employee = new Employee
            {
                UserId = user.Id,
                DepartmentId = dto.DepartmentId,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                Designation = dto.Designation.Trim(),
                JoiningDate = dto.JoiningDate,
                Salary = dto.Salary,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return new EmployeeDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                DepartmentId = employee.DepartmentId,
                DepartmentName = department.Name,
                Designation = employee.Designation,
                JoiningDate = employee.JoiningDate,
                Salary = employee.Salary,
                IsActive = employee.IsActive,
                CreatedAt = employee.CreatedAt
            };
        }

        public async Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null) return null;

            var department = await _context.Departments.FindAsync(dto.DepartmentId);
            if (department == null)
            {
                throw new InvalidOperationException("Selected department does not exist.");
            }

            employee.FirstName = dto.FirstName.Trim();
            employee.LastName = dto.LastName.Trim();
            employee.PhoneNumber = dto.PhoneNumber?.Trim();
            employee.DepartmentId = dto.DepartmentId;
            employee.Designation = dto.Designation.Trim();
            employee.Salary = dto.Salary;
            employee.IsActive = dto.IsActive;

            // Update user record name if exists
            if (employee.User != null)
            {
                employee.User.FirstName = dto.FirstName.Trim();
                employee.User.LastName = dto.LastName.Trim();
                employee.User.PhoneNumber = dto.PhoneNumber?.Trim();
            }

            await _context.SaveChangesAsync();

            return new EmployeeDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                DepartmentId = employee.DepartmentId,
                DepartmentName = department.Name,
                Designation = employee.Designation,
                JoiningDate = employee.JoiningDate,
                Salary = employee.Salary,
                IsActive = employee.IsActive,
                CreatedAt = employee.CreatedAt
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return false;

            // Soft-deactivate employee
            employee.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
