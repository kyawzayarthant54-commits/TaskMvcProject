using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskMvcProject.Models;

namespace TaskMvcProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6 || password.Length > 20)
            {
                ModelState.AddModelError("", "Password သည် အနည်းဆုံး ၆ လုံးမှ အများဆုံး ၂၀ လုံးအတွင်း ရှိရပါမည်။");
                return View(user);
            }

            if (await _context.Users.AsNoTracking().AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("", "This email is already registered!");
                return View(user);
            }

            user.PasswordHash = password;
            user.Status = 0;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "true";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password!");
                TempData["LoginFailed"] = "true";
                return View();
            }

            if (user.Status == 0)
            {
                ModelState.AddModelError("", "သင်၏ အကောင့်မှာ Pending အခြေအနေဖြစ်ပါသဖြင့် Admin ၏ အတည်ပြုချက်ကို စောင့်ဆိုင်းပေးပါ။");
                TempData["LoginFailed"] = "true";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            
            return RedirectToAction("Dashboard", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}