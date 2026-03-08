using MaestroSport.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace MaestroSport.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // تطبيق أي تحديثات (Migrations) معلقة تلقائياً على قاعدة البيانات
            context.Database.Migrate();

            // 1. إضافة صلاحية واسم مستخدم الأدمن
            if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
            }

            if (userManager.FindByNameAsync("admin").GetAwaiter().GetResult() == null)
            {
                var adminUser = new IdentityUser { UserName = "admin", Email = "admin@maestro.com" };
                var result = userManager.CreateAsync(adminUser, "Admin@123").GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
                }
            }

            // 2. إضافة إعدادات الموقع الافتراضية (شريط الإعلانات)
            if (!context.SiteSettings.Any(s => s.Key == "AnnouncementBar"))
            {
                context.SiteSettings.Add(new SiteSetting
                {
                    Key = "AnnouncementBar",
                    Value = "🚚 يوجد شحن لجميع الولايات ودول الخليج .. المايسترو للرياضة .. تميز باختيارك 🚚"
                });
                context.SaveChanges();
            }

            // 3. إضافة الأقسام والموديلات الوهمية إذا كانت قاعدة البيانات فارغة
            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category { Name = "بدلات رياضية", IconClass = "fa-tshirt", ColorClass = "gold", DisplayOrder = 1 },
                    new Category { Name = "بنطلونات", IconClass = "fa-running", ColorClass = "blue", DisplayOrder = 2 },
                    new Category { Name = "فالينات", IconClass = "fa-shirt", ColorClass = "green", DisplayOrder = 3 },
                    new Category { Name = "شورتات", IconClass = "fa-user-ninja", ColorClass = "gold", DisplayOrder = 4 },
                    new Category { Name = "جاكيتات شتوية", IconClass = "fa-mitten", ColorClass = "blue", DisplayOrder = 5 }
                };
                context.Categories.AddRange(categories);
                context.SaveChanges();

                var products = new Product[]
                {
                    // بدلات
                    new Product { Name = "بدلة المايسترو برو ذهبي", BasePrice = 18.00m, ImageUrl = "images/logo.png", IsCustomDesign = true, CategoryId = categories[0].Id },
                    new Product { Name = "بدلة كلاسيك", BasePrice = 15.00m, ImageUrl = "images/logo.png", IsCustomDesign = false, CategoryId = categories[0].Id },
                    // بنطلونات
                    new Product { Name = "بنطلون رياضي ضيق", BasePrice = 10.00m, ImageUrl = "images/logo.png", IsCustomDesign = false, CategoryId = categories[1].Id },
                    // فالينات
                    new Product { Name = "فالينة تدريب ملكي", BasePrice = 7.00m, ImageUrl = "images/logo.png", IsCustomDesign = false, CategoryId = categories[2].Id },
                    new Product { Name = "فالينة المايسترو الأصلية", BasePrice = 8.00m, ImageUrl = "images/logo.png", IsCustomDesign = true, CategoryId = categories[2].Id },
                    new Product { Name = "فالينة ريال مدريد", BasePrice = 9.00m, ImageUrl = "images/logo.png", IsCustomDesign = false, CategoryId = categories[2].Id },
                    // شورتات
                    new Product { Name = "شورت صيفي مريح", BasePrice = 5.00m, ImageUrl = "images/logo.png", IsCustomDesign = false, CategoryId = categories[3].Id },
                    // جاكيتات
                    new Product { Name = "جاكيت المايسترو الشتوي", BasePrice = 20.00m, ImageUrl = "images/logo.png", IsCustomDesign = true, CategoryId = categories[4].Id }
                };
                context.Products.AddRange(products);
                context.SaveChanges();
            }
        }
    }
}