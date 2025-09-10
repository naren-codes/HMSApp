using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HMSApp.Data;
using HMSApp.Models;
using Microsoft.AspNetCore.Http; // for session

namespace HMSApp.Controllers
{
    public partial class DoctorController : Controller
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

            // CLEANUP: Remove old unpaid bills for this doctor's patients (older than 1 hour) to prevent accumulation
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var doctorPatientIds = _context.Appointment
                .Where(a => a.DoctorName == doctor.Name)
                .Select(a => a.PatientId)
                .Distinct()
                .ToList();
            
            var oldUnpaidBills = _context.Bill
                .Where(b => b.PaymentStatus == "Unpaid" && 
                           b.BillDate < oneHourAgo && 
                           doctorPatientIds.Contains(b.PatientId) &&
                           b.DoctorName == doctor.Name)
                .ToList();
            
            if (oldUnpaidBills.Any())
            {
                _context.Bill.RemoveRange(oldUnpaidBills);
                _context.SaveChanges();
            }

            ViewData["DoctorName"] = doctor.Name;
            ViewData["DoctorSpecialization"] = doctor.Specialization;
            ViewData["DoctorContact"] = doctor.ContactNumber;
            ViewData["DoctorSchedule"] = doctor.AvailabilitySchedule;
            ViewData["DoctorAvailability"] = doctor.IsAvailable; 

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
                // DELETE ANY EXISTING UNPAID BILLS FOR THIS APPOINTMENT
                // Strategy 1: Delete by AppointmentId (most accurate)
                var existingUnpaidBills = await _context.Bill
                    .Where(b => b.AppointmentId == appointment.AppointmentId && b.PaymentStatus == "Unpaid")
                    .ToListAsync();
                
                // Strategy 2: Also delete any unpaid bills for this patient-doctor combination on the same date
                // (in case there are orphaned bills without proper AppointmentId)
                var additionalUnpaidBills = await _context.Bill
                    .Where(b => b.PatientId == appointment.PatientId && 
                               b.DoctorName == appointment.DoctorName &&
                               b.PaymentStatus == "Unpaid" &&
                               b.AppointmentDate.HasValue &&
                               b.AppointmentDate.Value.Date == appointment.AppointmentDate.Date &&
                               !existingUnpaidBills.Select(eb => eb.BillId).Contains(b.BillId))
                    .ToListAsync();
                
                var allUnpaidBillsToRemove = existingUnpaidBills.Concat(additionalUnpaidBills).ToList();
                
                if (allUnpaidBillsToRemove.Any())
                {
                    _context.Bill.RemoveRange(allUnpaidBillsToRemove);
                }

                // DO NOT set appointment status to "Completed" here - it should only be completed when payment is made
                var prescriptionText = request.prescription;
                
                // Store the prescription in the Appointment table's prescription column
                if (!string.IsNullOrWhiteSpace(prescriptionText))
                {
                    appointment.Prescription = prescriptionText;
                }
                
                var bill = new Bill
                {
                    PatientId = appointment.PatientId,
                    AppointmentId = appointment.AppointmentId, // Primary matching field
                    
                    // Additional fields for cross-environment matching
                    AppointmentDate = appointment.AppointmentDate,
                    DoctorName = appointment.DoctorName,
                    TimeSlot = appointment.TimeSlot,
                    
                    PatientName = appointment.PatientName,
                    Prescription = prescriptionText,
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
                case "cancelled":
                    query = query.Where(a => a.Status == "Cancelled");
                    break;
                case "all":
                    query = query.Where(a => a.Status == "Pending" || a.Status == "Completed" || a.Status == "Cancelled");
                    break;
                case "upcoming":
                default:
                    query = query.Where(a => (a.AppointmentDate.Date >= today && a.Status != "Completed" && a.Status != "Cancelled") || a.Status == "Pending");
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
                    a.Symptoms, // Changed from PatientDescription to Symptoms
                    a.PatientId
                }).ToListAsync();

            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin", "Account");
            var doctor = await _context.Doctor.FirstOrDefaultAsync(d => d.DoctorId == doctorId);
            if (doctor == null) return RedirectToAction("DoctorLogin", "Account");

            var doctorName = doctor.Name;
            var patientIds = await _context.Appointment
                .Where(a => a.DoctorName == doctorName)
                .Select(a => a.PatientId)
                .Distinct()
                .ToListAsync();

            var bills = await _context.Bill
                .Where(b => patientIds.Contains(b.PatientId) && b.PaymentStatus == "Paid")
                .OrderByDescending(b => b.BillDate)
                .Take(200)
                .ToListAsync();

            ViewData["DoctorName"] = doctor.Name;
            return View("Payments", bills);
        }

        [HttpGet]
        public async Task<IActionResult> PayBill(int billId)
        {
       
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin", "Account");
            var bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId);
            if (bill == null) return NotFound();
            ViewData["DoctorMode"] = true;
            return View("~/Views/Patient/Payment.cshtml", bill);
        }

        [HttpPost]
        public async Task<IActionResult> SetAvailability([FromBody] SetAvailabilityRequest request)
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return Unauthorized();

            var doctor = await _context.Doctor.FirstOrDefaultAsync(d => d.DoctorId == doctorId);
            if (doctor == null) return NotFound();

            doctor.IsAvailable = request.IsAvailable;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, isAvailable = doctor.IsAvailable });
        }

        [HttpPost]
        public async Task<IActionResult> CancelUnpaidBill([FromBody] CancelBillRequest request)
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return Unauthorized();

            try
            {
                var bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == request.billId && b.PaymentStatus == "Unpaid");
                if (bill != null)
                {
                    _context.Bill.Remove(bill);
                    await _context.SaveChangesAsync();
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error canceling bill: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment([FromBody] AppointmentCancelRequest request)
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return Unauthorized();

            var appointment = await _context.Appointment.FirstOrDefaultAsync(a => a.AppointmentId == request.AppointmentId);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found." });
            }

            try
            {
                
                appointment.Status = "Cancelled";

                // Remove any unpaid bills associated with this appointment
                // Strategy 1: Remove by AppointmentId (most accurate)
                var unpaidBillsByAppointmentId = await _context.Bill
                    .Where(b => b.AppointmentId == appointment.AppointmentId && b.PaymentStatus == "Unpaid")
                    .ToListAsync();

                // Strategy 2: Also remove any unpaid bills for this patient-doctor combination on the same date
                // (in case there are orphaned bills without proper AppointmentId)
                var unpaidBillsByPatientDoctor = await _context.Bill
                    .Where(b => b.PatientId == appointment.PatientId && 
                               b.DoctorName == appointment.DoctorName &&
                               b.PaymentStatus == "Unpaid" &&
                               b.AppointmentDate.HasValue &&
                               b.AppointmentDate.Value.Date == appointment.AppointmentDate.Date &&
                               !unpaidBillsByAppointmentId.Select(ub => ub.BillId).Contains(b.BillId))
                    .ToListAsync();

                var allUnpaidBillsToRemove = unpaidBillsByAppointmentId.Concat(unpaidBillsByPatientDoctor).ToList();

                if (allUnpaidBillsToRemove.Any())
                {
                    _context.Bill.RemoveRange(allUnpaidBillsToRemove);
                }

                // Save all changes to the database
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error cancelling appointment: {ex.Message}" });
            }
        }

        public class SetAvailabilityRequest
        {
            public bool IsAvailable { get; set; }
        }

        public class CancelBillRequest
        {
            public int billId { get; set; }
        }

        public class AppointmentCancelRequest
        {
            public int AppointmentId { get; set; }
        }
    }
}
