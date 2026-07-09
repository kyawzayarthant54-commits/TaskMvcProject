using System.ComponentModel.DataAnnotations;

namespace TaskMvcProject.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; 

        public string FullName { get; set; } = string.Empty;
    }
}