using MaestroSport.Data;
using MaestroSport.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Notes { get; set; }
        public string FabricType { get; set; }
        public decimal FabricExtraPrice { get; set; }
        public string CouponCode { get; set; }
        public string CartJson { get; set; }
        public IFormFile CustomImage { get; set; }
    }

    public class CartProductDto
    {
        public int ProductId { get; set; }
        public List<OrderItemDto> Sizes { get; set; }
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

        // ==========================================
        // دالة جديدة لحساب تاريخ التسليم الديناميكي للواجهة
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetEstimatedDeliveryDate(int requestedQty)
        {
            int dailyCapacity = 15;
            var capacitySetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "DailyCapacity");
            if (capacitySetting != null && int.TryParse(capacitySetting.Value, out int cap)) dailyCapacity = cap;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var todayItemsCount = await _context.OrderItems
                .Where(oi => oi.Order.CreatedAt >= today && oi.Order.CreatedAt < tomorrow)
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;

            int totalItems = todayItemsCount + requestedQty;
            int extraDays = 0;

            // معادلة حساب الأيام الإضافية (يضيف يومين لكل سعة ممتلئة)
            if (totalItems > 0 && dailyCapacity > 0)
            {
                extraDays = ((totalItems - 1) / dailyCapacity) * 2;
            }

            var deliveryDate = DateTime.Now.AddDays(2 + extraDays);
            return Json(new { date = deliveryDate.ToString("yyyy/MM/dd") });
        }

        [HttpGet]
        public async Task<IActionResult> ValidateCoupon(string code, int totalQty, int? categoryId, string fabricName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code)) return Json(new { valid = false });

                var cleanCode = code.Trim().ToUpper();
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == cleanCode && c.IsActive);

                if (coupon != null && (!coupon.ExpiryDate.HasValue || coupon.ExpiryDate >= DateTime.Now))
                {
                    if (categoryId.HasValue && coupon.TargetCategoryId.HasValue && coupon.TargetCategoryId.Value != categoryId.Value)
                        return Json(new { valid = false, message = "هذا الكود غير مخصص لموديلات هذا القسم." });

                    if (!string.IsNullOrEmpty(coupon.TargetFabricName) && coupon.TargetFabricName != fabricName)
                        return Json(new { valid = false, message = $"هذا الكود مخصص فقط إذا اخترت قماش ({coupon.TargetFabricName})." });

                    if (coupon.IsFreePiece)
                        return Json(new { valid = true, isFreePiece = true });

                    return Json(new { valid = true, discountPercentage = coupon.DiscountPercentage, isFreePiece = false });
                }

                return Json(new { valid = false, message = "الكود المدخل غير صحيح أو منتهي." });
            }
            catch (Exception) { return Json(new { valid = false, message = "حدث خطأ أثناء التحقق." }); }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder([FromForm] OrderSubmissionDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CartJson))
                {
                    return Json(new { success = false, message = "بيانات السلة مفقودة. يرجى تحديث الصفحة والمحاولة مجدداً." });
                }

                var cartItems = JsonSerializer.Deserialize<List<CartProductDto>>(request.CartJson);
                if (cartItems == null || !cartItems.Any())
                {
                    return Json(new { success = false, message = "السلة فارغة" });
                }

                int requestedQty = cartItems.SelectMany(c => c.Sizes).Sum(s => s.Quantity);

                // ==========================================
                // حساب تاريخ التسليم الديناميكي بدلاً من رفض الطلب
                // ==========================================
                int dailyCapacity = 15;
                var capacitySetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "DailyCapacity");
                if (capacitySetting != null && int.TryParse(capacitySetting.Value, out int cap)) dailyCapacity = cap;

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todayItemsCount = await _context.OrderItems
                    .Where(oi => oi.Order.CreatedAt >= today && oi.Order.CreatedAt < tomorrow)
                    .SumAsync(oi => (int?)oi.Quantity) ?? 0;

                int totalItems = todayItemsCount + requestedQty;
                int extraDays = 0;

                if (totalItems > 0 && dailyCapacity > 0)
                {
                    extraDays = ((totalItems - 1) / dailyCapacity) * 2;
                }

                DateTime finalDeliveryDate = DateTime.Now.AddDays(2 + extraDays);
                // ==========================================

                string generatedGiftCode = null;
                if (requestedQty >= 10)
                {
                    generatedGiftCode = "GIFT-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                    _context.Coupons.Add(new Coupon { Code = generatedGiftCode, IsFreePiece = true, IsActive = true });
                }

                var order = new Order
                {
                    CustomerName = request.CustomerName,
                    PhoneNumber = request.PhoneNumber,
                    Notes = request.Notes,
                    FabricType = request.FabricType,
                    FabricExtraPrice = request.FabricExtraPrice,
                    CouponCode = request.CouponCode,
                    ExpectedDeliveryDate = finalDeliveryDate // تم وضع التاريخ المحسوب هنا
                };

                if (request.CustomImage != null && request.CustomImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "custom_designs");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + request.CustomImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create)) { await request.CustomImage.CopyToAsync(fileStream); }
                    order.CustomDesignImageUrl = "images/custom_designs/" + uniqueFileName;
                }

                decimal totalAmount = 0;
                decimal firstProductBasePrice = 0;

                foreach (var cartItem in cartItems)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product == null) continue;

                    if (firstProductBasePrice == 0) firstProductBasePrice = product.BasePrice;

                    var groupedSizes = cartItem.Sizes.GroupBy(i => i.SizeId).Select(g => new OrderItemDto { SizeId = g.Key, Quantity = g.Sum(i => i.Quantity) }).ToList();
                    int productQty = 0;

                    foreach (var sizeItem in groupedSizes)
                    {
                        var size = await _context.Sizes.FindAsync(sizeItem.SizeId);
                        if (size == null) continue;

                        decimal unitPrice = product.BasePrice + size.AdditionalPrice;
                        totalAmount += (unitPrice * sizeItem.Quantity);
                        productQty += sizeItem.Quantity;

                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = product.Id,
                            SizeId = size.Id,
                            Quantity = sizeItem.Quantity,
                            UnitPrice = unitPrice,
                            CustomDesignImageUrl = order.CustomDesignImageUrl ?? ""
                        });
                    }

                    if (product.IsCustomDesign && requestedQty > 0 && requestedQty <= 10)
                    {
                        totalAmount += (2 * productQty);
                    }
                }

                totalAmount += (request.FabricExtraPrice * requestedQty);

                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.CouponCode && c.IsActive);
                    if (coupon != null && (!coupon.ExpiryDate.HasValue || coupon.ExpiryDate >= DateTime.Now))
                    {
                        bool isValid = true;
                        if (!string.IsNullOrEmpty(coupon.TargetFabricName) && coupon.TargetFabricName != request.FabricType) isValid = false;

                        if (isValid)
                        {
                            if (coupon.IsFreePiece)
                            {
                                decimal freePieceValue = firstProductBasePrice + request.FabricExtraPrice;
                                totalAmount -= freePieceValue;
                                coupon.IsActive = false;
                            }
                            else
                            {
                                decimal discountAmount = totalAmount * (coupon.DiscountPercentage / 100);
                                totalAmount -= discountAmount;
                            }
                        }
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
                    deliveryDate = order.ExpectedDeliveryDate.ToString("yyyy/MM/dd"),
                    giftCode = generatedGiftCode
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ داخلي: " + ex.Message });
            }
        }
    }
}