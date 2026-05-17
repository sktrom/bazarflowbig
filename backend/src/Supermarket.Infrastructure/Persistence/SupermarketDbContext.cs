using Microsoft.EntityFrameworkCore;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence
{
    public class SupermarketDbContext : DbContext
    {
        public SupermarketDbContext(DbContextOptions<SupermarketDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<AppScreen> AppScreens => Set<AppScreen>();
        public DbSet<EmployeeScreenPermission> EmployeeScreenPermissions => Set<EmployeeScreenPermission>();
        
        public DbSet<PosDevice> PosDevices => Set<PosDevice>();
        public DbSet<CashSession> CashSessions => Set<CashSession>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
        public DbSet<Offer> Offers => Set<Offer>();
        
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
        public DbSet<InvoiceLineBatchAllocation> InvoiceLineBatchAllocations => Set<InvoiceLineBatchAllocation>();
        
        public DbSet<AdjustmentRequest> AdjustmentRequests => Set<AdjustmentRequest>();
        public DbSet<AdjustmentRequestLine> AdjustmentRequestLines => Set<AdjustmentRequestLine>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupermarketDbContext).Assembly);
        }
    }
}
