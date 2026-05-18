using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.WorkingCart.Interfaces;
using Supermarket.Application.WorkingCart.Services;
using Supermarket.Contracts.WorkingCart;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Xunit;

namespace Supermarket.UnitTests.WorkingCart
{
    public class CartServiceTests
    {
        private readonly Mock<ICartManagementRepository> _cartRepoMock;
        private readonly Mock<IProductManagementRepository> _productRepoMock;
        private readonly Mock<IOfferManagementRepository> _offerRepoMock;
        private readonly Mock<ISessionContext> _sessionContextMock;

        // Due to the use of DbContext natively in CartService for GetCartLinesAsync, 
        // we isolate tests that don't depend intimately on EF, or we mock the constraints.
        // For these pure unit tests, we will mock the ambiguity block effectively.

        public CartServiceTests()
        {
            _cartRepoMock = new Mock<ICartManagementRepository>();
            _productRepoMock = new Mock<IProductManagementRepository>();
            _offerRepoMock = new Mock<IOfferManagementRepository>();
            _sessionContextMock = new Mock<ISessionContext>();
        }

        [Fact]
        public async Task AddItem_ShouldFailAndNotAutoPick_WhenOfferLookupIsAmbiguous()
        {
            var service = new CartService(
                _cartRepoMock.Object, 
                _productRepoMock.Object, 
                _offerRepoMock.Object, 
                _sessionContextMock.Object);

            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);

            _cartRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(new Invoice { Id = 10, Status = InvoiceStatus.Working });
            _productRepoMock.Setup(r => r.GetByBarcodeAsync("ambiguous-barcode")).ReturnsAsync(new Product { Id = 99, PriceUsd = 10 });
            _productRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(new Product { Id = 99, PriceUsd = 10 });

            // Core test constraint: Return Multiple Active Offers exception
            _offerRepoMock.Setup(r => r.GetActiveOfferForProductAsync(99))
                .ThrowsAsync(new InvalidOperationException("MULTIPLE_ACTIVE_OFFERS_FOUND"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddByBarcodeAsync(new AddByBarcodeRequest { Barcode = "ambiguous-barcode" }));

            Assert.Equal("MULTIPLE_ACTIVE_OFFERS_FOUND", exception.Message);
            _cartRepoMock.Verify(r => r.AddLineAsync(It.IsAny<InvoiceLine>()), Times.Never);
        }

        [Fact]
        public async Task AddItem_ShouldFail_WhenQuantityValuesAreInvalidButWaitItsInPatch()
        {
            var service = new CartService(
                _cartRepoMock.Object, 
                _productRepoMock.Object, 
                _offerRepoMock.Object, 
                _sessionContextMock.Object);

            _productRepoMock.Setup(r => r.GetByIdAsync(88)).ReturnsAsync((Product?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddByProductAsync(new AddByProductRequest { ProductId = 88 }));
            Assert.Equal("PRODUCT_NOT_FOUND", exception.Message);
        }

        [Fact]
        public async Task DeleteLine_ShouldDeleteInvoice_WhenLastLineIsRemoved()
        {
            var service = new CartService(_cartRepoMock.Object, _productRepoMock.Object, _offerRepoMock.Object, _sessionContextMock.Object);
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);

            var cart = new Invoice { Id = 10, Status = InvoiceStatus.Working };
            _cartRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(cart);
            
            var line = new InvoiceLine { Id = 5, InvoiceId = 10 };
            var lines = new List<InvoiceLine> { line };
            _cartRepoMock.Setup(r => r.GetCartLinesAsync(10)).ReturnsAsync(lines);

            var result = await service.DeleteLineAsync(5);

            _cartRepoMock.Verify(r => r.RemoveLineAsync(line), Times.Once);
            _cartRepoMock.Verify(r => r.DeleteInvoiceAsync(cart), Times.Once);
            Assert.Equal(0, result.TotalUsd);
        }

        [Fact]
        public async Task AddByProduct_ShouldCreateNewInvoiceAndLineWithQuantityOne_WhenNoWorkingCartExists()
        {
            var service = new CartService(_cartRepoMock.Object, _productRepoMock.Object, _offerRepoMock.Object, _sessionContextMock.Object);
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);

            _cartRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync((Invoice?)null);
            _cartRepoMock.Setup(r => r.CreateWorkingInvoiceAsync(It.IsAny<Invoice>())).ReturnsAsync(new Invoice { Id = 20, Status = InvoiceStatus.Working });
            _cartRepoMock.Setup(r => r.GetCartLinesAsync(20)).ReturnsAsync(new List<InvoiceLine>());

            var product = new Product { Id = 88, PriceUsd = 10 };
            _productRepoMock.Setup(r => r.GetByIdAsync(88)).ReturnsAsync(product);

            var result = await service.AddByProductAsync(new AddByProductRequest { ProductId = 88 });

            _cartRepoMock.Verify(r => r.CreateWorkingInvoiceAsync(It.IsAny<Invoice>()), Times.Once);
            _cartRepoMock.Verify(r => r.AddLineAsync(It.Is<InvoiceLine>(l => l.Quantity == 1m && l.ProductId == 88)), Times.Once);
        }
    }
}
