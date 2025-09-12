using HMSApp.Data;
using HMSApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HMSApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        // Constructor with all necessary services injected
        public AdminController(ILogger<AdminController> logger, ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult BookedScans()
        {
            var allScansFromDb = _context.Scan.OrderBy(s => s.Id).ToList();
            return View(allScansFromDb);
        }

        // Accept: open upload page for a specific scan by id
        public async Task<IActionResult> Accept(int id)
        {
            var scan = await _context.Scan.FirstOrDefaultAsync(a => a.Id == id);

            if (scan == null)
            {
                return NotFound();
            }

            return View(scan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var appointment = await _context.Scan.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }
            _context.Scan.Remove(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(BookedScans));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int id, IFormFile scanFile)
        {
            if (scanFile == null || scanFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Accept), new { id });
            }

            // --- START: Added File Type Validation ---
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(scanFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Invalid file type. Please upload a PDF, JPG, or PNG file.";
                return RedirectToAction(nameof(Accept), new { id });
            }
            // --- END: Added File Type Validation ---

            var scanToUpdate = await _context.Scan.FirstOrDefaultAsync(s => s.Id == id);

            if (scanToUpdate == null)
            {
                return NotFound();
            }

            string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            // Delete old file if exists
            if (!string.IsNullOrWhiteSpace(scanToUpdate.FileName))
            {
                var oldFilePath = Path.Combine(uploadsFolder, scanToUpdate.FileName);
                try
                {
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old scan file: {Path}", oldFilePath);
                }
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(scanFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await scanFile.CopyToAsync(fileStream);
            }

            scanToUpdate.FileName = uniqueFileName;

            try
            {
                _context.Update(scanToUpdate);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "File uploaded successfully!";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error occurred while saving the uploaded file to the database for ScanId {ScanId}", id);
                TempData["ErrorMessage"] = "An error occurred while saving the file.";
            }

            return RedirectToAction(nameof(BookedScans));
        }
    }
}