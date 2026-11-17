using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Models.DTOs.Departments;
using EmployeeManagementSystem.API.Models.Entities;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Services.Implementations
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;

        public DepartmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllAsync(bool activeOnly = false)
        {
            var query = _context.Departments.AsQueryable();
            if (activeOnly)
            {
                query = query.Where(d => d.IsActive);
            }

            return await query
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    CreatedAt = d.CreatedAt,
                    IsActive = d.IsActive,
                    EmployeeCount = d.Employees.Count(e => e.IsActive)
                })
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<DepartmentDto?> GetByIdAsync(int id)
        {
            var d = await _context.Departments
                .Include(dept => dept.Employees)
                .FirstOrDefaultAsync(dept => dept.Id == id);

            if (d == null) return null;

            return new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                CreatedAt = d.CreatedAt,
                IsActive = d.IsActive,
                EmployeeCount = d.Employees.Count(e => e.IsActive)
            };
        }

        public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
        {
            if (await _context.Departments.AnyAsync(d => d.Name.ToLower() == dto.Name.ToLower()))
            {
                throw new InvalidOperationException($"Department with name '{dto.Name}' already exists.");
            }

            var department = new Department
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                CreatedAt = department.CreatedAt,
                IsActive = department.IsActive,
                EmployeeCount = 0
            };
        }

        public async Task<DepartmentDto?> UpdateAsync(int id, UpdateDepartmentDto dto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return null;

            // Check duplicate name
            if (await _context.Departments.AnyAsync(d => d.Id != id && d.Name.ToLower() == dto.Name.ToLower()))
            {
                throw new InvalidOperationException($"Another department with name '{dto.Name}' already exists.");
            }

            department.Name = dto.Name.Trim();
            department.Description = dto.Description?.Trim();
            department.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            var count = await _context.Employees.CountAsync(e => e.DepartmentId == id && e.IsActive);

            return new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                CreatedAt = department.CreatedAt,
                IsActive = department.IsActive,
                EmployeeCount = count
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return false;

            // Soft-deactivate to preserve historical integrity
            department.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
