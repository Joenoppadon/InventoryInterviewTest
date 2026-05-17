using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryTestMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTestMvc.Data
{
    public class InventoryTestMvcDbContext : DbContext
    {
        public InventoryTestMvcDbContext(DbContextOptions<InventoryTestMvcDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            modelBuilder.Entity<PurchaseOrder>().HasIndex(po => po.PONumber).IsUnique();
            modelBuilder.Entity<Delivery>().HasIndex(d => d.DeliveryNumber).IsUnique();
        }
    }

}