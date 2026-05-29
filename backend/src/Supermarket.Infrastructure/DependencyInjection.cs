using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Sessions.Interfaces;
using Supermarket.Application.Employees.Interfaces;
using Supermarket.Application.Categories.Interfaces;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Application.ProductBatches.Interfaces;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Application.WorkingCart.Interfaces;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.InvoicesQuery.Interfaces;
using Supermarket.Application.AdjustmentRequests.Interfaces;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Application.Reports.Interfaces;
using Supermarket.Application.Suppliers.Interfaces;
using Supermarket.Application.PurchaseInvoices.Interfaces;
using Supermarket.Application.SystemMaintenance.Interfaces;
using Supermarket.Application.BlackBox.Interfaces;
using Supermarket.Infrastructure.Persistence;
using Supermarket.Infrastructure.Repositories;

namespace Supermarket.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("DefaultConnection is not configured. Set ConnectionStrings__DefaultConnection or user-secrets.");

            // EF Core — only Infrastructure knows about DbContext
            services.AddDbContext<SupermarketDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Repository implementations (all EF-backed)
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IBlackBoxEventRepository, BlackBoxEventRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IAppLoginAttemptRepository, AppLoginAttemptRepository>();
            services.AddScoped<IDeviceRepository, DeviceRepository>();
            services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
            services.AddScoped<ISetupStateRepository, SetupStateRepository>();
            services.AddScoped<IEmployeeScreenPermissionRepository, EmployeeScreenPermissionRepository>();
            services.AddScoped<ISessionHistoryRepository, SessionHistoryRepository>();

            services.AddScoped<IEmployeeManagementRepository, EmployeeManagementRepository>();
            services.AddScoped<IPermissionManagementRepository, PermissionManagementRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IProductManagementRepository, ProductManagementRepository>();
            services.AddScoped<IBatchManagementRepository, BatchManagementRepository>();
            services.AddScoped<IOfferManagementRepository, OfferManagementRepository>();
            services.AddScoped<IInvoiceManagementRepository, InvoiceManagementRepository>();
            services.AddScoped<ICartManagementRepository, CartManagementRepository>();
            services.AddScoped<ICartFinalizationRepository, CartFinalizationRepository>();
            services.AddScoped<IInventoryAllocationRepository, InventoryAllocationRepository>();
            services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
            services.AddScoped<IInvoicesQueryRepository, InvoicesQueryRepository>();
            services.AddScoped<IAdjustmentRequestRepository, AdjustmentRequestRepository>();
            services.AddScoped<IInventoryQueryRepository, InventoryQueryRepository>();
            services.AddScoped<IReportsRepository, ReportsRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
            services.AddScoped<IBackupRepository, BackupRepository>();

            return services;
        }
    }
}
