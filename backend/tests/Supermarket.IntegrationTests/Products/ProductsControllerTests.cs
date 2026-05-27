using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Supermarket.Api.Controllers;
using Supermarket.Application.Categories.Interfaces;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Contracts.Products;
using Xunit;

namespace Supermarket.IntegrationTests.Products
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _productServiceMock = new();
        private readonly Mock<ICategoryService> _categoryServiceMock = new();

        [Fact]
        public async Task Create_ShouldReturnConflict_WhenBarcodeAlreadyExists()
        {
            _productServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<CreateProductRequest>()))
                .ThrowsAsync(new InvalidOperationException("PRODUCT_BARCODE_ALREADY_EXISTS"));
            var controller = CreateController();

            var result = await controller.Create(new CreateProductRequest());

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task Update_ShouldReturnConflict_WhenBarcodeAlreadyExists()
        {
            _productServiceMock
                .Setup(s => s.UpdateAsync(1, It.IsAny<UpdateProductRequest>()))
                .ThrowsAsync(new InvalidOperationException("PRODUCT_BARCODE_ALREADY_EXISTS"));
            var controller = CreateController();

            var result = await controller.Update(1, new UpdateProductRequest());

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        private ProductsController CreateController()
        {
            return new ProductsController(_productServiceMock.Object, _categoryServiceMock.Object);
        }
    }
}
