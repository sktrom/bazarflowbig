using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Devices.Interfaces;
using Supermarket.Contracts.Devices;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/devices")]
    [RequireActiveSession]
    [RequireScreenPermission("Devices")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DevicesController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            return Ok(devices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
            {
                return NotFound(new { error = "DEVICE_NOT_FOUND" });
            }
            return Ok(device);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDeviceRequest request)
        {
            try
            {
                var created = await _deviceService.CreateDeviceAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateDeviceRequest request)
        {
            try
            {
                var updated = await _deviceService.UpdateDeviceAsync(id, request);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(long id)
        {
            try
            {
                var result = await _deviceService.EnableDeviceAsync(id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(long id)
        {
            try
            {
                var result = await _deviceService.DisableDeviceAsync(id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _deviceService.DeleteDeviceAsync(id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
