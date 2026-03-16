using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MaestroSport.Data;
using MaestroSport.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        public async Task<IActionResult> Index()
        {
            // جلب الكوبونات مع اسم القسم المرتبط بها (إن وجد)
            var coupons = await _context.Coupons.ToListAsync();
            ViewBag.Categories = await _context.Categories.ToDictionaryAsync(c => c.Id, c => c.Name);
            return View(coupons);
        }

        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.Fabrics = new SelectList(_context.Fabrics, "Name", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,DiscountPercentage,IsFreePiece,MinQuantity,TargetCategoryId,TargetFabricName,ExpiryDate,IsActive")] Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                coupon.Code = coupon.Code.ToUpper();
                _context.Add(coupon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", coupon.TargetCategoryId);
            ViewBag.Fabrics = new SelectList(_context.Fabrics, "Name", "Name", coupon.TargetFabricName);
            return View(coupon);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", coupon.TargetCategoryId);
            ViewBag.Fabrics = new SelectList(_context.Fabrics, "Name", "Name", coupon.TargetFabricName);
            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,DiscountPercentage,IsFreePiece,MinQuantity,TargetCategoryId,TargetFabricName,ExpiryDate,IsActive,CreatedAt")] Coupon coupon)
        {
            if (id != coupon.Id) return NotFound();

            if (ModelState.IsValid)
            {
                coupon.Code = coupon.Code.ToUpper();
                _context.Update(coupon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", coupon.TargetCategoryId);
            ViewBag.Fabrics = new SelectList(_context.Fabrics, "Name", "Name", coupon.TargetFabricName);
            return View(coupon);
        }

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