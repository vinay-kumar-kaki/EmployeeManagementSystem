using System.Security.Claims;
using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Models.DTOs.Leaves;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _leaveService;
        private readonly ApplicationDbContext _context;

        public LeaveController(ILeaveService leaveService, ApplicationDbContext context)
        {
            _leaveService = leaveService;
            _context = context;
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetTypes()
        {
            var types = await _leaveService.GetLeaveTypesAsync();
            return Ok(types);
        }

        /// <summary>Lists every leave type, including inactive ones (Admin only).</summary>
        [HttpGet("types/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllTypes()
        {
            return Ok(await _leaveService.GetAllLeaveTypesAsync());
        }

        [HttpPost("types")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateType([FromBody] CreateLeaveTypeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                return Ok(await _leaveService.CreateLeaveTypeAsync(dto));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("types/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateType(int id, [FromBody] UpdateLeaveTypeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _leaveService.UpdateLeaveTypeAsync(id, dto);
                if (updated == null) return NotFound(new { message = $"Leave type with ID {id} not found." });
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Soft-deactivates a leave type so historical requests remain intact.</summary>
        [HttpDelete("types/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateType(int id)
        {
            var deactivated = await _leaveService.DeactivateLeaveTypeAsync(id);
            if (!deactivated) return NotFound(new { message = $"Leave type with ID {id} not found." });
            return Ok(new { message = "Leave type deactivated." });
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] ApplyLeaveDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employeeId = await GetCurrentEmployeeIdAsync();
            if (!employeeId.HasValue)
            {
                return BadRequest(new { message = "Logged-in user is not associated with an Employee record." });
            }

            try
            {
                var result = await _leaveService.ApplyLeaveAsync(employeeId.Value, dto);
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
            if (!employeeId.HasValue)
            {
                return BadRequest(new { message = "Logged-in user is not associated with an Employee record." });
            }

            var history = await _leaveService.GetEmployeeLeavesAsync(employeeId.Value);
            return Ok(history);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPending()
        {
            var pending = await _leaveService.GetPendingLeavesAsync();
            return Ok(pending);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var all = await _leaveService.GetAllLeavesAsync();
            return Ok(all);
        }

        [HttpPut("{id}/review")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Review(int id, [FromBody] ReviewLeaveDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _leaveService.ReviewLeaveAsync(id, dto);
                if (updated == null) return NotFound(new { message = $"Leave request with ID {id} not found." });
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            // First try custom claim
            var empClaim = User.FindFirst("EmployeeId")?.Value;
            if (int.TryParse(empClaim, out var id)) return id;

            // Fallback: look up in Db using UserId claim
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            return employee?.Id;
        }
    }
}
