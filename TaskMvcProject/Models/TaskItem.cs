using System;
using System.ComponentModel.DataAnnotations;

namespace TaskMvcProject.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public string Priority { get; set; } = "Medium";

        public DateTime? DueDate { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}