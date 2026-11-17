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
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ApplicationDbContext _context;

        public AttendanceController(IAttendanceService attendanceService, ApplicationDbContext context)
        {
            _attendanceService = attendanceService;
            _context = context;
        }

        [HttpGet("today-status")]
        public async Task<IActionResult> GetTodayStatus()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (!employeeId.HasValue) return BadRequest(new { message = "User not associated with an Employee profile." });

            var status = await _attendanceService.GetTodayStatusAsync(employeeId.Value);
            return Ok(status);
        }

        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (!employeeId.HasValue) return BadRequest(new { message = "User not associated with an Employee profile." });

            try
            {
                var result = await _attendanceService.CheckInAsync(employeeId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("check-out")]
        public async Task<IActionResult> CheckOut()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (!employeeId.HasValue) return BadRequest(new { message = "User not associated with an Employee profile." });

            try
            {
                var result = await _attendanceService.CheckOutAsync(employeeId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var employeeId = await GetCurrentEmployeeIdAsync();
            if (!employeeId.HasValue) return BadRequest(new { message = "User not associated with an Employee profile." });

            var history = await _attendanceService.GetMyAttendanceHistoryAsync(employeeId.Value);
            return Ok(history);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? date = null)
        {
            var records = await _attendanceService.GetAllAttendanceAsync(date);
            return Ok(records);
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
