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

        // The correct version of Accept, using patientName
        public async Task<IActionResult> Accept(string patientName)
        {
            // The value from the link (e.g., "mani") is now in the patientName variable
            var appointment = await _context.Scan
                                      .FirstOrDefaultAsync(a => a.PatientName == patientName);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
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
        public async Task<IActionResult> Upload(string patientName, IFormFile scanFile)
        {
            if (scanFile == null || scanFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("Accept", new { patientName });
            }

            var scanToUpdate = await _context.Scan
                .FirstOrDefaultAsync(s => s.PatientName == patientName);

            if (scanToUpdate == null)
            {
                return NotFound();
            }

            string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + scanFile.FileName;
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
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "An error occurred while saving the file.";
            }

            return RedirectToAction("Accept", new { patientName = scanToUpdate.PatientName });
        }
    }
}