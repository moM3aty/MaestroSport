using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaestroSport.Data;
using MaestroSport.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaestroSport.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // جلب شريط الإعلانات
            var announcement = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "AnnouncementBar");
            ViewBag.AnnouncementBar = announcement?.Value ?? "مرحباً بكم في المايسترو للرياضة";

            // حل مشكلة الـ Object Cycle عبر اختيار الحقول المطلوبة فقط (Projection)
            var categories = await _context.Categories
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new {
                    Id = c.Id,
                    Name = c.Name,
                    IconClass = c.IconClass,
                    ColorClass = c.ColorClass,
                    Products = c.Products.Select(p => new {
                        Id = p.Id,
                        Name = p.Name,
                        BasePrice = p.BasePrice,
                        ImageUrl = p.ImageUrl,
                        IsCustomDesign = p.IsCustomDesign
                    }).ToList()
                })
                .ToListAsync();

            var sizes = await _context.Sizes
                .Select(s => new {
                    Id = s.Id,
                    Name = s.Name,
                    GroupName = s.GroupName,
                    AdditionalPrice = s.AdditionalPrice
                })
                .ToListAsync();

            // إعدادات الـ JSON لمنع أي حلقات مفرغة وللحفاظ على حالة الأحرف
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNamingPolicy = null
            };

            ViewBag.CategoriesJson = JsonSerializer.Serialize(categories, jsonOptions);
            ViewBag.SizesJson = JsonSerializer.Serialize(sizes, jsonOptions);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            if (string.IsNullOrEmpty(code)) return Json(new { valid = false });

            string cleanCode = code.Trim().ToUpper();

            // البحث عن الكوبون النشط والذي لم ينتهِ تاريخه بعد
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == cleanCode && c.IsActive == true);

            if (coupon != null)
            {
                // مقارنة تاريخ اليوم مع تاريخ الانتهاء (بدون ساعات)
                if (!coupon.ExpiryDate.HasValue || coupon.ExpiryDate.Value.Date >= DateTime.Today)
                {
                    return Json(new { valid = true, discount = coupon.DiscountAmount });
                }
            }

            return Json(new { valid = false });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder([FromBody] OrderSubmissionDto dto)
        {
            if (dto == null || dto.Items.Count == 0)
                return Json(new { success = false, message = "بيانات الطلب غير مكتملة" });

            try
            {
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null) return Json(new { success = false, message = "الموديل غير موجود" });

                var sizes = await _context.Sizes.ToListAsync();
                int totalQuantity = dto.Items.Sum(i => i.Quantity);
                decimal totalAmount = 0;

                // إنشاء كائن الطلب مع توفير قيم افتراضية للحقول الإلزامية في DB
                var order = new Order
                {
                    Notes = dto.Notes ?? "",
                    FabricType = string.IsNullOrEmpty(dto.FabricType) ? "قياسي" : dto.FabricType,
                    FabricExtraPrice = dto.FabricExtraPrice,
                    CouponCode = dto.CouponCode?.Trim().ToUpper(),
                    PhoneNumber = "تأكيد عبر واتساب", // تجنب خطأ NULL في PhoneNumber
                    CreatedAt = DateTime.Now,
                    Status = "قيد المراجعة",
                    OrderItems = new List<OrderItem>()
                };

                foreach (var item in dto.Items)
                {
                    var size = sizes.FirstOrDefault(s => s.Id == item.SizeId);
                    if (size == null) continue;

                    decimal unitPrice = product.BasePrice + size.AdditionalPrice + dto.FabricExtraPrice;

                    // رسوم التصميم الخاص (+2) إذا كان العدد 10 أو أقل
                    if (product.IsCustomDesign && totalQuantity <= 10)
                        unitPrice += 2;

                    totalAmount += (unitPrice * item.Quantity);

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        SizeId = size.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        CustomDesignImageUrl = "" // حل مشكلة الـ NULL التي تسببت في الخطأ بجدول OrderItems
                    });
                }

                // إعادة التحقق من الكوبون قبل الحسم النهائي
                if (!string.IsNullOrEmpty(order.CouponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == order.CouponCode && c.IsActive);
                    if (coupon != null && (!coupon.ExpiryDate.HasValue || coupon.ExpiryDate.Value.Date >= DateTime.Today))
                    {
                        totalAmount -= coupon.DiscountAmount;
                    }
                }

                order.TotalAmount = totalAmount < 0 ? 0 : totalAmount;

                // حساب تاريخ الاستلام المجدول (15 قطعة يومياً)
                DateTime deliveryDate = DateTime.Today.AddDays(1);
                int remaining = totalQuantity;
                while (remaining > 0)
                {
                    int bookedToday = await _context.OrderItems
                        .Where(oi => oi.Order.ExpectedDeliveryDate.Date == deliveryDate.Date)
                        .SumAsync(oi => (int?)oi.Quantity) ?? 0;

                    int available = 15 - bookedToday;
                    if (available > 0) { remaining -= Math.Min(available, remaining); }
                    if (remaining > 0) deliveryDate = deliveryDate.AddDays(1);
                }
                order.ExpectedDeliveryDate = deliveryDate;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    orderId = order.Id,
                    totalPrice = order.TotalAmount,
                    deliveryDate = order.ExpectedDeliveryDate.ToString("yyyy/MM/dd")
                });
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ وإرجاع رسالة للمستخدم
                return Json(new { success = false, message = "حدث خطأ داخلي أثناء حفظ الطلب." });
            }
        }

        // كلاسات نقل البيانات (DTOs)
        public class OrderSubmissionDto
        {
            public int ProductId { get; set; }
            public string? Notes { get; set; }
            public string FabricType { get; set; } = "";
            public decimal FabricExtraPrice { get; set; }
            public string? CouponCode { get; set; }
            public List<OrderItemDto> Items { get; set; } = new();
        }
        public class OrderItemDto { public int SizeId { get; set; } public int Quantity { get; set; } }
    }
}