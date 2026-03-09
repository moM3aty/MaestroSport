using MaestroSport.Data;
using MaestroSport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Threading.Tasks;

namespace MaestroSport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SettingsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.SiteSettings.ToListAsync();
            return View(settings);
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

            TempData["SuccessMessage"] = "تم تحديث شريط الإعلانات بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePromoBanner(IFormFile bannerImage)
        {
            if (bannerImage != null && bannerImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners");
                Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + bannerImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await bannerImage.CopyToAsync(fileStream);
                }

                string imageUrl = "images/banners/" + uniqueFileName;

                var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "PromoBannerUrl");
                if (setting != null)
                {
                    setting.Value = imageUrl;
                    _context.Update(setting);
                }
                else
                {
                    _context.SiteSettings.Add(new SiteSetting { Key = "PromoBannerUrl", Value = imageUrl });
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم تحديث البانر الإعلاني بنجاح";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemovePromoBanner()
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "PromoBannerUrl");
            if (setting != null)
            {
                _context.SiteSettings.Remove(setting);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تم إزالة البانر الإعلاني بنجاح";
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCapacity(int dailyCapacity)
        {
            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == "DailyCapacity");
            if (setting != null)
            {
                setting.Value = dailyCapacity.ToString();
                _context.Update(setting);
            }
            else
            {
                _context.SiteSettings.Add(new SiteSetting { Key = "DailyCapacity", Value = dailyCapacity.ToString() });
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تحديث السعة الإنتاجية اليومية بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }
}