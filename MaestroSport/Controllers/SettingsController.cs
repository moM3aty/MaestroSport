using MaestroSport.Data;
using MaestroSport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MaestroSport.Controllers
{
    // كنترولر للتحكم بإعدادات الموقع مثل شريط الإعلانات
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "AnnouncementBar");
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAnnouncement(string announcementValue)
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "AnnouncementBar");
            if (setting != null)
            {
                setting.Value = announcementValue;
                _context.Update(setting);
            }
            else
            {
                _context.SiteSettings.Add(new SiteSetting { Key = "AnnouncementBar", Value = announcementValue });
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تحديث شريط الإعلانات بنجاح وسيظهر للزبائن فوراً!";
            return RedirectToAction(nameof(Index));
        }
    }
}