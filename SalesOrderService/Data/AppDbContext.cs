using Microsoft.EntityFrameworkCore;
using SalesOrderService.Models;

namespace SalesOrderService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<SalesOrder> SalesOrders { get; set; }

        public DbSet<SalesOrderLineItem> SalesOrderLineItems { get; set; }

        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SalesOrder>()
                .HasMany(s => s.Items)
                .WithOne(i => i.SalesOrder)
                .HasForeignKey(i => i.SALES_SO_ID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}