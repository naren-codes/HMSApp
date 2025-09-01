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

            var appointmentsQuery = _context.Appointment
                .Where(a => a.DoctorName == doctor.Name)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot);

            var appointments = appointmentsQuery.ToList();
            var today = DateTime.Today;
            ViewData["TotalAppointments"] = appointments.Count;
            ViewData["UpcomingAppointments"] = appointments.Count(a => a.AppointmentDate.Date >= today);
            ViewData["PendingAppointments"] = appointments.Count(a => a.Status == "Pending");
            ViewData["TodayAppointments"] = appointments.Count(a => a.AppointmentDate.Date == today);

            return View(appointments);
        }

        // New: Create bill and mark appointment completed
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
                    PaymentStatus = "Unpaid", // changed from Pending to satisfy DB CHECK constraint
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
        // GET: Doctor
        public async Task<IActionResult> Index()
        {
            return View(await _context.Doctor.ToListAsync());
        }

        // GET: Doctor/Details/5
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
    }
}
