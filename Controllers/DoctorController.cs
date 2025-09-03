using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HMSApp.Data;
using HMSApp.Models;

namespace HMSApp.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Profile()
        {

            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            if (doctorId == null)
            {

                return RedirectToAction("Login", "Account");
            }

            var doctor = await _context.Doctor.FindAsync(doctorId);

            if (doctor == null)
            {

                return NotFound();
            }
            return View(doctor);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.DoctorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                // After a successful save, redirect back to the profile page.
                return RedirectToAction(nameof(DoctorDashboard));
            }
            // If the form data is not valid, return to the form with errors.
            return View("Profile", doctor);
        }

        public IActionResult DoctorDashboard()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                return RedirectToAction("DoctorLogin", "Account");
            }
            var doctor = _context.Doctor.FirstOrDefault(d => d.DoctorId == doctorId);
            if (doctor == null)
            {
                return RedirectToAction("DoctorLogin", "Account");
            }

            ViewData["DoctorName"] = doctor.Name;
            ViewData["DoctorSpecialization"] = doctor.Specialization;
            ViewData["DoctorContact"] = doctor.ContactNumber;
            ViewData["DoctorSchedule"] = doctor.AvailabilitySchedule;

            var allAppointments = _context.Appointment
                .Where(a => a.DoctorName == doctor.Name)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot)
                .ToList();

            var today = DateTime.Today;
            var pendingAppointmentsList = allAppointments
                .Where(a => string.Equals(a.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var upcomingAppointmentsList = allAppointments
                .Where(a => (a.AppointmentDate.Date >= today && !string.Equals(a.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                            || string.Equals(a.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                .DistinctBy(a => a.AppointmentId)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot)
                .ToList();

            ViewData["TotalAppointments"] = allAppointments.Count; 
            ViewData["UpcomingAppointments"] = upcomingAppointmentsList.Count; // adjusted logic
            ViewData["PendingAppointments"] = pendingAppointmentsList.Count; 
            ViewData["TodayAppointments"] = allAppointments.Count(a => a.AppointmentDate.Date == today);

            // Initial table shows upcoming (with all pending included)
            return View(upcomingAppointmentsList);
        }

        
        [HttpPost]
        public async Task<IActionResult> CompleteAppointmentAndCreateBill([FromBody] CompleteBillRequest request)
        {
            if (request == null) return BadRequest("Invalid data (empty body)");
            if (request.appointmentId <= 0) return BadRequest("Invalid appointment id");
            var appointment = await _context.Appointment.FirstOrDefaultAsync(a => a.AppointmentId == request.appointmentId);
            if (appointment == null) return NotFound("Appointment not found");

            try
            {
                appointment.Status = "Completed";
                var bill = new Bill
                {
                    PatientId = appointment.PatientId,
                    PatientName = appointment.PatientName,
                    Prescription = request.prescription,
                    TotalAmount = request.totalAmount,
                    PaymentStatus = "Unpaid", 
                    BillDate = DateTime.UtcNow
                };
                _context.Bill.Add(bill);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, billId = bill.BillId });
            }
            catch (Exception ex)
            {
                var root = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"Error saving bill: {root}");
            }
        }

        public class CompleteBillRequest
        {
            public int appointmentId { get; set; }
            public decimal totalAmount { get; set; }
            public string? prescription { get; set; }
        }
       
        public async Task<IActionResult> Index()
        {
            return View(await _context.Doctor.ToListAsync());
        }

       
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctor
                .FirstOrDefaultAsync(m => m.DoctorId == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DoctorId,Name,Specialization,ContactNumber,AvailabilitySchedule,Username,Password")] Doctor doctor)
        {
            if (string.IsNullOrWhiteSpace(doctor.Role))
            {
                doctor.Role = "Doctor";
            }


            if (!string.IsNullOrWhiteSpace(doctor.Username))
            {
                bool usernameExists = await _context.User.AnyAsync(u => u.Username == doctor.Username) ||
                                       await _context.Doctor.AnyAsync(d => d.Username == doctor.Username);
                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                }
            }
            if (ModelState.IsValid)
            {

                _context.Add(doctor);
                await _context.SaveChangesAsync();
                if (!string.IsNullOrWhiteSpace(doctor.Username) && !string.IsNullOrWhiteSpace(doctor.Password))
                {
                    var user = new User
                    {
                        Username = doctor.Username,
                        Password = doctor.Password,
                        role = doctor.Role ?? "Doctor"
                    };
                    _context.User.Add(user);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            return View(doctor);
        }

        // GET: Doctor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctor.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            return View(doctor);
        }

        // POST: Doctor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DoctorId,Name,Specialization,ContactNumber,AvailabilitySchedule,Username,Password")] Doctor doctor)
        {
            if (id != doctor.DoctorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.DoctorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(doctor);
        }

        // GET: Doctor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctor
                .FirstOrDefaultAsync(m => m.DoctorId == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctor.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctor.Remove(doctor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctor.Any(e => e.DoctorId == id);
        }

        [HttpGet]
        public async Task<IActionResult> AppointmentsData(string filter = "upcoming")
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return Unauthorized();
            var doctor = await _context.Doctor.FirstOrDefaultAsync(d => d.DoctorId == doctorId);
            if (doctor == null) return NotFound();

            var today = DateTime.Today;
            var query = _context.Appointment.AsNoTracking()
                .Where(a => a.DoctorName == doctor.Name);

            switch (filter?.ToLowerInvariant())
            {
                case "pending":
                    query = query.Where(a => a.Status == "Pending");
                    break;
                case "completed":
                    query = query.Where(a => a.Status == "Completed");
                    break;
                case "all":
                    query = query.Where(a => a.Status == "Pending" || a.Status == "Completed");
                    break;
                case "upcoming":
                default:
                    query = query.Where(a => (a.AppointmentDate.Date >= today && a.Status != "Completed") || a.Status == "Pending");
                    break;
            }

            var list = await query
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot)
                .Select(a => new {
                    a.AppointmentId,
                    a.AppointmentDate,
                    a.TimeSlot,
                    a.PatientName,
                    a.Status,
                    a.PatientDescription,
                    a.PatientId
                }).ToListAsync();

            return Json(list);
        }
    }
}
