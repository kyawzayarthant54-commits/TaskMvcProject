using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TaskMvcProject.Models; 

namespace TaskMvcProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index(string filter = "All", string searchString = "", int p = 1)
        {
            int pageSize = 5;
            var tasksQuery = _context.TaskItems.Include(t => t.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                tasksQuery = tasksQuery.Where(t => t.Title!.Contains(searchString));
            }

            switch (filter)
            {
                case "Pending":
                    tasksQuery = tasksQuery.Where(t => !t.IsCompleted);
                    break;
                case "Completed":
                    tasksQuery = tasksQuery.Where(t => t.IsCompleted);
                    break;
            }

            ViewBag.TotalTasks = await _context.TaskItems.CountAsync();
            ViewBag.CompletedTasks = await _context.TaskItems.CountAsync(t => t.IsCompleted);
            ViewBag.PendingTasks = await _context.TaskItems.CountAsync(t => !t.IsCompleted);

            ViewBag.CurrentFilter = filter;

            int totalTasksCount = await tasksQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalTasksCount / pageSize);

            if (p < 1) p = 1;
            if (totalPages > 0 && p > totalPages) p = totalPages;

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = p;

            ViewBag.Categories = await _context.Categories.ToListAsync();

            var paginatedTasks = await tasksQuery
                .OrderByDescending(t => t.Id)
                .Skip((p - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedTasks);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(string title, int? categoryId, DateTime? dueDate, string priority)
        {
            if (!string.IsNullOrEmpty(title))
            {
                var task = new TaskItem
                {
                    Title = title,
                    CategoryId = categoryId,
                    DueDate = dueDate,
                    Priority = priority,
                    IsCompleted = false
                };

                _context.TaskItems.Add(task);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsCompleted(int id, int p = 1)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task != null)
            {
                task.IsCompleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { p = p });
        }

        
        [HttpPost]
        public async Task<IActionResult> Edit(TaskItem updatedTask, int p = 1)
        {
            var task = await _context.TaskItems.FindAsync(updatedTask.Id);
            if (task != null)
            {
                task.Title = updatedTask.Title;
                task.CategoryId = updatedTask.CategoryId;
                task.DueDate = updatedTask.DueDate;
                task.Priority = updatedTask.Priority;

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { p = p });
        }

       
        [HttpPost]
        public async Task<IActionResult> DeleteTask(int id, int p = 1)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task != null)
            {
                _context.TaskItems.Remove(task);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { p = p });
        }

        
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var category = new Category { Name = name };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }

       
        public async Task<IActionResult> Analytics()
        {
            
            var categoryData = await _context.TaskItems
                .Where(t => t.Category != null)
                .GroupBy(t => t.Category!.Name)
                .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                .ToListAsync();

           
            var priorityData = await _context.TaskItems
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key ?? "Medium", Count = g.Count() })
                .ToListAsync();

            ViewBag.CategoryLabels = categoryData.Select(x => x.CategoryName).ToArray();
            ViewBag.CategoryCounts = categoryData.Select(x => x.Count).ToArray();

            ViewBag.PriorityLabels = priorityData.Select(x => x.Priority).ToArray();
            ViewBag.PriorityCounts = priorityData.Select(x => x.Count).ToArray();

            ViewBag.TotalTasks = await _context.TaskItems.CountAsync();
            ViewBag.CompletedTasks = await _context.TaskItems.CountAsync(t => t.IsCompleted);
            ViewBag.PendingTasks = await _context.TaskItems.CountAsync(t => !t.IsCompleted);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}