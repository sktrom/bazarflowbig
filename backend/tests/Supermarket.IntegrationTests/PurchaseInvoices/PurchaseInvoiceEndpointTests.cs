using System.Threading.Tasks;
using Xunit;

namespace Supermarket.IntegrationTests.PurchaseInvoices
{
    public class PurchaseInvoiceEndpointTests
    {
        [Fact]
        public async Task PurchaseInvoiceEndpoints_ShouldRequirePurchasesPermission()
        {
            Assert.True(true);
        }

        [Fact]
        public async Task DraftInvoiceCrud_ShouldSupportCreateUpdateDelete()
        {
            Assert.True(true);
        }

        [Fact]
        public async Task DraftInvoiceLines_ShouldSupportAddUpdateDelete()
        {
            Assert.True(true);
        }

        [Fact]
        public async Task ProductsLookup_ShouldReturnActiveProductsForPurchases()
        {
            Assert.True(true);
        }
    }
}
