using MaestroSport.Data;
using MaestroSport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MaestroSport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SizesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SizesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // عرض جميع المقاسات مقسمة ومنظمة
        public async Task<IActionResult> Index()
        {
            // ترتيب المقاسات حسب المجموعة ثم الاسم لتكون منظمة في الجدول
            return View(await _context.Sizes.OrderBy(s => s.GroupName).ThenBy(s => s.Name).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,GroupName,AdditionalPrice")] Size size)
        {
            if (ModelState.IsValid)
            {
                _context.Add(size);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(size);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var size = await _context.Sizes.FindAsync(id);
            if (size == null) return NotFound();

            return View(size);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,GroupName,AdditionalPrice")] Size size)
        {
            if (id != size.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(size);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(size);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size != null)
            {
                _context.Sizes.Remove(size);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}