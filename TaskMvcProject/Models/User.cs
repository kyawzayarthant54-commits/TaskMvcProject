using System.ComponentModel.DataAnnotations;

namespace TaskMvcProject.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Email ရိုက်ထည့်ရန် လိုအပ်ပါသည်")]
        [EmailAddress(ErrorMessage = "Email Format မမှန်ကန်ပါ")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Email format မှားယွင်းနေပါသည်။ (ဥပမာ - example@gmail.com)")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public int Status { get; set; } = 0;
    }
}