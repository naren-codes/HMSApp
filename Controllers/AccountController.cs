using HMSApp.Models;
using HMSApp.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult PatientLogin()
    {
        return View();
    }

    [HttpGet]
    public IActionResult AdminLogin()
    {
        return View();
    }
    public IActionResult DoctorLogin()
    {
        return View();
    }

    [HttpPost]
    public IActionResult LoginPatient(string username, string password)
    {
        var user = _context.User.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            if (user.role == "patient")
            {
                // Set session or redirect to patient dashboard
                return RedirectToAction("Index", "Home");
            }
            else if (user.role == "admin")
            {
                ViewBag.Error = "Admin login is not allowed here.";
                return View();
            }
            else
            {
                ViewBag.Error = "Unauthorized role.";
                return View();
            }
        }

        ViewBag.Error = "Invalid username or password";
        return View();
    }

    [HttpPost]
    public IActionResult LoginAdmin(string username, string password)
    {
        var user = _context.User.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            if (user.role == "admin")
            {
                // You can optionally store session info here
                return RedirectToAction("Index", "Admin");
            }
            else if (user.role == "patient")
            {
                ViewBag.Error = "Patient login is not allowed here.";
                return View("AdminLogin"); // Return to the correct login view
            }
            else
            {
                ViewBag.Error = "Unauthorized role.";
                return View("AdminLogin");
            }
        }

        ViewBag.Error = "Invalid username or password";
        return View("AdminLogin");
    }
    [HttpPost]
    public IActionResult LoginDoctor(string username, string password)
    {
        var user = _context.User.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            if (user.role == "doctor")
            {
                // You can optionally store session info here
                return RedirectToAction("Index", "Home");
            }
            else if (user.role == "patient")
            {
                ViewBag.Error = "Patient login is not allowed here.";
                return View("AdminLogin"); // Return to the correct login view
            }
            else
            {
                ViewBag.Error = "Unauthorized role.";
                return View("AdminLogin");
            }
        }

        ViewBag.Error = "Invalid username or password";
        return View("AdminLogin");
    }
}
