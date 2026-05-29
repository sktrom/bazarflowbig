using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Employees.Interfaces;
using Supermarket.Contracts.Employees;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/employees")]
    [RequireActiveSession]
    [RequireScreenPermission("Employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ISessionContext _sessionContext;

        public EmployeesController(IEmployeeService employeeService, ISessionContext sessionContext)
        {
            _employeeService = employeeService;
            _sessionContext = sessionContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _employeeService.GetAllAsync();
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var response = await _employeeService.GetByIdAsync(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
        {
            try
            {
                var response = await _employeeService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "USERNAME_ALREADY_EXISTS") return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeRequest request)
        {
            try
            {
                var response = await _employeeService.UpdateAsync(id, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "EMPLOYEE_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _employeeService.DeleteAsync(id, _sessionContext.EmployeeId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "CANNOT_DELETE_SELF") return Conflict(new { error = ex.Message });
                if (ex.Message == "EMPLOYEE_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                var response = await _employeeService.ResetPasswordAsync(id, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "EMPLOYEE_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
