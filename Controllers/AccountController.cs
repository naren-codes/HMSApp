using HMSApp.Models;
using HMSApp.Services;
using Microsoft.AspNetCore.Mvc;
using HMSApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

public class AccountController : Controller
{
    private readonly AccountService _accountService;
    private readonly PatientService _patientService;
    private readonly ApplicationDbContext _context;

    public AccountController(AccountService accountService, PatientService patientService, ApplicationDbContext context)
    {
        _accountService = accountService;
        _patientService = patientService;
        _context = context;
    }

    [HttpGet]
    public IActionResult PatientLogin() => View();

    [HttpGet]
    public IActionResult AdminLogin() => View();

    [HttpGet]
    public IActionResult DoctorLogin() => View();

    private IActionResult HandleLogin(string username, string password, string expectedRole, string redirectController)
    {
        var user = _accountService.Authenticate(username, password);
        if (user != null && user.role?.ToLower() == expectedRole.ToLower())
        {
            return RedirectToAction("Index", redirectController);
        }
        ViewBag.Error = user == null ? "Invalid username or password" : $"{expectedRole} Invalid credentials.";
        return View($"{expectedRole}Login");
    }

    [HttpPost]
    public IActionResult LoginPatient(string username, string password)
    {
        var user = _accountService.Authenticate(username, password);

        if (user != null && user.role?.ToLower() == "patient")
        {
            var patient = _context.Patient.FirstOrDefault(p => p.Username == username);
            if (patient != null)
            {
                HttpContext.Session.SetString("Username", user.Username!);
            }
            return RedirectToAction("Dashboard", "Patient");
        }
        ViewBag.Error = "Invalid login credentials.";
        return View("PatientLogin");
    }

    [HttpPost]
    public IActionResult LoginAdmin(string username, string password) => HandleLogin(username, password, "admin", "Admin");



    [HttpPost]
    public IActionResult LoginDoctor(string username, string password)
    {
        var user = _accountService.Authenticate(username, password);
        if (user != null && user.role?.ToLower() == "doctor")
        {
            var doctor = _context.Doctor.FirstOrDefault(d => d.Username == username);
            if (doctor != null)
            {
                HttpContext.Session.SetInt32("DoctorId", doctor.DoctorId);
            }
            ViewBag.Success = true;
            return View("DoctorLogin");

            return RedirectToAction("DoctorDashboard", "Doctor");
        }
        ViewBag.Error = "Invalid login credentials.";
        return View("DoctorLogin");
    }

    [HttpGet]
    public IActionResult PatientRegister() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PatientRegister(Patient patient)
    {
        // First, check if the standard model validation (e.g., required fields) passes.
        if (ModelState.IsValid)
        {
            bool usernameExists = await _context.User.AnyAsync(u => u.Username == patient.Username);

            if (usernameExists)
            {
                // If the username exists, add an error to the ModelState.
                // This error will be displayed by the <span asp-validation-for="Username"> tag in your view.
                ModelState.AddModelError("Username", "Username already exists. Please choose a different one.");
            }
            else
            {
                var user = new User
                {
                    Username = patient.Username,
                    Password = HashPassword(patient.Password),
                    role = "patient"
                };
                _context.User.Add(user);


                _context.Patient.Add(patient);
                await _context.SaveChangesAsync();

                return RedirectToAction("PatientLogin");
            }
        }


        return View(patient);
    }

    private string HashPassword(string password)
    {
        return password;
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("DoctorLogin");
    }
}