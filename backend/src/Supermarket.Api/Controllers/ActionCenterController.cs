using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Contracts.InventoryQueries;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/inventory/action-center")]
    public class ActionCenterController : ControllerBase
    {
        private readonly IActionCenterService _actionCenterService;

        public ActionCenterController(IActionCenterService actionCenterService)
        {
            _actionCenterService = actionCenterService;
        }

        [HttpGet]
        [RequireActiveSession]
        [RequireScreenPermission("Inventory")]
        public async Task<ActionResult<ActionCenterResponseDto>> GetActionCenterSummary()
        {
            var result = await _actionCenterService.GetActionCenterSummaryAsync();
            return Ok(result);
        }
    }
}
