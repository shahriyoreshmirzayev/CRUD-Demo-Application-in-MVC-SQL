using CRUDDEMO1.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRUDDEMO1.Controllers;

[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly Employee_dal _employeeDal;

    public AccountController()
    {
        _employeeDal = new Employee_dal();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ViewBag.ErrorMessage = "Username yoki parol kiritilmadi.";
            return View();
        }

        if (_employeeDal.ValidateUser(username, password))
        {
            var user = _employeeDal.GetUserByUsername(username);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Employee");
        }

        ViewBag.ErrorMessage = "Noto‘g‘ri username yoki parol.";
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string username, string password, string role)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
        {
            ViewBag.ErrorMessage = "Barcha maydonlarni to‘ldiring.";
            return View();
        }

        if (_employeeDal.UserExists(username))
        {
            ViewBag.ErrorMessage = "Bu username allaqachon ro‘yxatdan o‘tgan.";
            return View();
        }

        bool result = _employeeDal.RegisterUser(username, password, role);
        if (result)
        {
            TempData["SuccessMessage"] = "Ro‘yxatdan o‘tish muvaffaqiyatli amalga oshirildi! Iltimos, tizimga kiring.";
            return RedirectToAction("Login");
        }

        ViewBag.ErrorMessage = "Ro‘yxatdan o‘tishda xato yuz berdi.";
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ForgotPassword(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            ViewBag.ErrorMessage = "Username kiritilmadi.";
            return View();
        }

        if (!_employeeDal.UserExists(username))
        {
            ViewBag.ErrorMessage = "Bu username ro‘yxatdan o‘tmagan.";
            return View();
        }

        return RedirectToAction("ResetPassword", new { username });
    }

    [HttpGet]
    public IActionResult ResetPassword(string username)
    {
        ViewBag.Username = username;
        return View();
    }

    [HttpPost]
    public IActionResult ResetPassword(string username, string newPassword)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPassword))
        {
            ViewBag.ErrorMessage = "Username yoki yangi parol kiritilmadi.";
            return View();
        }

        bool result = _employeeDal.ResetPassword(username, newPassword);
        if (result)
        {
            TempData["SuccessMessage"] = "Parol muvaffaqiyatli yangilandi! Iltimos, tizimga kiring.";
            return RedirectToAction("Login");
        }

        ViewBag.ErrorMessage = "Parolni yangilashda xato yuz berdi.";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}