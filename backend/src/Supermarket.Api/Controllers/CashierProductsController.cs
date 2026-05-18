using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Products.Interfaces;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/cashier/products")]
    [RequireActiveSession]
    [RequireScreenPermission("Sales")]
    public class CashierProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public CashierProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllForCashier()
        {
            // Cashier screen needs the product list to render the grid
            // It relies on the existing IProductService but exposes it 
            // under the cashier-specific route protected by "Sales" permission.
            var response = await _productService.GetAllAsync();
            
            // Only return active products for the cashier screen
            if (response?.Items != null)
            {
                response.Items = response.Items.Where(p => p.IsActive).ToList();
            }

            return Ok(response);
        }
    }
}
