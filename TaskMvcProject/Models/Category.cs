using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskMvcProject.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        
        [StringLength(20, ErrorMessage = "Category name သည် အများဆုံး စာလုံး ၂၀ သာ ဖြစ်ရပါမည်။")]
        public string Name { get; set; } = string.Empty;

        
        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}