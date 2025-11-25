using System.ComponentModel.DataAnnotations;

namespace Veearve.Models
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(5)]
        public string Password { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string ApartmentNumber { get; set; }
    }
}
