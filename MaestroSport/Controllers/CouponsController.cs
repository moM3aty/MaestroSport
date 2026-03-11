using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MaestroSport.Data;
using MaestroSport.Models;

namespace MaestroSport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CouponsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // عرض كل الأكواد
        public async Task<IActionResult> Index()
        {
            return View(await _context.Coupons.OrderByDescending(c => c.CreatedAt).ToListAsync());
        }

        // إضافة كود جديد
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,DiscountPercentage,ExpiryDate,IsActive")] Coupon coupon) // تم التحديث هنا
        {
            if (ModelState.IsValid)
            {
                coupon.Code = coupon.Code.ToUpper(); // حفظ الكود دائماً بأحرف كبيرة
                _context.Add(coupon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // تعديل كود
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,DiscountPercentage,ExpiryDate,IsActive,CreatedAt")] Coupon coupon) // تم التحديث هنا
        {
            if (id != coupon.Id) return NotFound();

            if (ModelState.IsValid)
            {
                coupon.Code = coupon.Code.ToUpper();
                _context.Update(coupon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // حذف كود
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}