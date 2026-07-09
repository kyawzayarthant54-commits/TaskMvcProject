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

        // 🛠️ REGISTER (Optimized: Plain text ဖြင့် သမိုင်းရိုင်းအောင် မြန်အောင်လုပ်ခြင်း)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password)
        {
            if (await _context.Users.AsNoTracking().AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "This email is already registered!");
                return View();
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = password // ⚡ စာသားအတိုင်း တိုက်ရိုက်သိမ်းလိုက်ပါမယ်
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login() => View();

        // 🛠️ LOGIN (Optimized: AsNoTracking + Plain text match ဖြင့် ဒုန်းစိုင်းမောင်းခြင်း)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            // ⚡ Cryptography တွက်ချက်မှုတွေအကုန်ဖြုတ်ပြီး AsNoTracking() ဖြင့် တိုက်ရိုက်ဆွဲထုတ်ခြင်း
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password!");
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

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}