using Microsoft.Extensions.DependencyInjection;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Auth.Services;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Common.Services;
using Supermarket.Application.Sessions.Interfaces;
using Supermarket.Application.Sessions.Services;
using Supermarket.Application.Employees.Interfaces;
using Supermarket.Application.Employees.Services;
using Supermarket.Application.Categories.Interfaces;
using Supermarket.Application.Categories.Services;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Application.Products.Services;
using Supermarket.Application.ProductBatches.Interfaces;
using Supermarket.Application.ProductBatches.Services;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Application.Offers.Services;
using Supermarket.Application.Invoices.Interfaces;
using Supermarket.Application.Invoices.Services;
using Supermarket.Application.WorkingCart.Interfaces;
using Supermarket.Application.WorkingCart.Services;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.CartFinalization.Services;
using Supermarket.Application.InvoicesQuery.Interfaces;
using Supermarket.Application.InvoicesQuery.Services;
using Supermarket.Application.AdjustmentRequests.Interfaces;
using Supermarket.Application.AdjustmentRequests.Services;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Application.InventoryQueries.Services;
using Supermarket.Application.Reports.Interfaces;
using Supermarket.Application.Reports.Services;
using Supermarket.Application.Common.Exports;
using Supermarket.Application.Exports.Interfaces;
using Supermarket.Application.Exports.Services;
using Supermarket.Application.Suppliers.Interfaces;
using Supermarket.Application.Suppliers.Services;

namespace Supermarket.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Application services only — NO EF / DbContext / Infrastructure types here
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductBatchService, ProductBatchService>();
            services.AddScoped<IOfferService, OfferService>();
            services.AddScoped<IInvoiceAppService, InvoiceAppService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<ICartFinalizationService, CartFinalizationService>();
            services.AddScoped<IInvoicesQueryService, InvoicesQueryService>();
            services.AddScoped<IAdjustmentRequestService, AdjustmentRequestService>();
            services.AddScoped<IInventoryQueryService, InventoryQueryService>();
            services.AddScoped<IActionCenterService, ActionCenterService>();
            services.AddScoped<IReportsService, ReportsService>();
            services.AddScoped<ISupplierService, SupplierService>();
            
            services.AddScoped<IExportFormatBuilder, ExportFormatBuilder>();
            services.AddScoped<IPrintHtmlBuilder, PrintHtmlBuilder>();
            services.AddScoped<IExportsService, ExportsService>();

            return services;
        }
    }
}
