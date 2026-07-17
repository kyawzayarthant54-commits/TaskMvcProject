using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMvcProject.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace TaskMvcProject.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        public async Task<IActionResult> Dashboard()
        {
            int totalTasks = await _context.TaskItems.CountAsync();
            int completedTasks = await _context.TaskItems.CountAsync(t => t.IsCompleted);
            int pendingTasks = await _context.TaskItems.CountAsync(t => !t.IsCompleted);

            double completionRate = totalTasks > 0 ? ((double)completedTasks / totalTasks) * 100 : 0;

            ViewBag.TotalTasks = totalTasks;
            ViewBag.CompletedTasks = completedTasks;
            ViewBag.PendingTasks = pendingTasks;
            ViewBag.CompletionRate = Math.Round(completionRate, 1);

            var categoryData = await _context.TaskItems
                .Include(t => t.Category)
                .Where(t => t.Category != null)
                .GroupBy(t => t.Category.Name)
                .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.CategoryLabels = categoryData.Select(x => x.CategoryName).ToList();
            ViewBag.CategoryCounts = categoryData.Select(x => x.Count).ToList();

            var priorityData = await _context.TaskItems
                .GroupBy(t => t.Priority)
                .Select(g => new { PriorityName = g.Key ?? "Medium", Count = g.Count() })
                .ToListAsync();

            ViewBag.PriorityLabels = priorityData.Select(x => x.PriorityName).ToList();
            ViewBag.PriorityCounts = priorityData.Select(x => x.Count).ToList();

            return View();
        }

        
        public async Task<IActionResult> Task(string searchString, int? p, string filter, int? categoryFilter, string priorityFilter)
        {
            ViewBag.CurrentFilter = string.IsNullOrEmpty(filter) ? "All" : filter;
            ViewBag.SelectedCategory = categoryFilter;
            ViewBag.SelectedPriority = priorityFilter;
            ViewData["CurrentFilter"] = searchString;

            var tasksQuery = _context.TaskItems.Include(t => t.Category).AsQueryable();

            if (filter == "Pending")
            {
                tasksQuery = tasksQuery.Where(t => !t.IsCompleted);
            }
            else if (filter == "Completed")
            {
                tasksQuery = tasksQuery.Where(t => t.IsCompleted);
            }

            if (categoryFilter.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.CategoryId == categoryFilter.Value);
            }

            if (!string.IsNullOrEmpty(priorityFilter))
            {
                tasksQuery = tasksQuery.Where(t => t.Priority == priorityFilter);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                tasksQuery = tasksQuery.Where(s => s.Title.Contains(searchString));
            }

            ViewBag.TotalTasks = await _context.TaskItems.CountAsync();
            ViewBag.CompletedTasks = await _context.TaskItems.CountAsync(t => t.IsCompleted);
            ViewBag.PendingTasks = await _context.TaskItems.CountAsync(t => !t.IsCompleted);

            ViewBag.Categories = await _context.Categories.ToListAsync();

            int pageSize = 5;
            int pageIdx = p ?? 1;
            int totalItems = await tasksQuery.CountAsync();

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = pageIdx;

            var pagedData = await tasksQuery
                                  .OrderByDescending(t => t.Id)
                                  .Skip((pageIdx - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            return View(pagedData);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask([Bind("Id,Title,Description,CategoryId,Priority,DueDate")] TaskItem taskItem)
        {
            if (ModelState.IsValid)
            {
                taskItem.IsCompleted = false;
                _context.TaskItems.Add(taskItem);
                await _context.SaveChangesAsync();

                TempData["TaskCreateSuccess"] = "Task ကို အောင်မြင်စွာ ဖန်တီးပြီးပါပြီ။";
                return RedirectToAction(nameof(Task));
            }
            return RedirectToAction(nameof(Task));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,IsCompleted,CategoryId,Priority,DueDate")] TaskItem taskItem, string filter, int p = 1)
        {
            if (id != taskItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taskItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TaskItems.Any(e => e.Id == taskItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Task), new { filter = filter, p = p });
            }
            return RedirectToAction(nameof(Task), new { filter = filter, p = p });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id, int? p)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            task.IsCompleted = true;
            _context.Update(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Task), new { p = p });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id, int? p)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Task), new { p = p });
        }

       
        public async Task<IActionResult> Categories(int? cp)
        {
            int pageSize = 9;
            int pageIdx = cp ?? 1;

            var categoriesQuery = _context.Categories.AsQueryable();
            int totalItems = await categoriesQuery.CountAsync();

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = pageIdx;

            var pagedCategories = await categoriesQuery
                                        .OrderBy(c => c.Name)
                                        .Skip((pageIdx - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            return View(pagedCategories);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name, int? cp)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["CategoryDeleteError"] = "Category နာမည် အလွတ်ဖြစ်နေ၍ မရပါ!";
                return RedirectToAction(nameof(Categories), new { cp = cp });
            }

            if (name.Length > 20)
            {
                TempData["CategoryDeleteError"] = "Category နာမည်သည် စာလုံးရေ ၂၀ ထက် မကျော်ရပါ!";
                return RedirectToAction(nameof(Categories), new { cp = cp });
            }

            var category = new Category { Name = name };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["CategoryDeleteSuccess"] = "Category အသစ်ကို အောင်မြင်စွာ ထည့်သွင်းပြီးပါပြီ။";
            return RedirectToAction(nameof(Categories), new { cp = cp });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name, int? cp)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["CategoryDeleteError"] = "ပြင်ဆင်မည့် Category နာမည် အလွတ်ဖြစ်နေ၍ မရပါ!";
                return RedirectToAction(nameof(Categories), new { cp = cp });
            }

            if (name.Length > 20)
            {
                TempData["CategoryDeleteError"] = "Category နာမည်သည် စာလုံးရေ ၂၀ ထက် မကျော်ရပါ!";
                return RedirectToAction(nameof(Categories), new { cp = cp });
            }

            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                category.Name = name;
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                TempData["CategoryDeleteSuccess"] = "Category ကို အောင်မြင်စွာ ပြင်ဆင်ပြီးပါပြီ။";
            }
            return RedirectToAction(nameof(Categories), new { cp = cp });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int? cp)
        {
            bool isCategoryUsed = await _context.TaskItems.AnyAsync(t => t.CategoryId == id);

            if (isCategoryUsed)
            {
                TempData["CategoryDeleteError"] = "ဤ Category အား လက်ရှိ Task များတွင် အသုံးပြုနေဆဲဖြစ်ပါသဖြင့် ဖျက်၍မရနိုင်ပါ!";
                return RedirectToAction(nameof(Categories), new { cp = cp });
            }

            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["CategoryDeleteSuccess"] = "Category ကို အောင်မြင်စွာ ဖျက်သိမ်းပြီးပါပြီ။";
            }
            return RedirectToAction(nameof(Categories), new { cp = cp });
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}