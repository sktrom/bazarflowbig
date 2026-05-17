using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.Categories.Interfaces;
using Supermarket.Application.Categories.Services;
using Supermarket.Contracts.Categories;
using Supermarket.Domain.Entities;
using Xunit;

namespace Supermarket.UnitTests.Categories
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _repoMock;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            _repoMock = new Mock<ICategoryRepository>();
            _service = new CategoryService(_repoMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldSucceed_WhenNameIsUnique()
        {
            var request = new CreateCategoryRequest { Name = "  Test  " };
            _repoMock.Setup(r => r.GetByNameAsync("Test")).ReturnsAsync((Category?)null);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => 
            {
                c.Id = 1;
                return c;
            });

            var result = await _service.CreateAsync(request);

            Assert.Equal("Test", result.Name);
            Assert.True(result.IsActive);
            _repoMock.Verify(r => r.CreateAsync(It.Is<Category>(c => c.Name == "Test")), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldFail_WhenNameAlreadyExists()
        {
            var request = new CreateCategoryRequest { Name = "Test" };
            _repoMock.Setup(r => r.GetByNameAsync("Test")).ReturnsAsync(new Category());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
            Assert.Equal("CATEGORY_NAME_ALREADY_EXISTS", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldSucceed_WhenNameChangedAndUnique()
        {
            var request = new UpdateCategoryRequest { Name = "New Name", IsActive = false };
            var existingCategory = new Category { Id = 1, Name = "Old Name", IsActive = true };
            
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCategory);
            _repoMock.Setup(r => r.GetByNameAsync("New Name")).ReturnsAsync((Category?)null);

            var result = await _service.UpdateAsync(1, request);

            Assert.Equal("New Name", result.Name);
            Assert.False(result.IsActive);
            _repoMock.Verify(r => r.UpdateAsync(existingCategory), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldFail_WhenNameConflictsWithOther()
        {
            var request = new UpdateCategoryRequest { Name = "Existing Name" };
            var existingCategory = new Category { Id = 1, Name = "Old Name" };
            var conflictingCategory = new Category { Id = 2, Name = "Existing Name" };
            
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCategory);
            _repoMock.Setup(r => r.GetByNameAsync("Existing Name")).ReturnsAsync(conflictingCategory);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(1, request));
            Assert.Equal("CATEGORY_NAME_ALREADY_EXISTS", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_ShouldPhysicalDelete_WhenNoProducts()
        {
            var existingCategory = new Category { Id = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCategory);
            _repoMock.Setup(r => r.HasProductsAsync(1)).ReturnsAsync(false);

            var result = await _service.DeleteAsync(1);

            Assert.Equal("DELETED", result.Action);
            _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDisable_WhenHasProducts()
        {
            var existingCategory = new Category { Id = 1, IsActive = true };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingCategory);
            _repoMock.Setup(r => r.HasProductsAsync(1)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(1);

            Assert.Equal("DISABLED", result.Action);
            Assert.False(existingCategory.IsActive);
            _repoMock.Verify(r => r.UpdateAsync(existingCategory), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        }
    }
}
