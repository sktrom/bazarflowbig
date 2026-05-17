using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Application.Categories.Interfaces;
using Supermarket.Contracts.Products;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    [RequireActiveSession]
    [RequireScreenPermission("Products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public ProductsController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        [HttpGet("categories-lookup")]
        public async Task<IActionResult> GetCategoriesLookup()
        {
            // Specifically for the products screen dropdown, protected by "Products" permission
            var response = await _categoryService.GetAllAsync();
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _productService.GetAllAsync();
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var response = await _productService.GetByIdAsync(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            try
            {
                var response = await _productService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "BARCODE_ALREADY_EXISTS") return Conflict(new { error = ex.Message });
                if (ex.Message == "CATEGORY_NOT_FOUND") return BadRequest(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var response = await _productService.UpdateAsync(id, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "BARCODE_ALREADY_EXISTS") return Conflict(new { error = ex.Message });
                if (ex.Message == "CATEGORY_NOT_FOUND") return BadRequest(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _productService.DeleteAsync(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
