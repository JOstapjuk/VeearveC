namespace Veearve.Models
{
    public class UnpaidBillsDto
    {
        public int Count { get; set; }
        public string TotalAmount { get; set; }
        public List<Reading> Bills { get; set; }
    }
}
