using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskMvcProject.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        
        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}