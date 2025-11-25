using System.ComponentModel.DataAnnotations;

namespace Veearve.Models
{
    public class CreateReadingDto
    {
        [Required]
        public string ApartmentNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public double ColdWater { get; set; }

        [Required]
        public double HotWater { get; set; }
    }
}
