using MaestroSport.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MaestroSport.Controllers
{
    [Authorize(Roles = "Admin")]
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

            // حساب السعة الإنتاجية (عدد القطع المطلوبة اليوم)
            var todayItemsCount = await _context.OrderItems
                .Where(oi => oi.Order.CreatedAt.Date == today)
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;

            ViewBag.TodayItemsCount = todayItemsCount;
            ViewBag.CapacityLeft = Math.Max(0, 15 - todayItemsCount);

            // إحصائيات لوحة التحكم
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "قيد المراجعة");
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();

            // إضافة إحصائية المقاسات المفقودة
            ViewBag.TotalSizes = await _context.Sizes.CountAsync();

            // أحدث 5 طلبات
            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(recentOrders);
        }
    }
}