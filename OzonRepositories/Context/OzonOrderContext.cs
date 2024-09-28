using Microsoft.AspNet.Identity;
using Microsoft.EntityFrameworkCore;
using OzonDomains.Models;

namespace OzonRepositories.Context
{
    public class OzonOrderContext : DbContext, IOzonContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Currency> Currencys { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<AppStatus> AppStatuses { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<OzonClient> OzonClients { get; set; }
        public DbSet<UserAccess> UserAccess { get; set; } 
        public DbSet<ColumnMapping> ColumnMappings { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<OrdersFileMetadata> OrdersFileMetadata { get; set; }

        public OzonOrderContext()
        {
        }

        public OzonOrderContext(DbContextOptions<OzonOrderContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Order>()
                .ToTable("Orders", t => t.IsTemporal());
        }

    }
}
