using MaestroSport.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MaestroSport.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Fabric> Fabrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Size>().HasData(
                new Size { Id = 1, Name = "S", GroupName = "S-XXL", AdditionalPrice = 0 },
                new Size { Id = 2, Name = "M", GroupName = "S-XXL", AdditionalPrice = 0 },
                new Size { Id = 3, Name = "L", GroupName = "S-XXL", AdditionalPrice = 0 },
                new Size { Id = 4, Name = "XL", GroupName = "S-XXL", AdditionalPrice = 0 },
                new Size { Id = 5, Name = "XXL", GroupName = "S-XXL", AdditionalPrice = 0 },
                new Size { Id = 6, Name = "3XL", GroupName = "3XL-7XL", AdditionalPrice = 1 },
                new Size { Id = 7, Name = "4XL", GroupName = "3XL-7XL", AdditionalPrice = 1 },
                new Size { Id = 8, Name = "5XL", GroupName = "3XL-7XL", AdditionalPrice = 1 },
                new Size { Id = 9, Name = "14", GroupName = "14-28", AdditionalPrice = -1 },
                new Size { Id = 10, Name = "16", GroupName = "14-28", AdditionalPrice = -1 }
            );
        }
    }
}