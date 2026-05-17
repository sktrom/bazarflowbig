using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Categories.Interfaces;
using Supermarket.Contracts.Categories;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [RequireActiveSession]
    [RequireScreenPermission("Settings")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _categoryService.GetAllAsync();
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            try
            {
                var response = await _categoryService.CreateAsync(request);
                return CreatedAtAction(nameof(GetAll), new { id = response.Id }, response); // Normally GetById, but not in scope
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "CATEGORY_NAME_ALREADY_EXISTS") return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCategoryRequest request)
        {
            try
            {
                var response = await _categoryService.UpdateAsync(id, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "CATEGORY_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "CATEGORY_NAME_ALREADY_EXISTS") return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _categoryService.DeleteAsync(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "CATEGORY_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
