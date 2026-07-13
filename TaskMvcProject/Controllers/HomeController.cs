using Microsoft.AspNetCore.Mvc;
using TaskMvcProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskMvcProject.Controllers
{
    // 1. [Authorize]
    // [Authorize]
    public class HomeController : Controller
    {
        // private readonly ApplicationDbContext _context;

        // public HomeController(ApplicationDbContext context)
        // {
        //     _context = context;
        // }

        #region 📊 Static Data Generator (Database အစား ယာယီသုံးရန်)

        private List<Category> GetMockCategories()
        {
            return new List<Category>
            {
                new Category { Id = 1, Name = "Work" },
                new Category { Id = 2, Name = "Personal" },
                new Category { Id = 3, Name = "Education" }
            };
        }

        private List<TaskItem> GetMockTasks(List<Category> categories)
        {
            return new List<TaskItem>
            {
                new TaskItem { Id = 1, Title = "Finish MVC Project Report", Priority = "High", DueDate = DateTime.Now.AddDays(-1), IsCompleted = false, CategoryId = 1, Category = categories[0] },
                new TaskItem { Id = 2, Title = "Buy Groceries", Priority = "Medium", DueDate = DateTime.Now.AddDays(2), IsCompleted = true, CategoryId = 2, Category = categories[1] },
                new TaskItem { Id = 3, Title = "Read C# Advanced Book", Priority = "Low", DueDate = DateTime.Now.AddDays(5), IsCompleted = false, CategoryId = 3, Category = categories[2] },
                new TaskItem { Id = 4, Title = "Fix Dashboard UI Bug", Priority = "High", DueDate = DateTime.Now.AddDays(1), IsCompleted = false, CategoryId = 1, Category = categories[0] },
                new TaskItem { Id = 5, Title = "Gym Session", Priority = "Medium", DueDate = DateTime.Now.AddDays(-2), IsCompleted = false, CategoryId = 2, Category = categories[1] }
            };
        }

        #endregion

        public async Task<IActionResult> Index(string searchString, int? p, string filter, int? categoryFilter, string priorityFilter)
        {
            ViewBag.CurrentFilter = string.IsNullOrEmpty(filter) ? "All" : filter;
            ViewBag.SelectedCategory = categoryFilter;
            ViewBag.SelectedPriority = priorityFilter;
            ViewData["CurrentFilter"] = searchString;

            // Database အစား Mock Data သုံးခြင်း
            var categories = GetMockCategories();
            var tasksList = GetMockTasks(categories);
            var tasksQuery = tasksList.AsQueryable();

            // Status Filter (Pending / Completed)
            if (filter == "Pending")
            {
                tasksQuery = tasksQuery.Where(t => !t.IsCompleted);
            }
            else if (filter == "Completed")
            {
                tasksQuery = tasksQuery.Where(t => t.IsCompleted);
            }

            // Category Filter
            if (categoryFilter.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.CategoryId == categoryFilter.Value);
            }

            // Priority Filter
            if (!string.IsNullOrEmpty(priorityFilter))
            {
                tasksQuery = tasksQuery.Where(t => t.Priority == priorityFilter);
            }

            // Search Title Filter
            if (!string.IsNullOrEmpty(searchString))
            {
                tasksQuery = tasksQuery.Where(s => s.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            // Statistics Counts
            ViewBag.TotalTasks = tasksList.Count;
            ViewBag.CompletedTasks = tasksList.Count(t => t.IsCompleted);
            ViewBag.PendingTasks = tasksList.Count(t => !t.IsCompleted);
            ViewBag.OverdueCount = tasksList.Count(t => !t.IsCompleted && t.DueDate < DateTime.Now);

            ViewBag.Categories = categories;

            // Pagination Logic
            int pageSize = 5;
            int pageIdx = p ?? 1;
            var filteredList = tasksQuery.ToList();
            int totalItems = filteredList.Count;

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = pageIdx;

            var pagedData = filteredList
                                .OrderByDescending(t => t.Id)
                                .Skip((pageIdx - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            return View(pagedData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask(TaskItem taskItem)
        {
            // Database မရှိ၍ ဘာမှမလုပ်ဘဲ Dashboard ကို ပြန်ညွှန်းသည်
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskItem taskItem)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id, int? p)
        {
            return RedirectToAction(nameof(Index), new { p = p });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id, int? p)
        {
            return RedirectToAction(nameof(Index), new { p = p });
        }

        public async Task<IActionResult> Categories()
        {
            var categories = GetMockCategories();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name)
        {
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name)
        {
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Analytics()
        {
            var categories = GetMockCategories();
            var tasks = GetMockTasks(categories);

            int totalTasks = tasks.Count;
            int completedTasks = tasks.Count(t => t.IsCompleted);
            int pendingTasks = tasks.Count(t => !t.IsCompleted);

            double completionRate = totalTasks > 0 ? ((double)completedTasks / totalTasks) * 100 : 0;

            ViewBag.TotalTasks = totalTasks;
            ViewBag.CompletedTasks = completedTasks;
            ViewBag.PendingTasks = pendingTasks;
            ViewBag.CompletionRate = Math.Round(completionRate, 1);

            // Chart Data Generation
            var categoryData = tasks
                .GroupBy(t => t.Category?.Name ?? "No Category")
                .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.CategoryLabels = categoryData.Select(x => x.CategoryName).ToList();
            ViewBag.CategoryCounts = categoryData.Select(x => x.Count).ToList();

            var priorityData = tasks
                .GroupBy(t => t.Priority)
                .Select(g => new { PriorityName = g.Key ?? "Medium", Count = g.Count() })
                .ToList();

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