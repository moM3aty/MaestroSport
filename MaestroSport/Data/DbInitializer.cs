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
            context.Database.Migrate();

            // 1. إضافة صلاحيات الأدمن والعمال
            if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
            }

            if (!roleManager.RoleExistsAsync("Worker").GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole("Worker")).GetAwaiter().GetResult();
            }

            // حساب الأدمن
            if (userManager.FindByNameAsync("admin").GetAwaiter().GetResult() == null)
            {
                var adminUser = new IdentityUser { UserName = "admin", Email = "admin@maestro.com" };
                var result = userManager.CreateAsync(adminUser, "Admin@123").GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
                }
            }

            // حساب تجريبي للعمال
            if (userManager.FindByNameAsync("worker").GetAwaiter().GetResult() == null)
            {
                var workerUser = new IdentityUser { UserName = "worker", Email = "worker@maestro.com" };
                var result = userManager.CreateAsync(workerUser, "Worker@123").GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(workerUser, "Worker").GetAwaiter().GetResult();
                }
            }

            // 2. إعدادات الموقع الافتراضية
            if (!context.SiteSettings.Any(s => s.Key == "AnnouncementBar"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "AnnouncementBar", Value = "🚚 يوجد شحن لجميع الولايات ودول الخليج .. المايسترو للرياضة 🚚" });
                context.SaveChanges();
            }

            if (!context.SiteSettings.Any(s => s.Key == "DailyCapacity"))
            {
                context.SiteSettings.Add(new SiteSetting { Key = "DailyCapacity", Value = "15" });
                context.SaveChanges();
            }

            // 3. الأقسام والموديلات الوهمية (إذا كانت فارغة)
            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category { Name = "بدلات رياضية", IconClass = "fa-tshirt", ColorClass = "gold", DisplayOrder = 1 }
                };
                context.Categories.AddRange(categories);
                context.SaveChanges();
            }
        }
    }
}