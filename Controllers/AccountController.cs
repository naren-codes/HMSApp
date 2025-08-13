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
    [HttpPost]
    public IActionResult LoginPatient(string username, string password)
    {
        var user = _context.User.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            if (user.role == "patient")
            {
                return RedirectToAction("Index", "Home");
            }
            else if (user.role == "admin")
            {
                ViewBag.Error = "Admin login is not allowed here.";
                return View("PatientLogin");
            }
            else
            {
                ViewBag.Error = "Unauthorized role.";
                return View("PatientLogin");
            }
        }

        ViewBag.Error = "Invalid username or password";
        return View("PatientLogin");
    }

    [HttpPost]
    public IActionResult LoginAdmin(string username, string password)
    {
        var user = _context.User.FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            if (user.role == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (user.role == "patient")
            {
                ViewBag.Error = "Patient login is not allowed here.";
                return View("AdminLogin");
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
