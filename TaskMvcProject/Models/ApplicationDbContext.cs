using Microsoft.EntityFrameworkCore;
using TaskMvcProject.Models;

namespace TaskMvcProject.Models 
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; } 
    }
}