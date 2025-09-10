using HMSApp.Data;
using HMSApp.Models;
using HMSApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HMSApp.ViewModels;

namespace HMSApp.Controllers
{
    public class PatientController : Controller
    {
        private readonly PatientService _patientService;
        private readonly ApplicationDbContext _context;

        public PatientController(PatientService patientService, ApplicationDbContext context)
        {
            _patientService = patientService;
            _context = context;
        }

        // Admin-facing list of all patients
        public IActionResult Index()
        {
            var patients = _patientService.GetAllPatients();
            return View(patients);
        }

        // =========================================================================
        // START: THIS IS THE CORRECTED AND FINAL APPOINTMENT HISTORY ACTION
        // =========================================================================
        public async Task<IActionResult> AppointmentHistory()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("PatientLogin", "Account");
            }

            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null)
            {
                return RedirectToAction("PatientLogin", "Account");
            }

            // 1. Fetch appointments for the patient
            var appointments = await _patientService.GetPatientAppointmentsAsync(patient.PatientId);
            var sortedAppointments = appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.TimeSlot)
                .ToList();

            // 2. Fetch scans for the patient using the reliable PatientId
            var scans = await _context.Scan
                                      .Where(s => s.PatientId == patient.PatientId)
                                      .OrderByDescending(s => s.AppointmentDate)
                                      .ToListAsync();

            // 3. Create the ViewModel to hold both lists
            var viewModel = new AppointmentHistoryViewModel
            {
                Appointment = sortedAppointments,
                Scan = scans
            };

            // 4. Pass the single viewModel to the view
            return View(viewModel);
        }
        // =======================================================================
        // END: APPOINTMENT HISTORY ACTION
        // =======================================================================


        // =======================================================================
        // START: NEW ACTION FOR VIEWING SCAN FILES
        // =======================================================================
        public async Task<IActionResult> ViewScanDocument(int id)
        {
            var scan = await _context.Scan.FindAsync(id);
            if (scan == null || string.IsNullOrEmpty(scan.FileName))
            {
                return NotFound("File not found.");
            }

            // Assumes files are in a folder named "uploads" inside "wwwroot"
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", scan.FileName);

            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("File does not exist on the server.");
            }

            // Returns the file for viewing in the browser. 
            // Adjust "application/pdf" to "image/jpeg", etc., if you have different file types.
            return PhysicalFile(physicalPath, "application/pdf");
        }
        // =======================================================================
        // END: NEW ACTION
        // =======================================================================

        private int GetCurrentPatientId()
        {
            return HttpContext.Session.GetInt32("PatientId") ?? 0;
        }

        // Admin-facing patient details
        public IActionResult Details(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        // Admin-facing create patient form (GET)
        public IActionResult Create() => View();

        // Admin-facing create patient form (POST)
        [HttpPost]
        public IActionResult Create(Patient patient)
        {
            if (ModelState.IsValid)
            {
                _patientService.AddPatient(patient);
                return RedirectToAction("Index");
            }
            return View(patient);
        }

        // Admin-facing edit patient form (GET)
        public IActionResult Edit(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        // Admin-facing edit patient form (POST)
        [HttpPost]
        public IActionResult Edit(Patient patient)
        {
            if (ModelState.IsValid)
            {
                _patientService.UpdatePatient(patient);
                return RedirectToAction("Index");
            }
            return View(patient);
        }

        // Admin-facing delete confirmation page (GET)
        public IActionResult Delete(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        // Admin-facing delete confirmation (POST)
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _patientService.DeletePatient(id);
            return RedirectToAction("Index");
        }

        // Patient-facing dashboard
        public async Task<IActionResult> Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("PatientLogin", "Account");
            }

            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null)
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            ViewData["PatientName"] = char.ToUpper(patient.Name[0]) + patient.Name.Substring(1);
            ViewData["PatientContact"] = patient.ContactNumber ?? "N/A";
            ViewData["PatientAddress"] = patient.Address ?? "N/A";
            ViewData["PatientGender"] = patient.Gender ?? "N/A";

            var appointments = await _patientService.GetPatientAppointmentsAsync(patient.PatientId);

            // Fetch all bills for this patient
            var bills = await _context.Bill
                .Where(b => b.PatientId == patient.PatientId)
                .ToListAsync();

            List<PatientApptDashboardRow> enriched = new();

            foreach (var appt in appointments)
            {
                Bill? matchedBill = null;

                // Strategy 1: Direct appointment ID matching (best for same environment)
                matchedBill = bills.FirstOrDefault(b => b.AppointmentId == appt.AppointmentId);

                // Strategy 2: Multi-criteria matching (works across different environments)
                if (matchedBill == null)
                {
                    matchedBill = bills.FirstOrDefault(b =>
                        b.AppointmentDate.HasValue &&
                        b.AppointmentDate.Value.Date == appt.AppointmentDate.Date &&
                        b.DoctorName == appt.DoctorName &&
                        b.TimeSlot == appt.TimeSlot &&
                        b.PatientName == appt.PatientName);
                }

                // Strategy 3: Date and patient matching (broader fallback)
                if (matchedBill == null)
                {
                    var appointmentDate = appt.AppointmentDate.Date;

                    var relevantBills = bills.Where(b =>
                        b.AppointmentDate.HasValue &&
                        b.AppointmentDate.Value.Date == appointmentDate &&
                        b.PatientName == appt.PatientName &&
                        b.DoctorName == appt.DoctorName).ToList();

                    if (relevantBills.Any())
                    {
                        matchedBill = relevantBills.OrderBy(b => Math.Abs((b.BillDate - appt.AppointmentDate).TotalHours)).FirstOrDefault();
                    }
                }

                // Strategy 4: Legacy date-based matching for older bills
                if (matchedBill == null && string.Equals(appt.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    var usedBillIds = enriched.Where(e => e.Bill != null).Select(e => e.Bill!.BillId).ToList();
                    var appointmentDate = appt.AppointmentDate.Date;

                    matchedBill = bills
                        .Where(b => !usedBillIds.Contains(b.BillId) &&
                                    (!b.AppointmentDate.HasValue || b.AppointmentDate.Value.Date == appointmentDate) &&
                                    b.PatientName == appt.PatientName)
                        .OrderByDescending(b => b.BillDate)
                        .FirstOrDefault();
                }

                enriched.Add(new PatientApptDashboardRow
                {
                    Appointment = appt,
                    Bill = matchedBill
                });
            }

            // Show latest appointments first in UI
            enriched = enriched.OrderByDescending(r => r.Appointment.AppointmentDate).ToList();
            return View(enriched);
        }

        // Patient-facing "Book Appointment" page (GET)
        public IActionResult BookAppointment()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("PatientLogin", "Account");

            var patient = _context.Patient.FirstOrDefault(p => p.Username == username);
            if (patient == null)
                return RedirectToAction("PatientLogin", "Account");

            ViewBag.PatientName = patient.Name;

            // Get all doctors with availability status
            var allDoctors = _context.Doctor
                .OrderBy(d => d.Name)
                .ToList();

            var doctorOptions = allDoctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.IsAvailable
                    ? (d.Specialization != null && d.Specialization != "" ? $"{d.Name} ({d.Specialization})" : d.Name)
                    : (d.Specialization != null && d.Specialization != "" ? $"{d.Name} ({d.Specialization}) - Unavailable" : $"{d.Name} - Unavailable"),
                Disabled = !d.IsAvailable
            }).ToList(); 

            ViewBag.Doctors = doctorOptions;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookAppointment(Appointment model, string PatientDescription, string PatientName)
        {
            var username = HttpContext.Session.GetString("Username");
            var patient = _context.Patient.FirstOrDefault(p => p.Username == username);
            if (patient == null)
                return RedirectToAction("PatientLogin", "Account");

            model.PatientId = patient.PatientId;
            model.PatientName = string.IsNullOrWhiteSpace(PatientName) ? patient.Name : PatientName.Trim();
            model.Symptoms = PatientDescription;
            model.Status = "Pending";

            // Check if selected doctor is available
            var selectedDoctor = _context.Doctor.FirstOrDefault(d => d.DoctorId == model.DoctorId);
            if (selectedDoctor == null)
            {
                ModelState.AddModelError("DoctorId", "Please select a valid doctor.");
            }
            else if (!selectedDoctor.IsAvailable)
            {
                ModelState.AddModelError("DoctorId", "This doctor is currently unavailable. Please select another doctor.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.PatientName = model.PatientName;

                // Get all doctors with availability status for validation errors
                var allDoctors = _context.Doctor
                    .OrderBy(d => d.Name)
                    .ToList();

                var doctorOptions = allDoctors.Select(d => new SelectListItem
                {
                    Value = d.DoctorId.ToString(),
                    Text = d.IsAvailable
                        ? (d.Specialization != null && d.Specialization != "" ? $"{d.Name} ({d.Specialization})" : d.Name)
                        : (d.Specialization != null && d.Specialization != "" ? $"{d.Name} ({d.Specialization}) - Unavailable" : $"{d.Name} - Unavailable"),
                    Disabled = !d.IsAvailable
                }).ToList();

                ViewBag.Doctors = doctorOptions;
                return View(model);
            }

            var doctor = _context.Doctor.FirstOrDefault(d => d.DoctorId == model.DoctorId);
            model.DoctorName = doctor?.Name;

            _context.Appointment.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(BookingConfirmation), new { id = model.AppointmentId });
        }

        
        public IActionResult BookingConfirmation(int id)
        {
            var appt = _context.Appointment.FirstOrDefault(a => a.AppointmentId == id);
            return View(appt);
        }

        // Payment page
        [HttpGet]
        public async Task<IActionResult> Pay(int billId)
        {
            // Patient normal flow OR doctor on-spot flow
            var username = HttpContext.Session.GetString("Username");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            Bill? bill = null;
            Patient? patient = null;
            if (!string.IsNullOrEmpty(username))
            {
                patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
                if (patient == null) return RedirectToAction("PatientLogin", "Account");
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId && b.PatientId == patient.PatientId);
            }
            else if (doctorId != null)
            {
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId);
                if (bill != null)
                {
                    patient = await _context.Patient.FirstOrDefaultAsync(p => p.PatientId == bill.PatientId);
                }
            }
            else
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            if (bill == null || patient == null) return NotFound();
            return View("Payment", bill);
        }

        [HttpPost("/Patient/ProcessPayment")]
        public async Task<IActionResult> ProcessPayment(int billId, string mode, string? upiId)
        {
            var username = HttpContext.Session.GetString("Username");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            Bill? bill = null;
            Patient? patient = null;
            if (!string.IsNullOrEmpty(username))
            {
                patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
                if (patient == null) return RedirectToAction("PatientLogin", "Account");
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId && b.PatientId == patient.PatientId);
            }
            else if (doctorId != null) // doctor on-spot payment
            {
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId);
                if (bill != null)
                    patient = await _context.Patient.FirstOrDefaultAsync(p => p.PatientId == bill.PatientId);
            }
            else
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            if (bill == null || patient == null) return NotFound();

            if (bill.PaymentStatus == "Paid")
            {
                if (bill.Prescription?.Contains("[ON-SPOT]") == true)
                    return RedirectToAction("OnSpotBill", new { billId = bill.BillId });
                return RedirectToAction("Bill", new { billId });
            }

            const string offlineTag = "";
            if (mode == "Online")
            {
                if (string.IsNullOrWhiteSpace(upiId))
                {
                    ModelState.AddModelError("UpiId", "UPI Id required for online payment");
                    return View("Payment", bill);
                }
                bill.PaymentStatus = "Paid";
            }
            else
            {
                bill.PaymentStatus = "Paid";
                if (bill.Prescription == null || !bill.Prescription.Contains(offlineTag))
                {
                    bill.Prescription = (bill.Prescription == null ? offlineTag : bill.Prescription + "\n" + offlineTag);
                }
            }
            bill.BillDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            if (bill.Prescription?.Contains("[ON-SPOT]") == true)
                return RedirectToAction("OnSpotBill", new { billId = bill.BillId });
            return RedirectToAction("Bill", new { billId = bill.BillId });
        }

        // Fallback GET to prevent 404 if someone navigates directly (will redirect appropriately)
        [HttpGet("/Patient/ProcessPayment")]
        public IActionResult ProcessPaymentGet(int billId)
        {
            return RedirectToAction("Pay", new { billId });
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmQrPayment(int billId)
        {
            var username = HttpContext.Session.GetString("Username");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            Bill? bill = null;
            Patient? patient = null;
            if (!string.IsNullOrEmpty(username))
            {
                patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
                if (patient == null) return RedirectToAction("PatientLogin", "Account");
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId && b.PatientId == patient.PatientId);
            }
            else if (doctorId != null)
            {
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId);
                if (bill != null)
                    patient = await _context.Patient.FirstOrDefaultAsync(p => p.PatientId == bill.PatientId);
            }
            else
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            if (bill == null || patient == null) return NotFound();
            if (bill.PaymentStatus != "Paid")
            {
                bill.PaymentStatus = "Paid";
                bill.BillDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            if (bill.Prescription?.Contains("[ON-SPOT]") == true)
                return RedirectToAction("OnSpotBill", new { billId = bill.BillId });
            return RedirectToAction("Bill", new { billId = bill.BillId });
        }

        [HttpGet]
        public async Task<IActionResult> OnSpotBill(int billId)
        {
            var username = HttpContext.Session.GetString("Username");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            Bill? bill = null;
            Patient? patient = null;
            if (!string.IsNullOrEmpty(username))
            {
                patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
                if (patient == null) return RedirectToAction("PatientLogin", "Account");
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId && b.PatientId == patient.PatientId);
            }
            else if (doctorId != null)
            {
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId);
                if (bill != null)
                    patient = await _context.Patient.FirstOrDefaultAsync(p => p.PatientId == bill.PatientId);
            }
            else
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            if (bill == null || patient == null) return NotFound();
            if (bill.Prescription?.Contains("[ON-SPOT]") != true)
                return RedirectToAction("Bill", new { billId });
            var vm = new BillDisplayViewModel { Bill = bill, Patient = patient };
            return View("Bill_2", vm);
        }

        [HttpGet]
        public async Task<IActionResult> PayStatus(int billId)
        {
            var username = HttpContext.Session.GetString("Username");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            Bill? bill = null;
            if (!string.IsNullOrEmpty(username))
            {
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId && b.PatientId == _context.Patient.Where(p => p.Username == username).Select(p => p.PatientId).FirstOrDefault());
            }
            else if (doctorId != null)
            {
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId);
            }
            else return Unauthorized();
            if (bill == null) return NotFound();
            return Json(new { paid = bill.PaymentStatus == "Paid" });
        }

        // Patient-facing profile page
        public async Task<IActionResult> Profile()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Patient patient)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _patientService.UpdatePatient(patient);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (_patientService.GetPatientById(patient.PatientId) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Dashboard));
            }

            return View("Profile", patient);
        }

        [HttpGet]
        public async Task<IActionResult> Bill(int billId)
        {
            var username = HttpContext.Session.GetString("Username");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            Bill? bill = null; Patient? patient = null;
            if (!string.IsNullOrEmpty(username))
            {
                patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
                if (patient == null) return RedirectToAction("PatientLogin", "Account");
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId && b.PatientId == patient.PatientId);
            }
            else if (doctorId != null)
            {
                bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId);
                if (bill != null)
                    patient = await _context.Patient.FirstOrDefaultAsync(p => p.PatientId == bill.PatientId);
            }
            else
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            if (bill == null || patient == null) return NotFound();
            if (bill.Prescription?.Contains("[ON-SPOT]") == true)
                return RedirectToAction("OnSpotBill", new { billId });
            var vm = new BillDisplayViewModel { Bill = bill, Patient = patient };
            return View("Bill", vm);
        }


        public IActionResult BookScan()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Mri()
        {
            var scan = new Scan { LabName = "Lab A", ScanType = "MRI" };
            return View("Mri", scan);
        }
        [HttpPost]
        public async Task<IActionResult> AddMri(Scan scanFromForm)
        {
            var username = HttpContext.Session.GetString("Username");
            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null) return RedirectToAction("PatientLogin", "Account");

            var newScan = new Scan
            {
                PatientName = scanFromForm.PatientName,
                AppointmentDate = scanFromForm.AppointmentDate,
                PatientId = patient.PatientId,
                LabName = "Lab A",
                ScanType = "MRI"
            };
            _context.Scan.Add(newScan);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }


        [HttpGet]
        public IActionResult CTScan()
        {
            var scan = new Scan { LabName = "Lab B", ScanType = "CTScan" };
            return View("CTScan", scan);
        }

        [HttpPost]
        public async Task<IActionResult> CTScan(Scan scan)
        {
            var username = HttpContext.Session.GetString("Username");
            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null) return RedirectToAction("PatientLogin", "Account");

            scan.PatientId = patient.PatientId;
            scan.LabName = "Lab B";
            scan.ScanType = "CTScan";

            if (ModelState.IsValid)
            {
                _context.Scan.Add(scan);
                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }

            return View("CTScan", scan);
        }

        [HttpGet]
        public IActionResult XRay()
        {
            var scan = new Scan { LabName = "Lab C", ScanType = "XRay" };
            return View("XRay", scan);
        }

        [HttpPost]
        public async Task<IActionResult> XRay(Scan scan)
        {
            var username = HttpContext.Session.GetString("Username");
            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null) return RedirectToAction("PatientLogin", "Account");

            var newScan = new Scan
            {
                PatientName = scan.PatientName,
                AppointmentDate = scan.AppointmentDate,
                PatientId = patient.PatientId,
                LabName = "Lab C",
                ScanType = "X-RAY"
            };

            _context.Scan.Add(newScan);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }
        [HttpGet]
        public IActionResult Ultrasound()
        {
            return View("Ultrasound", new Scan());
        }

        [HttpPost]
        public async Task<IActionResult> AddUltrasound(Scan scan)
        {
            var username = HttpContext.Session.GetString("Username");
            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null) return RedirectToAction("PatientLogin", "Account");

            scan.PatientId = patient.PatientId;
            scan.LabName = "Lab C";

            if (scan.AppointmentDate == default(DateTime))
            {
                scan.AppointmentDate = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                _context.Scan.Add(scan);
                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }

            return View("Ultrasound", scan);
        }

        public class PatientApptDashboardRow
        {
            public Appointment Appointment { get; set; } = null!;
            public Bill? Bill { get; set; }
        }

        public class BillDisplayViewModel
        {
            public Bill Bill { get; set; } = null!;
            public Patient Patient { get; set; } = null!;
        }
    }
}