using MaestroSport.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MaestroSport.Controllers
{
    [Authorize(Roles = "Admin,Worker")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // 1. حساب السعة الإنتاجية لليوم (عدد القطع المطلوبة اليوم)
            var todayItemsCount = await _context.OrderItems
                .Where(oi => oi.Order.CreatedAt >= today && oi.Order.CreatedAt < tomorrow)
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;

            // قراءة السعة القصوى من الإعدادات (أو استخدام 15 كافتراضي)
            int dailyCapacity = 15;
            var capacitySetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "DailyCapacity");
            if (capacitySetting != null && int.TryParse(capacitySetting.Value, out int cap))
            {
                dailyCapacity = cap;
            }

            ViewBag.TodayItemsCount = todayItemsCount;
            // حساب المتبقي من السعة والتأكد من أنه لا يقل عن صفر
            ViewBag.CapacityLeft = Math.Max(0, dailyCapacity - todayItemsCount);
            ViewBag.DailyCapacity = dailyCapacity; // تمرير السعة القصوى للـ View لعرضها مثلاً: المتبقي 5 / 20

            // 2. إحصائيات لوحة التحكم العامة
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "قيد المراجعة");
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            ViewBag.TotalSizes = await _context.Sizes.CountAsync();

            // 3. أحدث 5 طلبات لعرضها في الجدول
            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(recentOrders);
        }
    }
}