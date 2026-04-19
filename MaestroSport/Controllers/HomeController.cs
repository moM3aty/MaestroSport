using MaestroSport.Data;
using MaestroSport.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

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
        public string PaymentType { get; set; }
        public IFormFile CustomImage { get; set; }
    }

    public class CartProductDto { public int ProductId { get; set; } public List<OrderItemDto> Sizes { get; set; } }
    public class OrderItemDto { public int SizeId { get; set; } public int Quantity { get; set; } }

    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // --- بيانات Paymob ---
        private const string PaymobApiKey = "ZXlKaGJHY2lPaUpJVXpVeE1pSXNJblI1Y0NJNklrcFhWQ0o5LmV5SmpiR0Z6Y3lJNklrMWxjbU5vWVc1MElpd2ljSEp2Wm1sc1pWOXdheUk2TkRNMk56VXNJbTVoYldVaU9pSnBibWwwYVdGc0luMC5WbkgzYXE5SnZBSjltQUMwc2pvNjJ3eVRWWDBIaFNEa1g0UllOMmppQklablRnRmI1aHNWOFpaUUVxRmU3UFlyY0FiMm11NXpuYzZqM2RsX25ORGtBQQ==";
        private const string PaymobHmacKey = "91ECCACD2AFB70ACF6ABCC1993EDCDEA";
        private const string PaymobIframeId = "43077";
        private const int PaymobIntegrationId = 63327;

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

            var jsonOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles, PropertyNamingPolicy = null };

            ViewBag.CategoriesJson = JsonSerializer.Serialize(categories, jsonOptions);
            ViewBag.SizesJson = JsonSerializer.Serialize(sizes, jsonOptions);
            ViewBag.FabricsJson = JsonSerializer.Serialize(fabrics, jsonOptions);

            return View();
        }

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

            if (dailyCapacity > 0 && totalItems > dailyCapacity) extraDays = ((totalItems - 1) / dailyCapacity) * 2;

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

                    if (coupon.IsFreePiece) return Json(new { valid = true, isFreePiece = true });
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
                if (string.IsNullOrWhiteSpace(request.CartJson)) return Json(new { success = false, message = "بيانات السلة مفقودة. يرجى تحديث الصفحة." });

                var cartItems = JsonSerializer.Deserialize<List<CartProductDto>>(request.CartJson);
                if (cartItems == null || !cartItems.Any()) return Json(new { success = false, message = "السلة فارغة" });

                int requestedQty = cartItems.SelectMany(c => c.Sizes).Sum(s => s.Quantity);

                // حساب السعة الإنتاجية
                int dailyCapacity = 15;
                var capacitySetting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "DailyCapacity");
                if (capacitySetting != null && int.TryParse(capacitySetting.Value, out int cap)) dailyCapacity = cap;

                var todayItemsCount = await _context.OrderItems.Where(oi => oi.Order.CreatedAt >= DateTime.Today && oi.Order.CreatedAt < DateTime.Today.AddDays(1)).SumAsync(oi => (int?)oi.Quantity) ?? 0;
                int totalItems = todayItemsCount + requestedQty;
                int extraDays = (dailyCapacity > 0 && totalItems > dailyCapacity) ? ((totalItems - 1) / dailyCapacity) * 2 : 0;
                DateTime finalDeliveryDate = DateTime.Now.AddDays(2 + extraDays);

                string generatedGiftCode = null;
                if (requestedQty >= 10)
                {
                    generatedGiftCode = "GIFT-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                    _context.Coupons.Add(new Coupon { Code = generatedGiftCode, IsFreePiece = true, IsActive = true });
                }

                // ===============================================
                // الحساب الدقيق للإجمالي من قاعدة البيانات مباشرة
                // لضمان تطابق مبلغ Paymob مع الظاهر للعميل
                // ===============================================

                decimal totalAmount = 0;
                decimal firstProductBasePrice = 0;
                bool hasCustomDesign = false;

                // جلب سعر القماش من قاعدة البيانات لتجنب مشاكل قراءة الكسور العشرية
                decimal safeFabricExtraPrice = 0;
                var dbFabric = await _context.Fabrics.FirstOrDefaultAsync(f => f.Name == request.FabricType);
                if (dbFabric != null)
                {
                    safeFabricExtraPrice = dbFabric.AdditionalPrice;
                }

                var order = new Order
                {
                    CustomerName = request.CustomerName,
                    PhoneNumber = request.PhoneNumber,
                    Notes = request.Notes,
                    FabricType = request.FabricType,
                    FabricExtraPrice = safeFabricExtraPrice, // استخدام السعر الآمن من الداتابيز
                    CouponCode = request.CouponCode,
                    ExpectedDeliveryDate = finalDeliveryDate,
                    PaymentType = request.PaymentType == "deposit" ? "عربون 30%" : "كامل",
                    Status = "بانتظار الدفع"
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

                foreach (var cartItem in cartItems)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product == null) continue;

                    if (firstProductBasePrice == 0) firstProductBasePrice = product.BasePrice;
                    if (product.IsCustomDesign) hasCustomDesign = true; // التأكد مما إذا كان أي منتج يتطلب تصميم خاص

                    var groupedSizes = cartItem.Sizes.GroupBy(i => i.SizeId).Select(g => new OrderItemDto { SizeId = g.Key, Quantity = g.Sum(i => i.Quantity) }).ToList();

                    foreach (var sizeItem in groupedSizes)
                    {
                        var size = await _context.Sizes.FindAsync(sizeItem.SizeId);
                        if (size == null) continue;

                        decimal unitPrice = product.BasePrice + size.AdditionalPrice;
                        totalAmount += (unitPrice * sizeItem.Quantity);

                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = product.Id,
                            SizeId = size.Id,
                            Quantity = sizeItem.Quantity,
                            UnitPrice = unitPrice,
                            CustomDesignImageUrl = order.CustomDesignImageUrl ?? ""
                        });
                    }
                }

                // 1. إضافة سعر القماش الكلي
                totalAmount += (safeFabricExtraPrice * requestedQty);

                // 2. إضافة رسوم التصميم الخاص (مطابق للفرونت إند تماماً)
                if (hasCustomDesign && requestedQty > 0 && requestedQty <= 10)
                {
                    totalAmount += (2 * requestedQty);
                }

                // 3. تطبيق الخصومات بشكل دقيق
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
                                totalAmount -= (firstProductBasePrice + safeFabricExtraPrice);
                                coupon.IsActive = false;
                            }
                            else
                            {
                                totalAmount -= (totalAmount * (coupon.DiscountPercentage / 100m));
                            }
                        }
                    }
                }

                // تأكيد تقريب الإجمالي لعلامتين عشريتين لتجنب أي كسور طويلة
                order.TotalAmount = Math.Round(Math.Max(0, totalAmount), 2);

                // حساب المدفوع الآن بناءً على الإجمالي الحقيقي الدقيق
                if (request.PaymentType == "deposit")
                    order.PaidAmount = Math.Round(order.TotalAmount * 0.3m, 2);
                else
                    order.PaidAmount = order.TotalAmount;

                order.RemainingAmount = order.TotalAmount - order.PaidAmount;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // ===============================================
                // إرسال المبلغ الحقيقي الدقيق (النهائي) إلى Paymob 
                // ===============================================
                using (var httpClient = new HttpClient())
                {
                    var authPayload = new { api_key = PaymobApiKey };
                    var authContent = new StringContent(JsonSerializer.Serialize(authPayload), Encoding.UTF8, "application/json");
                    var authResponse = await httpClient.PostAsync("https://oman.paymob.com/api/auth/tokens", authContent);

                    string authResponseString = await authResponse.Content.ReadAsStringAsync();
                    if (!authResponse.IsSuccessStatusCode)
                        return Json(new { success = false, message = "Paymob Auth Error: " + authResponseString });

                    var authResult = JsonSerializer.Deserialize<JsonElement>(authResponseString);
                    string authToken = authResult.GetProperty("token").GetString();

                    // تحويل المبلغ لبيسة / Cents (نضرب الإجمالي النهائي في 100)
                    int amountInCents = (int)Math.Round(order.PaidAmount * 100m);

                    var orderPayload = new
                    {
                        auth_token = authToken,
                        delivery_needed = "false",
                        amount_cents = amountInCents.ToString(),
                        currency = "OMR",
                        merchant_order_id = order.Id.ToString()
                    };
                    var orderContent = new StringContent(JsonSerializer.Serialize(orderPayload), Encoding.UTF8, "application/json");
                    var orderResponse = await httpClient.PostAsync("https://oman.paymob.com/api/ecommerce/orders", orderContent);

                    string orderResponseString = await orderResponse.Content.ReadAsStringAsync();
                    if (!orderResponse.IsSuccessStatusCode)
                        return Json(new { success = false, message = "Paymob Order Error: " + orderResponseString });

                    var orderResult = JsonSerializer.Deserialize<JsonElement>(orderResponseString);
                    string paymobOrderId = orderResult.GetProperty("id").GetInt32().ToString();

                    order.PaymobOrderId = paymobOrderId;
                    await _context.SaveChangesAsync();

                    var keyPayload = new
                    {
                        auth_token = authToken,
                        amount_cents = amountInCents.ToString(),
                        expiration = 3600,
                        order_id = paymobOrderId,
                        billing_data = new
                        {
                            first_name = string.IsNullOrWhiteSpace(order.CustomerName) ? "NA" : order.CustomerName.Split(' ').FirstOrDefault(),
                            last_name = string.IsNullOrWhiteSpace(order.CustomerName) ? "NA" : order.CustomerName.Split(' ').Skip(1).FirstOrDefault() ?? "NA",
                            email = "customer@maestrosport.com",
                            phone_number = string.IsNullOrWhiteSpace(order.PhoneNumber) ? "NA" : order.PhoneNumber,
                            apartment = "NA",
                            floor = "NA",
                            street = "NA",
                            building = "NA",
                            shipping_method = "NA",
                            postal_code = "NA",
                            city = "NA",
                            country = "OM",
                            state = "NA"
                        },
                        currency = "OMR",
                        integration_id = PaymobIntegrationId
                    };
                    var keyContent = new StringContent(JsonSerializer.Serialize(keyPayload), Encoding.UTF8, "application/json");
                    var keyResponse = await httpClient.PostAsync("https://oman.paymob.com/api/acceptance/payment_keys", keyContent);

                    string keyResponseString = await keyResponse.Content.ReadAsStringAsync();
                    if (!keyResponse.IsSuccessStatusCode)
                        return Json(new { success = false, message = "Paymob Key Error: " + keyResponseString });

                    var keyResult = JsonSerializer.Deserialize<JsonElement>(keyResponseString);
                    string paymentToken = keyResult.GetProperty("token").GetString();

                    string iframeUrl = $"https://oman.paymob.com/api/acceptance/iframes/{PaymobIframeId}?payment_token={paymentToken}";

                    return Json(new { success = true, iframeUrl = iframeUrl });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ غير متوقع: " + ex.Message });
            }
        }

        [HttpPost]
        [Route("Home/PaymobWebhook")]
        public async Task<IActionResult> PaymobWebhook()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var body = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body)) return Ok();

                try
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(body);
                    var obj = json.GetProperty("obj");
                    bool success = obj.GetProperty("success").GetBoolean();
                    string orderId = obj.GetProperty("order").GetProperty("id").GetInt32().ToString();

                    if (success)
                    {
                        var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymobOrderId == orderId);
                        if (order != null && !order.IsPaid)
                        {
                            order.IsPaid = true;
                            order.Status = "قيد المراجعة";
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception) { /* تجاهل أخطاء المعالجة هنا لكي لا يعود خطأ لـ Paymob */ }
            }
            return Ok();
        }

        [HttpGet]
        public IActionResult PaymentSuccess(int order)
        {
            TempData["PaymentSuccess"] = "تم تأكيد طلبك والدفع بنجاح! رقم طلبك: #" + order;
            return RedirectToAction(nameof(Index));
        }
    }
}