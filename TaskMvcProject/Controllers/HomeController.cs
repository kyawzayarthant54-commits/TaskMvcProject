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


        public async Task<IActionResult> Index(string searchString, int? p, string filter, int? categoryFilter, string priorityFilter)
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
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,IsCompleted,CategoryId,Priority,DueDate")] TaskItem taskItem)
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
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index), new { p = p });
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

            return RedirectToAction(nameof(Index), new { p = p });
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return RedirectToAction(nameof(Categories));
            }

            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                category.Name = name;
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }


        public async Task<IActionResult> Analytics()
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

        
        public IActionResult Privacy()
        {
            return View();
        }
    }
}