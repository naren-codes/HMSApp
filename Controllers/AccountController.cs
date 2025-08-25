using HMSApp.Models;
using HMSApp.Services;
using Microsoft.AspNetCore.Mvc;
using HMSApp.Data;

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

    [HttpGet]
    public IActionResult DoctorRegister() => View();

    private IActionResult HandleLogin(string username, string password, string expectedRole, string redirectController)
    {
        var user = _accountService.Authenticate(username, password);
        if (user != null && user.role?.ToLower() == expectedRole.ToLower())
        {
            return RedirectToAction("Index", redirectController);
        }
        ViewBag.Error = user == null ? "Invalid username or password" : $"{expectedRole} login is not allowed here.";
        return View($"{expectedRole}Login");
    }

    [HttpPost]
    public IActionResult LoginPatient(string username, string password)
    {
        var user = _accountService.Authenticate(username, password);

        if (user != null && user.role?.ToLower() == "patient")
        {
            HttpContext.Session.SetInt32("UserId", user.userId); 
            return RedirectToAction("Dashboard", "Patient");
        }

        ViewBag.Error = "Invalid login credentials.";
        return View("PatientLogin");
    }


    [HttpPost]
    public IActionResult LoginAdmin(string username, string password) => HandleLogin(username, password, "admin", "Admin");

    [HttpPost]
    public IActionResult LoginDoctor(string username, string password, string userType)
    {
        var user = _accountService.Authenticate(username, password);
        if (user != null && user.role?.ToLower() == "doctor")
        {
            // find doctor row
            var doctor = _context.Doctor.FirstOrDefault(d => d.Username == username);
            if (doctor != null)
            {
                HttpContext.Session.SetInt32("DoctorId", doctor.DoctorId);
            }
            return RedirectToAction("DoctorDashboard", "Doctor");
        }
        ViewBag.Error = "Invalid login credentials.";
        return View("DoctorLogin");
    }

    [HttpPost]
    public IActionResult Register(Patient model)
    {
        if (ModelState.IsValid)
        {
            _accountService.RegisterPatient(model);
            return RedirectToAction("Login");
        }
        return View(model);
    }

    [HttpPost]
    public IActionResult RegisterDoctor(Doctor model)
    {
        if (ModelState.IsValid)
        {
            _accountService.RegisterDoctor(model);
            return RedirectToAction("DoctorLogin");
        }
        return View("DoctorRegister", model);
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("DoctorLogin");
    }
}