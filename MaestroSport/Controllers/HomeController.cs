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
                if (string.IsNullOrWhiteSpace(code)) return Json(new { valid = false });

                var cleanCode = code.Trim().ToUpper();
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == cleanCode && c.IsActive);

                if (coupon != null && (!coupon.ExpiryDate.HasValue || coupon.ExpiryDate >= DateTime.Now))
                {
                    // إذا كان الكوبون يمنح قطعة مجانية
                    if (coupon.IsFreePiece)
                    {
                        return Json(new { valid = true, isFreePiece = true });
                    }

                    // إذا كان الكوبون نسبة خصم عادية
                    return Json(new { valid = true, discountPercentage = coupon.DiscountPercentage, isFreePiece = false });
                }

                return Json(new { valid = false, message = "الكود المدخل غير صحيح أو تم استخدامه مسبقاً." });
            }
            catch (Exception) { return Json(new { valid = false, message = "حدث خطأ أثناء التحقق." }); }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder([FromForm] OrderSubmissionDto request)
        {
            try
            {
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null) return Json(new { success = false, message = "الموديل غير موجود" });

                var itemsList = JsonSerializer.Deserialize<List<OrderItemDto>>(request.ItemsJson);
                if (itemsList == null || !itemsList.Any()) return Json(new { success = false, message = "الطلب فارغ" });

                var groupedItems = itemsList.GroupBy(i => i.SizeId).Select(g => new OrderItemDto { SizeId = g.Key, Quantity = g.Sum(i => i.Quantity) }).ToList();
                int requestedQty = groupedItems.Sum(i => i.Quantity);

                int dailyCapacity = 15;
                var capacitySetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "DailyCapacity");
                if (capacitySetting != null && int.TryParse(capacitySetting.Value, out int cap)) dailyCapacity = cap;

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todayItemsCount = await _context.OrderItems.Where(oi => oi.Order.CreatedAt >= today && oi.Order.CreatedAt < tomorrow).SumAsync(oi => (int?)oi.Quantity) ?? 0;

                if (todayItemsCount + requestedQty > dailyCapacity)
                {
                    int capacityLeft = Math.Max(0, dailyCapacity - todayItemsCount);
                    if (capacityLeft == 0) return Json(new { success = false, message = "عذراً، اكتملت السعة الإنتاجية لطلبات اليوم. نتشرف باستقبال طلبك غداً." });
                    else return Json(new { success = false, message = $"عذراً، السعة الإنتاجية المتبقية لهذا اليوم هي {capacityLeft} قطعة فقط." });
                }

                // ==========================================
                // 1. توليد كود الهدية آلياً إذا كان العدد >= 10
                // ==========================================
                string generatedGiftCode = null;
                if (requestedQty >= 10)
                {
                    // مثال للكود: GIFT-A8B2
                    generatedGiftCode = "GIFT-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                    _context.Coupons.Add(new Coupon
                    {
                        Code = generatedGiftCode,
                        IsFreePiece = true,
                        DiscountPercentage = 0,
                        IsActive = true
                    });
                }

                var order = new Order
                {
                    CustomerName = request.CustomerName,
                    PhoneNumber = request.PhoneNumber,
                    Notes = request.Notes,
                    FabricType = request.FabricType,
                    FabricExtraPrice = request.FabricExtraPrice,
                    CouponCode = request.CouponCode,
                    ExpectedDeliveryDate = DateTime.Now.AddDays(2)
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
                int totalQty = 0;

                foreach (var item in groupedItems)
                {
                    var size = await _context.Sizes.FindAsync(item.SizeId);
                    if (size == null) continue;

                    decimal unitPrice = product.BasePrice + size.AdditionalPrice;
                    totalAmount += (unitPrice * item.Quantity);
                    totalQty += item.Quantity;

                    order.OrderItems.Add(new OrderItem { ProductId = product.Id, SizeId = size.Id, Quantity = item.Quantity, UnitPrice = unitPrice, CustomDesignImageUrl = order.CustomDesignImageUrl ?? "" });
                }

                totalAmount += (request.FabricExtraPrice * totalQty);

                if (product.IsCustomDesign && totalQty > 0 && totalQty <= 10)
                {
                    totalAmount += (2 * totalQty);
                }

                // ==========================================
                // 2. تطبيق الكوبون الحالي (سواء هدية أو نسبة)
                // ==========================================
                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.CouponCode && c.IsActive);
                    if (coupon != null && (!coupon.ExpiryDate.HasValue || coupon.ExpiryDate >= DateTime.Now))
                    {
                        if (coupon.IsFreePiece)
                        {
                            // خصم قيمة قطعة واحدة شاملة القماش
                            decimal freePieceValue = product.BasePrice + request.FabricExtraPrice;
                            totalAmount -= freePieceValue;

                            // تعطيل كود الهدية لكي يستخدم لمرة واحدة فقط
                            coupon.IsActive = false;
                        }
                        else
                        {
                            decimal discountAmount = totalAmount * (coupon.DiscountPercentage / 100);
                            totalAmount -= discountAmount;
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
                    giftCode = generatedGiftCode // إرسال الكود للواجهة لعرضه للزبون
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ داخلي: " + ex.Message });
            }
        }
    }
}