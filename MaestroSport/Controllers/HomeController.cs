using MaestroSport.Data;
using MaestroSport.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace MaestroSport.Controllers
{
    public class OrderSubmissionDto
    {
        public int ProductId { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Notes { get; set; }
        public string FabricType { get; set; }
        public decimal FabricExtraPrice { get; set; }
        public string CouponCode { get; set; }
        public string ItemsJson { get; set; }
        public IFormFile CustomImage { get; set; }
    }

    public class OrderItemDto
    {
        public int SizeId { get; set; }
        public int Quantity { get; set; }
    }

    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var announcement = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "AnnouncementBar");
            ViewBag.AnnouncementBar = announcement?.Value ?? "مرحباً بكم في المايسترو للرياضة";

            var promoBanner = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "PromoBannerUrl");
            ViewBag.PromoBannerUrl = promoBanner?.Value ?? "";

            var categories = await _context.Categories.Include(c => c.Products).OrderBy(c => c.DisplayOrder).ToListAsync();
            var sizes = await _context.Sizes.OrderBy(s => s.GroupName).ThenBy(s => s.Name).ToListAsync();

            var fabrics = await _context.Fabrics.OrderBy(f => f.AdditionalPrice).ToListAsync();

            var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles, PropertyNamingPolicy = null };

            ViewBag.CategoriesJson = JsonSerializer.Serialize(categories, jsonOptions);
            ViewBag.SizesJson = JsonSerializer.Serialize(sizes, jsonOptions);
            ViewBag.FabricsJson = JsonSerializer.Serialize(fabrics, jsonOptions);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return Json(new { valid = false });

                var cleanCode = code.Trim().ToUpper();

                // تم تبسيط الاستعلام لتجنب مشاكل الترجمة في SQL
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == cleanCode && c.IsActive);

                // استخدام DateTime.Now بدلاً من .Value.Date لمنع الأخطاء
                if (coupon != null && (!coupon.ExpiryDate.HasValue || coupon.ExpiryDate >= DateTime.Now))
                {
                    return Json(new { valid = true, discount = coupon.DiscountAmount });
                }

                return Json(new { valid = false });
            }
            catch (Exception)
            {
                // حماية الكنترولر من التوقف في حال وجود خطأ في قاعدة البيانات
                return Json(new { valid = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder([FromForm] OrderSubmissionDto request)
        {
            try
            {
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null) return Json(new { success = false, message = "الموديل غير موجود" });

                var itemsList = JsonSerializer.Deserialize<List<OrderItemDto>>(request.ItemsJson);
                if (itemsList == null || !itemsList.Any())
                    return Json(new { success = false, message = "الطلب فارغ" });

                var groupedItems = itemsList
                    .GroupBy(i => i.SizeId)
                    .Select(g => new OrderItemDto { SizeId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                    .ToList();

                var order = new Order
                {
                    CustomerName = request.CustomerName,
                    PhoneNumber = request.PhoneNumber,
                    Notes = request.Notes,
                    FabricType = request.FabricType,
                    FabricExtraPrice = request.FabricExtraPrice,
                    CouponCode = request.CouponCode,
                    ExpectedDeliveryDate = DateTime.Now.AddDays(14)
                };

                if (request.CustomImage != null && request.CustomImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "custom_designs");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + request.CustomImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.CustomImage.CopyToAsync(fileStream);
                    }
                    order.CustomDesignImageUrl = "images/custom_designs/" + uniqueFileName;
                }

                decimal totalAmount = 0;
                int totalQty = 0;

                foreach (var item in groupedItems)
                {
                    var size = await _context.Sizes.FindAsync(item.SizeId);
                    if (size == null) continue;

                    decimal unitPrice = product.BasePrice + size.AdditionalPrice;
                    totalAmount += (unitPrice * item.Quantity);
                    totalQty += item.Quantity;

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        SizeId = size.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        // ------------- الحل الأساسي لمشكلة الداتا بيز -------------
                        // قاعدة البيانات ترفض القيم الفارغة في هذا العمود، لذا نمرر لها الصورة أو نص فارغ
                        CustomDesignImageUrl = order.CustomDesignImageUrl ?? ""
                    });
                }

                totalAmount += (request.FabricExtraPrice * totalQty);

                if (product.IsCustomDesign && totalQty > 0 && totalQty <= 10)
                {
                    totalAmount += (2 * totalQty);
                }

                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.CouponCode && c.IsActive);
                    if (coupon != null && (!coupon.ExpiryDate.HasValue || coupon.ExpiryDate >= DateTime.Now))
                    {
                        totalAmount -= coupon.DiscountAmount;
                    }
                }

                order.TotalAmount = Math.Max(0, totalAmount);

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
                // إرجاع رسالة الخطأ لتظهر في الشاشة لمعرفة السبب إن تكرر
                return Json(new { success = false, message = "حدث خطأ داخلي: " + ex.Message });
            }
        }
    }
}