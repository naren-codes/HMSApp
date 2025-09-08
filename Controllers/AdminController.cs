using HMSApp.Data;
using HMSApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HMSApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        public AdminController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult BookedScans()
        {

            var allScansFromDb = _context.Scan.OrderBy(s => s.Id).ToList();


            var bookedScans = allScansFromDb.Select(s => new Scan
            {
                Id = s.Id,
                PatientName = s.PatientName,
                AppointmentDate = s.AppointmentDate,
                LabName = s.LabName,
                ScanType = s.LabName == "Lab A" ? "MRI" :
                           s.LabName == "Lab B" ? "CT Scan" :
                           s.LabName == "Lab C" ? "X-Ray" : "Unknown"
            }).ToList();

            return View(bookedScans);
        }

        public IActionResult Accept(int id)
        {
            // Your code to handle the accept action
            var appointment = _context.Scan.FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }
        // In your AdminController.cs
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            // Find the record to be deleted
            var appointment = await _context.Scan.FindAsync(id);

            if (appointment == null)
            {
                // Handle case where appointment is not found
                return NotFound();
            }

            // Remove the appointment from the database
            _context.Scan.Remove(appointment);

            // Save the changes to the database
            await _context.SaveChangesAsync();

            // Redirect back to the scan list page
            return RedirectToAction(nameof(Index));
        }
    }
}
