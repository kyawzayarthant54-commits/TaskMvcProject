using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMvcProject.Models;
using System.Threading.Tasks;

namespace TaskMvcProject.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            
            bool isCategoryUsed = await _context.TaskItems.AnyAsync(t => t.CategoryId == id);

            if (isCategoryUsed)
            {
                
                TempData["CategoryDeleteError"] = "ဤ Category အား လက်ရှိ Task များတွင် အသုံးပြုနေဆဲဖြစ်ပါသဖြင့် ဖျက်၍မရနိုင်ပါ!";
                return RedirectToAction(nameof(Index));
            }

           
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["CategoryDeleteSuccess"] = "Category ကို အောင်မြင်စွာ ဖျက်သိမ်းပြီးပါပြီ။";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}