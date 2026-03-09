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
    public class FabricsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FabricsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Fabrics.OrderBy(f => f.AdditionalPrice).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,AdditionalPrice")] Fabric fabric)
        {
            if (ModelState.IsValid)
            {
                _context.Add(fabric);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fabric);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var fabric = await _context.Fabrics.FindAsync(id);
            if (fabric == null) return NotFound();
            return View(fabric);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,AdditionalPrice")] Fabric fabric)
        {
            if (id != fabric.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(fabric);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fabric);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var fabric = await _context.Fabrics.FindAsync(id);
            if (fabric != null)
            {
                _context.Fabrics.Remove(fabric);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}