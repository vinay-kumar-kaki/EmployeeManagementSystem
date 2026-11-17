using System.Security.Claims;
using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ApplicationDbContext _context;

        public DashboardController(IDashboardService dashboardService, ApplicationDbContext context)
        {
            _dashboardService = dashboardService;
            _context = context;
        }

        [HttpGet("admin-stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminStats()
        {
            var stats = await _dashboardService.GetAdminStatsAsync();
            return Ok(stats);
        }

        [HttpGet("employee-stats")]
        public async Task<IActionResult> GetEmployeeStats()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (!employeeId.HasValue) return BadRequest(new { message = "User not associated with an Employee profile." });

            var stats = await _dashboardService.GetEmployeeStatsAsync(employeeId.Value);
            return Ok(stats);
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var empClaim = User.FindFirst("EmployeeId")?.Value;
            if (int.TryParse(empClaim, out var id)) return id;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            return employee?.Id;
        }
    }
}
